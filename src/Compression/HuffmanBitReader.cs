namespace StuffItReader.Compression;

/// <summary>
/// Reads bits from a stream LSB-first for Huffman decoding.
/// </summary>
internal sealed class HuffmanBitReader
{
    private readonly Stream _stream;
    private byte _buffer;
    private int _bitsInBuffer;

    public HuffmanBitReader(Stream stream)
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
                throw new EndOfStreamException("Unexpected end of stream while reading bits");
            }
            _buffer = (byte)b;
            _bitsInBuffer = 8;
        }

        int bit = _buffer & 1;
        _buffer >>= 1;
        _bitsInBuffer--;
        return bit;
    }

    public int ReadBits(int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            result |= ReadBit() << i;
        }
        return result;
    }
}
