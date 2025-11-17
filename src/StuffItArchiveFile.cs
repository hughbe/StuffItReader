namespace StuffItReader;

/// <summary>
/// Represents a file entry within a StuffIt archive.
/// </summary>
public abstract class StuffItArchiveFile : StuffItArchiveEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFile"/> class.
    /// </summary>
    /// <param name="dataStartOffset">The starting offset of the file entry in the archive.</param>
    /// <param name="entryHeaderV1">The entry header for the file entry.</param>
    public StuffItArchiveFile(long dataStartOffset, StuffItArchiveEntryHeaderV1 entryHeaderV1) : base(entryHeaderV1)
    {
        DataStartOffset = dataStartOffset;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFile"/> class.
    /// </summary>
    /// <param name="dataStartOffset">The starting offset of the file entry in the archive.</param>
    /// <param name="entryHeaderV5">The entry header for the file entry.</param>
    public StuffItArchiveFile(long dataStartOffset, StuffItArchiveEntryHeaderV5 entryHeaderV5) : base(entryHeaderV5)
    {
        DataStartOffset = dataStartOffset;
    }

    /// <summary>
    /// The starting offset of the file entry's data within the archive.
    /// </summary>
    public long DataStartOffset { get; }

    /// <summary>
    /// Gets the compressed length of the resource fork.
    /// </summary>
    public abstract uint ResourceForkCompressedLength { get; }

    /// <summary>
    /// Gets the uncompressed length of the resource fork.
    /// </summary>
    public abstract uint ResourceForkUncompressedLength { get; }

    /// <summary>
    /// Gets the CRC-16 of the resource fork.
    /// </summary>
    public abstract ushort ResourceForkCRC { get; }

    /// <summary>
    /// Gets the compression method used for the resource fork.
    /// </summary>
    public abstract StuffItArchiveCompressionMethod ResourceForkCompressionMethod { get; }

    /// <summary>
    /// Gets the compressed length of the data fork.
    /// </summary>
    public abstract uint DataForkCompressedLength { get; }

    /// <summary>
    /// Gets the uncompressed length of the data fork.
    /// </summary>
    public abstract uint DataForkUncompressedLength { get; }

    /// <summary>
    /// Gets the CRC-16 of the data fork.
    /// </summary>
    public abstract ushort DataForkCRC { get; }

    /// <summary>
    /// Gets the compression method used for the data fork.
    /// </summary>
    public abstract StuffItArchiveCompressionMethod DataForkCompressionMethod { get; }
}
