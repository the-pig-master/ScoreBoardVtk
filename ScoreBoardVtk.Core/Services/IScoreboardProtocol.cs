using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public interface IScoreboardProtocol
{
    string CreateGamePayload(ScoreboardState state, DateTime currentTime);

    byte[] CreateGamePacket(ScoreboardState state, DateTime currentTime);
}
