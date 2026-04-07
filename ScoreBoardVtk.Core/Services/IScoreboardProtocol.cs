using System.Text;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public interface IScoreboardProtocol
{
    string CreateGamePayload(ScoreboardState state, DateTime currentTime);

    byte[] CreateGamePacket(ScoreboardState state, DateTime currentTime);

    byte[] CreateGamePacket(string payload);

    byte[] CreateGamePacket(string payload, Encoding encoding);
}
