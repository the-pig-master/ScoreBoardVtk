using System.Globalization;
using System.Text;
using SportsScoreboardModern.Core.Models;

namespace SportsScoreboardModern.Core.Services;

public static class SerialProtocol
{
    private static readonly Encoding Win1251 = Encoding.GetEncoding(1251);

    public static byte[] CreateGamePacket(ScoreboardSnapshot snapshot)
    {
        const int bodyLength = 49;
        const int totalLength = 51;

        var result = Enumerable.Repeat((byte)' ', totalLength).ToArray();
        var command = "AT+GD" + snapshot.PayloadText;
        var encoded = Win1251.GetBytes(command);
        var bytesToCopy = Math.Min(bodyLength, encoded.Length);

        Array.Copy(encoded, result, bytesToCopy);

        for (var index = 0; index < bodyLength; index++)
        {
            result[index] = LegacyCharMap.Translate(result[index]);
        }

        var crc = Crc16.Compute(result.AsSpan(0, bodyLength));
        result[49] = (byte)((crc & 0xFF00) >> 8);
        result[50] = (byte)(crc & 0x00FF);

        return result;
    }

    public static byte[] CreateTimeSyncPacket(DateTime currentTime)
    {
        const int payloadLength = 13;
        const int totalLength = 15;

        var result = Enumerable.Repeat((byte)' ', totalLength).ToArray();
        var command = "AT+ST" + currentTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var encoded = Win1251.GetBytes(command);

        Array.Copy(encoded, result, Math.Min(payloadLength, encoded.Length));

        var crc = Crc16.Compute(result.AsSpan(0, payloadLength));
        result[13] = (byte)((crc & 0xFF00) >> 8);
        result[14] = (byte)(crc & 0x00FF);

        return result;
    }
}
