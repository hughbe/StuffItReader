namespace StuffItReader;

/// <summary>
/// Represents an entry in a StuffIt archive.
/// </summary>
public abstract class StuffItArchiveEntry
{
    /// <summary>
    /// The v1 entry header information.
    /// </summary>
    public StuffItArchiveEntryHeaderV1 EntryHeaderV1 { get; }

    /// <summary>
    /// The v5 entry header information.
    /// </summary>
    public StuffItArchiveEntryHeaderV5 EntryHeaderV5 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveEntry"/> class.
    /// </summary>
    /// <param name="entryHeaderV1">The entry header information.</param>
    public StuffItArchiveEntry(StuffItArchiveEntryHeaderV1 entryHeaderV1)
    {
        EntryHeaderV1 = entryHeaderV1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveEntry"/> class.
    /// </summary>
    /// <param name="entryHeaderV5">The entry header information.</param>
    public StuffItArchiveEntry(StuffItArchiveEntryHeaderV5 entryHeaderV5)
    {
        EntryHeaderV5 = entryHeaderV5;
    }

    /// <summary>
    /// Gets the name of the entry.
    /// </summary>
    public abstract string Name { get; }
}