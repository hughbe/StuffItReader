using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Reads bits from a stream MSB-first for arithmetic decoding.
/// Optimized with buffered reading and 32-bit buffer.
/// </summary>
internal sealed class BitReader
{
    private const int BufferSize = 8192;
    
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private int _bufferPos;
    private int _bufferLength;
    private uint _bitBuffer;
    private int _bitsInBuffer;

    public BitReader(Stream stream)
    {
        _stream = stream;
        _buffer = new byte[BufferSize];
        _bufferPos = 0;
        _bufferLength = 0;
        _bitBuffer = 0;
        _bitsInBuffer = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadByteFromBuffer()
    {
        if (_bufferPos >= _bufferLength)
        {
            _bufferLength = _stream.Read(_buffer, 0, BufferSize);
            _bufferPos = 0;
            if (_bufferLength == 0)
            {
                return -1;
            }
        }
        return _buffer[_bufferPos++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBit()
    {
        if (_bitsInBuffer == 0)
        {
            int b = ReadByteFromBuffer();
            if (b == -1)
            {
                return 0; // Return 0 on EOF
            }
            _bitBuffer = (uint)b;
            _bitsInBuffer = 8;
        }

        _bitsInBuffer--;
        return (int)((_bitBuffer >> _bitsInBuffer) & 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadBits(int count)
    {
        long result = 0;
        
        // Fast path: read full bytes directly when bit buffer is empty
        while (count >= 8 && _bitsInBuffer == 0)
        {
            int b = ReadByteFromBuffer();
            if (b == -1) b = 0;
            result = (result << 8) | (uint)b;
            count -= 8;
        }
        
        // Read remaining bits
        for (int i = 0; i < count; i++)
        {
            result = (result << 1) | (uint)ReadBit();
        }
        return result;
    }
}
