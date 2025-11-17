namespace StuffItReader.Compression;

/// <summary>
/// Reads bits from a stream LSB-first for arithmetic decoding.
/// </summary>
internal sealed class BitReader
{
    private readonly Stream _stream;
    private byte _buffer;
    private int _bitsInBuffer;

    public BitReader(Stream stream)
    {
        _stream = stream;
        _buffer = 0;
        _bitsInBuffer = 0;
    }

    public int ReadBit()
    {
        if (_bitsInBuffer == 0)
        {
            int b = _stream.ReadByte();
            if (b == -1)
            {
                return 0; // Return 0 on EOF
            }
            _buffer = (byte)b;
            _bitsInBuffer = 8;
        }

        int bit = (_buffer >> 7) & 1;  // Read MSB first
        _buffer <<= 1;
        _bitsInBuffer--;
        return bit;
    }

    public long ReadBits(int count)
    {
        long result = 0;
        for (int i = 0; i < count; i++)
        {
            result = (result << 1) | ReadBit();  // Shift existing bits left, add new bit at LSB
        }
        return result;
    }
}
