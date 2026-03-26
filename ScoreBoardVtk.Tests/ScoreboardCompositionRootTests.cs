using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardCompositionRootTests
{
    [Theory]
    [InlineData(ApplicationProfile.Hardware, typeof(SerialTransport))]
    [InlineData(ApplicationProfile.Mock, typeof(MockSerialTransport))]
    [InlineData(ApplicationProfile.Development, typeof(CompositeSerialTransport))]
    public void CreateTransport_UsesExpectedTransport(ApplicationProfile profile, Type expectedType)
    {
        using var transport = ScoreboardCompositionRoot.CreateTransport(profile);

        Assert.IsType(expectedType, transport);
    }

    [Fact]
    public void CreateDesktopServices_CreatesDefaultHostSettingsFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var hostSettingsPath = Path.Combine(directory, "hostsettings.json");
            using var services = ScoreboardCompositionRoot.CreateDesktopServices(hostSettingsPath);

            Assert.True(File.Exists(hostSettingsPath));
            Assert.NotNull(services.SettingsStore);
            Assert.NotNull(services.ScoreboardApi);
            Assert.NotNull(services.Runtime);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
