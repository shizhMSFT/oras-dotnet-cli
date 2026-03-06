namespace Oras.Output;

/// <summary>
/// Abstraction for formatting command output in different modes (text, JSON, etc.)
/// </summary>
internal interface IOutputFormatter
{
    /// <summary>
    /// Write a status message (e.g., "Login Succeeded", "Pulled image...")
    /// </summary>
    void WriteStatus(string message);

    /// <summary>
    /// Write an error message
    /// </summary>
    void WriteError(string message, string? recommendation = null);

    /// <summary>
    /// Write a descriptor object (used by resolve, blob fetch --descriptor, etc.)
    /// </summary>
    void WriteDescriptor(object descriptor);

    /// <summary>
    /// Write a list of items in table format (e.g., repo ls, repo tags)
    /// </summary>
    void WriteTable(string[] headers, IEnumerable<string[]> rows);

    /// <summary>
    /// Write a tree structure (e.g., discover command output)
    /// </summary>
    void WriteTree(TreeNode root);

    /// <summary>
    /// Write JSON object (manifest, config, etc.)
    /// </summary>
    void WriteJson(string json, bool pretty = false);

    /// <summary>
    /// Write arbitrary structured data as JSON
    /// </summary>
    void WriteObject(object obj);

    /// <summary>
    /// Check if the formatter supports interactive features (progress bars, prompts)
    /// </summary>
    bool SupportsInteractivity { get; }
}

/// <summary>
/// Represents a node in a tree structure for hierarchical output
/// </summary>
internal sealed class TreeNode
{
    public string Label { get; set; } = string.Empty;
    public List<TreeNode> Children { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
