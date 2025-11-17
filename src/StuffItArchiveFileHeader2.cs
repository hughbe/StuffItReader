using System.Buffers.Binary;
using System.Diagnostics;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// Represents the flags of a StuffIt archive file header 2.
/// </summary>
public struct StuffItArchiveFileHeader2
{
    /// <summary>
    /// The size of the StuffIt archive file header 2 in bytes.
    /// </summary>
    public const int MinSize = 42;

    /// <summary>
    /// The maximum size of the StuffIt archive file header 2 in bytes.
    /// </summary>
    public const int MaxSize = 50;

    /// <summary>
    /// Represents the flags of a StuffIt archive file header.
    /// </summary>
    public StuffItArchiveFileHeader2Flags Flags { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Reserved1 { get; }

    /// <summary>
    /// The Mac OS file type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The Mac OS file creator.
    /// </summary>
    public string Creator { get; }

    /// <summary>
    /// The Mac OS Finder flags.
    /// </summary>
    public ushort FinderFlags { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Reserved2 { get; }

    /// <summary>
    /// Unknown (A date value in version 3?)
    /// </summary>
    public uint Reserved3 { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public uint Reserved4 { get; }

    /// <summary>
    /// Unknown, not included in version 3, included in version 1.
    /// </summary>
    public uint Reserved5 { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public uint Reserved6 { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public uint Reserved7 { get; }

    /// <summary>
    /// The uncompressed length of the resource fork.
    /// </summary>
    public uint ResourceForkUncompressedLength { get; }

    /// <summary>
    /// The compressed length of the resource fork.
    /// </summary>
    public uint ResourceForkCompressedLength { get; }

    /// <summary>
    /// The CRC-16 of the resource fork.
    /// </summary>
    public ushort ResourceForkCRC { get; }

    /// <summary>
    /// The actual size of this header structure in bytes.
    /// </summary>
    public int ActualSize { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public ushort Reserved8 { get; }

    /// <summary>
    /// The compression method used for the resource fork.
    /// </summary>
    public StuffItArchiveCompressionMethod ResourceForkCompressionMethod { get; }

    /// <summary>
    /// Unknown
    /// </summary>
    public byte Reserved9 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveFileHeader2"/> class.
    /// </summary>
    /// <param name="data">The raw data for the header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or too short.</exception>
    public StuffItArchiveFileHeader2(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException("Data is too short to contain a valid StuffIt archive file header.", nameof(data));
        }

        int offset = 0;

        // Flags 2. Bit 0 indicates the presence of a resource fork.
        Flags = (StuffItArchiveFileHeader2Flags)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Unknown
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Mac OS file type
        Type = SpanUtilities.ReadString(data, offset, 4);
        offset += 4;

        // Mac OS file creator
        Creator = SpanUtilities.ReadString(data, offset, 4);
        offset += 4;

        // Mac OS Finder flags
        FinderFlags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Unknown
        Reserved2 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Unknown (A date value in version 3?)
        Reserved3 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        Reserved4 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        Reserved5 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        Reserved6 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown, not included in version 3, included in version 1.
        Reserved7 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4)); ;
        offset += 4;

        // The rest of the fields are only present if the resource fork flag is set.
        if (Flags.HasFlag(StuffItArchiveFileHeader2Flags.HasResourceFork))
        {
            // Resource fork uncompressed length. Only included if bit 0 of "Flags 2" is set.
            ResourceForkUncompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
            offset += 4;

            // Resource fork compressed length. Only included if bit 0 of "Flags 2" is set.
            ResourceForkCompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
            offset += 4;

            // Resource fork CRC-16 (Set to zero for method 15). Only included if bit 0 of "Flags 2" is set.
            if (Flags.HasFlag(StuffItArchiveFileHeader2Flags.HasResourceFork))
            {
                ResourceForkCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
                offset += 2;
            }

            // Unknown
            Reserved8 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            offset += 2;

            // Resource fork compression method
            ResourceForkCompressionMethod = (StuffItArchiveCompressionMethod)data[offset];
            offset += 1;

            // Unknown
            Reserved9 = data[offset];
            offset += 1;
        }

        Debug.Assert(Flags.HasFlag(StuffItArchiveFileHeader2Flags.HasResourceFork)
            ? offset == MaxSize
            : offset == MinSize);
        
        ActualSize = offset;
    }
}