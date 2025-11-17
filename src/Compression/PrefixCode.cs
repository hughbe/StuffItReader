namespace StuffItReader.Compression;

/// <summary>
/// Prefix (Huffman) code implementation for LZSS decompression.
/// </summary>
internal sealed class PrefixCode
{
    private readonly int[] _codes;
    private readonly int[] _lengths;
    private readonly int[] _values;
    private readonly int _numCodes;
    private readonly int _maxLength;

    private PrefixCode(int[] codes, int[] lengths, int[] values, int numCodes, int maxLength)
    {
        _codes = codes;
        _lengths = lengths;
        _values = values;
        _numCodes = numCodes;
        _maxLength = maxLength;
    }

    public static PrefixCode FromLengths(int[] lengths, int numSymbols, int maxLength)
    {
        // Count frequency of each code length
        int[] lengthCounts = new int[maxLength + 1];
        for (int i = 0; i < numSymbols; i++)
        {
            if (lengths[i] > 0 && lengths[i] <= maxLength)
            {
                lengthCounts[lengths[i]]++;
            }
        }

        // Calculate first code for each length
        int[] firstCodes = new int[maxLength + 1];
        int code = 0;
        for (int len = 1; len <= maxLength; len++)
        {
            firstCodes[len] = code;
            code = (code + lengthCounts[len]) << 1;
        }

        // Build code tables
        var codes = new List<int>();
        var codeLengths = new List<int>();
        var values = new List<int>();

        int[] nextCode = new int[maxLength + 1];
        Array.Copy(firstCodes, nextCode, maxLength + 1);

        for (int symbol = 0; symbol < numSymbols; symbol++)
        {
            int len = lengths[symbol];
            if (len > 0 && len <= maxLength)
            {
                codes.Add(nextCode[len]);
                codeLengths.Add(len);
                values.Add(symbol);
                nextCode[len]++;
            }
        }

        return new PrefixCode(codes.ToArray(), codeLengths.ToArray(), values.ToArray(), codes.Count, maxLength);
    }

    public static PrefixCode FromExplicitCodes(int[] codes, int[] lengths, int[] values)
    {
        int maxLen = 0;
        for (int i = 0; i < lengths.Length; i++)
        {
            if (lengths[i] > maxLen) maxLen = lengths[i];
        }
        
        // Reverse the bits in each code since the Objective-C uses LowBitFirst
        int[] reversedCodes = new int[codes.Length];
        for (int i = 0; i < codes.Length; i++)
        {
            reversedCodes[i] = ReverseBits(codes[i], lengths[i]);
        }
        
        return new PrefixCode(reversedCodes, lengths, values, codes.Length, maxLen);
    }

    private static int ReverseBits(int value, int bitCount)
    {
        int result = 0;
        for (int i = 0; i < bitCount; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }

    public int DecodeSymbol(HuffmanBitReader reader)
    {
        int code = 0;
        for (int len = 1; len <= _maxLength; len++)
        {
            code = (code << 1) | reader.ReadBit();
            
            // Search for matching code at this length
            for (int i = 0; i < _numCodes; i++)
            {
                if (_lengths[i] == len && _codes[i] == code)
                {
                    return _values[i];
                }
            }
        }

        throw new InvalidDataException("Invalid Huffman code");
    }
}
