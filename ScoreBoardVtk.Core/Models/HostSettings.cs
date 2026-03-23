namespace ScoreBoardVtk.Core.Models;

public sealed class HostSettings
{
    public ApplicationProfile Profile { get; set; } = ApplicationProfile.Development;

    public string GameSettingsFileName { get; set; } = "settings.json";

    public HostRuntimeSettings Runtime { get; set; } = new();
}
