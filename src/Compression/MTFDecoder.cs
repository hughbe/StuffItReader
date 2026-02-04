using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Move-To-Front decoder state.
/// Optimized to reduce array element shifting.
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Decode(int position)
    {
        byte value = _table[position];
        
        // Move to front - use Buffer.BlockCopy for larger moves
        if (position > 8)
        {
            // For larger positions, memmove is more efficient
            Buffer.BlockCopy(_table, 0, _table, 1, position);
        }
        else
        {
            // For small positions, manual shift is faster
            for (int i = position; i > 0; i--)
            {
                _table[i] = _table[i - 1];
            }
        }
        _table[0] = value;

        return value;
    }
}
