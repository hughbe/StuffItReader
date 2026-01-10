namespace StuffItReader.Compression;

/// <summary>
/// LZW decompressor for StuffIt compression method 2.
/// </summary>
internal sealed class LzwDecompressor
{
    private const int MaxTableSize = 4096;
    
    private readonly Stream _input;
    private Dictionary<int, byte[]> _dictionary;
    private int _tableSize;
    private int _codeBits;

    public LzwDecompressor(Stream input)
    {
        _input = input;
        _dictionary = new Dictionary<int, byte[]>(MaxTableSize);
        _tableSize = 256;
        _codeBits = 9;
        
        // Initialize dictionary with single-byte entries
        for (int i = 0; i < 256; i++)
        {
            _dictionary[i] = new byte[] { (byte)i };
        }
    }

    public void Decompress(Stream output, long decompressedLength)
    {
        var bitReader = new LzwBitReader(_input);
        
        long outputCount = 0;
        int previousCode = -1;

        while (outputCount < decompressedLength)
        {
            int code = bitReader.ReadBits(_codeBits);

            if (code == MaxTableSize)
            {
                // End of data marker
                break;
            }

            if (code >= _tableSize)
            {
                // Invalid code
                break;
            }

            byte[] sequence = _dictionary[code];

            // Write the sequence to output
            for (int i = 0; i < sequence.Length && outputCount < decompressedLength; i++)
            {
                output.WriteByte(sequence[i]);
                outputCount++;
            }

            // Add entry to dictionary if we have room
            if (previousCode != -1 && _tableSize < MaxTableSize)
            {
                byte[] prevSeq = _dictionary[previousCode];
                byte[] newSeq = new byte[prevSeq.Length + 1];
                Array.Copy(prevSeq, newSeq, prevSeq.Length);
                newSeq[prevSeq.Length] = sequence[0];
                
                _dictionary[_tableSize] = newSeq;
                _tableSize++;

                // Increase code bits when needed
                if (_tableSize == (1 << _codeBits) && _codeBits < 12)
                {
                    _codeBits++;
                }
            }

            previousCode = code;
        }
    }
}

/// <summary>
/// Bit reader for LZW decompression.
/// </summary>
internal sealed class LzwBitReader
{
    private readonly Stream _stream;
    private byte[] _buffer = new byte[4096];
    private int _bufferSize = 0;
    private int _bitPos = 0;
    private int _bytePos = 0;

    public LzwBitReader(Stream stream)
    {
        _stream = stream;
        FillBuffer();
    }

    public int ReadBits(int numBits)
    {
        int result = 0;
        int bitsRead = 0;

        while (bitsRead < numBits)
        {
            if (_bytePos >= _bufferSize)
            {
                FillBuffer();
                if (_bytePos >= _bufferSize)
                {
                    break;
                }
            }

            byte currentByte = _buffer[_bytePos];
            int bitsAvailable = 8 - _bitPos;
            int bitsToRead = Math.Min(numBits - bitsRead, bitsAvailable);

            int mask = (1 << bitsToRead) - 1;
            int bits = (currentByte >> (8 - _bitPos - bitsToRead)) & mask;

            result = (result << bitsToRead) | bits;
            bitsRead += bitsToRead;
            _bitPos += bitsToRead;

            if (_bitPos >= 8)
            {
                _bitPos = 0;
                _bytePos++;
            }
        }

        return result;
    }

    private void FillBuffer()
    {
        _bufferSize = _stream.Read(_buffer, 0, _buffer.Length);
        _bytePos = 0;
        _bitPos = 0;
    }
}
