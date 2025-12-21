namespace StuffItReader;

/// <summary>
/// Represents the flags of a StuffIt archive header.
/// </summary>
[Flags]
public enum StuffItArchiveHeaderFlags : byte
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Archive has extended info (alternative comment format).
    /// </summary>
    HasExtendedInfo = 0x10,

    /// <summary>
    /// Archive has comments.
    /// </summary>
    HasComments = 0x20,

    /// <summary>
    /// Archive is encrypted.
    /// </summary>
    Encrypted = 0x80,
}
