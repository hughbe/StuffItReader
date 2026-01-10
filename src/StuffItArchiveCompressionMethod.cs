namespace StuffItReader;

/// <summary>
/// Defines the compression types used in StuffIt archives.
/// </summary>
public enum StuffItArchiveCompressionMethod
{
    /// <summary>
    /// No compression.
    /// </summary>
    None = 0,

    /// <summary>
    /// LZW compression.
    /// </summary>
    LZW = 2,

    /// <summary>
    /// LZ + Huffman coding.
    /// </summary>
    LZSS = 13,

    /// <summary>
    /// Arsenic: Arithmetic, RLE and block_s_orting. BWT + Arithmetic.
    /// </summary>
    Arsenic = 15
}
