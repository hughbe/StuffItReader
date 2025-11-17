namespace StuffItReader;

/// <summary>
/// Represents the flags of a StuffIt archive entry header.
/// </summary>
[Flags]
public enum StuffItArchiveEntryHeaderFlags : byte
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Entry is encrypted.
    /// </summary>
    Encrypted = 0x20,

    /// <summary>
    /// Entry is a directory.
    /// </summary>
    IsDirectory = 0x40,
}
