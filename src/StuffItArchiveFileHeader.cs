using System.Buffers.Binary;
using System.Diagnostics;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents the file header of a StuffIt archive file.
/// </summary>
public struct StuffItArchiveFileHeader
{
    /// <summary>
    /// The size of the StuffIt archive file header in bytes.
    /// </summary>
    public const int MinSize = 14;

    /// <summary>
    /// The uncompressed length of the data fork.
    /// </summary>
    public uint DataForkUncompressedLength { get; }

    /// <summary>
    /// The compressed length of the data fork.
    /// </summary>
    public uint DataForkCompressedLength { get; }

    /// <summary>
    /// The CRC-16 of the data fork.
    /// </summary>
    public ushort DataForkCRC { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public ushort Reserved1 { get; }

    /// <summary>
    /// The compression method used for the data fork.
    /// </summary>
    public StuffItArchiveCompressionMethod DataForkCompressionMethod { get; }

    /// <summary>
    /// The length of the password data.
    /// </summary>
    public byte PasswordLength { get; }

    /// <summary>
    /// The password data.
    /// </summary>
    public byte[] PasswordData { get; }

    /// <summary>
    /// The name of the file.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The length of the comment.
    /// </summary>
    public byte CommentLength { get; }

    /// <summary>
    /// The comment associated with the file.
    /// </summary>
    public string Comment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFileHeader"/> class.
    /// </summary>
    /// <param name="header">The header associated with this file.</param>
    /// <param name="entryHeader">The entry header associated with this file.</param>
    /// <param name="data">The raw data for the header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or too short.</exception>
    public StuffItArchiveFileHeader(StuffItArchiveHeaderV5 header, StuffItArchiveEntryHeaderV5 entryHeader, ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException("Data is too short to contain a valid StuffIt archive file header.", nameof(data));
        }

        int offset = 0;

        // Data fork uncompressed length
        DataForkUncompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Data fork compressed length
        DataForkCompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Data fork CRC-16 (Set to zero for method 15)
        DataForkCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Unknown
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Data fork compression method (only 0, 13 or 15)
        DataForkCompressionMethod = (StuffItArchiveCompressionMethod)data[offset];
        offset += 1;

        // Password data length (called M after)
        PasswordLength = data[offset];
        offset += 1;

        Debug.Assert(offset == MinSize);

        // Password information
        PasswordData = data.Slice(offset, PasswordLength).ToArray();
        offset += PasswordLength;

        // Filename
        Name = SpanUtilities.ReadString(data, offset, entryHeader.NameLength);
        offset += entryHeader.NameLength;

        if (header.Flags.HasFlag(StuffItArchiveHeaderFlags.HasComments))
        {
            // Comment size (called K after)
            CommentLength = data[offset];
            offset += 1;

            // Comment
            Comment = SpanUtilities.ReadString(data, offset, CommentLength);
            offset += CommentLength;
        }
        else
        {
            CommentLength = 0;
            Comment = string.Empty;
        }

        Debug.Assert(offset == data.Length);
    }
}