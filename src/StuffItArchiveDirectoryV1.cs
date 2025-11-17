namespace StuffItReader;

/// <summary>
/// Represents a directory entry within a v1 StuffIt archive.
/// </summary>
public class StuffItArchiveDirectoryV1 : StuffItArchiveDirectory
{
    /// <summary>
    /// The header information for the directory entry.
    /// </summary>
    public StuffItArchiveEntryHeaderV1 Header { get; }

    /// <summary>
    /// The entries contained within the directory.
    /// </summary>
    public List<StuffItArchiveEntry> Entries { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveDirectoryV1"/> class.
    /// </summary>
    /// <param name="header">The header for the directory entry.</param>
    public StuffItArchiveDirectoryV1(StuffItArchiveEntryHeaderV1 header) : base(header)
    {
        Header = header;
    }

    /// <inheritdoc/>
    public override string Name => Header.FileName;

    /// <inheritdoc/>
    public override int EntryCount => Entries.Count;

    /// <inheritdoc/>
    public override uint TotalSize => Header.DataForkCompressedLength;
}
