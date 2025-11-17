namespace StuffItReader;

/// <summary>
/// Represents a file entry within a v5 StuffIt archive.
/// </summary>
public class StuffItArchiveFileV5 : StuffItArchiveFile
{
    /// <summary>
    /// The primary header information for the file entry.
    /// </summary>
    public StuffItArchiveFileHeader Header { get; }

    /// <summary>
    /// The secondary header information for the file entry.
    /// </summary>
    public StuffItArchiveFileHeader2 Header2 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFileV5"/> class.
    /// </summary>
    /// <param name="dataStartOffset">The starting offset of the file entry in the archive.</param>
    /// <param name="entryHeaderV5">The entry header for the file entry.</param>
    /// <param name="header">The primary header for the file entry.</param>
    /// <param name="header2">The secondary header for the file entry.</param>
    public StuffItArchiveFileV5(long dataStartOffset, StuffItArchiveEntryHeaderV5 entryHeaderV5, StuffItArchiveFileHeader header, StuffItArchiveFileHeader2 header2) : base(dataStartOffset, entryHeaderV5)
    {
        Header = header;
        Header2 = header2;
    }

    /// <inheritdoc/>
    public override string Name => Header.Name;

    /// <inheritdoc/>
    public override uint ResourceForkCompressedLength => Header2.ResourceForkCompressedLength;

    /// <inheritdoc/>
    public override uint ResourceForkUncompressedLength => Header2.ResourceForkUncompressedLength;

    /// <inheritdoc/>
    public override ushort ResourceForkCRC => Header2.ResourceForkCRC;

    /// <inheritdoc/>
    public override StuffItArchiveCompressionMethod ResourceForkCompressionMethod => Header2.ResourceForkCompressionMethod;

    /// <inheritdoc/>
    public override uint DataForkCompressedLength => Header.DataForkCompressedLength;

    /// <inheritdoc/>
    public override uint DataForkUncompressedLength => Header.DataForkUncompressedLength;

    /// <inheritdoc/>
    public override ushort DataForkCRC => Header.DataForkCRC;

    /// <inheritdoc/>
    public override StuffItArchiveCompressionMethod DataForkCompressionMethod => Header.DataForkCompressionMethod;
}
