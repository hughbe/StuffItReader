namespace StuffItReader.Compression;

/// <summary>
/// 26-bit precision arithmetic decoder for Arsenic compression.
/// </summary>
internal sealed class ArithmeticDecoder
{
    private const int NumBits = 26;
    private const int One = 1 << (NumBits - 1);
    private const int Half = 1 << (NumBits - 2);

    private readonly BitReader _input;
    private int _range;
    private int _code;
    
    public int Range => _range;
    public int Code => _code;

    public ArithmeticDecoder(BitReader input)
    {
        _input = input;
        _range = One;
        _code = (int)input.ReadBits(NumBits);
    }

    public int DecodeFrequency(int symTot)
    {
        int freq = _code / (_range / symTot);
        // Clamp to prevent overflow
        if (freq >= symTot) freq = symTot - 1;
        return freq;
    }

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

        while (_range <= Half)
        {
            _range <<= 1;
            _code = (_code << 1) | _input.ReadBit();
            _code &= ((1 << NumBits) - 1);  // Mask to NumBits
        }
    }
}
