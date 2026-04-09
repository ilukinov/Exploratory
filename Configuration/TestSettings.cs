using System.Text.Json;

namespace HPAICOmpanionTester.Configuration;

/// <summary>
/// Strongly-typed configuration loaded from testSettings.json.
/// All timeouts are in seconds.
/// </summary>
public sealed class TestSettings
{
    public string Aumid { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    public string ExpectedWindowTitle { get; init; } = string.Empty;
    public int LaunchTimeoutSeconds { get; init; } = 30;
    public int ActionTimeoutSeconds { get; init; } = 10;
    public bool ScreenshotOnSuccess { get; init; } = false;

    /// <summary>
    /// Derives the package family name from the AUMID (everything before the '!').
    /// E.g. "AD2F1837.HPAIExperienceCenter_v10z8vjag6ke6!App"
    ///    → "AD2F1837.HPAIExperienceCenter_v10z8vjag6ke6"
    /// </summary>
    public string PackageFamilyName =>
        Aumid.Contains('!') ? Aumid[..Aumid.IndexOf('!')] : Aumid;

    /// <summary>
    /// The LocalState directory for the app's MSIX package, where logs are written.
    /// </summary>
    public string AppLocalStatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Packages",
        PackageFamilyName,
        "LocalState");

    private static readonly Lazy<TestSettings> _instance = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testSettings.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TestSettings>(
                   json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("testSettings.json could not be deserialized.");
    });

    public static TestSettings Load() => _instance.Value;
}
