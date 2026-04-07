using System.Diagnostics;
using System.Text;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Cmd;

internal static class Program
{
    private const int BuzzerDurationMilliseconds = 300;
    private const int SignalPacketIntervalMilliseconds = 50;
    private const int ScanPauseMilliseconds = 1000;
    private const int WatchRefreshMilliseconds = 1000;

    private static int Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            var hostSettingsStore = new HostSettingsStore();
            var hostSettings = hostSettingsStore.Load();
            using var scoreboard = ScoreboardCompositionRoot.CreateConsoleApi(hostSettingsStore.FilePath);
            var session = CreateCommandSession(hostSettings);
            return Execute(args, scoreboard, session);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static int Execute(string[] args, IScoreboardApi scoreboard, CommandSession session, bool interactiveShell = false)
    {
        if (args.Length == 0)
        {
            if (!interactiveShell && CanUseInteractiveConsole())
            {
                return RunInteractive(scoreboard, session);
            }

            PrintHeader();
            PrintPortList(scoreboard);
            PrintUsage(session);
            return 0;
        }

        var command = args[0].Trim().ToLowerInvariant();

        return command switch
        {
            "list" => ExecuteList(scoreboard),
            "buzz" => ExecuteBuzz(args.Skip(1).ToArray(), scoreboard),
            "scan-all" or "scan" => ExecuteScanAll(scoreboard),
            "payload" or "send" => ExecutePayload(args.Skip(1).ToArray(), scoreboard, session, interactiveShell),
            "encodings" or "list-encodings" => ExecuteListEncodings(session),
            "encoding" or "set-encoding" => ExecuteSetEncoding(args.Skip(1).ToArray(), session),
            "watch" or "monitor" => interactiveShell ? ExecuteWatchInteractive(scoreboard) : ExecuteWatch(scoreboard),
            "help" or "--help" or "-h" or "/?" => ExecuteHelp(scoreboard, session),
            _ => ExecuteUnknownCommand(args[0], scoreboard, session),
        };
    }

    private static int RunInteractive(IScoreboardApi scoreboard, CommandSession session)
    {
        var selectedPort = GetPreferredPort(scoreboard, null);

        while (true)
        {
            selectedPort = GetPreferredPort(scoreboard, selectedPort);

            var options = new[]
            {
                $"Select COM port ({selectedPort ?? "none"})",
                $"Set payload encoding ({FormatEncodingDisplay(session.PayloadEncoding)})",
                "List COM ports",
                $"Send buzzer to selected port ({selectedPort ?? "none"})",
                $"Send custom payload to selected port ({selectedPort ?? "none"})",
                "List encodings",
                "Scan all COM ports",
                "Watch COM ports",
                "Enter command",
                "Help",
                "Exit",
            };

            var selection = ShowMenu(
                "Main Menu",
                options,
                [
                    $"Selected port: {selectedPort ?? "none"}",
                "Use Up/Down arrows and Enter.",
                $"Payload encoding: {FormatEncodingDisplay(session.PayloadEncoding)}",
            ]);

            switch (selection)
            {
                case 0:
                    selectedPort = SelectPortInteractive(scoreboard, selectedPort);
                    break;

                case 1:
                    RunInteractiveAction(() => ExecuteSetEncoding(Array.Empty<string>(), session, promptWhenMissing: true));
                    break;

                case 2:
                    RunInteractiveAction(() => ExecuteList(scoreboard));
                    break;

                case 3:
                    if (string.IsNullOrWhiteSpace(selectedPort))
                    {
                        RunInteractiveMessage("No COM port selected.");
                        break;
                    }

                    RunInteractiveAction(() => ExecuteBuzz([selectedPort], scoreboard));
                    break;

                case 4:
                    if (string.IsNullOrWhiteSpace(selectedPort))
                    {
                        RunInteractiveMessage("No COM port selected.");
                        break;
                    }

                    RunInteractiveAction(() => ExecutePayload([selectedPort], scoreboard, session, interactiveShell: true));
                    break;

                case 5:
                    RunInteractiveAction(() => ExecuteListEncodings(session));
                    break;

                case 6:
                    if (Confirm(
                            "Scan all COM ports?",
                            [
                                "This sends a test AT+GD packet to every COM port.",
                                "Use this only when it is safe to touch the scoreboard state.",
                            ]))
                    {
                        RunInteractiveAction(() => ExecuteScanAll(scoreboard));
                    }

                    break;

                case 7:
                    RunInteractiveAction(() => ExecuteWatchInteractive(scoreboard), pauseAfter: false);
                    Pause("Press any key to return to the menu...");
                    break;

                case 8:
                    RunCommandPrompt(scoreboard, session);
                    break;

                case 9:
                    RunInteractiveAction(() => ExecuteHelp(scoreboard, session));
                    break;

                case 10:
                    Console.Clear();
                    return 0;
            }
        }
    }

    private static int ExecuteList(IScoreboardApi scoreboard)
    {
        PrintPortList(scoreboard);
        return 0;
    }

    private static int ExecuteBuzz(string[] args, IScoreboardApi scoreboard)
    {
        var portName = ResolvePortName(args, scoreboard);

        if (string.IsNullOrWhiteSpace(portName))
        {
            Console.Error.WriteLine("COM port is not specified.");
            Console.WriteLine();
            PrintUsage();
            return 1;
        }

        try
        {
            Console.WriteLine($"Sending buzzer signal to {portName}...");
            SendBuzzerSignal(portName);
            Console.WriteLine("Signal sent.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to send signal to {portName}: {exception.Message}");
            return 1;
        }
    }

    private static int ExecuteScanAll(IScoreboardApi scoreboard)
    {
        var ports = scoreboard.GetAvailablePorts().ToArray();

        if (ports.Length == 0)
        {
            Console.WriteLine("No COM ports found.");
            return 0;
        }

        Console.WriteLine($"Ports found: {ports.Length}");

        for (var index = 0; index < ports.Length; index++)
        {
            var portName = ports[index];
            Console.WriteLine();
            Console.WriteLine($"[{index + 1}/{ports.Length}] {portName}: sending test signal...");

            try
            {
                SendBuzzerSignal(portName);
                Console.WriteLine("Signal sent.");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}");
            }

            if (index < ports.Length - 1)
            {
                Thread.Sleep(ScanPauseMilliseconds);
            }
        }

        return 0;
    }

    private static int ExecutePayload(string[] args, IScoreboardApi scoreboard, CommandSession session, bool interactiveShell)
    {
        var encoding = session.PayloadEncoding;
        var positionalArguments = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.Equals("--encoding", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("-e", StringComparison.OrdinalIgnoreCase))
            {
                if (index == args.Length - 1)
                {
                    Console.Error.WriteLine("Encoding value is missing.");
                    return 1;
                }

                if (!TryResolveEncoding(args[++index], out encoding, out var errorMessage))
                {
                    Console.Error.WriteLine(errorMessage);
                    return 1;
                }

                continue;
            }

            positionalArguments.Add(argument);
        }

        var portName = positionalArguments.Count > 0
            ? positionalArguments[0].Trim()
            : ResolvePortName(Array.Empty<string>(), scoreboard);

        if (string.IsNullOrWhiteSpace(portName))
        {
            Console.Error.WriteLine("COM port is not specified.");
            Console.WriteLine();
            PrintUsage(session);
            return 1;
        }

        var payload = positionalArguments.Count > 1
            ? string.Join(' ', positionalArguments.Skip(1))
            : PromptForPayload(interactiveShell);

        if (string.IsNullOrWhiteSpace(payload))
        {
            Console.Error.WriteLine("Payload is not specified.");
            return 1;
        }

        try
        {
            Console.WriteLine($"Sending payload to {portName} using {FormatEncodingDisplay(encoding)}...");
            SendCustomPayload(portName, payload, encoding);
            Console.WriteLine("Payload sent.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to send payload to {portName}: {exception.Message}");
            return 1;
        }
    }

    private static int ExecuteListEncodings(CommandSession session)
    {
        var encodings = GetSupportedEncodings();

        Console.WriteLine("Available single-byte encodings:");
        Console.WriteLine($"Current payload encoding: {FormatEncodingDisplay(session.PayloadEncoding)}");
        Console.WriteLine();

        foreach (var encoding in encodings)
        {
            Console.WriteLine($"  {encoding.CodePage,5}  {encoding.WebName,-18}  {encoding.EncodingName}");
        }

        return 0;
    }

    private static int ExecuteSetEncoding(string[] args, CommandSession session, bool promptWhenMissing = false)
    {
        string? encodingValue = null;

        if (args.Length > 0)
        {
            encodingValue = args[0];
        }
        else if (promptWhenMissing)
        {
            Console.Write($"Encoding ({FormatEncodingDisplay(session.PayloadEncoding)}): ");
            encodingValue = Console.ReadLine()?.Trim();
        }

        if (string.IsNullOrWhiteSpace(encodingValue))
        {
            Console.WriteLine($"Current payload encoding: {FormatEncodingDisplay(session.PayloadEncoding)}");
            Console.WriteLine("Use the 'encodings' command to see the available single-byte encodings.");
            return 0;
        }

        if (!TryResolveEncoding(encodingValue, out var encoding, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            return 1;
        }

        session.PayloadEncoding = encoding;
        Console.WriteLine($"Payload encoding set to {FormatEncodingDisplay(session.PayloadEncoding)}.");
        return 0;
    }

    private static int ExecuteWatch(IScoreboardApi scoreboard)
    {
        return ExecuteWatchCore(scoreboard, stopOnEscape: false);
    }

    private static int ExecuteWatchInteractive(IScoreboardApi scoreboard)
    {
        return ExecuteWatchCore(scoreboard, stopOnEscape: true);
    }

    private static int ExecuteWatchCore(IScoreboardApi scoreboard, bool stopOnEscape)
    {
        var knownPorts = new HashSet<string>(scoreboard.GetAvailablePorts(), StringComparer.OrdinalIgnoreCase);
        var lastPollAt = DateTime.UtcNow;

        Console.WriteLine("COM port watch started.");
        Console.WriteLine(stopOnEscape
            ? "Connect or disconnect the scoreboard adapter. Press Esc to stop."
            : "Connect or disconnect the scoreboard adapter. Press Ctrl+C to stop.");
        PrintPorts(knownPorts.OrderBy(static port => port, StringComparer.OrdinalIgnoreCase).ToArray());

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;

        if (!stopOnEscape)
        {
            cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;
        }

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                if (stopOnEscape && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;

                    if (key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                if ((DateTime.UtcNow - lastPollAt).TotalMilliseconds < WatchRefreshMilliseconds)
                {
                    Thread.Sleep(100);
                    continue;
                }

                lastPollAt = DateTime.UtcNow;

                var currentPorts = scoreboard.GetAvailablePorts();
                var currentSet = new HashSet<string>(currentPorts, StringComparer.OrdinalIgnoreCase);

                var addedPorts = currentSet.Except(knownPorts, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var removedPorts = knownPorts.Except(currentSet, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (addedPorts.Length == 0 && removedPorts.Length == 0)
                {
                    continue;
                }

                var timeStamp = DateTime.Now.ToString("HH:mm:ss");

                foreach (var port in addedPorts)
                {
                    Console.WriteLine($"[{timeStamp}] Connected: {port}");
                }

                foreach (var port in removedPorts)
                {
                    Console.WriteLine($"[{timeStamp}] Disconnected: {port}");
                }

                knownPorts = currentSet;
            }
        }
        finally
        {
            if (cancelHandler is not null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        Console.WriteLine("Watch stopped.");
        return 0;
    }

    private static int ExecuteHelp(IScoreboardApi scoreboard, CommandSession session)
    {
        PrintHeader();
        PrintPortList(scoreboard);
        PrintUsage(session);
        return 0;
    }

    private static int ExecuteUnknownCommand(string command, IScoreboardApi scoreboard, CommandSession session)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.WriteLine();
        PrintHeader();
        PrintPortList(scoreboard);
        PrintUsage(session);
        return 1;
    }

    private static void RunCommandPrompt(IScoreboardApi scoreboard, CommandSession session)
    {
        while (true)
        {
            Console.Clear();
            PrintHeader();
            Console.WriteLine("Enter a command exactly as you would type it on the command line.");
            Console.WriteLine("Examples: list, buzz COM3, payload COM3 \"TEST\", encoding 866, encodings, scan-all, watch");
            Console.WriteLine("Press Enter on an empty line to return to the menu.");
            Console.WriteLine();
            Console.Write("cmd> ");

            var line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var args = SplitCommandLine(line);

            if (args.Length == 0)
            {
                return;
            }

            if (IsExitCommand(args[0]))
            {
                return;
            }

            Console.WriteLine();
            Execute(args, scoreboard, session, interactiveShell: true);
            Console.WriteLine();
            Pause("Press any key to continue...");
        }
    }

    private static void SendBuzzerSignal(string portName)
    {
        portName = portName.Trim().ToUpperInvariant();
        using var scoreboard = ScoreboardCompositionRoot.CreateProbeApi();

        try
        {
            scoreboard.Connect(portName);
            scoreboard.Execute(new SetManualSignalCommand(true));

            var startedAt = Stopwatch.GetTimestamp();

            do
            {
                scoreboard.Publish();
                Thread.Sleep(SignalPacketIntervalMilliseconds);
            }
            while (Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds < BuzzerDurationMilliseconds);

            scoreboard.Execute(new SetManualSignalCommand(false));
            scoreboard.Publish();
            Thread.Sleep(SignalPacketIntervalMilliseconds);
        }
        finally
        {
            scoreboard.Disconnect();
        }
    }

    private static void SendCustomPayload(string portName, string payload, Encoding encoding)
    {
        portName = portName.Trim().ToUpperInvariant();
        using var scoreboard = ScoreboardCompositionRoot.CreateConsoleApi();

        try
        {
            scoreboard.Connect(portName);
            scoreboard.SendPayload(payload, encoding);
            Thread.Sleep(SignalPacketIntervalMilliseconds);
        }
        finally
        {
            scoreboard.Disconnect();
        }
    }

    private static string? ResolvePortName(string[] args, IScoreboardApi scoreboard)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0].Trim();
        }

        var ports = scoreboard.GetAvailablePorts().ToArray();

        if (ports.Length == 0)
        {
            return null;
        }

        PrintPorts(ports);
        Console.Write("Enter COM port: ");
        return Console.ReadLine()?.Trim();
    }

    private static string? PromptForPayload(bool interactiveShell)
    {
        Console.Write(interactiveShell ? "payload> " : "Enter payload: ");
        return Console.ReadLine();
    }

    private static string? SelectPortInteractive(IScoreboardApi scoreboard, string? currentPort)
    {
        while (true)
        {
            var ports = scoreboard.GetAvailablePorts().ToArray();

            if (ports.Length == 0)
            {
                RunInteractiveMessage("No COM ports found.");
                return null;
            }

            var options = ports
                .Concat(["Refresh port list", "Back"])
                .ToArray();

            var selectedIndex = Array.FindIndex(ports, port => string.Equals(port, currentPort, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            var choice = ShowMenu(
                "Select COM Port",
                options,
                [
                    $"Current port: {currentPort ?? "none"}",
                    "Choose a port and press Enter.",
                ],
                selectedIndex);

            if (choice == options.Length - 1)
            {
                return currentPort;
            }

            if (choice == options.Length - 2)
            {
                continue;
            }

            return ports[choice];
        }
    }

    private static void RunInteractiveAction(Func<int> action, bool pauseAfter = true)
    {
        Console.Clear();
        PrintHeader();
        action();

        if (pauseAfter)
        {
            Console.WriteLine();
            Pause("Press any key to return to the menu...");
        }
    }

    private static void RunInteractiveMessage(string message)
    {
        Console.Clear();
        PrintHeader();
        Console.WriteLine(message);
        Console.WriteLine();
        Pause("Press any key to return to the menu...");
    }

    private static bool Confirm(string question, IReadOnlyList<string>? details = null)
    {
        var lines = details?.ToArray() ?? [];
        var selection = ShowMenu(question, ["No", "Yes"], lines);
        return selection == 1;
    }

    private static int ShowMenu(
        string title,
        IReadOnlyList<string> options,
        IReadOnlyList<string>? infoLines = null,
        int initialSelection = 0)
    {
        if (options.Count == 0)
        {
            throw new InvalidOperationException("Menu options are required.");
        }

        var selection = Math.Clamp(initialSelection, 0, options.Count - 1);
        while (true)
        {
            Console.Clear();
            PrintHeader();

            if (infoLines is not null)
            {
                foreach (var line in infoLines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine();
            }

            Console.WriteLine(title);
            Console.WriteLine();

            for (var index = 0; index < options.Count; index++)
            {
                var prefix = index == selection ? "> " : "  ";
                Console.WriteLine($"{prefix}{options[index]}");
            }

            var key = Console.ReadKey(intercept: true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    selection = selection == 0 ? options.Count - 1 : selection - 1;
                    break;

                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    selection = selection == options.Count - 1 ? 0 : selection + 1;
                    break;

                case ConsoleKey.Home:
                    selection = 0;
                    break;

                case ConsoleKey.End:
                    selection = options.Count - 1;
                    break;

                case ConsoleKey.Enter:
                    return selection;
            }
        }
    }

    private static string? GetPreferredPort(IScoreboardApi scoreboard, string? currentPort)
    {
        var ports = scoreboard.GetAvailablePorts().ToArray();

        if (ports.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(currentPort) &&
            ports.Contains(currentPort, StringComparer.OrdinalIgnoreCase))
        {
            return ports.First(port => string.Equals(port, currentPort, StringComparison.OrdinalIgnoreCase));
        }

        return ports[0];
    }

    private static string[] SplitCommandLine(string commandLine)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var character in commandLine)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result.ToArray();
    }

    private static bool IsExitCommand(string value)
    {
        return value.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("back", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanUseInteractiveConsole()
    {
        return Environment.UserInteractive &&
               !Console.IsInputRedirected &&
               !Console.IsOutputRedirected;
    }

    private static void Pause(string prompt)
    {
        Console.Write(prompt);
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }

    private static void PrintHeader()
    {
        Console.WriteLine("ScoreBoardVtk CMD");
        Console.WriteLine("Utility for finding and testing the scoreboard COM port.");
        Console.WriteLine();
    }

    private static void PrintPortList(IScoreboardApi scoreboard)
    {
        var ports = scoreboard.GetAvailablePorts();
        PrintPorts(ports);
    }

    private static void PrintPorts(IReadOnlyList<string> ports)
    {
        if (ports.Count == 0)
        {
            Console.WriteLine("No COM ports found.");
            return;
        }

        Console.WriteLine("Available COM ports:");

        for (var index = 0; index < ports.Count; index++)
        {
            Console.WriteLine($"  {index + 1}. {ports[index]}");
        }
    }

    private static void PrintUsage()
    {
        PrintUsage(new CommandSession());
    }

    private static void PrintUsage(CommandSession session)
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  list");
        Console.WriteLine("      Print the list of COM ports.");
        Console.WriteLine("  buzz <COMx>");
        Console.WriteLine("      Send a 0.3 second buzzer signal to the specified port.");
        Console.WriteLine("  payload <COMx> <payload> [--encoding <name|codepage>]");
        Console.WriteLine("      Send a custom AT+GD payload to the specified port.");
        Console.WriteLine($"      Default payload encoding: {FormatEncodingDisplay(session.PayloadEncoding)}.");
        Console.WriteLine("  encodings");
        Console.WriteLine("      Print the list of supported single-byte encodings.");
        Console.WriteLine("  encoding <name|codepage>");
        Console.WriteLine("      Set the payload encoding for the current interactive session.");
        Console.WriteLine("  scan-all");
        Console.WriteLine("      Walk through all COM ports and send a test signal to each.");
        Console.WriteLine("      A 1 second pause is used between ports.");
        Console.WriteLine("  watch");
        Console.WriteLine("      Watch for COM ports being connected or disconnected.");
        Console.WriteLine();
        Console.WriteLine("Interactive mode:");
        Console.WriteLine("  Start the app without arguments to open the keyboard menu.");
        Console.WriteLine("  The menu supports arrow keys, Enter, and direct command input.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- list");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- buzz COM3");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- payload COM3 \"TEST\"");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- payload COM3 \"Привет\" --encoding 866");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- encodings");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- buzz MOCK");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- scan-all");
        Console.WriteLine("  dotnet run --project ScoreBoardVtk.Cmd -- watch");
        Console.WriteLine();
        Console.WriteLine("Note:");
        Console.WriteLine("  The buzz and scan-all commands send the same AT+GD packet as the main app,");
        Console.WriteLine("  with the manual-signal flag enabled. During a live game, it is safer to use");
        Console.WriteLine("  watch or disconnect the scoreboard from the main application first.");
        Console.WriteLine("  A built-in MOCK port is available for testing without real hardware.");
    }

    private static Encoding[] GetSupportedEncodings()
    {
        return Encoding.GetEncodings()
            .Select(static info => info.GetEncoding())
            .Where(static encoding => encoding.IsSingleByte)
            .OrderBy(static encoding => encoding.CodePage)
            .ThenBy(static encoding => encoding.WebName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CommandSession CreateCommandSession(HostSettings hostSettings)
    {
        ArgumentNullException.ThrowIfNull(hostSettings);

        if (TryResolveEncoding(hostSettings.PayloadEncoding, out var encoding, out _))
        {
            return new CommandSession { PayloadEncoding = encoding };
        }

        return new CommandSession();
    }

    private static bool TryResolveEncoding(string value, out Encoding encoding, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            encoding = Encoding.GetEncoding(1251);
            errorMessage = "Encoding value is empty.";
            return false;
        }

        try
        {
            encoding = int.TryParse(value, out var codePage)
                ? Encoding.GetEncoding(codePage)
                : Encoding.GetEncoding(value);

            if (!encoding.IsSingleByte)
            {
                errorMessage = $"Encoding '{value}' is not supported. Only single-byte encodings can be used for legacy AT+GD payloads.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            encoding = Encoding.GetEncoding(1251);
            errorMessage = $"Unknown encoding '{value}': {exception.Message}";
            return false;
        }
    }

    private static string FormatEncodingDisplay(Encoding encoding)
    {
        return $"{encoding.CodePage} / {encoding.WebName}";
    }

    private sealed class CommandSession
    {
        public Encoding PayloadEncoding { get; set; } = Encoding.GetEncoding(1251);
    }
}
