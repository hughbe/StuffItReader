using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents the header of a v5 StuffIt archive file.
/// </summary>
public struct StuffItArchiveHeaderV5
{
    /// <summary>
    /// The size of the StuffIt archive header in bytes.
    /// </summary>
    public const int Size = 100;

    /// <summary>
    /// The signature/magic string of the StuffIt archive.
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public uint Reserved1 { get; }

    /// <summary>
    /// The version of the StuffIt archive.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// The flags of the StuffIt archive.
    /// </summary>
    public StuffItArchiveHeaderFlags Flags { get; }

    /// <summary>
    /// The total size of the StuffIt archive.
    /// </summary>
    public uint TotalSize { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public uint Reserved2 { get; }

    /// <summary>
    /// The number of entries in the root directory of the StuffIt archive.
    /// </summary>
    public ushort RootDirectoryEntryCount { get; }

    /// <summary>
    /// The offset of the first entry in the root directory of the StuffIt archive.
    /// </summary>
    public uint RootDirectoryEntryOffset { get; }

    /// <summary>
    /// The CRC of the header.
    /// </summary>
    public ushort HeaderCRC { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveHeaderV5"/> class by parsing header data.
    /// </summary>
    /// <param name="data">A span containing the header data.</param>
    /// <exception cref="ArgumentException">>Thrown when <paramref name="data"/> is invalid or too small.</exception>
    public StuffItArchiveHeaderV5(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Magic string (always StuffIt (c)1997-???? Aladdin Systems, Inc., http://www.aladdinsys.com/StuffIt/ followed by 0x0D 0x0A, where characters marked ? can vary)
        var signature = data.Slice(offset, 80);
        ReadOnlySpan<byte> expectedSignatureStart1 = "StuffIt (c)1997-"u8;
        if (!signature.StartsWith(expectedSignatureStart1))
        {
            throw new ArgumentException("Invalid StuffIt archive signature.", nameof(data));
        }
        // Next 4 bytes can be anything.
        var signatureStart2 = signature[(expectedSignatureStart1.Length + 4)..];
        ReadOnlySpan<byte> signatureStart2Expected = " Aladdin Systems, Inc., http://www.aladdinsys.com/StuffIt/\r\n"u8;
        if (!signatureStart2.SequenceEqual(signatureStart2Expected))
        {
            throw new ArgumentException("Invalid StuffIt archive signature.", nameof(data));
        }

        Signature = Encoding.ASCII.GetString(signature);
        offset += 80;

        // Unknown
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Version (always 5)
        Version = data[offset];
        offset += 1;
        if (Version != 5)
        {
            throw new ArgumentException("Unsupported StuffIt archive version.", nameof(data));
        }

        // Flags (0x80 = encrypted)
        Flags = (StuffItArchiveHeaderFlags)data[offset];
        offset += 1;

        // Total size of archive
        TotalSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        // Note - always the same as RootDirectoryEntryOffset?
        Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Number of entries in root directory
        RootDirectoryEntryCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Offset of first entry in root directory
        RootDirectoryEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Header CRC-16?
        HeaderCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
