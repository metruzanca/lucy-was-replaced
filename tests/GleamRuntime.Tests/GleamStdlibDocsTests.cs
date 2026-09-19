using System;
using System.IO;
using System.Linq;
using System.Text;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests;

[Collection("gleam")]
public class GleamStdlibDocsTests
{
    private static readonly Lazy<string> Combined = new(() =>
    {
        var embedded = Path.Combine(AppContext.BaseDirectory, "Embedded");
        var sb = new StringBuilder(File.ReadAllText(Path.Combine(embedded, "docs", "game-reference.md")));
        sb.Append("\n\n").Append(File.ReadAllText(Path.Combine(embedded, "docs", "gleam-primer.md")));
        sb.Append("\n\n").Append(GleamStdlibDocs.Generate(GleamStdlib.LoadSources(Path.Combine(embedded, "stdlib"))));
        return sb.ToString();
    });

    public GleamStdlibDocsTests() => GleamDocs.Parse(Combined.Value);

    private static System.Collections.Generic.IEnumerable<(string Label, string Page)> ExtractLinks(string markdown) =>
        System.Text.RegularExpressions.Regex.Matches(markdown, @"\[([^\]]+)\]\(([^)]+)\)")
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value));

    [Fact]
    public void EveryCuratedModuleHasAnOverviewPage()
    {
        foreach (var module in GleamStdlibDocs.CuratedModules)
        {
            Assert.False(
                string.IsNullOrEmpty(GleamDocs.Page("functions/gleam_" + module)),
                $"missing overview page for {module}");
        }
    }

    [Fact]
    public void EveryCuratedModuleHasAnAutocompleteDomain()
    {
        foreach (var module in GleamStdlibDocs.CuratedModules)
        {
            Assert.NotEmpty(GleamDocs.StdlibMembers(module));
        }
    }

    [Theory]
    [InlineData("int", "absolute_value")]
    [InlineData("int", "power")]
    [InlineData("list", "map")]
    [InlineData("list", "fold")]
    [InlineData("string", "trim")]
    [InlineData("string", "uppercase")]
    [InlineData("option", "unwrap")]
    [InlineData("result", "unwrap")]
    [InlineData("bool", "to_string")]
    [InlineData("dict", "insert")]
    [InlineData("set", "contains")]
    [InlineData("pair", "first")]
    public void DocumentedFunctionsGetPages(string module, string function)
    {
        var body = GleamDocs.Page($"functions/gleam_{module}_{function}");
        Assert.False(string.IsNullOrEmpty(body), $"missing page for {module}.{function}");
        Assert.Contains($"`{module}.{function}(", body); // carries the signature
    }

    [Fact]
    public void ExamplesHeadingsAreDemoted()
    {
        // int.absolute_value's doc contains a `## Examples` block; it must be a `### `
        // subsection inside the page, not a new top-level section.
        var body = GleamDocs.Page("functions/gleam_int_absolute_value");
        Assert.Contains("### Examples", body);
        Assert.DoesNotContain(body.Split('\n'), line => line.StartsWith("## ", StringComparison.Ordinal));
    }

    [Fact]
    public void ModuleOverviewListsItsFunctions()
    {
        var overview = GleamDocs.Page("functions/gleam_int");
        Assert.Contains("### Functions", overview);
        Assert.Contains("functions/gleam_int_absolute_value", overview);
    }

    [Theory]
    [InlineData("int.absolute_value", "functions/gleam_int_absolute_value")]
    [InlineData("list.map", "functions/gleam_list_map")]
    [InlineData("int", "functions/gleam_int")]
    [InlineData("bool.to_string", "functions/gleam_bool_to_string")]
    public void DottedLookupsRouteToStdlibPages(string word, string page)
    {
        var hit = GleamDocs.LookupDotted(word);
        Assert.NotNull(hit);
        Assert.Equal(page, hit.Value.Page);
    }

    [Fact]
    public void UnknownWordsDoNotResolve()
    {
        Assert.Null(GleamDocs.LookupDotted("not_a_module"));
        Assert.Null(GleamDocs.LookupDotted("int.does_not_exist"));
    }

    [Fact]
    public void GameApiTocExcludesStdlibAndPrimer()
    {
        var labels = GleamDocs.Builtins().Select(e => e.Label).ToList();
        Assert.Contains("game.harvest", labels);
        Assert.DoesNotContain("int", labels);
        Assert.DoesNotContain("primer:Expressions and values", labels);
    }

    [Fact]
    public void GleamStdlibSectionListsPrimerThenModules()
    {
        var primer = GleamDocs.PrimerToc();
        var stdlib = GleamDocs.StdlibToc();

        Assert.Contains("1. [Expressions and values]", primer);
        Assert.Contains("2. [Functions]", primer);
        Assert.Contains("6. [Option and Result]", primer);

        foreach (var module in GleamStdlibDocs.CuratedModules)
            Assert.Contains($"[{module}](", stdlib);

        // Every link in both sections resolves to a real page.
        foreach (var (label, page) in ExtractLinks(primer).Concat(ExtractLinks(stdlib)))
            Assert.False(string.IsNullOrEmpty(GleamDocs.Page(page)), $"link [{label}] -> missing {page}");
    }

    [Fact]
    public void TocEntriesAllResolveToPages()
    {
        foreach (var (_, page, _) in GleamDocs.Builtins())
            Assert.False(string.IsNullOrEmpty(GleamDocs.Page(page)), $"TOC link to missing page {page}");
    }

    [Fact]
    public void PrimerSectionsAreLoaded()
    {
        Assert.False(string.IsNullOrEmpty(GleamDocs.Page("functions/gleam_primer_expressions_and_values")));
        Assert.False(string.IsNullOrEmpty(GleamDocs.Page("functions/gleam_primer_case_expressions")));
    }
}