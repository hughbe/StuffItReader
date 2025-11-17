namespace StuffItReader.Compression;

/// <summary>
/// Move-To-Front decoder state.
/// </summary>
internal sealed class MTFDecoder
{
    private readonly byte[] _table;

    public MTFDecoder()
    {
        _table = new byte[256];
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < 256; i++)
        {
            _table[i] = (byte)i;
        }
    }

    public byte Decode(int position)
    {
        byte value = _table[position];
        
        // Move to front
        for (int i = position; i > 0; i--)
        {
            _table[i] = _table[i - 1];
        }
        _table[0] = value;

        return value;
    }
}
