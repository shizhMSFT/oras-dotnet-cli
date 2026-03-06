using Spectre.Console;

namespace Oras.Output;

/// <summary>
/// Progress renderer for push/pull operations using Spectre.Console with plain text fallback
/// </summary>
public sealed class ProgressRenderer : IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly bool _isInteractive;
    private ProgressContext? _progressContext;
    private readonly Dictionary<string, ProgressTask> _layerTasks = new();
    private ProgressTask? _overallTask;
    private int _totalLayers;
    private int _completedLayers;

    public ProgressRenderer(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
        _isInteractive = !Console.IsOutputRedirected && !Console.IsErrorRedirected && _console.Profile.Capabilities.Interactive;
    }

    /// <summary>
    /// Start a progress operation with an expected number of layers
    /// </summary>
    public void Start(string operation, int totalLayers)
    {
        _totalLayers = totalLayers;
        _completedLayers = 0;

        if (!_isInteractive)
        {
            _console.WriteLine($"{operation} ({totalLayers} layers)");
            return;
        }

        // Create interactive progress display
        _console.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new TransferSpeedColumn(),
                new RemainingTimeColumn())
            .Start(ctx =>
            {
                _progressContext = ctx;
                _overallTask = ctx.AddTask($"[bold]{operation}[/]", maxValue: totalLayers);
            });
    }

    /// <summary>
    /// Called when a layer download/upload starts
    /// </summary>
    public void OnLayerStart(string digest, string? filename, long size)
    {
        var shortDigest = digest.Length > 12 ? digest.Substring(0, 12) : digest;
        var description = !string.IsNullOrEmpty(filename)
            ? $"{shortDigest} {filename}"
            : shortDigest;

        if (!_isInteractive)
        {
            var sizeStr = FormatSize(size);
            _console.WriteLine($"  [{_completedLayers + 1}/{_totalLayers}] {description} ({sizeStr})");
            return;
        }

        if (_progressContext != null)
        {
            var task = _progressContext.AddTask($"[dim]{description}[/]", maxValue: size);
            _layerTasks[digest] = task;
        }
    }

    /// <summary>
    /// Called during layer transfer to update progress
    /// </summary>
    public void OnLayerProgress(string digest, long bytesTransferred, long totalBytes)
    {
        if (!_isInteractive)
        {
            return;
        }

        if (_layerTasks.TryGetValue(digest, out var task))
        {
            task.Value = bytesTransferred;
        }
    }

    /// <summary>
    /// Called when a layer transfer completes
    /// </summary>
    public void OnLayerComplete(string digest, string? filename, long size)
    {
        var shortDigest = digest.Length > 12 ? digest.Substring(0, 12) : digest;
        var description = !string.IsNullOrEmpty(filename)
            ? $"{shortDigest} {filename}"
            : shortDigest;

        _completedLayers++;

        if (!_isInteractive)
        {
            var sizeStr = FormatSize(size);
            _console.MarkupLine($"  [green]✓[/] {description} ({sizeStr})");
            return;
        }

        if (_layerTasks.TryGetValue(digest, out var task))
        {
            task.Value = task.MaxValue;
            task.Description = $"[green]✓[/] {description}";
            task.StopTask();
        }

        if (_overallTask != null)
        {
            _overallTask.Increment(1);
        }
    }

    /// <summary>
    /// Update overall progress (can be called independently of layer events)
    /// </summary>
    public void OnOverallProgress(int completed, int total)
    {
        _completedLayers = completed;
        _totalLayers = total;

        if (!_isInteractive)
        {
            return;
        }

        if (_overallTask != null)
        {
            _overallTask.MaxValue = total;
            _overallTask.Value = completed;
        }
    }

    /// <summary>
    /// Complete the progress operation
    /// </summary>
    public void Complete(string? summary = null)
    {
        if (!_isInteractive && !string.IsNullOrEmpty(summary))
        {
            _console.WriteLine(summary);
        }

        if (_overallTask != null)
        {
            _overallTask.Value = _overallTask.MaxValue;
            _overallTask.StopTask();
        }
    }

    public void Dispose()
    {
        _layerTasks.Clear();
        _progressContext = null;
        _overallTask = null;
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Custom column to show transfer speed
/// </summary>
internal sealed class TransferSpeedColumn : ProgressColumn
{
    protected override bool NoWrap => true;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        if (task.Value == 0 || deltaTime.TotalSeconds == 0)
        {
            return new Text("--");
        }

        var speed = task.Value / deltaTime.TotalSeconds;
        var speedStr = FormatSpeed(speed);
        return new Text(speedStr, new Style(foreground: Color.Blue));
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
        double speed = bytesPerSecond;
        int order = 0;
        while (speed >= 1024 && order < sizes.Length - 1)
        {
            order++;
            speed = speed / 1024;
        }
        return $"{speed:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Callback interface for hooking into copy operations
/// </summary>
public interface IProgressCallback
{
    void OnLayerStart(string digest, string? filename, long size);
    void OnLayerProgress(string digest, long bytesTransferred, long totalBytes);
    void OnLayerComplete(string digest, string? filename, long size);
}

/// <summary>
/// Adapter to connect ProgressRenderer to library copy callbacks
/// </summary>
public sealed class ProgressCallbackAdapter : IProgressCallback
{
    private readonly ProgressRenderer _renderer;

    public ProgressCallbackAdapter(ProgressRenderer renderer)
    {
        _renderer = renderer;
    }

    public void OnLayerStart(string digest, string? filename, long size)
    {
        _renderer.OnLayerStart(digest, filename, size);
    }

    public void OnLayerProgress(string digest, long bytesTransferred, long totalBytes)
    {
        _renderer.OnLayerProgress(digest, bytesTransferred, totalBytes);
    }

    public void OnLayerComplete(string digest, string? filename, long size)
    {
        _renderer.OnLayerComplete(digest, filename, size);
    }
}
