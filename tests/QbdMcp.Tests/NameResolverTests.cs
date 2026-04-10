using QbdMcp.Services;

namespace QbdMcp.Tests;

public class NameResolverTests
{
    [Fact]
    public void Resolve_ExactMatch_ReturnsFound()
    {
        var candidates = new List<string> { "Rogers Communications Inc.", "Bell Canada", "Telus" };
        var result = NameResolver.Resolve("Rogers Communications Inc.", candidates);

        Assert.True(result.Success);
        Assert.Equal("Rogers Communications Inc.", result.ResolvedName);
    }

    [Fact]
    public void Resolve_ExactMatch_CaseInsensitive()
    {
        var candidates = new List<string> { "Rogers Communications Inc.", "Bell Canada" };
        var result = NameResolver.Resolve("rogers communications inc.", candidates);

        Assert.True(result.Success);
        Assert.Equal("Rogers Communications Inc.", result.ResolvedName);
    }

    [Fact]
    public void Resolve_SinglePartialMatch_ReturnsFound()
    {
        var candidates = new List<string> { "Rogers Communications Inc." };
        var result = NameResolver.Resolve("Rogers", candidates);

        Assert.True(result.Success);
        Assert.Equal("Rogers Communications Inc.", result.ResolvedName);
    }

    [Fact]
    public void Resolve_NoMatches_ReturnsNotFound()
    {
        var candidates = new List<string>();
        var result = NameResolver.Resolve("Nonexistent Corp", candidates);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Contains("No similar matches", result.ErrorMessage);
    }

    [Fact]
    public void Resolve_MultipleMatches_ReturnsAmbiguous()
    {
        var candidates = new List<string> { "Rogers Communications Inc.", "Rogers Wireless" };
        var result = NameResolver.Resolve("Rogers", candidates);

        Assert.False(result.Success);
        Assert.Contains("matched multiple", result.ErrorMessage);
        Assert.Contains("Rogers Communications Inc.", result.ErrorMessage);
        Assert.Contains("Rogers Wireless", result.ErrorMessage);
    }

    [Fact]
    public void Resolve_MultipleMatches_ExactHitWins()
    {
        var candidates = new List<string> { "Utilities", "Utilities:Hydro", "Utilities:Gas" };
        var result = NameResolver.Resolve("Utilities", candidates);

        Assert.True(result.Success);
        Assert.Equal("Utilities", result.ResolvedName);
    }

    [Fact]
    public void Resolve_ExactMatch_TakesPriorityOverMultiple()
    {
        var candidates = new List<string> { "Bell Canada", "Bell Mobility", "Bell" };
        var result = NameResolver.Resolve("Bell", candidates);

        Assert.True(result.Success);
        Assert.Equal("Bell", result.ResolvedName);
    }
}

public class ResolveResultTests
{
    [Fact]
    public void Found_SetsSuccessAndName()
    {
        var result = Models.ResolveResult.Found("Test Corp");

        Assert.True(result.Success);
        Assert.Equal("Test Corp", result.ResolvedName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void NotFound_WithSuggestions_IncludesDidYouMean()
    {
        var result = Models.ResolveResult.NotFound("Rogerz", new List<string> { "Rogers Communications Inc." });

        Assert.False(result.Success);
        Assert.Contains("Did you mean", result.ErrorMessage);
        Assert.Contains("Rogers Communications Inc.", result.ErrorMessage);
    }

    [Fact]
    public void NotFound_NoSuggestions_SaysNoMatches()
    {
        var result = Models.ResolveResult.NotFound("XYZZY", new List<string>());

        Assert.False(result.Success);
        Assert.Contains("No similar matches", result.ErrorMessage);
    }

    [Fact]
    public void Ambiguous_ListsAllMatches()
    {
        var result = Models.ResolveResult.Ambiguous("Rogers", new List<string> { "Rogers A", "Rogers B" });

        Assert.False(result.Success);
        Assert.Contains("Rogers A", result.ErrorMessage);
        Assert.Contains("Rogers B", result.ErrorMessage);
        Assert.Contains("more specific", result.ErrorMessage);
    }
}
