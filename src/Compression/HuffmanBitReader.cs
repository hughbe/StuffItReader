using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Reads bits from a stream LSB-first for Huffman decoding.
/// Uses a 32-bit buffer to reduce stream read calls.
/// </summary>
internal sealed class HuffmanBitReader
{
    private readonly Stream _stream;
    private uint _buffer;
    private int _bitsInBuffer;

    public HuffmanBitReader(Stream stream)
    {
        _stream = stream;
        _buffer = 0;
        _bitsInBuffer = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBit()
    {
        if (_bitsInBuffer == 0)
        {
            // Read up to 4 bytes at once
            int b0 = _stream.ReadByte();
            if (b0 == -1)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading bits");
            }
            
            int b1 = _stream.ReadByte();
            int b2 = _stream.ReadByte();
            int b3 = _stream.ReadByte();
            
            // Build buffer LSB-first: first byte is LSB
            _buffer = (uint)b0;
            _bitsInBuffer = 8;
            
            if (b1 != -1)
            {
                _buffer |= (uint)b1 << 8;
                _bitsInBuffer = 16;
                
                if (b2 != -1)
                {
                    _buffer |= (uint)b2 << 16;
                    _bitsInBuffer = 24;
                    
                    if (b3 != -1)
                    {
                        _buffer |= (uint)b3 << 24;
                        _bitsInBuffer = 32;
                    }
                }
            }
        }

        int bit = (int)(_buffer & 1);
        _buffer >>= 1;
        _bitsInBuffer--;
        return bit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
