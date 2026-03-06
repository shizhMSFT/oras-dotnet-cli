namespace Oras.Output;

/// <summary>
/// Shared formatting utilities.
/// </summary>
internal static class FormatHelper
{
    /// <summary>
    /// Formats a byte count into a human-readable size string (e.g., "4.5 MB").
    /// </summary>
    public static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
