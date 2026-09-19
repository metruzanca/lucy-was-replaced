using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests;

[Collection("gleam")]
public class GleamDocsTests
{
    private static readonly Lazy<string> Reference = new(() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Embedded", "docs", "game-reference.md")));

    private static readonly Lazy<string> GameModule = new(() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Embedded", "game.gleam")));

    private static readonly Lazy<string> ItemModule = new(() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Embedded", "game", "item.gleam")));

    private static readonly Regex PubFn = new(@"pub fn\s+(\w+)", RegexOptions.Compiled);

    public GleamDocsTests()
    {
        if (!GleamDocs.IsLoaded)
            GleamDocs.Parse(Reference.Value);
    }

    [Fact]
    public void EveryGameFunctionHasAPage()
    {
        // Internal FFI helpers (entity_code) are intentionally not documented.
        var internalHelpers = new[] { "entity_code" };
        foreach (Match match in PubFn.Matches(GameModule.Value))
        {
            var name = match.Groups[1].Value;
            if (internalHelpers.Contains(name)) continue;
            Assert.True(
                !string.IsNullOrEmpty(GleamDocs.FunctionDoc(name)),
                $"missing '## game.{name}' section");
        }
    }

    [Fact]
    public void EveryItemFunctionHasAPage()
    {
        // Internal FFI helpers (item_code, item_from_id) are intentionally not documented.
        var internalHelpers = new[] { "item_code", "item_from_id" };
        foreach (Match match in PubFn.Matches(ItemModule.Value))
        {
            var name = match.Groups[1].Value;
            if (internalHelpers.Contains(name)) continue;
            Assert.True(
                !string.IsNullOrEmpty(GleamDocs.FunctionDoc(name)),
                $"missing '## game.item.{name}' section");
        }
    }

    [Fact]
    public void EveryEntityAndGroundHasAnObjectPage()
    {
        var expected = new[] { "grass", "bush", "carrot", "pumpkin", "sunflower", "tree", "cactus", "treasure", "hedge", "soil", "grassland" };
        foreach (var name in expected)
            Assert.True(!string.IsNullOrEmpty(GleamDocs.ObjectDoc(name)), $"missing objects/{name} page");
    }

    [Fact]
    public void EveryItemConstantHasAnItemPage()
    {
        var expected = new[] { "hay", "wood", "carrot", "pumpkin", "power", "gold", "bones", "water", "fertilizer" };
        foreach (var name in expected)
            Assert.True(!string.IsNullOrEmpty(GleamDocs.ItemDoc(name)), $"missing items/{name} page");
    }

    [Fact]
    public void TocEntriesAllResolveToPages()
    {
        foreach (var (_, page, _) in GleamDocs.Builtins().Concat(GleamDocs.Entities())
                     .Concat(GleamDocs.Grounds()).Concat(GleamDocs.Items()))
            Assert.True(GleamDocs.Page(page) != "", $"dead TOC link to {page}");
    }

    [Fact]
    public void FunctionPagesCarryASignature()
    {
        foreach (var (_, page, _) in GleamDocs.Builtins())
        {
            if (page == "game") continue;
            var body = GleamDocs.Page(page);
            Assert.Matches(@"`game\.(?:item\.)?[\w.]+[^`]*->[^`]*`", body);
        }
    }

    [Fact]
    public void OverviewMapsPythonBuiltinsToTheStdlib()
    {
        var overview = GleamDocs.Overview;
        foreach (var pythonBuiltin in new[] { "range", "len", "min", "max", "abs", "str", "list", "set", "dict" })
            Assert.Contains(pythonBuiltin, overview);
    }

    [Fact]
    public void DottedLookupsRouteToTheRightPages()
    {
        Assert.Equal(("functions/plant", "game.plant"), GleamDocs.LookupDotted("game.plant"));
        Assert.Equal(("objects/carrot", "game.Carrot"), GleamDocs.LookupDotted("game.Carrot"));
        Assert.Equal(("objects/soil", "game.Soil"), GleamDocs.LookupDotted("game.Soil"));
        Assert.Equal(("directions/north", "game.North"), GleamDocs.LookupDotted("game.North"));
        Assert.Equal(("items/hay", "game.item.Hay"), GleamDocs.LookupDotted("game.item.Hay"));
        Assert.Equal(("functions/num_items", "game.item.num_items"), GleamDocs.LookupDotted("game.item.num_items"));
        Assert.Equal(("game", "game"), GleamDocs.LookupDotted("game"));
        Assert.Null(GleamDocs.LookupDotted("game.Nope"));
        Assert.Null(GleamDocs.LookupDotted("Entities.Carrot"));
    }

    [Fact]
    public void AutocompleteDomainsAreWellFormed()
    {
        var members = GleamDocs.Members();
        Assert.Contains("item", members);
        Assert.Contains("plant", members);
        Assert.Contains("Carrot", members);
        Assert.Contains("North", members);
        Assert.DoesNotContain(members, m => m.StartsWith("game.", StringComparison.Ordinal));

        var itemMembers = GleamDocs.ItemMembers();
        Assert.Contains("Hay", itemMembers);
        Assert.Contains("num_items", itemMembers);
        Assert.DoesNotContain(itemMembers, m => m.StartsWith("game.item.", StringComparison.Ordinal));
    }
}