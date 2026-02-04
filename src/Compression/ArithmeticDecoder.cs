using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// 26-bit precision arithmetic decoder for Arsenic compression.
/// Optimized with combined decode and remove operation.
/// </summary>
internal sealed class ArithmeticDecoder
{
    private const int NumBits = 26;
    private const int One = 1 << (NumBits - 1);
    private const int Half = 1 << (NumBits - 2);
    private const int CodeMask = (1 << NumBits) - 1;

    private readonly BitReader _input;
    private int _range;
    private int _code;

    public ArithmeticDecoder(BitReader input)
    {
        _input = input;
        _range = One;
        _code = (int)input.ReadBits(NumBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DecodeFrequency(int symTot)
    {
        int freq = _code / (_range / symTot);
        // Clamp to prevent overflow
        if (freq >= symTot) freq = symTot - 1;
        return freq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSymbol(int symLow, int symSize, int symTot)
    {
        int renormFactor = _range / symTot;
        int lowIncr = renormFactor * symLow;

        _code -= lowIncr;
        if (symLow + symSize == symTot)
        {
            _range -= lowIncr;
        }
        else
        {
            _range = symSize * renormFactor;
        }

        // Renormalize - unroll first iteration for common case
        if (_range <= Half)
        {
            _range <<= 1;
            _code = ((_code << 1) | _input.ReadBit()) & CodeMask;
            
            while (_range <= Half)
            {
                _range <<= 1;
                _code = ((_code << 1) | _input.ReadBit()) & CodeMask;
            }
        }
    }
}
