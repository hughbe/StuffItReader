using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Hashing;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents the header of a v5 StuffIt archive entry.
/// </summary>
public struct StuffItArchiveEntryHeaderV5
{
    /// <summary>
    /// The size of the StuffIt archive entry header in bytes.
    /// </summary>
    public const int Size = 34;

    /// <summary>
    /// The magic number of the StuffIt archive entry.
    /// </summary>
    public uint MagicNumber { get; }

    /// <summary>
    /// The version of the StuffIt archive entry.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Reserved byte.
    /// </summary>
    public byte Reserved1 { get; }

    /// <summary>
    /// The size of the header in bytes.
    /// </summary>
    public ushort HeaderSize { get; }

    /// <summary>
    /// Reserved byte.
    /// </summary>
    public byte Reserved2 { get; }

    /// <summary>
    /// The flags/type of the StuffIt archive entry.
    /// </summary>
    public StuffItArchiveEntryHeaderFlags Flags { get; }

    /// <summary>
    /// The creation date of the entry.
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// The modification date of the entry.
    /// </summary>
    public DateTime ModificationDate { get; }

    /// <summary>
    /// The offset of the previous entry in the archive.
    /// </summary>
    public uint PreviousEntryOffset { get; }

    /// <summary>
    /// The offset of the next entry in the archive.
    /// </summary>
    public uint NextEntryOffset { get; }

    /// <summary>
    /// The offset of the directory entry in the archive.
    /// </summary>
    public uint DirectoryEntryOffset { get; }

    /// <summary>
    /// The size of the entry name in bytes.
    /// </summary>
    public ushort NameLength { get; }

    /// <summary>
    /// The CRC-16 checksum of the header.
    /// </summary>
    public ushort HeaderCRC { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveEntryHeaderV5"/> class.
    /// </summary>
    /// <param name="data">The raw data for the header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or too short.</exception>
    public StuffItArchiveEntryHeaderV5(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Magic number (always 0xA5A5A5A5)
        MagicNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        if (MagicNumber != 0xA5A5A5A5)
        {
            throw new ArgumentException("Invalid magic number in StuffIt archive entry header.", nameof(data));
        }

        // Version
        Version = data[offset];
        offset += 1;

        // Unknown (but certainly 0x00)
        Reserved1 = data[offset];
        offset += 1;

        // Header size
        HeaderSize = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Unknown
        Reserved2 = data[offset];
        offset += 1;

        // Entry flags/type
        Flags = (StuffItArchiveEntryHeaderFlags)data[offset];
        offset += 1;

        // Creation date (Mac OS format)
        CreationDate = SpanUtilities.ReadMacOSDate(data, offset);
        offset += 4;

        // Modification date (Mac OS format)
        ModificationDate = SpanUtilities.ReadMacOSDate(data, offset);
        offset += 4;

        // Offset of previous entry
        PreviousEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Offset of next entry
        NextEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Offset of directory entry
        DirectoryEntryOffset = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Name size
        NameLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Header CRC-16
        HeaderCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
