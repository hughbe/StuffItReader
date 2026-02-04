using System.Buffers;
using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// LZW decompressor for StuffIt compression method 2.
/// Optimized to minimize allocations using pooled buffers.
/// </summary>
internal sealed class LzwDecompressor
{
    private const int MaxTableSize = 4096;
    private const int InitialSequenceBufferSize = 256;
    
    private readonly Stream _input;
    
    // Instead of Dictionary<int, byte[]>, use parallel arrays to avoid boxing and array allocations
    // _sequenceData holds all sequence bytes concatenated
    // _sequenceOffsets[i] = start offset in _sequenceData for entry i
    // _sequenceLengths[i] = length of sequence for entry i
    private byte[] _sequenceData;
    private int[] _sequenceOffsets;
    private int[] _sequenceLengths;
    private int _sequenceDataPosition;
    private int _tableSize;
    private int _codeBits;

    public LzwDecompressor(Stream input)
    {
        _input = input;
        
        // Pre-allocate with reasonable initial sizes
        _sequenceData = ArrayPool<byte>.Shared.Rent(65536);
        _sequenceOffsets = ArrayPool<int>.Shared.Rent(MaxTableSize);
        _sequenceLengths = ArrayPool<int>.Shared.Rent(MaxTableSize);
        
        Initialize();
    }

    private void Initialize()
    {
        _tableSize = 256;
        _codeBits = 9;
        _sequenceDataPosition = 256; // First 256 bytes are single-byte entries
        
        // Initialize dictionary with single-byte entries
        // Single bytes are stored implicitly: entry i has value (byte)i
        for (int i = 0; i < 256; i++)
        {
            _sequenceOffsets[i] = i;
            _sequenceLengths[i] = 1;
            _sequenceData[i] = (byte)i;
        }
    }

    public void Decompress(Stream output, long decompressedLength)
    {
        var bitReader = new LzwBitReader(_input);
        
        // Use a pooled output buffer for batched writes
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(8192);
        int outputBufferPos = 0;
        
        try
        {
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

                // Get the sequence for this code
                int seqOffset = _sequenceOffsets[code];
                int seqLength = _sequenceLengths[code];

                // Write the sequence to output buffer
                int bytesToWrite = (int)Math.Min(seqLength, decompressedLength - outputCount);
                
                for (int i = 0; i < bytesToWrite; i++)
                {
                    outputBuffer[outputBufferPos++] = _sequenceData[seqOffset + i];
                    outputCount++;
                    
                    if (outputBufferPos >= outputBuffer.Length)
                    {
                        output.Write(outputBuffer, 0, outputBufferPos);
                        outputBufferPos = 0;
                    }
                }

                // Add entry to dictionary if we have room
                if (previousCode != -1 && _tableSize < MaxTableSize)
                {
                    int prevOffset = _sequenceOffsets[previousCode];
                    int prevLength = _sequenceLengths[previousCode];
                    int newLength = prevLength + 1;
                    
                    // Ensure we have space in sequenceData
                    EnsureSequenceDataCapacity(_sequenceDataPosition + newLength);
                    
                    // Copy previous sequence and append first byte of current
                    Buffer.BlockCopy(_sequenceData, prevOffset, _sequenceData, _sequenceDataPosition, prevLength);
                    _sequenceData[_sequenceDataPosition + prevLength] = _sequenceData[seqOffset];
                    
                    _sequenceOffsets[_tableSize] = _sequenceDataPosition;
                    _sequenceLengths[_tableSize] = newLength;
                    _sequenceDataPosition += newLength;
                    _tableSize++;

                    // Increase code bits when needed
                    if (_tableSize == (1 << _codeBits) && _codeBits < 12)
                    {
                        _codeBits++;
                    }
                }

                previousCode = code;
            }

            // Flush remaining output
            if (outputBufferPos > 0)
            {
                output.Write(outputBuffer, 0, outputBufferPos);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outputBuffer);
            ArrayPool<byte>.Shared.Return(_sequenceData);
            ArrayPool<int>.Shared.Return(_sequenceOffsets);
            ArrayPool<int>.Shared.Return(_sequenceLengths);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureSequenceDataCapacity(int required)
    {
        if (required <= _sequenceData.Length)
            return;

        int newSize = Math.Max(_sequenceData.Length * 2, required);
        byte[] newData = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(_sequenceData, 0, newData, 0, _sequenceDataPosition);
        ArrayPool<byte>.Shared.Return(_sequenceData);
        _sequenceData = newData;
    }
}

/// <summary>
/// Bit reader for LZW decompression.
/// Optimized with inline methods and efficient bit extraction.
/// </summary>
internal sealed class LzwBitReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private int _bufferSize;
    private int _bytePos;
    private uint _bitBuffer;
    private int _bitsInBuffer;

    public LzwBitReader(Stream stream)
    {
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(8192);
        _bufferSize = 0;
        _bytePos = 0;
        _bitBuffer = 0;
        _bitsInBuffer = 0;
        FillBuffer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBits(int numBits)
    {
        // Ensure we have enough bits
        while (_bitsInBuffer < numBits)
        {
            if (_bytePos >= _bufferSize)
            {
                FillBuffer();
                if (_bytePos >= _bufferSize)
                {
                    break;
                }
            }

            // Read MSB first (big-endian bit order)
            _bitBuffer = (_bitBuffer << 8) | _buffer[_bytePos++];
            _bitsInBuffer += 8;
        }

        // Extract bits from the top
        _bitsInBuffer -= numBits;
        int result = (int)((_bitBuffer >> _bitsInBuffer) & ((1u << numBits) - 1));
        return result;
    }

    private void FillBuffer()
    {
        _bufferSize = _stream.Read(_buffer, 0, _buffer.Length);
        _bytePos = 0;
    }
}
