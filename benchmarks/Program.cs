using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace StuffItReader.Benchmarks;

[MemoryDiagnoser]
public class ArchiveReadingBenchmarks
{
    private byte[] _archiveData = null!;
    private MemoryStream _archiveStream = null!;
    
    // Use a representative sample file
    private const string SampleFile = "Samples/DiskCopy_6.0.sit";

    [GlobalSetup]
    public void Setup()
    {
        if (!File.Exists(SampleFile))
        {
            throw new FileNotFoundException($"Sample file not found: {SampleFile}. Please ensure the Samples folder is copied to the output directory.");
        }

        _archiveData = File.ReadAllBytes(SampleFile);
        _archiveStream = new MemoryStream(_archiveData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _archiveStream.Dispose();
    }

    [Benchmark(Description = "Open archive and parse header")]
    public StuffItArchive OpenArchive()
    {
        _archiveStream.Position = 0;
        return new StuffItArchive(_archiveStream);
    }

    [Benchmark(Description = "Open archive and get root entries")]
    public List<StuffItArchiveEntry> GetRootEntries()
    {
        _archiveStream.Position = 0;
        var archive = new StuffItArchive(_archiveStream);
        return archive.GetRootEntries();
    }

    [Benchmark(Description = "Open archive and enumerate all entries")]
    public int EnumerateAllEntries()
    {
        _archiveStream.Position = 0;
        var archive = new StuffItArchive(_archiveStream);
        return CountEntries(archive, archive.GetRootEntries());
    }

    private int CountEntries(StuffItArchive archive, List<StuffItArchiveEntry> entries)
    {
        int count = entries.Count;
        foreach (var entry in entries)
        {
            if (entry is StuffItArchiveDirectory dir)
            {
                count += CountEntries(archive, archive.GetEntries(dir));
            }
        }
        return count;
    }
}

[MemoryDiagnoser]
public class DecompressionBenchmarks
{
    private StuffItArchive _archive = null!;
    private StuffItArchiveFile? _lzwFile;
    private StuffItArchiveFile? _lzssFile;
    private StuffItArchiveFile? _arsenicFile;
    private MemoryStream _archiveStream = null!;
    private MemoryStream _outputStream = null!;
    
    private const string SampleFile = "Samples/DiskCopy_6.0.sit";

    [GlobalSetup]
    public void Setup()
    {
        if (!File.Exists(SampleFile))
        {
            throw new FileNotFoundException($"Sample file not found: {SampleFile}");
        }
        
        _archiveStream = new MemoryStream(File.ReadAllBytes(SampleFile));
        _archive = new StuffItArchive(_archiveStream);
        _outputStream = new MemoryStream();
        
        // Find files with different compression methods
        FindTestFiles(_archive.GetRootEntries());
    }

    private void FindTestFiles(List<StuffItArchiveEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry is StuffItArchiveDirectory dir)
            {
                FindTestFiles(_archive.GetEntries(dir));
            }
            else if (entry is StuffItArchiveFile file)
            {
                if (_lzwFile == null && file.DataForkCompressionMethod == StuffItArchiveCompressionMethod.LZW && file.DataForkUncompressedLength > 0)
                {
                    _lzwFile = file;
                }
                if (_lzssFile == null && file.DataForkCompressionMethod == StuffItArchiveCompressionMethod.LZSS && file.DataForkUncompressedLength > 0)
                {
                    _lzssFile = file;
                }
                if (_arsenicFile == null && file.DataForkCompressionMethod == StuffItArchiveCompressionMethod.Arsenic && file.DataForkUncompressedLength > 0)
                {
                    _arsenicFile = file;
                }
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _archiveStream.Dispose();
        _outputStream.Dispose();
    }

    [Benchmark(Description = "Decompress LZW data fork")]
    public long DecompressLzw()
    {
        if (_lzwFile == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _archive.ReadDecompressedDataFork(_lzwFile, _outputStream);
    }

    [Benchmark(Description = "Decompress LZSS data fork")]
    public long DecompressLzss()
    {
        if (_lzssFile == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _archive.ReadDecompressedDataFork(_lzssFile, _outputStream);
    }

    [Benchmark(Description = "Decompress Arsenic data fork")]
    public long DecompressArsenic()
    {
        if (_arsenicFile == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _archive.ReadDecompressedDataFork(_arsenicFile, _outputStream);
    }

    [Benchmark(Description = "Read compressed data fork")]
    public long ReadCompressedDataFork()
    {
        var file = _lzwFile ?? _lzssFile ?? _arsenicFile;
        if (file == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _archive.ReadCompressedDataFork(file, _outputStream);
    }
}

[MemoryDiagnoser]
public class MultiFileBenchmarks
{
    private readonly string[] _sampleFiles =
    [
        "Samples/DiskCopy_6.0.sit",
        "Samples/Disk_Copy_(v4.2).sit",
        "Samples/DART-153.sit",
    ];

    private List<(string Name, byte[] Data)> _archives = new();
    private MemoryStream _outputStream = null!;

    [GlobalSetup]
    public void Setup()
    {
        foreach (var file in _sampleFiles)
        {
            if (File.Exists(file))
            {
                _archives.Add((file, File.ReadAllBytes(file)));
            }
        }
        _outputStream = new MemoryStream();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _outputStream.Dispose();
    }

    [Benchmark(Description = "Extract all files from multiple archives")]
    public int ExtractAllFromMultipleArchives()
    {
        int totalFiles = 0;
        foreach (var (name, data) in _archives)
        {
            using var stream = new MemoryStream(data);
            var archive = new StuffItArchive(stream);
            totalFiles += ExtractAll(archive, archive.GetRootEntries());
        }
        return totalFiles;
    }

    private int ExtractAll(StuffItArchive archive, List<StuffItArchiveEntry> entries)
    {
        int count = 0;
        foreach (var entry in entries)
        {
            if (entry is StuffItArchiveDirectory dir)
            {
                count += ExtractAll(archive, archive.GetEntries(dir));
            }
            else if (entry is StuffItArchiveFile file)
            {
                _outputStream.Position = 0;
                _outputStream.SetLength(0);
                archive.ReadDecompressedDataFork(file, _outputStream);
                count++;
            }
        }
        return count;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}
