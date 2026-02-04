using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Adaptive arithmetic model for symbol encoding/decoding.
/// Optimized with fast paths for common cases.
/// </summary>
internal sealed class ArithmeticModel
{
    private readonly int _increment;
    private readonly int _frequencyLimit;
    private readonly int _numSymbols;
    private readonly int _firstSymbol;
    private readonly int[] _frequencies;
    private int _totalFrequency;

    public int TotalFrequency
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _totalFrequency;
    }

    public ArithmeticModel(int firstSymbol, int lastSymbol, int increment, int frequencyLimit)
    {
        _increment = increment;
        _frequencyLimit = frequencyLimit;
        _numSymbols = lastSymbol - firstSymbol + 1;
        _firstSymbol = firstSymbol;
        _frequencies = new int[_numSymbols];

        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _totalFrequency = _increment * _numSymbols;
        var frequencies = _frequencies;
        int increment = _increment;
        for (int i = 0; i < frequencies.Length; i++)
        {
            frequencies[i] = increment;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DecodeSymbol(int frequency, out int symLow, out int symSize)
    {
        var frequencies = _frequencies;
        
        // Fast path: check first symbol (very common in adaptive models)
        int firstFreq = frequencies[0];
        if (frequency < firstFreq)
        {
            symLow = 0;
            symSize = firstFreq;
            IncreaseFrequency(0);
            return _firstSymbol;
        }
        
        // Linear search for remaining symbols
        int cumulative = firstFreq;
        int n = 1;
        int numSymbolsMinusOne = _numSymbols - 1;
        
        while (n < numSymbolsMinusOne)
        {
            int freq = frequencies[n];
            int nextCumulative = cumulative + freq;
            if (nextCumulative > frequency)
            {
                symLow = cumulative;
                symSize = freq;
                IncreaseFrequency(n);
                return n + _firstSymbol;
            }
            cumulative = nextCumulative;
            n++;
        }

        // Last symbol
        symLow = cumulative;
        symSize = frequencies[n];
        IncreaseFrequency(n);
        return n + _firstSymbol;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncreaseFrequency(int symIndex)
    {
        _frequencies[symIndex] += _increment;
        _totalFrequency += _increment;

        if (_totalFrequency > _frequencyLimit)
        {
            RescaleFrequencies();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RescaleFrequencies()
    {
        _totalFrequency = 0;
        var frequencies = _frequencies;
        for (int i = 0; i < frequencies.Length; i++)
        {
            frequencies[i]++;
            frequencies[i] >>= 1;
            _totalFrequency += frequencies[i];
        }
    }
}
