# StuffItReader

A lightweight .NET library for reading StuffIt archive files (.sit). StuffIt was the most popular compression and archival tool for Macintosh computers, supporting multiple compression formats and preserving file metadata including resource forks and Finder information.

## Features

- Read StuffIt archives (.sit files)
- Support for StuffIt format versions 1 (classic) and 5 (modern)
- Parse archive headers and entry metadata
- Support for multiple compression formats:
  - Uncompressed
  - LZW compression
  - LZSS (LZ + Huffman coding)
  - Arsenic (BWT + Arithmetic coding)
- Extract data forks and resource forks
- Preserve file metadata (type, creator, Finder flags, dates)
- Support for nested directories
- Zero external dependencies (core library)
- Built for .NET 9.0

## Installation

Add the project reference to your .NET application:

```sh
dotnet add reference path/to/StuffItReader.csproj
```

Or, if published on NuGet:

```sh
dotnet add package StuffItReader
```

## Usage

### Opening a StuffIt Archive

```csharp
using StuffItReader;

// Open a StuffIt archive file
using var stream = File.OpenRead("archive.sit");

// Parse the archive
var archive = new StuffItArchive(stream);

// Get archive information
Console.WriteLine($"Archive Version: {archive.Version}");
if (archive.Version == 1)
{
    Console.WriteLine($"Total Size: {archive.HeaderV1?.TotalSize} bytes");
    Console.WriteLine($"Root Entries: {archive.HeaderV1?.RootDirectoryEntryCount}");
}
else
{
    Console.WriteLine($"Total Size: {archive.HeaderV5?.TotalSize} bytes");
    Console.WriteLine($"Root Entries: {archive.HeaderV5?.RootDirectoryEntryCount}");
}
```

### Listing Files in the Archive

```csharp
// Enumerate root entries in the archive
foreach (var entry in archive.GetRootEntries())
{
    Console.WriteLine($"{entry.Name}");

    if (entry is StuffItArchiveFile file)
    {
        Console.WriteLine($"  Data Fork: {file.DataForkUncompressedLength} bytes ({file.DataForkCompressionMethod})");
        Console.WriteLine($"  Resource Fork: {file.ResourceForkUncompressedLength} bytes ({file.ResourceForkCompressionMethod})");
    }
    else if (entry is StuffItArchiveDirectory directory)
    {
        Console.WriteLine($"  Directory with {directory.EntryCount} entries");

        // Enumerate entries within the directory
        foreach (var childEntry in archive.GetEntries(directory))
        {
            Console.WriteLine($"    {childEntry.Name}");
        }
    }
}
```

### Extracting File Data

```csharp
foreach (var entry in archive.GetRootEntries())
{
    if (entry is StuffItArchiveFile file)
    {
        var fileName = file.Name;

        // Extract data fork
        if (file.DataForkUncompressedLength > 0)
        {
            using var dataOutput = File.Create(fileName);
            archive.ReadDecompressedDataFork(file, dataOutput);
        }

        // Extract resource fork
        if (file.ResourceForkUncompressedLength > 0)
        {
            using var rsrcOutput = File.Create($"{fileName}.rsrc");
            archive.ReadDecompressedResourceFork(file, rsrcOutput);
        }
    }
}
```

### Examining File Metadata (V1 Archives)

```csharp
foreach (var entry in archive.GetRootEntries())
{
    if (entry is StuffItArchiveFileV1 file)
    {
        Console.WriteLine($"File: {file.Name}");
        Console.WriteLine($"  Type: {file.EntryHeaderV1.Type}");
        Console.WriteLine($"  Creator: {file.EntryHeaderV1.Creator}");
        Console.WriteLine($"  Finder Flags: 0x{file.EntryHeaderV1.FinderFlags:X4}");
        Console.WriteLine($"  Created: {file.EntryHeaderV1.CreationDate}");
        Console.WriteLine($"  Modified: {file.EntryHeaderV1.ModificationDate}");

        var dataRatio = file.DataForkCompressedLength > 0
            ? (1.0 - (double)file.DataForkCompressedLength / file.DataForkUncompressedLength) * 100
            : 0;
        Console.WriteLine($"  Data Fork Compression: {dataRatio:F1}% saved");
    }
}
```

## API Overview

### StuffItArchive

The main class for reading StuffIt archives.

- `StuffItArchive(Stream stream)` - Opens an archive from a stream
- `Version` - Gets the archive format version (1 or 5)
- `HeaderV1` - Gets the V1 header (if version 1)
- `HeaderV5` - Gets the V5 header (if version 5)
- `GetRootEntries()` - Gets the root-level entries in the archive
- `GetEntries(StuffItArchiveDirectory)` - Gets entries within a directory
- `ReadCompressedDataFork(StuffItArchiveFile, Stream)` - Reads compressed data fork
- `ReadCompressedResourceFork(StuffItArchiveFile, Stream)` - Reads compressed resource fork
- `ReadDecompressedDataFork(StuffItArchiveFile, Stream)` - Extracts decompressed data fork
- `ReadDecompressedResourceFork(StuffItArchiveFile, Stream)` - Extracts decompressed resource fork

### StuffItArchiveHeaderV1

Contains V1 archive-level metadata:

- `Signature1` - First magic number
- `Signature2` - Second magic number (0x724c6175)
- `RootDirectoryEntryCount` - Number of entries in root directory
- `TotalSize` - Total size of the archive in bytes
- `Version` - Archive version number
- `HeaderSize` - Header size (if version > 1)
- `HeaderCRC` - CRC-16 checksum of the header

### StuffItArchiveHeaderV5

Contains V5 archive-level metadata:

- `Signature` - Magic string identifying StuffIt format
- `Version` - Archive version (always 5)
- `Flags` - Archive header flags
- `TotalSize` - Total size of the archive in bytes
- `RootDirectoryEntryCount` - Number of entries in root directory
- `RootDirectoryEntryOffset` - Offset to first root directory entry
- `HeaderCRC` - CRC-16 checksum of the header

### StuffItArchiveEntry

Base class representing an entry in the archive:

- `Name` - The entry name
- `EntryHeaderV1` - V1 entry header (if V1 archive)
- `EntryHeaderV5` - V5 entry header (if V5 archive)

### StuffItArchiveFile

Represents a file entry within the archive:

- `DataStartOffset` - Offset to file data in archive
- `ResourceForkCompressedLength` - Compressed size of resource fork
- `ResourceForkUncompressedLength` - Original size of resource fork
- `ResourceForkCRC` - CRC-16 of resource fork
- `ResourceForkCompressionMethod` - Compression used for resource fork
- `DataForkCompressedLength` - Compressed size of data fork
- `DataForkUncompressedLength` - Original size of data fork
- `DataForkCRC` - CRC-16 of data fork
- `DataForkCompressionMethod` - Compression used for data fork

### StuffItArchiveDirectory

Represents a directory entry within the archive:

- `EntryCount` - Number of entries in the directory
- `TotalSize` - Total size of the directory

### StuffItArchiveEntryHeaderV1

Contains V1 file-level metadata:

- `FileName` - File name (up to 63 characters)
- `Type` - 4-character file type code (e.g., "TEXT", "APPL")
- `Creator` - 4-character creator code (e.g., "MSWD", "ttxt")
- `FinderFlags` - Finder flags (invisible, locked, etc.)
- `CreationDate` - File creation date
- `ModificationDate` - File modification date
- `ResourceForkCompressionMethod` - Compression for resource fork
- `DataForkCompressionMethod` - Compression for data fork

### StuffItArchiveEntryHeaderV5

Contains V5 entry-level metadata:

- `MagicNumber` - Entry magic (0xA5A5A5A5)
- `Version` - Entry version
- `HeaderSize` - Size of the entry header
- `Flags` - Entry flags (directory, encrypted, etc.)
- `CreationDate` - Entry creation date
- `ModificationDate` - Entry modification date
- `PreviousEntryOffset` - Offset to previous entry
- `NextEntryOffset` - Offset to next entry
- `DirectoryEntryOffset` - Offset to parent directory
- `NameLength` - Length of entry name
- `HeaderCRC` - CRC-16 of header

### Enumerations

#### StuffItArchiveCompressionMethod
- `None` - No compression ✓
- `LZW` - LZW compression ✓
- `LZSS` - LZ + Huffman coding ✓
- `Arsenic` - BWT + Arithmetic coding ✓

#### StuffItArchiveEntryHeaderFlags
- `IsDirectory` - Entry is a directory
- `IsEncrypted` - Entry is encrypted (not yet supported)

#### StuffItArchiveHeaderFlags
- Archive-level flags for V5 archives

## Building

Build the project using the .NET SDK:

```sh
dotnet build
```

Run tests:

```sh
dotnet test
```

## Requirements

- .NET 9.0 or later

## License

MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2025 Hugh Bellamy

## About StuffIt

StuffIt was created by Raymond Lau in 1987 and became the de facto standard for file compression and archival on Macintosh computers. The format evolved through multiple versions:

**Format Versions:**
- **V1 (Classic)** - Original StuffIt format with signatures like "SIT!", "ST46", "ST50", etc.
- **V5 (Modern)** - Later format with expanded header and more metadata

**Format Characteristics:**
- Preserves Macintosh file forks (data and resource)
- Maintains file type and creator codes
- Supports hierarchical directory structures
- Multiple compression algorithms for different data types
- CRC-16 checksums for data integrity

**File Extension:**
- `.sit` - StuffIt archive

**Compression Methods:**
- **LZW** - Classic Lempel-Ziv-Welch compression
- **LZSS** - LZ77 variant with Huffman coding (method 13)
- **Arsenic** - Advanced BWT + arithmetic coding (method 15)

## Related Projects

- [AppleDiskImageReader](https://github.com/hughbe/AppleDiskImageReader) - Reader for Apple II universal disk (.2mg) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 disk (.dsk) images
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS (.po) volumes
- [WozDiskImageReader](https://github.com/hughbe/WozDiskImageReader) - Reader for WOZ (.woz) disk images
- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes
- [ApplePartitionMapReader](https://github.com/hughbe/ApplePartitionMapReader) - Reader for Apple Partition Map (APM) images
- [ResourceForkReader](https://github.com/hughbe/ResourceForkReader) - Reader for Macintosh resource forks
- [BinaryIIReader](https://github.com/hughbe/BinaryIIReader) - Reader for Binary II (.bny, .bxy) archives
- [ShrinkItReader](https://github.com/hughbe/ShrinkItReader) - Reader for ShrinkIt/NuFX (.shk) archives

## Documentation

- [StuffIt Format Documentation](https://github.com/mietek/theunarchiver/wiki/StuffItFormat)
- [StuffIt Wikipedia](https://en.wikipedia.org/wiki/StuffIt)
