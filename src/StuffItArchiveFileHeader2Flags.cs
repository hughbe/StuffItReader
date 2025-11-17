namespace StuffItReader;

/// <summary>
/// Represents the flags of a StuffIt archive file header.
/// </summary>
[Flags]
public enum StuffItArchiveFileHeader2Flags : ushort
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// File has a resource fork.
    /// </summary>
    HasResourceFork = 0x01,
}