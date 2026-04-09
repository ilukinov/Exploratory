using System.Text.RegularExpressions;

namespace HPAICOmpanionTester.Support;

/// <summary>
/// Reads and parses the HP AI Companion HelicarrierLog files.
///
/// Log format:
///   2026-04-08 10:09:24.074 -06:00 &lt;:  9&gt; [INF] Final intent:set_audio_volume_speaker	Value:100 (at ...)
///   2026-04-08 10:09:24.074 -06:00 &lt;:  9&gt; [ERR] Some error message (at ...)
///
/// The reader captures a "bookmark" (file size) before a test action and then
/// reads only the new lines appended since that bookmark, avoiding false matches
/// from earlier test runs or sessions.
/// </summary>
public sealed class AppLogReader
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        @"Packages\AD2F1837.HPAIExperienceCenter_v10z8vjag6ke6\LocalState");

    private static readonly Regex IntentPattern = new(
        @"Final intent:(?<intent>\S+)\tValue:(?<value>.*?) \(at",
        RegexOptions.Compiled);

    private static readonly Regex LogLinePattern = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+? \[(?<level>\w{3})\] (?<message>.+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Returns the path to today's log file, or the most recent one if today's
    /// doesn't exist yet.
    /// </summary>
    public static string? GetCurrentLogPath()
    {
        if (!Directory.Exists(LogDirectory))
            return null;

        // Try today's log first
        var today = $"HelicarrierLog{DateTime.Now:yyyyMMdd}.log";
        var todayPath = Path.Combine(LogDirectory, today);
        if (File.Exists(todayPath))
            return todayPath;

        // Fall back to the most recent log
        return Directory.GetFiles(LogDirectory, "HelicarrierLog*.log")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns the current file size as a bookmark. Call this BEFORE
    /// the action you want to observe, then pass the bookmark to
    /// <see cref="GetNewLinesSince"/> after the action completes.
    /// </summary>
    public static long Bookmark()
    {
        var path = GetCurrentLogPath();
        if (path is null) return 0;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Reads all lines appended to the log since the given bookmark position.
    /// </summary>
    public static List<string> GetNewLinesSince(long bookmark)
    {
        var path = GetCurrentLogPath();
        if (path is null) return [];

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length <= bookmark) return [];

            fs.Seek(bookmark, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);
            return lines;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Extracts all "Final intent" entries from lines appended since the bookmark.
    /// Returns tuples of (intent, value).
    /// </summary>
    public static List<(string Intent, string Value)> GetIntentsSince(long bookmark)
    {
        var lines = GetNewLinesSince(bookmark);
        var results = new List<(string, string)>();

        foreach (var line in lines)
        {
            var match = IntentPattern.Match(line);
            if (match.Success)
                results.Add((match.Groups["intent"].Value, match.Groups["value"].Value.Trim()));
        }

        return results;
    }

    /// <summary>
    /// Returns all error-level ([ERR]) log lines since the bookmark.
    /// </summary>
    public static List<string> GetErrorsSince(long bookmark)
    {
        var lines = GetNewLinesSince(bookmark);
        return lines
            .Where(l => l.Contains("[ERR]"))
            .ToList();
    }

    /// <summary>
    /// Waits until a specific intent appears in the log since the bookmark,
    /// or the timeout expires.
    /// </summary>
    public static (string Intent, string Value)? WaitForIntent(
        long bookmark, string expectedIntent, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var intents = GetIntentsSince(bookmark);
            var match = intents.FirstOrDefault(i =>
                i.Intent.Equals(expectedIntent, StringComparison.OrdinalIgnoreCase));

            if (match != default)
                return match;

            Thread.Sleep(500);
        }

        return null;
    }
}
