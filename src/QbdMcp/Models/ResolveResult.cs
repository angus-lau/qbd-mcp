namespace QbdMcp.Models;

public class ResolveResult
{
    public bool Success { get; init; }
    public string ResolvedName { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public List<string> Suggestions { get; init; } = new();

    public static ResolveResult Found(string name) => new() { Success = true, ResolvedName = name };

    public static ResolveResult NotFound(string input, List<string> suggestions)
    {
        var msg = suggestions.Count > 0
            ? $"Error: '{input}' not found. Did you mean: {string.Join(", ", suggestions)}?"
            : $"Error: '{input}' not found. No similar matches.";
        return new() { Success = false, ErrorMessage = msg, Suggestions = suggestions };
    }

    public static ResolveResult Ambiguous(string input, List<string> matches)
    {
        var msg = $"Error: '{input}' matched multiple entries: {string.Join(", ", matches)}. Please be more specific.";
        return new() { Success = false, ErrorMessage = msg, Suggestions = matches };
    }
}
