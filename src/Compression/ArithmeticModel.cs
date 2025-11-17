namespace StuffItReader.Compression;

internal struct ArithmeticSymbol
{
    public int Symbol;
    public int Frequency;
}

internal sealed class ArithmeticModel
{
    private readonly int _increment;
    private readonly int _frequencyLimit;
    private readonly int _numSymbols;
    private readonly ArithmeticSymbol[] _symbols;
    private int _totalFrequency;

    public int TotalFrequency => _totalFrequency;

    public ArithmeticModel(int firstSymbol, int lastSymbol, int increment, int frequencyLimit)
    {
        _increment = increment;
        _frequencyLimit = frequencyLimit;
        _numSymbols = lastSymbol - firstSymbol + 1;
        _symbols = new ArithmeticSymbol[_numSymbols];
        
        for (int i = 0; i < _numSymbols; i++)
        {
            _symbols[i].Symbol = i + firstSymbol;
        }

        Reset();
    }

    public void Reset()
    {
        _totalFrequency = _increment * _numSymbols;
        for (int i = 0; i < _numSymbols; i++)
        {
            _symbols[i].Frequency = _increment;
        }
    }

    public int DecodeSymbol(int frequency, out int symLow, out int symSize)
    {
        int cumulative = 0;
        int n;
        
        for (n = 0; n < _numSymbols - 1; n++)
        {
            if (cumulative + _symbols[n].Frequency > frequency)
            {
                break;
            }
            cumulative += _symbols[n].Frequency;
        }

        symLow = cumulative;
        symSize = _symbols[n].Frequency;

        IncreaseFrequency(n);

        return _symbols[n].Symbol;
    }

    private void IncreaseFrequency(int symIndex)
    {
        _symbols[symIndex].Frequency += _increment;
        _totalFrequency += _increment;

        if (_totalFrequency > _frequencyLimit)
        {
            _totalFrequency = 0;
            for (int i = 0; i < _numSymbols; i++)
            {
                _symbols[i].Frequency++;
                _symbols[i].Frequency >>= 1;
                _totalFrequency += _symbols[i].Frequency;
            }
        }
    }
}
