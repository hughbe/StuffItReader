namespace StuffItReader;

/// <summary>
/// Represents a file entry within a v1 StuffIt archive.
/// </summary>
public class StuffItArchiveFileV1 : StuffItArchiveFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFileV1"/> class.
    /// </summary>
    /// <param name="dataStartOffset">The starting offset of the file entry in the archive.</param>
    /// <param name="entryHeaderV1">The entry header for the file entry.</param>
    public StuffItArchiveFileV1(long dataStartOffset, StuffItArchiveEntryHeaderV1 entryHeaderV1) : base(dataStartOffset, entryHeaderV1)
    {
    }

    /// <inheritdoc/>
    public override string Name => EntryHeaderV1.FileName;

    /// <inheritdoc/>
    public override uint ResourceForkCompressedLength => EntryHeaderV1.ResourceForkCompressedLength;

    /// <inheritdoc/>
    public override uint ResourceForkUncompressedLength => EntryHeaderV1.ResourceForkUncompressedLength;

    /// <inheritdoc/>
    public override ushort ResourceForkCRC => EntryHeaderV1.ResourceForkCRC;

    /// <inheritdoc/>
    public override StuffItArchiveCompressionMethod ResourceForkCompressionMethod => EntryHeaderV1.ResourceForkCompressionMethod;
    
    /// <inheritdoc/>
    public override uint DataForkCompressedLength => EntryHeaderV1.DataForkCompressedLength;

    /// <inheritdoc/>
    public override uint DataForkUncompressedLength => EntryHeaderV1.DataForkUncompressedLength;

    /// <inheritdoc/>
    public override ushort DataForkCRC => EntryHeaderV1.DataForkCRC;

    /// <inheritdoc/>
    public override StuffItArchiveCompressionMethod DataForkCompressionMethod => EntryHeaderV1.DataForkCompressionMethod;
}
