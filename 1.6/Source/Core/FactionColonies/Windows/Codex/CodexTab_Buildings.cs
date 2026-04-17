using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    /// <summary>
    /// The "Buildings" tab in the Codex — a reference for all empire buildings.
    /// Left pane: search bar + buildings grouped by tech level.
    /// Center pane: selected building detail (stats, modifiers, upgrades, restrictions).
    /// No right pane.
    /// </summary>
    public class CodexTab_Buildings : ICodexTab
    {
        // ── Layout constants ──
        private const float EntryRowHeight = 24f;
        private const float GroupHeaderHeight = 28f;
        private const float AccentBarWidth = 3f;
        private const float Margin = 8f;
        private const float SmallMargin = 4f;
        private const float SectionHeaderHeight = 22f;
        private const float StatRowHeight = 22f;
        private const float SearchBarHeight = 28f;
        private const float TitleIconSize = 28f;
        private const float IconSmall = 16f;
        private const float IndentWidth = 20f;
        private const float UpgradeRowHeight = 22f;

        private static readonly Color DefaultAccent = new Color(0.83f, 0.68f, 0.21f);
        private static readonly Color GroupBgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        private static readonly Color SectionBgColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);

        // ── Tech level colors ──
        private static readonly Dictionary<TechLevel, Color> TechColors = new Dictionary<TechLevel, Color>
        {
            { TechLevel.Neolithic, new Color(0.6f, 0.5f, 0.3f) },
            { TechLevel.Medieval, new Color(0.5f, 0.5f, 0.6f) },
            { TechLevel.Industrial, new Color(0.4f, 0.6f, 0.4f) },
            { TechLevel.Spacer, new Color(0.4f, 0.5f, 0.8f) },
            { TechLevel.Ultra, new Color(0.7f, 0.4f, 0.7f) },
            { TechLevel.Archotech, new Color(0.8f, 0.7f, 0.3f) },
        };

        // ── Data model ──
        private readonly List<TechGroup> allTechGroups;
        private BuildingFCDef selectedBuilding;
        private string searchTerm = "";

        // ── Scroll state ──
        private Vector2 leftScroll;
        private Vector2 centerScroll;

        // ── Expand/collapse state ──
        private readonly HashSet<TechLevel> expandedGroups = new HashSet<TechLevel>();

        // ── Truncation cache ──
        private readonly Dictionary<string, string> truncateCache = new Dictionary<string, string>();

        // ── Filtered building cache (avoids per-frame allocations) ──
        private string lastAppliedSearch = "";
        private readonly Dictionary<TechLevel, List<BuildingFCDef>> filteredCache = new Dictionary<TechLevel, List<BuildingFCDef>>();

        private class TechGroup
        {
            public TechLevel techLevel;
            public List<BuildingFCDef> buildings = new List<BuildingFCDef>();
        }

        public string TabLabel => "FCCodexTabBuildings".Translate();
        public bool HasRightPane => false;

        public CodexTab_Buildings()
        {
            allTechGroups = new List<TechGroup>();

            var grouped = DefDatabase<BuildingFCDef>.AllDefsListForReading
                .Where(d => d.defName != "Empty" && d.defName != "Construction")
                .GroupBy(d => d.techLevel)
                .OrderBy(g => (int)g.Key);

            foreach (var g in grouped)
            {
                TechGroup tg = new TechGroup
                {
                    techLevel = g.Key,
                    buildings = g.OrderBy(b => b.LabelCap.RawText).ToList()
                };
                allTechGroups.Add(tg);
                expandedGroups.Add(g.Key);
            }

            // Select first building
            if (allTechGroups.Count > 0 && allTechGroups[0].buildings.Count > 0)
                selectedBuilding = allTechGroups[0].buildings[0];
        }

        public void OnTabSelected() { }
        public void OnTabDeselected() { }

        private static Color GetTechColor(TechLevel level)
        {
            Color c;
            if (TechColors.TryGetValue(level, out c))
                return c;
            return DefaultAccent;
        }

        private bool MatchesSearch(BuildingFCDef building)
        {
            if (searchTerm.NullOrEmpty()) return true;
            return (building.label ?? building.defName).IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RebuildFilteredCache()
        {
            if (lastAppliedSearch == (searchTerm ?? "")) return;
            lastAppliedSearch = searchTerm ?? "";
            filteredCache.Clear();
            foreach (TechGroup tg in allTechGroups)
            {
                List<BuildingFCDef> visible = new List<BuildingFCDef>();
                foreach (BuildingFCDef b in tg.buildings)
                {
                    if (MatchesSearch(b))
                        visible.Add(b);
                }
                filteredCache[tg.techLevel] = visible;
            }
        }

        private List<BuildingFCDef> GetFilteredBuildings(TechGroup tg)
        {
            List<BuildingFCDef> result;
            if (filteredCache.TryGetValue(tg.techLevel, out result))
                return result;
            return tg.buildings;
        }

        // ══════════════════════════════════════════════════════════════
        // LEFT PANE
        // ══════════════════════════════════════════════════════════════

        public void DrawLeftPane(Rect rect)
        {
            // Search bar at top
            Rect searchRect = new Rect(rect.x, rect.y, rect.width, SearchBarHeight);
            string prevSearch = searchTerm;
            Text.Font = GameFont.Small;
            searchTerm = Widgets.TextField(searchRect, searchTerm);
            if (searchTerm != prevSearch)
            {
                truncateCache.Clear();
                lastAppliedSearch = ""; // force rebuild
            }
            RebuildFilteredCache();
            if (searchTerm.NullOrEmpty())
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(searchRect.x + 5f, searchRect.y, searchRect.width - 10f, searchRect.height),
                    "FCCodexSearchBuildings".Translate());
                GUI.color = prevColor;
            }
            ResetText();

            // Building list below search bar
            float listTop = searchRect.yMax + SmallMargin;
            Rect listRect = new Rect(rect.x, listTop, rect.width, rect.yMax - listTop);
            float totalHeight = CalculateLeftPaneHeight(listRect.width);
            float viewWidth = listRect.width - (totalHeight > listRect.height ? 16f : 0f);
            Rect viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

            Widgets.BeginScrollView(listRect, ref leftScroll, viewRect);
            float curY = 0f;

            foreach (TechGroup tg in allTechGroups)
            {
                List<BuildingFCDef> visible = GetFilteredBuildings(tg);
                if (visible.Count == 0) continue;

                bool isExpanded = expandedGroups.Contains(tg.techLevel);
                Color techColor = GetTechColor(tg.techLevel);

                // Group header
                Rect groupRect = new Rect(0f, curY, viewWidth, GroupHeaderHeight);
                Widgets.DrawBoxSolid(groupRect, GroupBgColor);
                TexLoad.DrawHorizontalGradient(groupRect, techColor * new Color(1f, 1f, 1f, 0.2f));
                Widgets.DrawBoxSolid(new Rect(0f, curY, AccentBarWidth, GroupHeaderHeight), techColor);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = techColor * new Color(1.3f, 1.3f, 1.3f, 1f);
                Widgets.Label(new Rect(AccentBarWidth + Margin, curY, viewWidth - AccentBarWidth - Margin * 2 - 20f, GroupHeaderHeight),
                    tg.techLevel.ToStringHuman().CapitalizeFirst());

                Rect arrowRect = new Rect(groupRect.xMax - 20f - 2f, curY + (GroupHeaderHeight - 20f) * 0.5f, 20f, 20f);
                GUI.color = Color.white;
                Widgets.DrawTextureFitted(arrowRect, isExpanded ? TexButton.Collapse : TexButton.Reveal, 1f);
                ResetText();

                if (Widgets.ButtonInvisible(groupRect))
                {
                    if (isExpanded) expandedGroups.Remove(tg.techLevel);
                    else expandedGroups.Add(tg.techLevel);
                    (isExpanded ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
                }

                curY += GroupHeaderHeight + 2f;
                if (!isExpanded) continue;

                // Building entries
                foreach (BuildingFCDef building in visible)
                {
                    Rect entryRect = new Rect(10f, curY, viewWidth - 10f, EntryRowHeight);
                    bool isSelected = selectedBuilding == building;

                    if (isSelected)
                        Widgets.DrawBoxSolid(entryRect, techColor * new Color(1f, 1f, 1f, 0.35f));
                    else if (Mouse.IsOver(entryRect))
                        Widgets.DrawBoxSolid(entryRect, techColor * new Color(1f, 1f, 1f, 0.15f));

                    Color barColor = isSelected ? techColor : techColor * new Color(1f, 1f, 1f, 0.4f);
                    Widgets.DrawBoxSolid(new Rect(entryRect.x, entryRect.y, 2f, entryRect.height), barColor);

                    float textX = entryRect.x + Margin;

                    // Icon
                    if (building.Icon is object)
                    {
                        Rect iconRect = new Rect(entryRect.x + 4f, entryRect.y + (EntryRowHeight - IconSmall) * 0.5f, IconSmall, IconSmall);
                        GUI.DrawTexture(iconRect, building.Icon);
                        textX = iconRect.xMax + 4f;
                    }

                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = isSelected ? Color.white : new Color(0.9f, 0.9f, 0.9f);
                    float labelWidth = entryRect.xMax - textX - 4f;
                    string fullLabel = building.LabelCap;
                    string truncated = fullLabel.Truncate(labelWidth, truncateCache);
                    Widgets.Label(new Rect(textX, entryRect.y, labelWidth, entryRect.height), truncated);
                    if (truncated != fullLabel)
                        TooltipHandler.TipRegion(entryRect, fullLabel);
                    ResetText();

                    if (Widgets.ButtonInvisible(entryRect))
                    {
                        selectedBuilding = building;
                        centerScroll = Vector2.zero;
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }

                    curY += EntryRowHeight;
                }
            }

            Widgets.EndScrollView();
            ResetText();
        }

        private float CalculateLeftPaneHeight(float width)
        {
            float total = 0f;
            foreach (TechGroup tg in allTechGroups)
            {
                List<BuildingFCDef> visible = GetFilteredBuildings(tg);
                if (visible.Count == 0) continue;

                total += GroupHeaderHeight + 2f;
                if (expandedGroups.Contains(tg.techLevel))
                    total += visible.Count * EntryRowHeight;
            }
            return total;
        }

        // ══════════════════════════════════════════════════════════════
        // CENTER PANE
        // ══════════════════════════════════════════════════════════════

        public void DrawCenterPane(Rect rect)
        {
            if (selectedBuilding is null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "FCCodexSelectBuilding".Translate());
                ResetText();
                return;
            }

            float contentWidth = rect.width - 16f;
            float contentHeight = CalculateCenterHeight(contentWidth);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            Color accent = GetTechColor(selectedBuilding.techLevel);

            Widgets.BeginScrollView(rect, ref centerScroll, viewRect);
            float curY = 0f;

            // ── Title with icon ──
            float titleTextX = 0f;
            if (selectedBuilding.Icon is object)
            {
                Rect iconBgRect = new Rect(0f, curY, TitleIconSize, TitleIconSize);
                Widgets.DrawBoxSolid(iconBgRect, accent * new Color(1f, 1f, 1f, 0.25f));
                Color prevColor = GUI.color;
                GUI.color = accent * new Color(1f, 1f, 1f, 0.6f);
                Widgets.DrawBox(iconBgRect);
                GUI.color = prevColor;
                GUI.DrawTexture(iconBgRect.ContractedBy(3f), selectedBuilding.Icon);
                titleTextX = TitleIconSize + Margin;
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Widgets.Label(new Rect(titleTextX, curY, contentWidth - titleTextX, 30f), selectedBuilding.LabelCap);
            ResetText();
            curY += 30f;

            // Accent gradient line
            TexLoad.DrawHorizontalGradient(new Rect(0f, curY, contentWidth, 2f), accent);
            curY += 2f + Margin;

            // ── Core Stats ──
            curY = DrawSection(curY, contentWidth, "FCCodexBuildingStats".Translate(), accent, DrawCoreStats);

            // ── Description ──
            if (!selectedBuilding.desc.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                Rect descRect = new Rect(0f, curY, contentWidth, 100f);
                Widgets.LabelCacheHeight(ref descRect, selectedBuilding.desc);
                curY += descRect.height + Margin;
                ResetText();
            }

            // ── Stat Modifiers ──
            TaggedString attrDesc = selectedBuilding.AttributeDesc;
            if (!attrDesc.RawText.NullOrEmpty())
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingModifiers".Translate(), accent,
                    (y, w) => DrawTextBlock(y, w, attrDesc));

            // ── Extension Sections ──
            curY = DrawExtensionSections(curY, contentWidth, accent);

            // ── Required Buildings ──
            if (selectedBuilding.requiredBuildings.Count > 0)
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingRequired".Translate(), accent, DrawRequiredBuildings);

            // ── Upgrade Tree ──
            List<BuildingUpgradeEntry> upgradeTree;
            if (FactionCache.UpgradeTrees.TryGetValue(selectedBuilding, out upgradeTree) && upgradeTree.Count > 0)
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingUpgrades".Translate(), accent,
                    (y, w) => DrawUpgradeTree(y, w, upgradeTree));

            // ── Required By ──
            List<BuildingFCDef> requiredBy;
            if (FactionCache.RequiredByBuildingMap.TryGetValue(selectedBuilding, out requiredBy) && requiredBy.Count > 0)
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingRequiredBy".Translate(), accent,
                    (y, w) => DrawBuildingList(y, w, requiredBy));

            // ── Settlement Type Restrictions ──
            if (selectedBuilding.settlementTypeAllowList.Count > 0 || selectedBuilding.settlementTypeBlockList.Count > 0)
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingSettlementRestrictions".Translate(), accent, DrawSettlementRestrictions);

            // ── Biome/Hilliness Restrictions ──
            if (HasTerrainRestrictions())
                curY = DrawSection(curY, contentWidth, "FCCodexBuildingBiomeRestrictions".Translate(), accent, DrawTerrainRestrictions);

            Widgets.EndScrollView();
            ResetText();
        }

        public void DrawRightPane(Rect rect) { }

        // ══════════════════════════════════════════════════════════════
        // SECTION DRAWING HELPERS
        // ══════════════════════════════════════════════════════════════

        private delegate float SectionDrawer(float curY, float width);

        private float DrawSection(float startY, float width, string header, Color accent, SectionDrawer drawer)
        {
            float curY = startY;

            // Header
            Rect headerRect = new Rect(0f, curY, width, SectionHeaderHeight);
            Widgets.DrawBoxSolid(headerRect, SectionBgColor);
            TexLoad.DrawHorizontalGradient(headerRect, accent * new Color(1f, 1f, 1f, 0.15f));
            Widgets.DrawBoxSolid(new Rect(0f, curY, AccentBarWidth, SectionHeaderHeight), accent);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = accent * new Color(1.3f, 1.3f, 1.3f, 1f);
            Widgets.Label(new Rect(AccentBarWidth + Margin, curY, width - AccentBarWidth - Margin, SectionHeaderHeight), header);
            ResetText();
            curY += SectionHeaderHeight + SmallMargin;

            curY = drawer(curY, width);
            curY += Margin;

            return curY;
        }

        private float DrawCoreStats(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            curY = DrawStatLine(curY, x, textW, "FCCodexBuildingCost".Translate(selectedBuilding.cost.ToString("F0")));
            curY = DrawStatLine(curY, x, textW, "FCCodexBuildingDuration".Translate(selectedBuilding.constructionDuration.ToTimeString()));

            if (selectedBuilding.upkeep > 0)
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingUpkeep".Translate(selectedBuilding.upkeep.ToString()));
            else if (selectedBuilding.upkeep < 0)
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingIncome".Translate(Math.Abs(selectedBuilding.upkeep).ToString()));

            curY = DrawStatLine(curY, x, textW, "FCCodexBuildingTechLevel".Translate(selectedBuilding.techLevel.ToStringHuman()));

            return curY;
        }

        private float DrawStatLine(float curY, float x, float width, string text)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            Widgets.Label(new Rect(x, curY, width, StatRowHeight), text);
            ResetText();
            return curY + StatRowHeight;
        }

        private float DrawTextBlock(float curY, float width, TaggedString text)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            float h = Text.CalcHeight(text, textW);
            Widgets.Label(new Rect(x, curY, textW, h), text);
            ResetText();

            return curY + h;
        }

        private float DrawExtensionSections(float curY, float width, Color accent)
        {
            if (selectedBuilding.modExtensions is null) return curY;
            foreach (IBuildingDetailSection section in selectedBuilding.modExtensions.OfType<IBuildingDetailSection>())
            {
                float contentWidth = width - AccentBarWidth - Margin * 2;
                float contentHeight = section.GetSectionHeight(selectedBuilding, contentWidth);
                if (contentHeight <= 0) continue;

                curY = DrawSection(curY, width, section.SectionLabel, accent, (y, w) =>
                {
                    float cx = AccentBarWidth + Margin;
                    float cw = w - cx - Margin;
                    Rect contentRect = new Rect(cx, y, cw, contentHeight);
                    section.DrawSection(selectedBuilding, contentRect);
                    return y + contentHeight;
                });
            }
            return curY;
        }

        private float DrawRequiredBuildings(float curY, float width)
        {
            float x = AccentBarWidth + Margin;

            foreach (BuildingFCDef req in selectedBuilding.requiredBuildings)
                curY = DrawClickableBuilding(curY, x, width, req);

            return curY;
        }

        private float DrawUpgradeTree(float curY, float width, List<BuildingUpgradeEntry> tree)
        {
            float baseX = AccentBarWidth + Margin;

            foreach (BuildingUpgradeEntry entry in tree)
            {
                float indent = entry.depth * IndentWidth;
                string prefix = entry.depth > 0 ? "\u2514 " : "";
                curY = DrawClickableBuilding(curY, baseX + indent, width, entry.def, prefix);
            }

            return curY;
        }

        private float DrawBuildingList(float curY, float width, List<BuildingFCDef> buildings)
        {
            float x = AccentBarWidth + Margin;

            foreach (BuildingFCDef building in buildings)
                curY = DrawClickableBuilding(curY, x, width, building);

            return curY;
        }

        private float DrawClickableBuilding(float curY, float x, float width, BuildingFCDef building, string prefix = "")
        {
            Rect rowRect = new Rect(x, curY, width - x - Margin, UpgradeRowHeight);
            float textX = x;

            if (building.Icon is object)
            {
                Rect iconRect = new Rect(x, curY + (UpgradeRowHeight - IconSmall) * 0.5f, IconSmall, IconSmall);
                GUI.DrawTexture(iconRect, building.Icon);
                textX = iconRect.xMax + SmallMargin;
            }

            bool isHover = Mouse.IsOver(rowRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = isHover ? new Color(0.4f, 0.6f, 0.9f) : Color.white;
            Widgets.Label(new Rect(textX, curY, width - textX - Margin, UpgradeRowHeight), prefix + building.LabelCap);
            ResetText();

            if (isHover)
                Widgets.DrawHighlight(rowRect);

            if (Widgets.ButtonInvisible(rowRect))
            {
                selectedBuilding = building;
                centerScroll = Vector2.zero;
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            return curY + UpgradeRowHeight;
        }

        private float DrawSettlementRestrictions(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            if (selectedBuilding.settlementTypeAllowList.Count > 0)
            {
                string names = string.Join(", ", selectedBuilding.settlementTypeAllowList.Select(d => d.LabelCap.RawText).ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingAllowedSettlements".Translate(names));
            }

            if (selectedBuilding.settlementTypeBlockList.Count > 0)
            {
                string names = string.Join(", ", selectedBuilding.settlementTypeBlockList.Select(d => d.LabelCap.RawText).ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingBlockedSettlements".Translate(names));
            }

            return curY;
        }

        private bool HasTerrainRestrictions()
        {
            return selectedBuilding.applicableBiomes.Count > 0
                || selectedBuilding.minhilliness != Hilliness.Undefined
                || selectedBuilding.maxhilliness != Hilliness.Undefined;
        }

        private float DrawTerrainRestrictions(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            if (selectedBuilding.applicableBiomes.Count > 0)
            {
                List<string> biomeLabels = new List<string>();
                foreach (string biomeName in selectedBuilding.applicableBiomes)
                {
                    BiomeDef biome = DefDatabase<BiomeDef>.GetNamedSilentFail(biomeName);
                    biomeLabels.Add(biome is object ? biome.LabelCap.RawText : biomeName);
                }
                string biomes = string.Join(", ", biomeLabels.ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingApplicableBiomes".Translate(biomes));
            }

            if (selectedBuilding.minhilliness != Hilliness.Undefined && selectedBuilding.maxhilliness != Hilliness.Undefined)
            {
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingHilliness".Translate(
                    selectedBuilding.minhilliness.ToString(), selectedBuilding.maxhilliness.ToString()));
            }
            else if (selectedBuilding.maxhilliness != Hilliness.Undefined)
            {
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingMaxHilliness".Translate(selectedBuilding.maxhilliness.ToString()));
            }
            else if (selectedBuilding.minhilliness != Hilliness.Undefined)
            {
                curY = DrawStatLine(curY, x, textW, "FCCodexBuildingMinHilliness".Translate(selectedBuilding.minhilliness.ToString()));
            }

            return curY;
        }

        // ══════════════════════════════════════════════════════════════
        // HEIGHT CALCULATION
        // ══════════════════════════════════════════════════════════════

        private float CalculateCenterHeight(float width)
        {
            if (selectedBuilding is null) return 0f;

            float total = 30f + 2f + Margin; // title + accent line

            // Core stats section
            int statLines = 3; // cost, duration, tech level
            if (selectedBuilding.upkeep != 0) statLines++;
            total += SectionHeaderHeight + SmallMargin + statLines * StatRowHeight + Margin;

            // Description
            if (!selectedBuilding.desc.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                total += Text.CalcHeight(selectedBuilding.desc, width) + Margin;
            }

            // Stat Modifiers
            TaggedString attrDesc = selectedBuilding.AttributeDesc;
            if (!attrDesc.RawText.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                total += SectionHeaderHeight + SmallMargin + Text.CalcHeight(attrDesc, width - AccentBarWidth - Margin * 2) + Margin;
            }

            // Extension sections
            if (selectedBuilding.modExtensions is object)
            {
                foreach (IBuildingDetailSection section in selectedBuilding.modExtensions.OfType<IBuildingDetailSection>())
                {
                    float contentWidth = width - AccentBarWidth - Margin * 2;
                    float h = section.GetSectionHeight(selectedBuilding, contentWidth);
                    if (h > 0)
                        total += SectionHeaderHeight + SmallMargin + h + Margin;
                }
            }

            // Required Buildings
            if (selectedBuilding.requiredBuildings.Count > 0)
                total += SectionHeaderHeight + SmallMargin + selectedBuilding.requiredBuildings.Count * UpgradeRowHeight + Margin;

            // Upgrade Tree
            List<BuildingUpgradeEntry> upgradeTree;
            if (FactionCache.UpgradeTrees.TryGetValue(selectedBuilding, out upgradeTree) && upgradeTree.Count > 0)
                total += SectionHeaderHeight + SmallMargin + upgradeTree.Count * UpgradeRowHeight + Margin;

            // Required By
            List<BuildingFCDef> requiredBy;
            if (FactionCache.RequiredByBuildingMap.TryGetValue(selectedBuilding, out requiredBy) && requiredBy.Count > 0)
                total += SectionHeaderHeight + SmallMargin + requiredBy.Count * UpgradeRowHeight + Margin;

            // Settlement Type Restrictions
            if (selectedBuilding.settlementTypeAllowList.Count > 0 || selectedBuilding.settlementTypeBlockList.Count > 0)
            {
                int lines = 0;
                if (selectedBuilding.settlementTypeAllowList.Count > 0) lines++;
                if (selectedBuilding.settlementTypeBlockList.Count > 0) lines++;
                total += SectionHeaderHeight + SmallMargin + lines * StatRowHeight + Margin;
            }

            // Terrain Restrictions
            if (HasTerrainRestrictions())
            {
                int lines = 0;
                if (selectedBuilding.applicableBiomes.Count > 0) lines++;
                if (selectedBuilding.minhilliness != Hilliness.Undefined || selectedBuilding.maxhilliness != Hilliness.Undefined) lines++;
                total += SectionHeaderHeight + SmallMargin + lines * StatRowHeight + Margin;
            }

            return total + 50f; // padding
        }

        private void ResetText()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}
