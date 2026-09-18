using System;
using System.Collections.Generic;
using System.Linq;

namespace GleamRuntime
{
    /// <summary>
    /// Parses the bundled in-game Gleam reference (<c>docs/game-reference.md</c>) into
    /// per-page markdown keyed by the game's own doc ids ("functions/plant",
    /// "objects/carrot", "items/hay", "game"). The plugin swaps the game's Python docs,
    /// tooltips and autocomplete for these pages while Gleam mode is enabled.
    /// </summary>
    public static class GleamDocs
    {
        private static readonly HashSet<string> EntityNames = new(StringComparer.Ordinal)
        {
            "grass", "bush", "carrot", "pumpkin", "sunflower", "tree", "cactus", "treasure", "hedge",
        };

        private static readonly HashSet<string> GroundNames = new(StringComparer.Ordinal)
        {
            "soil", "grassland",
        };

        private static readonly HashSet<string> DirectionNames = new(StringComparer.Ordinal)
        {
            "north", "east", "south", "west",
        };

        private static readonly HashSet<string> ItemNames = new(StringComparer.Ordinal)
        {
            "hay", "wood", "carrot", "pumpkin", "power", "gold", "bones", "water", "fertilizer",
        };

        /// <summary>Page ids in reference order ("functions/plant", "game", "items/hay", …).</summary>
        private static readonly List<string> Order = new();

        /// <summary>Page body markdown (no heading) keyed by game doc id.</summary>
        private static readonly Dictionary<string, string> Pages = new(StringComparer.Ordinal);

        /// <summary>Display label ("game.plant", "game.Carrot", "game.item.Hay") keyed by page id.</summary>
        private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal);

        /// <summary>Unlock gate name ("plant", "carrot", "hay") keyed by page id; absent = always shown.</summary>
        private static readonly Dictionary<string, string> Gates = new(StringComparer.Ordinal);

        public static bool IsLoaded { get; private set; }

        /// <summary>Parse the reference markdown. Safe to call again to reload.</summary>
        public static void Parse(string markdown)
        {
            Order.Clear();
            Pages.Clear();
            Labels.Clear();
            Gates.Clear();

            foreach (var (title, body) in SplitSections(markdown))
                AddSection(title, body);

            IsLoaded = true;
        }

        public static string FunctionDoc(string name) => Page("functions/" + name);

        public static string ObjectDoc(string name) => Page("objects/" + name);

        public static string ItemDoc(string name) => Page("items/" + name);

        public static string DirectionDoc(string name) => Page("directions/" + name);

        public static string Overview => Page("game");

        public static string Page(string pageId) =>
            Pages.TryGetValue(pageId, out var body) ? body : "";

        public static string LabelFor(string pageId) => GetValue(Labels, pageId);

        /// <summary>
        /// Resolve a dotted Gleam reference like "game.plant", "game.Carrot" or
        /// "game.item.Hay" to its page id and label; null when unknown.
        /// </summary>
        public static (string Page, string Label)? LookupDotted(string word)
        {
            if (string.IsNullOrEmpty(word) || !word.StartsWith("game", StringComparison.Ordinal))
                return null;

            if (word == "game")
            {
                return Pages.ContainsKey("game") ? ("game", "game") : null;
            }

            if (!word.StartsWith("game.", StringComparison.Ordinal))
                return null;

            if (word.StartsWith("game.item.", StringComparison.Ordinal))
            {
                var item = word.Substring("game.item.".Length).ToLowerInvariant();
                var itemPage = ItemNames.Contains(item) ? "items/" + item : "functions/" + item;
                return Pages.ContainsKey(itemPage)
                    ? (itemPage, GetValue(Labels, itemPage, word))
                    : null;
            }

            var name = word.Substring("game.".Length).ToLowerInvariant();
            string page;
            if (EntityNames.Contains(name) || GroundNames.Contains(name))
                page = "objects/" + name;
            else if (DirectionNames.Contains(name))
                page = "directions/" + name;
            else
                page = "functions/" + name;
            return Pages.ContainsKey(page)
                ? (page, GetValue(Labels, page, word))
                : null;
        }

        /// <summary>Ordered TOC entries for the builtins section, overview first.</summary>
        public static List<(string Label, string Page, string Gate)> Builtins()
        {
            var entries = new List<(string, string, string)>();
            foreach (var page in Order)
            {
                if (page == "game")
                    entries.Add(("game module", "game", ""));
                else if (page.StartsWith("functions/", StringComparison.Ordinal))
                    entries.Add((Labels[page], page, GetValue(Gates, page)));
            }
            return entries;
        }

        /// <summary>Ordered TOC entries for the entities section.</summary>
        public static List<(string Label, string Page, string Gate)> Entities()
        {
            return TocFor(EntityNames, "objects/");
        }

        /// <summary>Ordered TOC entries for the grounds section.</summary>
        public static List<(string Label, string Page, string Gate)> Grounds()
        {
            return TocFor(GroundNames, "objects/");
        }

        /// <summary>Ordered TOC entries for the items section.</summary>
        public static List<(string Label, string Page, string Gate)> Items()
        {
            var entries = new List<(string, string, string)>();
            foreach (var page in Order)
            {
                if (page.StartsWith("items/", StringComparison.Ordinal))
                    entries.Add((Labels[page], page, GetValue(Gates, page)));
            }
            return entries;
        }

        /// <summary>Bare member names for the "game." autocomplete domain.</summary>
        public static List<string> Members()
        {
            var members = new List<string> { "item" };
            foreach (var page in Order)
            {
                var label = GetValue(Labels, page);
                if (label.StartsWith("game.item.", StringComparison.Ordinal))
                    continue;
                if (label.StartsWith("game.", StringComparison.Ordinal) && label != "game module")
                    members.Add(label.Substring("game.".Length));
            }
            return members;
        }

        /// <summary>Bare member names for the "game.item." autocomplete domain.</summary>
        public static List<string> ItemMembers()
        {
            var members = new List<string>();
            foreach (var page in Order)
            {
                var label = GetValue(Labels, page);
                if (label.StartsWith("game.item.", StringComparison.Ordinal))
                    members.Add(label.Substring("game.item.".Length));
            }
            return members;
        }

        private static List<(string Label, string Page, string Gate)> TocFor(HashSet<string> names, string prefix)
        {
            var entries = new List<(string, string, string)>();
            foreach (var page in Order)
            {
                if (!page.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                var name = page.Substring(prefix.Length);
                if (names.Contains(name))
                    entries.Add((Labels[page], page, GetValue(Gates, page)));
            }
            return entries;
        }

        private static string GetValue(Dictionary<string, string> map, string key, string fallback = "") =>
            map.TryGetValue(key, out var value) ? value : fallback;

        private static IEnumerable<(string Title, string Body)> SplitSections(string markdown)
        {
            var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');
            var sections = new List<(string, string)>();
            var currentTitle = (string?)null;
            var current = new List<string>();

            void Flush()
            {
                if (currentTitle != null)
                {
                    sections.Add((currentTitle, string.Join("\n", current).Trim()));
                    current.Clear();
                }
            }

            foreach (var line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    Flush();
                    currentTitle = line.Substring(3).Trim();
                }
                else if (currentTitle != null)
                {
                    current.Add(line);
                }
                // Preamble before the first "## " heading is ignored.
            }
            Flush();
            return sections;
        }

        private static void AddSection(string title, string body)
        {
            string page;
            string? gate = null;

            if (title == "game module")
            {
                page = "game";
            }
            else if (title.StartsWith("game.item.", StringComparison.Ordinal))
            {
                var name = title.Substring("game.item.".Length).ToLowerInvariant();
                // Item constants ("game.item.Hay") are items/* pages; item module
                // functions ("game.item.num_items") are functions/* pages.
                page = ItemNames.Contains(name) ? "items/" + name : "functions/" + name;
                gate = name;
            }
            else if (title.StartsWith("game.", StringComparison.Ordinal))
            {
                var name = title.Substring("game.".Length).ToLowerInvariant();
                if (EntityNames.Contains(name) || GroundNames.Contains(name))
                {
                    page = "objects/" + name;
                    gate = name;
                }
                else if (DirectionNames.Contains(name))
                {
                    page = "directions/" + name;
                }
                else
                {
                    page = "functions/" + name;
                    gate = name;
                }
            }
            else
            {
                return;
            }

            if (Pages.ContainsKey(page))
                return;

            Order.Add(page);
            Pages[page] = body;
            Labels[page] = title;
            if (gate != null)
                Gates[page] = gate;
        }
    }
}