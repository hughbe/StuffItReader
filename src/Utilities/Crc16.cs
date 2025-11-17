using System.Buffers.Binary;
using System.IO.Hashing;

namespace StuffItReader.Utilities;

internal class Crc16 : NonCryptographicHashAlgorithm
{
    private readonly ushort _polynomial;
    private readonly ushort _initialRemainder;
    private readonly ushort _finalXor;
    private ushort _currentCrc;

    private readonly ushort[] _table = new ushort[256];

    public Crc16(ushort polynomial, ushort initialRemainder, ushort finalXor) : base(2)
    {
        _polynomial = polynomial;
        _initialRemainder = initialRemainder;
        _finalXor = finalXor;
        _currentCrc = initialRemainder;

        // Compute the table.
        for (var dividend = 0; dividend < 256; dividend++)
        {
            var remainder = (ushort)dividend;
            for (var bit = 8; bit > 0; bit--)
            {
                if ((remainder & 0x0001) != 0)
                {
                    remainder = (ushort)((remainder >> 1) ^ _polynomial);
                }
                else
                {
                    remainder >>= 1;
                }
            }

            _table[dividend] = remainder;
        }
    }

    public override void Append(ReadOnlySpan<byte> source)
    {
        foreach (byte b in source)
        {
            var index = (b ^ _currentCrc) & 0xFF;
            _currentCrc = (ushort)(_table[index] ^ (_currentCrc >> 8));
        }
    }

    public override void Reset()
    {
        _currentCrc = _initialRemainder;
    }

    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        _currentCrc ^= _finalXor;
        BinaryPrimitives.WriteUInt16LittleEndian(destination, _currentCrc);
    }
}
