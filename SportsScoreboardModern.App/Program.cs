using System.Text;
using SportsScoreboardModern.Core.Services;

namespace SportsScoreboardModern.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();

        using var transport = new SerialTransport();
        using var form = new MainForm(transport);
        Application.Run(form);
    }
}
