using System.Runtime.CompilerServices;

namespace StuffItReader.Compression;

/// <summary>
/// Move-To-Front decoder state.
/// Optimized with unrolled loop for small positions and Span for larger.
/// </summary>
internal sealed class MTFDecoder
{
    private readonly byte[] _table;

    public MTFDecoder()
    {
        _table = new byte[256];
        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        
        if (position == 0)
        {
            return value;
        }
        
        // For small positions, manual unrolled shift is faster
        if (position <= 3)
        {
            if (position >= 3) _table[3] = _table[2];
            if (position >= 2) _table[2] = _table[1];
            _table[1] = _table[0];
        }
        else
        {
            // Use Span.CopyTo for overlapping copy (handles memmove correctly)
            _table.AsSpan(0, position).CopyTo(_table.AsSpan(1));
        }
        
        _table[0] = value;
        return value;
    }
}
