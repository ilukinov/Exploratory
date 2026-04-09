using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace HPAICOmpanionTester.Support;

/// <summary>
/// Utility for capturing screenshots on test failure.
/// Screenshots are saved to bin/Debug/net10.0-windows/screenshots/.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string ScreenshotDir =
        Path.Combine(AppContext.BaseDirectory, "screenshots");

    static ScreenshotHelper()
    {
        Directory.CreateDirectory(ScreenshotDir);
    }

    /// <summary>
    /// Captures the entire screen and saves it with a timestamp-based filename.
    /// </summary>
    public static string CaptureScreen()
    {
        try
        {
            var filename = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";
            var filepath = Path.Combine(ScreenshotDir, filename);

            using (var bitmap = new Bitmap(
                SystemInformation.VirtualScreen.Width,
                SystemInformation.VirtualScreen.Height,
                PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        SystemInformation.VirtualScreen.Location,
                        Point.Empty,
                        SystemInformation.VirtualScreen.Size);
                }

                bitmap.Save(filepath, ImageFormat.Png);
            }

            return filepath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScreenshotHelper] Failed to capture screenshot: {ex.Message}");
            return string.Empty;
        }
    }
}
