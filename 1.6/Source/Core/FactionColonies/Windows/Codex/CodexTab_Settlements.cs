using FactionColonies.util;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    /// <summary>
    /// The "Settlements" tab in the Codex — a reference for all settlement types.
    /// Left pane: flat list of available settlement types.
    /// Center pane: selected settlement type detail (stats, modifiers, restrictions).
    /// Right pane: source mod banner + available resources.
    /// </summary>
    public class CodexTab_Settlements : ICodexTab
    {
        // ── Layout constants ──
        private const float EntryRowHeight = 28f;
        private const float AccentBarWidth = 3f;
        private const float Margin = 8f;
        private const float SectionHeaderHeight = 22f;
        private const float StatRowHeight = 22f;
        private const float ResourceRowHeight = 24f;
        private const float SmallMargin = 4f;
        private const float BannerHeight = 70f;

        private static readonly Color DefaultAccent = new Color(0.83f, 0.68f, 0.21f);
        private static readonly Color SectionBgColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);

        // ── Data ──
        private readonly CodexWindow parentWindow;
        private readonly List<WorldSettlementDef> settlementDefs;
        private WorldSettlementDef selectedDef;

        // ── Scroll state ──
        private Vector2 leftScroll;
        private Vector2 centerScroll;
        private Vector2 rightScroll;

        // ── Truncation cache ──
        private readonly Dictionary<string, string> truncateCache = new Dictionary<string, string>();

        // ── Banner cache ──
        private readonly Dictionary<string, Texture2D> bannerCache = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> bannerLookedUp = new HashSet<string>();

        public string TabLabel => "FCCodexTabSettlements".Translate();
        public bool HasRightPane => true;

        public CodexTab_Settlements(CodexWindow window)
        {
            parentWindow = window;
            settlementDefs = DefDatabase<WorldSettlementDef>.AllDefsListForReading
                .Where(d => d.available)
                .OrderBy(d => d.LabelCap.RawText)
                .ToList();

            if (settlementDefs.Count > 0)
                selectedDef = settlementDefs[0];
        }

        public void SelectDef(WorldSettlementDef def)
        {
            if (def is object && settlementDefs.Contains(def))
            {
                selectedDef = def;
                centerScroll = Vector2.zero;
                rightScroll = Vector2.zero;
            }
        }

        public void OnTabSelected() { }
        public void OnTabDeselected() { }

        private Color GetAccent(WorldSettlementDef def)
        {
            return def.accentColor ?? DefaultAccent;
        }

        // ══════════════════════════════════════════════════════════════
        // LEFT PANE
        // ══════════════════════════════════════════════════════════════

        public void DrawLeftPane(Rect rect)
        {
            float totalHeight = settlementDefs.Count * EntryRowHeight;
            float viewWidth = rect.width - (totalHeight > rect.height ? 16f : 0f);
            Rect viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

            Widgets.BeginScrollView(rect, ref leftScroll, viewRect);
            float curY = 0f;

            foreach (WorldSettlementDef def in settlementDefs)
            {
                Rect entryRect = new Rect(0f, curY, viewWidth, EntryRowHeight);
                bool isSelected = selectedDef == def;
                Color accent = GetAccent(def);

                if (isSelected)
                    Widgets.DrawBoxSolid(entryRect, accent * new Color(1f, 1f, 1f, 0.35f));
                else if (Mouse.IsOver(entryRect))
                    Widgets.DrawBoxSolid(entryRect, accent * new Color(1f, 1f, 1f, 0.15f));

                Color barColor = isSelected ? accent : accent * new Color(1f, 1f, 1f, 0.4f);
                Widgets.DrawBoxSolid(new Rect(entryRect.x, entryRect.y, AccentBarWidth, entryRect.height), barColor);

                float textX = entryRect.x + AccentBarWidth + Margin;
                float labelWidth = entryRect.xMax - textX - SmallMargin;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = isSelected ? Color.white : new Color(0.9f, 0.9f, 0.9f);
                string fullLabel = def.LabelCap;
                string truncated = fullLabel.Truncate(labelWidth, truncateCache);
                Widgets.Label(new Rect(textX, entryRect.y, labelWidth, entryRect.height), truncated);
                if (truncated != fullLabel)
                    TooltipHandler.TipRegion(entryRect, fullLabel);
                ResetText();

                if (Widgets.ButtonInvisible(entryRect))
                {
                    selectedDef = def;
                    centerScroll = Vector2.zero;
                    rightScroll = Vector2.zero;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }

                curY += EntryRowHeight;
            }

            Widgets.EndScrollView();
            ResetText();
        }

        // ══════════════════════════════════════════════════════════════
        // CENTER PANE
        // ══════════════════════════════════════════════════════════════

        public void DrawCenterPane(Rect rect)
        {
            if (selectedDef is null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "FCCodexSelectSettlement".Translate());
                ResetText();
                return;
            }

            float contentWidth = rect.width - 16f;
            float contentHeight = CalculateCenterHeight(contentWidth);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            Color accent = GetAccent(selectedDef);

            Widgets.BeginScrollView(rect, ref centerScroll, viewRect);
            float curY = 0f;

            // ── Title ──
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Widgets.Label(new Rect(0f, curY, contentWidth, 30f), selectedDef.LabelCap);
            ResetText();
            curY += 30f;

            // Accent gradient line
            TexLoad.DrawHorizontalGradient(new Rect(0f, curY, contentWidth, 2f), accent);
            curY += 2f + Margin;

            // ── Description ──
            if (!selectedDef.description.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                Rect descRect = new Rect(0f, curY, contentWidth, 100f);
                Widgets.LabelCacheHeight(ref descRect, selectedDef.description);
                curY += descRect.height + Margin;
                ResetText();
            }

            // ── Key Stats ──
            curY = DrawSection(curY, contentWidth, "FCCodexSettlementStats".Translate(), accent, DrawKeyStats);

            // ── Stat Modifiers ──
            TaggedString statDesc = FCStatModifier.GetDescription(selectedDef.statModifiers);
            if (!statDesc.RawText.NullOrEmpty())
                curY = DrawSection(curY, contentWidth, "FCCodexSettlementStatModifiers".Translate(), accent, (y, w) => DrawStatModifiers(y, w, statDesc));

            // ── Tech Requirements ──
            if (selectedDef.techLevel != TechLevel.Undefined || selectedDef.researchProjects.Count > 0)
                curY = DrawSection(curY, contentWidth, "FCCodexSettlementTechReqs".Translate(), accent, DrawTechRequirements);

            // ── Biome Restrictions ──
            curY = DrawSection(curY, contentWidth, "FCCodexSettlementBiomes".Translate(), accent, DrawBiomeRestrictions);

            Widgets.EndScrollView();
            ResetText();
        }

        // ══════════════════════════════════════════════════════════════
        // RIGHT PANE
        // ══════════════════════════════════════════════════════════════

        public void DrawRightPane(Rect rect)
        {
            float estHeight = CalculateRightPaneHeight(rect.width - 16f);
            float contentWidth = rect.width - (estHeight > rect.height ? 16f : 0f);
            float contentHeight = CalculateRightPaneHeight(contentWidth);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

            Widgets.BeginScrollView(rect, ref rightScroll, viewRect);
            float curY = 0f;

            // ── Banner ──
            if (selectedDef is object)
            {
                Texture2D banner = GetBanner(selectedDef);
                if (banner is object)
                {
                    Rect bannerRect = new Rect(0f, curY, contentWidth, BannerHeight);
                    GUI.DrawTexture(bannerRect, banner, ScaleMode.ScaleToFit);
                    curY += BannerHeight + SmallMargin;
                }

                // Mod name
                string modName = selectedDef.modContentPack?.ModMetaData?.Name ?? "";
                if (!modName.NullOrEmpty())
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Color.gray;
                    Widgets.Label(new Rect(0f, curY, contentWidth, 20f), modName);
                    ResetText();
                    curY += 24f;
                }

                GUI.color = Color.gray;
                Widgets.DrawLineHorizontal(Margin, curY, contentWidth - Margin * 2);
                GUI.color = Color.white;
                curY += Margin;
            }

            // ── Available Resources ──
            if (selectedDef is object && selectedDef.resources.Count > 0)
            {
                Color accent = GetAccent(selectedDef);
                curY = DrawSection(curY, contentWidth, "FCCodexSettlementResources".Translate(), accent, DrawResources);
            }

            Widgets.EndScrollView();
            ResetText();
        }

        // ══════════════════════════════════════════════════════════════
        // SECTION DRAWING HELPERS
        // ══════════════════════════════════════════════════════════════

        private delegate float SectionDrawer(float curY, float width);

        private float DrawSection(float startY, float width, string header, Color accent, SectionDrawer drawer)
        {
            float curY = startY;

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

        private float DrawKeyStats(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            curY = DrawStatLine(curY, x, textW, "FCCodexSettlementWorkers".Translate(
                selectedDef.workersMaxBase.ToString(), selectedDef.workersMaxMult.ToString()));

            if (selectedDef.maxSettlementLevel < 99)
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementMaxLevel".Translate(selectedDef.maxSettlementLevel.ToString()));

            // Building slots progression — query the extension at representative levels
            string slotsProgression = BuildSlotsProgression(selectedDef);
            curY = DrawStatLine(curY, x, textW, "FCCodexSettlementBuildingSlots".Translate(slotsProgression));

            if (selectedDef.planetLayers.Count > 0)
            {
                string layers = string.Join(", ", selectedDef.planetLayers.Select(p => p.LabelCap.RawText).ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementPlanetLayer".Translate(layers));
            }

            string yesStr = "FCCodexYes".Translate();
            string noStr = "FCCodexNo".Translate();

            curY = DrawStatLine(curY, x, textW, "FCCodexSettlementManualBattle".Translate(
                selectedDef.supportsManualBattle ? yesStr : noStr));

            curY = DrawStatLine(curY, x, textW, "FCCodexSettlementCanBeRaided".Translate(
                selectedDef.canBeRaided ? yesStr : noStr));

            if (selectedDef.raidTargetingWeight != 1.0f)
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementRaidWeight".Translate(
                    selectedDef.raidTargetingWeight.ToString("F1")));

            return curY;
        }

        /// <summary>
        /// Builds a compact building slot progression string by querying the settlement type extension
        /// at representative levels and only showing levels where the slot count changes.
        /// </summary>
        private static string BuildSlotsProgression(WorldSettlementDef def)
        {
            SettlementTypeExtension ext = def.GetSettlementTypeExtension();
            if (ext is null)
                return def.baseUnlockedBuildings.ToString();

            int maxLevel = def.maxSettlementLevel < 99 ? def.maxSettlementLevel : 20;
            int maxCount = def.maxBuildingCount;

            // Sample levels 0 through maxLevel
            List<int> levels = new List<int>();
            List<int> slots = new List<int>();
            int lastSlots = -1;

            for (int lvl = 0; lvl <= maxLevel; lvl++)
            {
                int s = ext.GetBuildingSlots(lvl, maxCount);
                if (s != lastSlots)
                {
                    levels.Add(lvl);
                    slots.Add(s);
                    lastSlots = s;
                    // If we've hit max, no need to continue
                    if (s >= maxCount) break;
                }
            }

            if (levels.Count <= 1)
                return slots.Count > 0 ? slots[0].ToString() : "0";

            // Format: "3 (lvl 1) → 5 (lvl 5) → 8 (lvl 10)"
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < levels.Count; i++)
            {
                if (i > 0) sb.Append(" \u2192 ");
                sb.Append(slots[i]);
                sb.Append(" (lvl ");
                sb.Append(levels[i]);
                sb.Append(")");
            }
            return sb.ToString();
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

        private float DrawResources(float curY, float width)
        {
            float x = AccentBarWidth + Margin;

            foreach (ResourceAvailability ra in selectedDef.resources)
            {
                if (ra.resourceDef is null) continue;

                Rect iconRect = new Rect(x, curY + (ResourceRowHeight - 20f) * 0.5f, 20f, 20f);
                GUI.DrawTexture(iconRect, ra.resourceDef.Icon);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;

                string label = ra.resourceDef.LabelCap;
                if (ra.additive != 0 && !double.IsNaN(ra.additive))
                    label += " (+" + ra.additive.ToString("F1") + " base)";
                if (ra.multiplier != 1)
                    label += " (\u00d7" + ra.multiplier.ToString("F1") + ")";

                Widgets.Label(new Rect(iconRect.xMax + SmallMargin, curY, width - iconRect.xMax - SmallMargin - Margin, ResourceRowHeight), label);
                ResetText();

                curY += ResourceRowHeight;
            }

            return curY;
        }

        private float DrawStatModifiers(float curY, float width, TaggedString desc)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            float h = Text.CalcHeight(desc, textW);
            Widgets.Label(new Rect(x, curY, textW, h), desc);
            ResetText();

            return curY + h;
        }

        private float DrawTechRequirements(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            if (selectedDef.techLevel != TechLevel.Undefined)
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementTechLevel".Translate(selectedDef.techLevel.ToStringHuman()));

            foreach (ResearchProjectDef rp in selectedDef.researchProjects)
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementResearch".Translate(rp.LabelCap));

            return curY;
        }

        private float DrawBiomeRestrictions(float curY, float width)
        {
            float x = AccentBarWidth + Margin;
            float textW = width - x - Margin;

            if (selectedDef.blockedBiomes.Count > 0)
            {
                string biomes = string.Join(", ", selectedDef.blockedBiomes.Select(b => b.LabelCap.RawText).ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementBlockedBiomes".Translate(biomes));
            }
            else if (selectedDef.allowedBiomes.Count > 0)
            {
                string biomes = string.Join(", ", selectedDef.allowedBiomes.Select(b => b.LabelCap.RawText).ToArray());
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementAllowedBiomes".Translate(biomes));
            }
            else
            {
                curY = DrawStatLine(curY, x, textW, "FCCodexSettlementAllBiomes".Translate());
            }

            return curY;
        }

        // ══════════════════════════════════════════════════════════════
        // BANNER HELPER
        // ══════════════════════════════════════════════════════════════

        private Texture2D GetBanner(WorldSettlementDef def)
        {
            string modId = def.modContentPack?.PackageId;
            if (modId.NullOrEmpty()) return null;

            if (bannerLookedUp.Contains(modId))
            {
                Texture2D cached;
                bannerCache.TryGetValue(modId, out cached);
                return cached;
            }
            bannerLookedUp.Add(modId);

            PatchNoteDef patchNote = PatchNoteDef.GetLatestForMod(modId);
            Texture2D banner = patchNote?.BannerImage;
            if (banner is object)
                bannerCache[modId] = banner;
            return banner;
        }

        // ══════════════════════════════════════════════════════════════
        // HEIGHT CALCULATIONS
        // ══════════════════════════════════════════════════════════════

        private float CalculateCenterHeight(float width)
        {
            if (selectedDef is null) return 0f;

            float total = 30f + 2f + Margin; // title + accent line

            if (!selectedDef.description.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                total += Text.CalcHeight(selectedDef.description, width) + Margin;
            }

            // Key Stats section
            total += SectionHeaderHeight + SmallMargin;
            int statLines = 4; // workers, building slots, manual battle, can be raided
            if (selectedDef.maxSettlementLevel < 99) statLines++;
            if (selectedDef.planetLayers.Count > 0) statLines++;
            if (selectedDef.raidTargetingWeight != 1.0f) statLines++;
            total += statLines * StatRowHeight + Margin;

            // Stat Modifiers section
            TaggedString statDesc = FCStatModifier.GetDescription(selectedDef.statModifiers);
            if (!statDesc.RawText.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                total += SectionHeaderHeight + SmallMargin + Text.CalcHeight(statDesc, width - AccentBarWidth - Margin * 2) + Margin;
            }

            // Tech Requirements
            if (selectedDef.techLevel != TechLevel.Undefined || selectedDef.researchProjects.Count > 0)
            {
                int techLines = 0;
                if (selectedDef.techLevel != TechLevel.Undefined) techLines++;
                techLines += selectedDef.researchProjects.Count;
                total += SectionHeaderHeight + SmallMargin + techLines * StatRowHeight + Margin;
            }

            // Biome Restrictions
            total += SectionHeaderHeight + SmallMargin + StatRowHeight + Margin;

            return total + 50f;
        }

        private float CalculateRightPaneHeight(float width)
        {
            float total = 0f;

            if (selectedDef is object)
            {
                if (GetBanner(selectedDef) is object)
                    total += BannerHeight + SmallMargin;
                string modName = selectedDef.modContentPack?.ModMetaData?.Name ?? "";
                if (!modName.NullOrEmpty())
                    total += 24f;
                total += Margin; // divider

                // Resources
                if (selectedDef.resources.Count > 0)
                    total += SectionHeaderHeight + SmallMargin + selectedDef.resources.Count * ResourceRowHeight + Margin;
            }

            return total + 50f;
        }

        private void ResetText()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}
