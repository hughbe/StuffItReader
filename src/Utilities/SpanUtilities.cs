namespace StuffItReader.Utilities;

/// <summary>
/// Provides small helpers for reading values from spans not covered by BinaryPrimitives.
/// </summary>
internal static class SpanUtilities
{
    public static string ReadString(ReadOnlySpan<byte> data, int offset, int length)
    {
        return System.Text.Encoding.ASCII.GetString(data.Slice(offset, length));
    }

    public static DateTime ReadMacOSDate(ReadOnlySpan<byte> data, int offset)
    {
        // 4 bytes timestamp
        // Big-endian seconds since 00:00:00 on January 1, 1904 (Mac OS epoch)
        uint macOSTimestamp = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

        var macOSEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return macOSEpoch.AddSeconds(macOSTimestamp);
    }
}