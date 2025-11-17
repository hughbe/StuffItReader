using System.Buffers.Binary;
using System.Diagnostics;

namespace StuffItReader;

/// <summary>
/// The V1 header of a StuffIt archive.
/// </summary>
public struct StuffItArchiveHeaderV1
{
    /// <summary>
    /// The size of the StuffIt archive V1 header in bytes.
    /// </summary>
    public const int Size = 22;

    /// <summary>
    /// The signature part 1 of the StuffIt archive.
    /// </summary>
    public uint Signature1 { get; }

    /// <summary>
    /// The number of entries in the root directory.
    /// </summary>
    public ushort RootDirectoryEntryCount { get; }

    /// <summary>
    /// The total size of the StuffIt archive.
    /// </summary>
    public uint TotalSize { get; }

    /// <summary>
    /// The signature part 2 of the StuffIt archive.
    /// </summary>
    public uint Signature2 { get; }

    /// <summary>
    /// The version of the StuffIt archive.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public byte Reserved { get; }

    /// <summary>
    /// The header size (if version is not 1).
    /// </summary>
    public uint HeaderSize { get; }

    /// <summary>
    /// The CRC-16 of the header.
    /// </summary>
    public ushort HeaderCRC { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveHeaderV1"/> struct by parsing the provided data.
    /// </summary>
    /// <param name="data">The raw data for the header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or of incorrect size.</exception>
    public StuffItArchiveHeaderV1(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes in length.", nameof(data));
        }

        int offset = 0; 

        // Magic number 1 (see below)
        var signatureBuffer = data.Slice(offset, 4);
        Signature1 = BinaryPrimitives.ReadUInt32BigEndian(signatureBuffer);
        offset += 4;

        if (!signatureBuffer.SequenceEqual("SIT!"u8) &&
            !signatureBuffer.SequenceEqual("ST46"u8) &&
            !signatureBuffer.SequenceEqual("ST50"u8) &&
            !signatureBuffer.SequenceEqual("ST60"u8) &&
            !signatureBuffer.SequenceEqual("ST65"u8) &&
            !signatureBuffer.SequenceEqual("STin"u8) &&
            !signatureBuffer.SequenceEqual("STi2"u8) &&
            !signatureBuffer.SequenceEqual("STi3"u8) &&
            !signatureBuffer.SequenceEqual("STi4"u8))
        {
            throw new ArgumentException("Invalid StuffIt archive V1 signature.", nameof(data));
        }

        // Number of entries in root directory
        RootDirectoryEntryCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Total size of archive
        TotalSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Magic number 2 (always 0x724c6175)
        Signature2 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        if (Signature2 != 0x724C6175)
        {
            throw new ArgumentException("Invalid StuffIt archive V1 signature.", nameof(data));
        }

        // Version
        Version = data[offset];
        offset += 1;

        // Unknown
        Reserved = data[offset];
        offset += 1;

        // Header size (if version not 1)
        HeaderSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // CRC-16 of header
        HeaderCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
