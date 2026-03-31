namespace ScoreBoardVtk.Core.Models;

public sealed class HostSettings
{
    public ApplicationProfile Profile { get; set; } = ApplicationProfile.Development;

    public string GameSettingsFileName { get; set; } = "settings.json";

    public bool ShowDebugTab { get; set; } = true;

    public HostRuntimeSettings Runtime { get; set; } = new();
}
