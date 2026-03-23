namespace ScoreBoardVtk.Core.Services;

internal static class Crc16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort temp = 0xFFFF;

        foreach (var value in data)
        {
            temp ^= value;

            for (var bit = 0; bit < 8; bit++)
            {
                var lsb = (temp & 0x0001) != 0;
                temp >>= 1;

                if (lsb)
                {
                    temp ^= 0xA001;
                }
            }
        }

        return temp;
    }
}
