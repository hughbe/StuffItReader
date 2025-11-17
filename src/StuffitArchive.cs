using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using StuffItReader.Compression;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents a StuffIt archive file and provides methods for reading archive data.
/// </summary>
public class StuffItArchive
{
    private readonly Stream _stream;

    /// <summary>
    /// The version of the StuffIt archive.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// The header of the StuffIt archive if version 1.
    /// </summary>
    public StuffItArchiveHeaderV1? HeaderV1 { get; }

    /// <summary>
    /// The header of the StuffIt archive if version 5.
    /// </summary>
    public StuffItArchiveHeaderV5? HeaderV5 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchive"/> class by reading from a stream.
    /// </summary>
    /// <param name="stream">A seekable and readable stream containing StuffIt archive data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> is not seekable or readable.</exception>
    public StuffItArchive(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;

        // Use the v5 header as it is the largest and most common.
        Span<byte> buffer = stackalloc byte[StuffItArchiveHeaderV5.Size];
        if (_stream.Length < 4)
        {
            throw new ArgumentException("Stream is too small to contain a valid StuffIt archive header.", nameof(stream));
        }

        // Peek the first four bytes to determine the version.
        var position = _stream.Position;
        var peekBuffer = buffer[..4];
        _stream.ReadExactly(peekBuffer);
        _stream.Seek(position, SeekOrigin.Begin);

        if (peekBuffer.SequenceEqual("SIT!"u8) ||
            peekBuffer.SequenceEqual("ST46"u8) ||
            peekBuffer.SequenceEqual("ST50"u8) ||
            peekBuffer.SequenceEqual("ST60"u8) ||
            peekBuffer.SequenceEqual("ST65"u8) ||
            peekBuffer.SequenceEqual("STin"u8) ||
            peekBuffer.SequenceEqual("STi2"u8) ||
            peekBuffer.SequenceEqual("STi3"u8) ||
            peekBuffer.SequenceEqual("STi4"u8))
        {
            Version = 1;

            // Read and parse the archive header for v1.
            if (_stream.Length < StuffItArchiveHeaderV1.Size)
            {
                throw new ArgumentException("Stream is too small to contain a valid StuffIt archive header.", nameof(stream));
            }

            var headerBuffer = buffer[..StuffItArchiveHeaderV1.Size];
            _stream.ReadExactly(headerBuffer);

            HeaderV1 = new StuffItArchiveHeaderV1(headerBuffer);
            if (_stream.Length < HeaderV1.Value.TotalSize)
            {
                throw new ArgumentException("Stream is smaller than the total size specified in the archive header.", nameof(stream));
            }
            return;
        }
        else
        {
            Version = 5;
            
            // Read and parse the archive header for v5.
            if (_stream.Length < StuffItArchiveHeaderV5.Size)
            {
                throw new ArgumentException("Stream is too small to contain a valid StuffIt archive header.", nameof(stream));
            }
            _stream.ReadExactly(buffer);

            HeaderV5 = new StuffItArchiveHeaderV5(buffer);
            if (_stream.Length < HeaderV5.Value.TotalSize)
            {
                throw new ArgumentException("Stream is smaller than the total size specified in the archive header.", nameof(stream));
            }

            // Read the root directory entries.
            if (HeaderV5.Value.RootDirectoryEntryOffset > _stream.Length)
            {
                throw new ArgumentException("Root directory entry offset is beyond the end of the stream.", nameof(stream));
            }
        }
    }

    /// <summary>
    /// Gets the root entries of the StuffIt archive.
    /// </summary>
    /// <returns>An enumerable of <see cref="StuffItArchiveEntry"/> representing the root entries.</returns>
    public List<StuffItArchiveEntry> GetRootEntries()
    {
        if (Version == 1)
        {
            // In v1, the archive header is followed by the first file header.
            return ReadEntriesAtOffsetV1(StuffItArchiveHeaderV1.Size, HeaderV1!.Value.RootDirectoryEntryCount);
        }
        else
        {
            // In v5, use the root directory entry offset and count from the header.
            return ReadEntriesAtOffsetV5(HeaderV5!.Value.RootDirectoryEntryOffset, HeaderV5.Value.RootDirectoryEntryCount);
        }
    }

    /// <summary>
    /// Gets the entries within a specified directory.
    /// </summary>
    /// <param name="directory">The directory whose entries are to be retrieved.</param>
    /// <returns>An enumerable of <see cref="StuffItArchiveEntry"/> representing the entries within the directory.</returns>
    public List<StuffItArchiveEntry> GetEntries(StuffItArchiveDirectory directory)
    {
        if (directory is StuffItArchiveDirectoryV1 v1Directory)
        {
            return v1Directory.Entries;
        }
        else if (directory is StuffItArchiveDirectoryV5 v5Directory)
        {
            return ReadEntriesAtOffsetV5(v5Directory.EntryHeaderV5.DirectoryEntryOffset, v5Directory.Header.FileCount);
        }
        else
        {
            throw new NotSupportedException("Unsupported directory type for the current StuffIt archive version.");
        }
    }

    private List<StuffItArchiveEntry> ReadEntriesAtOffsetV1(long offset, int count)
    {
        var entries = new List<StuffItArchiveEntry>(count);
        var directoriesStack = new Stack<StuffItArchiveDirectoryV1>();
        Span<byte> entryHeaderBuffer = stackalloc byte[StuffItArchiveEntryHeaderV1.Size];

        while (offset < _stream.Length)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(entryHeaderBuffer);
            offset += StuffItArchiveEntryHeaderV1.Size;

            var entryHeader = new StuffItArchiveEntryHeaderV1(entryHeaderBuffer);
            if (entryHeader.DataForkCompressionMethod == (StuffItArchiveCompressionMethod)32 ||
                entryHeader.ResourceForkCompressionMethod == (StuffItArchiveCompressionMethod)32)
            {
                // This is a folder.
                var directory = new StuffItArchiveDirectoryV1(entryHeader);
                directoriesStack.Push(directory);
                entries.Add(directory);
            }
            else if (entryHeader.DataForkCompressionMethod == (StuffItArchiveCompressionMethod)33 ||
                    entryHeader.ResourceForkCompressionMethod == (StuffItArchiveCompressionMethod)33)
            {
                if (directoriesStack.Count == 0)
                {
                    throw new InvalidDataException("Encountered a folder end entry without a matching folder start.");
                }

                directoriesStack.Pop();
            }
            else
            {
                long dataStartOffset = _stream.Position;
                var file = new StuffItArchiveFileV1(dataStartOffset, entryHeader);
                if (directoriesStack.Count > 0)
                {
                    directoriesStack.Peek().Entries.Add(file);
                }
                else
                {
                    entries.Add(file);
                }

                // Move to the next entry.
                offset += entryHeader.ResourceForkCompressedLength + entryHeader.DataForkCompressedLength;
            }
        }

        return entries;
    }

    private List<StuffItArchiveEntry> ReadEntriesAtOffsetV5(long offset, int count)
    {
        Span<byte> buffer = stackalloc byte[512];
        var entries = new List<StuffItArchiveEntry>(count);
        
        Span<byte> crcBytes = stackalloc byte[2];
        for (int i = 0; i < count; i++)
        {
            _stream.Seek(offset, SeekOrigin.Begin);

            // Peek the header size (offset 6, 2 bytes)
            _stream.Seek(6, SeekOrigin.Current);
            _stream.ReadExactly(buffer[..2]);
            _stream.Seek(-8, SeekOrigin.Current);
            ushort entryHeaderSize = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));

            Span<byte> entryHeaderBuffer = entryHeaderSize <= buffer.Length
                ? buffer[..entryHeaderSize]
                : new byte[entryHeaderSize];
            _stream.ReadExactly(entryHeaderBuffer);
            var entryHeader = new StuffItArchiveEntryHeaderV5(entryHeaderBuffer);

            if (entryHeader.Flags.HasFlag(StuffItArchiveEntryHeaderFlags.IsDirectory))
            {
                var directoryHeader = new StuffItArchiveDirectoryHeader(entryHeader, entryHeaderBuffer[StuffItArchiveEntryHeaderV5.Size..]);
                entries.Add(new StuffItArchiveDirectoryV5(entryHeader, directoryHeader));
            }
            else
            {
                var fileEntry = new StuffItArchiveFileHeader(HeaderV5!.Value, entryHeader, entryHeaderBuffer[StuffItArchiveEntryHeaderV5.Size..]);

                // Read File Header 2 if present (max 50 bytes).
                long fileHeader2Start = _stream.Position;
                _stream.ReadExactly(buffer[..StuffItArchiveFileHeader2.MaxSize]);
                var fileEntry2 = new StuffItArchiveFileHeader2(buffer[..StuffItArchiveFileHeader2.MaxSize]);

                // Calculate the actual data start position
                long dataStartOffset = fileHeader2Start + fileEntry2.ActualSize;
                entries.Add(new StuffItArchiveFileV5(dataStartOffset, entryHeader, fileEntry, fileEntry2));
            }

            offset = entryHeader.NextEntryOffset;
        }

        return entries;
    }

    /// <summary>
    /// Gets the compressed resource fork of a specified file entry.
    /// </summary>
    /// <param name="file">The file entry whose resource fork is to be retrieved.</param>
    /// <param name="output">The output stream to write the compressed resource fork to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long ReadCompressedResourceFork(StuffItArchiveFile file, Stream output)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(output);

        // The resource fork immediately follows the entry headers.
        var offset = file.DataStartOffset;
        return ReadCompressedData(file, offset, file.ResourceForkCompressedLength, file.ResourceForkCRC, output);
    }

    /// <summary>
    /// Gets the compressed data fork of a specified file entry.
    /// </summary>
    /// <param name="file">The file entry whose data fork is to be retrieved.</param>
    /// <param name="output">The output stream to write the compressed data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long ReadCompressedDataFork(StuffItArchiveFile file, Stream output)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(output);

        // The data fork immediately follows the entry headers and the resource fork.
        var offset = file.DataStartOffset + file.ResourceForkCompressedLength;
        return ReadCompressedData(file, offset, file.DataForkCompressedLength, file.DataForkCRC, output);
    }

    private long ReadCompressedData(StuffItArchiveFile file, long offset, long length, long expectedCrc, Stream output)
    {
        Span<byte> buffer = stackalloc byte[512];

        _stream.Seek(offset, SeekOrigin.Begin);
        long remaining = length;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int bytesRead = _stream.Read(buffer[..toRead]);
            if (bytesRead == 0)
            {
                break;
            }

            output.Write(buffer[..bytesRead]);
            remaining -= bytesRead;
        }

        return length;
    }

    /// <summary>
    /// Gets the decompressed resource fork of a specified file entry.
    /// </summary>
    /// <param name="file">The file entry whose resource fork is to be retrieved.</param>
    /// <param name="output">The output stream to write the decompressed resource fork to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long ReadDecompressedResourceFork(StuffItArchiveFile file, Stream output)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(output);

        // The resource fork immediately follows the entry headers.
        var offset = file.DataStartOffset;
        return DecompressData(file, offset, file.ResourceForkUncompressedLength, file.ResourceForkCRC, file.ResourceForkCompressionMethod, output);
    }

    /// <summary>
    /// Gets the decompressed data fork of a specified file entry.
    /// </summary>
    /// <param name="file">The file entry whose data fork is to be retrieved.</param>
    /// <param name="output">The output stream to write the decompressed data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long ReadDecompressedDataFork(StuffItArchiveFile file, Stream output)
    {
        // The data fork immediately follows the entry headers and the COMPRESSED resource fork.
        var offset = file.DataStartOffset + file.ResourceForkCompressedLength;
        return DecompressData(file, offset, file.DataForkUncompressedLength, file.DataForkCRC, file.DataForkCompressionMethod, output);
    }

    private long DecompressData(StuffItArchiveFile file, long offset, long length, long expectedCrc, StuffItArchiveCompressionMethod compressionMethod, Stream output)
    {
        return compressionMethod switch
        {
            StuffItArchiveCompressionMethod.None => ReadCompressedData(file, offset, length, expectedCrc, output),
            StuffItArchiveCompressionMethod.LZSS => DecompressLZSS(offset, length, output),
            StuffItArchiveCompressionMethod.Arsenic => DecompressArsenic(offset, output),
            _ => throw new NotSupportedException($"Unsupported compression method: {compressionMethod}."),
        };
    }

    private long DecompressLZSS(long offset, long decompressedLength, Stream output)
    {
        _stream.Seek(offset, SeekOrigin.Begin);
        
        long startPosition = output.Position;
        var decompressor = new LzssDecompressor(_stream);
        decompressor.Decompress(output, decompressedLength);
        
        return output.Position - startPosition;
    }

    private long DecompressArsenic(long offset, Stream output)
    {
        _stream.Seek(offset, SeekOrigin.Begin);
        
        long startPosition = output.Position;
        var decompressor = new ArsenicDecompressor(_stream);
        decompressor.Decompress(output);
        
        return output.Position - startPosition;
    }
}
