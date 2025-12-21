using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents the directory header of a StuffIt archive entry.
/// </summary>
public struct StuffItArchiveDirectoryHeader
{
    /// <summary>
    /// The minimum size of the StuffIt archive directory header in bytes.
    /// </summary>
    public const int MinSize = 14;

    /// <summary>
    /// The offset of the first entry in the directory.
    /// </summary>
    public uint FirstEntryOffset { get; }

    /// <summary>
    /// The total size of the directory.
    /// </summary>
    public uint TotalSize { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public uint Reserved { get; }

    /// <summary>
    /// The number of files in the directory.
    /// </summary>
    public ushort FileCount { get; }

    /// <summary>
    /// The name of the directory.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The length of the comment.
    /// </summary>
    public ushort CommentLength { get; }

    /// <summary>
    /// Reserved field for comment (unknown purpose).
    /// </summary>
    public ushort CommentReserved { get; }

    /// <summary>
    /// The comment associated with the directory.
    /// </summary>
    public string Comment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveDirectoryHeader"/> class
    /// by reading from the provided data and stream.
    /// </summary>
    /// <param name="header">The header associated with this archive.</param>
    /// <param name="entryHeader">The entry header associated with this directory.</param>
    /// <param name="data">The raw data for the directory header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or too short.</exception>
    public StuffItArchiveDirectoryHeader(StuffItArchiveHeaderV5 header, StuffItArchiveEntryHeaderV5 entryHeader, ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Data is too small to be a valid StuffIt archive directory header. Expected at least {MinSize} bytes, got {data.Length} bytes.", nameof(data));
        }

        int offset = 0;

        // Offset of first entry in folder
        FirstEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(0, 4));
        offset += 4;

        // Size of complete directory
        TotalSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        Reserved = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Number of files in folder
        FileCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Folder name
        Name = SpanUtilities.ReadString(data, offset, entryHeader.NameLength);
        offset += entryHeader.NameLength;

        if (offset < data.Length)
        {
            // Comment size
            CommentLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            offset += 2;

            // Reserved
            CommentReserved = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            offset += 2;

            // Comment
            Comment = SpanUtilities.ReadString(data, offset, CommentLength);
            offset += CommentLength;
        }
        else
        {
            CommentLength = 0;
            CommentReserved = 0;
            Comment = string.Empty;
        }

        Debug.Assert(offset == data.Length, "Did not consume all directory header data.");
    }
}
