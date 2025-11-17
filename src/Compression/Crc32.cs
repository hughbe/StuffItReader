namespace StuffItReader.Compression;

/// <summary>
/// CRC-32 with polynomial 0xEDB88320 (reflected form of 0x04C11DB7).
/// </summary>
internal sealed class Crc32
{
    private static readonly uint[] Table = GenerateTable();
    private uint _crc;

    public Crc32()
    {
        _crc = 0xFFFFFFFF;
    }

    private static uint[] GenerateTable()
    {
        var table = new uint[256];
        const uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
            table[i] = crc;
        }

        return table;
    }

    public void Append(byte value)
    {
        _crc = Table[(_crc ^ value) & 0xFF] ^ (_crc >> 8);
    }

    public uint GetValue()
    {
        return ~_crc;
    }
}
