using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using StuffItReader.Utilities;

namespace StuffItReader;

/// <summary>
/// The v1 entry header structure.
/// </summary>
public struct StuffItArchiveEntryHeaderV1
{
    /// <summary>
    /// The size of the v1 entry header structure.
    /// </summary>
    public const int Size = 112;

    /// <summary>
    /// The resource fork compression method.
    /// </summary>
    public StuffItArchiveCompressionMethod ResourceForkCompressionMethod { get; }

    /// <summary>
    /// The data fork compression method.
    /// </summary>
    public StuffItArchiveCompressionMethod DataForkCompressionMethod { get; }

    /// <summary>
    /// The length of the file name.
    /// </summary>
    public byte FileNameLength { get; }

    /// <summary>
    /// The file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The file type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The file creator.
    /// </summary>
    public string Creator { get; }

    /// <summary>
    /// The Finder flags.
    /// </summary>
    public ushort FinderFlags { get; }

    /// <summary>
    /// The creation date.
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// The modification date.
    /// </summary>
    public DateTime ModificationDate { get; }

    /// <summary>
    /// The uncompressed length of the resource fork.
    /// </summary>
    public uint ResourceForkUncompressedLength { get; }

    /// <summary>
    /// The uncompressed length of the data fork.
    /// </summary>
    public uint DataForkUncompressedLength { get; }

    /// <summary>
    /// The compressed length of the resource fork.
    /// </summary>
    public uint ResourceForkCompressedLength { get; }

    /// <summary>
    /// The compressed length of the data fork.
    /// </summary>
    public uint DataForkCompressedLength { get; }

    /// <summary>
    /// The CRC-16 of the resource fork.
    /// </summary>
    public ushort ResourceForkCRC { get; }

    /// <summary>
    /// The CRC-16 of the data fork.
    /// </summary>
    public ushort DataForkCRC { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public uint Reserved1 { get; }

    /// <summary>
    /// Reserved field (unknown purpose).
    /// </summary>
    public ushort Reserved2 { get; }

    /// <summary>
    /// The header CRC-16.
    /// </summary>
    public ushort HeaderCRC { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StuffItArchiveEntryHeaderV1"/> class.
    /// </summary>
    /// <param name="data">The raw data for the header.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or too short.</exception>
    public StuffItArchiveEntryHeaderV1(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data length must be {Size} bytes.", nameof(data));
        }

        int offset = 0;

        // Resource fork compression method
        ResourceForkCompressionMethod = (StuffItArchiveCompressionMethod)data[offset];
        offset += 1;

        // Data fork compression method
        DataForkCompressionMethod = (StuffItArchiveCompressionMethod)data[offset];
        offset += 1;

        // File name length (in range 1-31)
        FileNameLength = data[offset];
        offset += 1;

        // File name (remaning bytes are zero)
        FileName = Encoding.ASCII.GetString(data.Slice(offset, FileNameLength));
        offset += 63;

        // Mac OS file type
        Type = Encoding.ASCII.GetString(data.Slice(offset, 4));
        offset += 4;

        // Mac OS file creator
        Creator = Encoding.ASCII.GetString(data.Slice(offset, 4));
        offset += 4;

        // Mac OS Finder flags
        FinderFlags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 4));
        offset += 2;

        // Creation date (Mac OS format)
        CreationDate = SpanUtilities.ReadMacOSDate(data, offset);
        offset += 4;

        // Modification date (Mac OS format)
        ModificationDate = SpanUtilities.ReadMacOSDate(data, offset);
        offset += 4;

        // Resource fork uncompressed length
        ResourceForkUncompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Data fork uncompressed length
        DataForkUncompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Resource fork compressed length
        ResourceForkCompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Data fork compressed length
        DataForkCompressedLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Resource fork CRC-16
        ResourceForkCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Data fork CRC-16
        DataForkCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;
        
        // Unknown
        Reserved1 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Unknown
        Reserved2 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // Header CRC-16
        HeaderCRC = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
