namespace StuffItReader;

/// <summary>
/// Represents a directory entry within a v5 StuffIt archive.
/// </summary>
public class StuffItArchiveDirectoryV5 : StuffItArchiveDirectory
{
    /// <summary>
    /// The header information for the directory entry.
    /// </summary>
    public StuffItArchiveDirectoryHeader Header { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveDirectoryV5"/> class.
    /// </summary>
    /// <param name="entryHeaderV1">The entry header for the directory entry.</param>
    /// <param name="header">The header for the directory entry.</param>
    public StuffItArchiveDirectoryV5(StuffItArchiveEntryHeaderV5 entryHeaderV1, StuffItArchiveDirectoryHeader header) : base(entryHeaderV1)
    {
        Header = header;
    }

    /// <inheritdoc/>
    public override string Name => Header.Name;

    /// <inheritdoc/>
    public override int EntryCount => Header.FileCount;

    /// <inheritdoc/>
    public override uint TotalSize => Header.TotalSize;
}
