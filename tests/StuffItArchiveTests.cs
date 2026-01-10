using System.Diagnostics;

namespace StuffItReader.Tests;

public class StuffItArchiveTests
{
    [Theory]
    [InlineData("ms-excel-early-1-4.sit")]
    [InlineData("Microsoft Excel 2.2 for Macintosh.img.sit")]
    [InlineData("Microsoft-Excel_13-4.0.sit")]
    [InlineData("Excel_5.0_English.sit")]
    [InlineData("Disk_Copy_(v4.2).sit")]
    [InlineData("DiskCopy_6.0.sit")]
    [InlineData("DiskCopy_6.4.sit")]
    [InlineData("OrgPlus1.0.sit")]
    [InlineData("ResEdit-2.1-1.sit")]
    [InlineData("System-3-1-1.sit")]
    [InlineData("System_1.x.sit")]
    [InlineData("DART-153.sit")]
    [InlineData("macbinary2.sit")]
    [InlineData("XLerator-Utilities-v2.sit")]
    public void Ctor_Stream(string fileName)
    {
        var stream = File.OpenRead(Path.Combine("Samples", fileName));
        var archive = new StuffItArchive(stream);
        DumpArchive(archive);

        // Debug: Check first compressed file
        var entries = archive.GetRootEntries();
        foreach (var entry in entries)
        {
            if (entry is StuffItArchiveFile file && file.DataForkCompressionMethod == StuffItArchiveCompressionMethod.Arsenic)
            {
                Debug.WriteLine($"Found Arsenic-compressed file: {file.Name}");
                Debug.WriteLine($"  Data start offset: 0x{file.DataStartOffset:X}");
                stream.Seek(file.DataStartOffset, SeekOrigin.Begin);
                var buffer = new byte[32];
                stream.ReadExactly(buffer);
                Debug.WriteLine($"  First 32 bytes: {BitConverter.ToString(buffer)}");
                break;
            }
        }

        ExtractArchive(archive, Path.Combine("Output", Path.GetFileNameWithoutExtension(fileName)));
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new StuffItArchive(null!));
    }

    [Fact]
    public void Ctor_EmptyStream_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>("stream", () => new StuffItArchive(stream));
    }

    private static void DumpArchive(StuffItArchive archive, string indent = "")
    {
        foreach (var entry in archive.GetRootEntries())
        {
            DumpEntry(archive, entry, indent);
        }
    }

    private static void DumpEntry(StuffItArchive archive, StuffItArchiveEntry entry, string indent)
    {
        if (entry is StuffItArchiveDirectory directory)
        {
            Debug.WriteLine($"{indent}{entry.Name} ({directory.EntryCount} items, {directory.TotalSize} bytes)");
            foreach (var child in archive.GetEntries(directory))
            {
                DumpEntry(archive, child, indent + "    ");
            }
        }
        else if (entry is StuffItArchiveFile file)
        {
            Debug.WriteLine($"{indent}{entry.Name}, data fork {file.DataForkUncompressedLength} bytes (compressed {file.DataForkCompressedLength} bytes, {file.DataForkCompressionMethod}), resource fork {file.ResourceForkUncompressedLength} bytes (compressed {file.ResourceForkCompressedLength} bytes (compressed {file.ResourceForkCompressedLength} bytes, {file.ResourceForkCompressionMethod})");
        }
    }

    private static void ExtractArchive(StuffItArchive archive, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var entry in archive.GetRootEntries())
        {
            ExtractEntry(archive, entry, outputDirectory);
        }
    }

    private static void ExtractEntry(StuffItArchive archive, StuffItArchiveEntry entry, string outputDirectory)
    {
        if (entry is StuffItArchiveDirectory directory)
        {
            var dirPath = Path.Combine(outputDirectory, entry.Name);
            Directory.CreateDirectory(dirPath);
            foreach (var child in archive.GetEntries(directory))
            {
                ExtractEntry(archive, child, dirPath);
            }
        }
        else if (entry is StuffItArchiveFile file)
        {
            // Extract data fork (compressed)
            var dataForkCompressedPath = Path.Combine(outputDirectory, entry.Name + ".data.sit");
            using var dataForkCompressedOutputStream = File.Create(dataForkCompressedPath);
            archive.ReadCompressedDataFork(file, dataForkCompressedOutputStream);

            // Extract resource fork (compressed) if present
            if (file.ResourceForkCompressedLength > 0)
            {
                var resourceForkCompressedPath = Path.Combine(outputDirectory, entry.Name + ".rsrc.sit");
                using var resourceForkCompressedOutputStream = File.Create(resourceForkCompressedPath);
                archive.ReadCompressedResourceFork(file, resourceForkCompressedOutputStream);
            }

            // Extract data fork (decompressed)
            var dataForkPath = Path.Combine(outputDirectory, entry.Name);
            using var dataForkOutputStream = File.Create(dataForkPath);
            archive.ReadDecompressedDataFork(file, dataForkOutputStream);

            // Extract resource fork (decompressed) if present
            if (file.ResourceForkUncompressedLength > 0)
            {
                var resourceForkPath = Path.Combine(outputDirectory, entry.Name + ".rsrc");
                using var resourceForkOutputStream = File.Create(resourceForkPath);
                archive.ReadDecompressedResourceFork(file, resourceForkOutputStream);
            }
        }
    }
}
