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
    // Separate archives for each compression method
    private StuffItArchive _lzwArchive = null!;
    private StuffItArchive _lzssArchive = null!;
    private StuffItArchive _arsenicArchive = null!;
    
    private StuffItArchiveFile? _lzwFile;
    private StuffItArchiveFile? _lzssFile;
    private StuffItArchiveFile? _arsenicFile;
    
    private MemoryStream _lzwArchiveStream = null!;
    private MemoryStream _lzssArchiveStream = null!;
    private MemoryStream _arsenicArchiveStream = null!;
    private MemoryStream _outputStream = null!;
    
    // Use different sample files for each compression method
    private const string LzwSampleFile = "Samples/macbinary2.sit";      // Has LZW files
    private const string LzssSampleFile = "Samples/DiskCopy_6.0.sit";   // Has LZSS files
    private const string ArsenicSampleFile = "Samples/Excel_5.0_English.sit"; // Has Arsenic files

    [GlobalSetup]
    public void Setup()
    {
        _outputStream = new MemoryStream();
        
        // Setup LZW archive
        if (File.Exists(LzwSampleFile))
        {
            _lzwArchiveStream = new MemoryStream(File.ReadAllBytes(LzwSampleFile));
            _lzwArchive = new StuffItArchive(_lzwArchiveStream);
            _lzwFile = FindFile(_lzwArchive, _lzwArchive.GetRootEntries(), StuffItArchiveCompressionMethod.LZW);
        }
        
        // Setup LZSS archive
        if (File.Exists(LzssSampleFile))
        {
            _lzssArchiveStream = new MemoryStream(File.ReadAllBytes(LzssSampleFile));
            _lzssArchive = new StuffItArchive(_lzssArchiveStream);
            _lzssFile = FindFile(_lzssArchive, _lzssArchive.GetRootEntries(), StuffItArchiveCompressionMethod.LZSS);
        }
        
        // Setup Arsenic archive
        if (File.Exists(ArsenicSampleFile))
        {
            _arsenicArchiveStream = new MemoryStream(File.ReadAllBytes(ArsenicSampleFile));
            _arsenicArchive = new StuffItArchive(_arsenicArchiveStream);
            _arsenicFile = FindFile(_arsenicArchive, _arsenicArchive.GetRootEntries(), StuffItArchiveCompressionMethod.Arsenic);
        }
    }

    private static StuffItArchiveFile? FindFile(StuffItArchive archive, List<StuffItArchiveEntry> entries, StuffItArchiveCompressionMethod method)
    {
        foreach (var entry in entries)
        {
            if (entry is StuffItArchiveDirectory dir)
            {
                var found = FindFile(archive, archive.GetEntries(dir), method);
                if (found != null)
                    return found;
            }
            else if (entry is StuffItArchiveFile file)
            {
                if (file.DataForkCompressionMethod == method && file.DataForkUncompressedLength > 0)
                {
                    return file;
                }
            }
        }
        return null;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _lzwArchiveStream?.Dispose();
        _lzssArchiveStream?.Dispose();
        _arsenicArchiveStream?.Dispose();
        _outputStream.Dispose();
    }

    [Benchmark(Description = "Decompress LZW data fork")]
    public long DecompressLzw()
    {
        if (_lzwFile == null || _lzwArchive == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _lzwArchive.ReadDecompressedDataFork(_lzwFile, _outputStream);
    }

    [Benchmark(Description = "Decompress LZSS data fork")]
    public long DecompressLzss()
    {
        if (_lzssFile == null || _lzssArchive == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _lzssArchive.ReadDecompressedDataFork(_lzssFile, _outputStream);
    }

    [Benchmark(Description = "Decompress Arsenic data fork")]
    public long DecompressArsenic()
    {
        if (_arsenicFile == null || _arsenicArchive == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _arsenicArchive.ReadDecompressedDataFork(_arsenicFile, _outputStream);
    }

    [Benchmark(Description = "Read compressed data fork")]
    public long ReadCompressedDataFork()
    {
        // Use the LZSS file for this benchmark since it's commonly available
        if (_lzssFile == null || _lzssArchive == null)
            return 0;
            
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        return _lzssArchive.ReadCompressedDataFork(_lzssFile, _outputStream);
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
        if (args.Length > 0 && args[0] == "--check-methods")
        {
            CheckCompressionMethods();
            return;
        }
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }

    private static void CheckCompressionMethods()
    {
        var samplesDir = "Samples";
        Console.WriteLine("Checking compression methods in sample files...\n");

        foreach (var file in Directory.GetFiles(samplesDir, "*.sit", SearchOption.AllDirectories).OrderBy(f => f))
        {
            try
            {
                using var stream = File.OpenRead(file);
                var archive = new StuffItArchive(stream);
                var methodCounts = new Dictionary<StuffItArchiveCompressionMethod, int>();
                FindMethods(archive, archive.GetRootEntries(), methodCounts);
                
                var fileName = Path.GetFileName(file);
                var methodStr = string.Join(", ", methodCounts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}({kv.Value})"));
                Console.WriteLine($"{fileName}: {methodStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Path.GetFileName(file)}: ERROR - {ex.Message}");
            }
        }

        static void FindMethods(StuffItArchive archive, List<StuffItArchiveEntry> entries, Dictionary<StuffItArchiveCompressionMethod, int> methods)
        {
            foreach (var entry in entries)
            {
                if (entry is StuffItArchiveDirectory dir)
                {
                    FindMethods(archive, archive.GetEntries(dir), methods);
                }
                else if (entry is StuffItArchiveFile f)
                {
                    if (f.DataForkUncompressedLength > 0)
                    {
                        methods.TryGetValue(f.DataForkCompressionMethod, out var count);
                        methods[f.DataForkCompressionMethod] = count + 1;
                    }
                    if (f.ResourceForkUncompressedLength > 0)
                    {
                        methods.TryGetValue(f.ResourceForkCompressionMethod, out var count);
                        methods[f.ResourceForkCompressionMethod] = count + 1;
                    }
                }
            }
        }
    }
}
