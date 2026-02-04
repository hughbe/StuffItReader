using System.Diagnostics;
using System.Security.Cryptography;

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
    [InlineData("macintoshgarden.org/apps/mactech-vol-1-12/NestedVolumes.sit")]
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

    [Fact]
    public void NestedVolumes_ExtractsCorrectHierarchyAndContent()
    {
        // Expected file hashes (SHA256) from verified third-party extraction
        // The archive contains nested directories: "Nested Volumes" contains "Nest" and "Nest Manager"
        // Plus two additional root directories: "nest accessory folder" and "nest manager folder"
        var expectedFiles = new Dictionary<string, string>
        {
            // Nested Volumes folder (at root) contains:
            ["Nested Volumes/Nest"] = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ["Nested Volumes/Nest Manager"] = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            
            // nest accessory folder (at root)
            ["nest accessory folder/-Nest"] = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ["nest accessory folder/drvr.c"] = "012bc49f951a80e3ebef4073d1df3270e3ce21b9597c94ef6d7988e0d6ceae72",
            ["nest accessory folder/drvr.h"] = "ad63e9e52cd87d3ecdd0bd9c59ca92606ebac1eb05d6ecc4f8880c3abeb78894",
            ["nest accessory folder/file.h"] = "7d59a163e5bbb266e88cb45e7dc597e3c3e4612b4bd5952f47993ca851774fca",
            ["nest accessory folder/nest.c"] = "f612d80ee2e289545d3e22bc1d22bdfe41fab95d57bbb7e5c774a0056d65e33e",
            ["nest accessory folder/nest.h"] = "bf057875182686994ac7b82f8a7b3abf846753cb4456645ae1aab542b8bd256d",
            ["nest accessory folder/nest.r"] = "9152ba2e0b63fb9d8e9f94903eb7ea7b40f7fac5ea3562d41837453ea05ee1e5",
            ["nest accessory folder/nest.rsrc"] = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ["nest accessory folder/nestdrvr.c"] = "51547a82fa5afae9fdb37bebf0d13c2312c275b7abe3a19c7b0eb07adc47e439",
            
            // nest manager folder (at root)
            ["nest manager folder/nest manager.c"] = "3a2419d061cc829d7d5bc5e2878acaa3305545ada08cd46d801acced60ee615f",
            ["nest manager folder/nest manager.icon"] = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ["nest manager folder/nest manager.r"] = "5e20b3423886d28112bdbaa1e26260630c0666da644b69a000e4f4c6c5e1148c",
        };

        using var stream = File.OpenRead(Path.Combine("Samples", "macintoshgarden.org", "apps", "mactech-vol-1-12", "NestedVolumes.sit"));
        var archive = new StuffItArchive(stream);

        // Verify structure and content
        var extractedFiles = new Dictionary<string, string>();
        ExtractAndHashEntries(archive, archive.GetRootEntries(), "", extractedFiles);

        // Verify all expected files are present with correct hashes
        foreach (var (path, expectedHash) in expectedFiles)
        {
            Assert.True(extractedFiles.ContainsKey(path), $"Missing expected file: {path}");
            Assert.Equal(expectedHash, extractedFiles[path]);
        }
    }

    [Fact]
    public void NestedVolumes_HasCorrectDirectoryStructure()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "NestedVolumes.sit"));
        var archive = new StuffItArchive(stream);

        var rootEntries = archive.GetRootEntries().ToList();
        
        // Should have 3 root entries: "Nested Volumes" folder, "nest accessory folder", "nest manager folder"
        Assert.Equal(3, rootEntries.Count);
        
        // Verify "Nested Volumes" directory at root
        var nestedVolumesFolder = rootEntries.OfType<StuffItArchiveDirectory>().FirstOrDefault(d => d.Name == "Nested Volumes");
        Assert.NotNull(nestedVolumesFolder);
        var nestedVolumesEntries = archive.GetEntries(nestedVolumesFolder).ToList();
        Assert.Equal(2, nestedVolumesEntries.Count);
        
        // Verify files inside "Nested Volumes" folder
        var nestFile = nestedVolumesEntries.OfType<StuffItArchiveFile>().FirstOrDefault(f => f.Name == "Nest");
        Assert.NotNull(nestFile);
        
        var nestManagerFile = nestedVolumesEntries.OfType<StuffItArchiveFile>().FirstOrDefault(f => f.Name == "Nest Manager");
        Assert.NotNull(nestManagerFile);
        
        // Verify "nest accessory folder" directory at root
        var nestAccessoryFolder = rootEntries.OfType<StuffItArchiveDirectory>().FirstOrDefault(d => d.Name == "nest accessory folder");
        Assert.NotNull(nestAccessoryFolder);
        var nestAccessoryEntries = archive.GetEntries(nestAccessoryFolder).ToList();
        Assert.Equal(12, nestAccessoryEntries.Count); // Includes .o files
        
        // Verify "nest manager folder" directory at root
        var nestManagerFolder = rootEntries.OfType<StuffItArchiveDirectory>().FirstOrDefault(d => d.Name == "nest manager folder");
        Assert.NotNull(nestManagerFolder);
        var nestManagerEntries = archive.GetEntries(nestManagerFolder).ToList();
        Assert.Equal(4, nestManagerEntries.Count); // Includes .o file
        
        // Verify specific files in nest accessory folder (verified against third-party extraction)
        var expectedNestAccessoryFiles = new[] { "-Nest", "drvr.c", "drvr.h", "drvr.o", "file.h", "nest.c", "nest.h", "nest.o", "nest.r", "nest.rsrc", "nestdrvr.c", "nestdrvr.o" };
        foreach (var expectedFile in expectedNestAccessoryFiles)
        {
            Assert.Contains(nestAccessoryEntries, e => e.Name == expectedFile);
        }
        
        // Verify specific files in nest manager folder
        var expectedNestManagerFiles = new[] { "nest manager.c", "nest manager.icon", "nest manager.o", "nest manager.r" };
        foreach (var expectedFile in expectedNestManagerFiles)
        {
            Assert.Contains(nestManagerEntries, e => e.Name == expectedFile);
        }
    }

    private static void ExtractAndHashEntries(StuffItArchive archive, IEnumerable<StuffItArchiveEntry> entries, string basePath, Dictionary<string, string> result)
    {
        foreach (var entry in entries)
        {
            var entryPath = string.IsNullOrEmpty(basePath) ? entry.Name : $"{basePath}/{entry.Name}";
            
            if (entry is StuffItArchiveDirectory directory)
            {
                ExtractAndHashEntries(archive, archive.GetEntries(directory), entryPath, result);
            }
            else if (entry is StuffItArchiveFile file)
            {
                using var memoryStream = new MemoryStream();
                archive.ReadDecompressedDataFork(file, memoryStream);
                memoryStream.Position = 0;
                
                var hash = SHA256.HashData(memoryStream.ToArray());
                var hashString = Convert.ToHexStringLower(hash);
                result[entryPath] = hashString;
            }
        }
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
