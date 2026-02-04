using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Prefix (Huffman) code implementation for LZSS decompression.
/// Optimized with sorted tables for binary search and early termination.
/// </summary>
internal sealed class PrefixCode
{
    // Sorted by length then by code for fast searching
    private readonly int[] _sortedCodes;
    private readonly int[] _sortedLengths;
    private readonly int[] _sortedValues;
    private readonly int _numCodes;
    private readonly int _maxLength;
    
    // Index of first code of each length for O(1) length lookup
    private readonly int[] _lengthStartIndex;

    private PrefixCode(int[] codes, int[] lengths, int[] values, int numCodes, int maxLength)
    {
        _numCodes = numCodes;
        _maxLength = maxLength;
        
        // Create sorted indices by (length, code)
        var indices = new int[numCodes];
        for (int i = 0; i < numCodes; i++) indices[i] = i;
        
        Array.Sort(indices, (a, b) =>
        {
            int cmp = lengths[a].CompareTo(lengths[b]);
            return cmp != 0 ? cmp : codes[a].CompareTo(codes[b]);
        });
        
        // Build sorted arrays
        _sortedCodes = new int[numCodes];
        _sortedLengths = new int[numCodes];
        _sortedValues = new int[numCodes];
        
        for (int i = 0; i < numCodes; i++)
        {
            _sortedCodes[i] = codes[indices[i]];
            _sortedLengths[i] = lengths[indices[i]];
            _sortedValues[i] = values[indices[i]];
        }
        
        // Build length start index
        _lengthStartIndex = new int[maxLength + 2];
        int currentLength = 0;
        for (int i = 0; i < numCodes; i++)
        {
            while (currentLength < _sortedLengths[i])
            {
                _lengthStartIndex[++currentLength] = i;
            }
        }
        while (currentLength <= maxLength)
        {
            _lengthStartIndex[++currentLength] = numCodes;
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DecodeSymbol(HuffmanBitReader reader)
    {
        int code = 0;
        for (int len = 1; len <= _maxLength; len++)
        {
            code = (code << 1) | reader.ReadBit();
            
            // Binary search for matching code at this length
            int start = _lengthStartIndex[len];
            int end = _lengthStartIndex[len + 1];
            
            // Binary search within codes of this length
            while (start < end)
            {
                int mid = (start + end) >> 1;
                int midCode = _sortedCodes[mid];
                
                if (midCode == code)
                {
                    return _sortedValues[mid];
                }
                else if (midCode < code)
                {
                    start = mid + 1;
                }
                else
                {
                    end = mid;
                }
            }
        }

        throw new InvalidDataException("Invalid Huffman code");
    }
}
