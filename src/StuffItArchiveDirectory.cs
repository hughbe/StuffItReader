namespace StuffItReader;

/// <summary>
/// Represents a directory entry within a StuffIt archive.
/// </summary>
public abstract class StuffItArchiveDirectory : StuffItArchiveEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveDirectory"/> class.
    /// </summary>
    /// <param name="entryHeaderV1">The entry header for the directory entry.</param>
    public StuffItArchiveDirectory(StuffItArchiveEntryHeaderV1 entryHeaderV1) : base(entryHeaderV1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveDirectory"/> class.
    /// </summary>
    /// <param name="entryHeaderV5">The entry header for the directory entry.</param>
    public StuffItArchiveDirectory(StuffItArchiveEntryHeaderV5 entryHeaderV5) : base(entryHeaderV5)
    {
    }
    
    /// <summary>
    /// Gets the number of entries in the directory.
    /// </summary>
    public abstract int EntryCount { get;}

    /// <summary>
    /// Gets the total size of the directory in bytes.
    /// </summary>
    public abstract uint TotalSize { get; }
}
