using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    /// <summary>
    /// In-game reference window that explains Empire mechanics, formulas, and UI navigation.
    /// Entries are loaded from <see cref="CodexEntryDef"/> via DefDatabase and grouped
    /// first by modId (top-level), then by category, then sorted by displayOrder.
    /// </summary>
    public class CodexWindow : Window
    {
        // ── Layout constants ──
        private const float LeftPaneWidth = 220f;
        private const float DividerWidth = 1f;
        private const float margin = 8f;
        private const float TitleBarHeight = 35f;
        private const float GroupHeaderHeight = 30f;
        private const float CategoryHeaderHeight = 26f;
        private const float EntryRowHeight = 24f;
        private const float ImageMaxHeight = 300f;
        private const float ImageNavButtonSize = 28f;
        private const float SeeAlsoButtonHeight = 24f;
        private const float IconSize = 20f;

        private static readonly Color GroupBgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        private static readonly Color CategoryBgColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);
        private static readonly Color SelectedEntryColor = new Color(0.3f, 0.4f, 0.55f, 0.5f);
        private static readonly Color HoverColor = new Color(0.25f, 0.25f, 0.3f, 0.3f);
        private static readonly Color DynamicContentBg = new Color(0.12f, 0.18f, 0.12f, 0.3f);
        private static readonly Color SeeAlsoColor = new Color(0.4f, 0.6f, 0.9f);

        public override Vector2 InitialSize => new Vector2(800f, 650f);

        // ── Data model ──
        private readonly List<ModGroup> modGroups;
        private CodexEntryDef selectedEntry;

        // ── Scroll state ──
        private Vector2 leftScroll;
        private Vector2 rightScroll;

        // ── Image carousel state ──
        private int currentImageIndex;

        // ── Expand/collapse state ──
        private readonly HashSet<string> expandedMods = new HashSet<string>();
        private readonly HashSet<string> expandedCategories = new HashSet<string>();

        private class ModGroup
        {
            public string modId;
            public string modName;
            public List<CategoryGroup> categories = new List<CategoryGroup>();
        }

        private class CategoryGroup
        {
            public string category;
            public string modId;
            public List<CodexEntryDef> entries = new List<CodexEntryDef>();
        }

        public CodexWindow()
        {
            doCloseButton = false;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = false;
            draggable = true;
            resizeable = true;

            modGroups = BuildModGroups();

            // Auto-expand the first mod group and its categories
            if (modGroups.Count > 0)
            {
                expandedMods.Add(modGroups[0].modId);
                foreach (CategoryGroup cat in modGroups[0].categories)
                    expandedCategories.Add(CatKey(modGroups[0].modId, cat.category));

                // Auto-select first entry
                if (modGroups[0].categories.Count > 0 && modGroups[0].categories[0].entries.Count > 0)
                    SelectEntry(modGroups[0].categories[0].entries[0]);
            }
        }

        /// <summary>
        /// Opens the Codex and pre-selects the given entry.
        /// </summary>
        public CodexWindow(CodexEntryDef preselect) : this()
        {
            if (preselect is object)
                SelectEntry(preselect);
        }

        private void SelectEntry(CodexEntryDef entry)
        {
            selectedEntry = entry;
            currentImageIndex = 0;
            rightScroll = Vector2.zero;

            // Ensure its mod and category are expanded
            expandedMods.Add(entry.modId);
            expandedCategories.Add(CatKey(entry.modId, entry.category));
        }

        private static string CatKey(string modId, string category) => modId + "|" + category;

        private static List<ModGroup> BuildModGroups()
        {
            List<CodexEntryDef> allDefs = DefDatabase<CodexEntryDef>.AllDefsListForReading.ToList();

            // Group by modId, then category, then sort by displayOrder
            var byMod = new Dictionary<string, ModGroup>();
            foreach (CodexEntryDef def in allDefs)
            {
                string mid = def.modId.NullOrEmpty() ? "unknown" : def.modId;
                ModGroup mg;
                if (!byMod.TryGetValue(mid, out mg))
                {
                    mg = new ModGroup { modId = mid, modName = def.ModName };
                    byMod[mid] = mg;
                }

                CategoryGroup cg = mg.categories.FirstOrDefault(c => c.category == def.category);
                if (cg is null)
                {
                    cg = new CategoryGroup { category = def.category, modId = mid };
                    mg.categories.Add(cg);
                }
                cg.entries.Add(def);
            }

            // Sort entries within each category
            foreach (ModGroup mg in byMod.Values)
            {
                foreach (CategoryGroup cg in mg.categories)
                    cg.entries.SortBy(e => e.displayOrder);
                mg.categories.SortBy(c => c.entries.Count > 0 ? c.entries[0].displayOrder : 0);
            }

            // Put base Empire mod first, then alphabetical
            List<ModGroup> result = byMod.Values.ToList();
            result.SortBy(mg => mg.modId == "matathias.empire" ? "!" : mg.modName);
            return result;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title bar
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, TitleBarHeight);
            DrawTitle(titleRect);

            float bodyTop = inRect.y + TitleBarHeight + margin;
            float bodyHeight = inRect.height - TitleBarHeight - margin;

            // Left pane
            Rect leftRect = new Rect(inRect.x, bodyTop, LeftPaneWidth, bodyHeight);
            DrawLeftPane(leftRect);

            // Divider
            GUI.color = Color.gray;
            Widgets.DrawLineVertical(leftRect.xMax + margin * 0.5f, bodyTop, bodyHeight);
            GUI.color = Color.white;

            // Right pane
            float rightX = leftRect.xMax + margin + DividerWidth;
            Rect rightRect = new Rect(rightX, bodyTop, inRect.xMax - rightX, bodyHeight);
            DrawRightPane(rightRect);
        }

        // ── Title ──
        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, "FCCodexTitle".Translate());
            ResetText();
        }

        // ── Left Pane: mod groups → categories → entries ──
        private void DrawLeftPane(Rect rect)
        {
            float totalHeight = CalculateLeftPaneHeight();
            Rect viewRect = new Rect(0f, 0f, rect.width - (totalHeight > rect.height ? 16f : 0f), totalHeight);

            Widgets.BeginScrollView(rect, ref leftScroll, viewRect);
            float curY = 0f;

            foreach (ModGroup mg in modGroups)
            {
                bool modExpanded = expandedMods.Contains(mg.modId);

                // Mod group header
                Rect groupRect = new Rect(0f, curY, viewRect.width, GroupHeaderHeight);
                Widgets.DrawBoxSolid(groupRect, GroupBgColor);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;
                Rect labelRect = new Rect(groupRect.x + margin, groupRect.y, groupRect.width - margin * 2 - IconSize, groupRect.height);
                Widgets.Label(labelRect, mg.modName);

                // Expand/collapse icon
                Rect arrowRect = new Rect(groupRect.xMax - IconSize - 2f, groupRect.y + (GroupHeaderHeight - IconSize) * 0.5f, IconSize, IconSize);
                Widgets.DrawTextureFitted(arrowRect, modExpanded ? TexButton.Collapse : TexButton.Reveal, 1f);

                if (Widgets.ButtonInvisible(groupRect))
                {
                    if (modExpanded)
                        expandedMods.Remove(mg.modId);
                    else
                        expandedMods.Add(mg.modId);
                    (modExpanded ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
                }

                curY += GroupHeaderHeight + 2f;

                if (!modExpanded) continue;

                foreach (CategoryGroup cg in mg.categories)
                {
                    string catKey = CatKey(mg.modId, cg.category);
                    bool catExpanded = expandedCategories.Contains(catKey);

                    // Category header (indented)
                    Rect catRect = new Rect(10f, curY, viewRect.width - 10f, CategoryHeaderHeight);
                    Widgets.DrawBoxSolid(catRect, CategoryBgColor);

                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = new Color(0.85f, 0.85f, 0.7f);
                    Widgets.Label(new Rect(catRect.x + margin, catRect.y, catRect.width - margin * 2 - IconSize, catRect.height), cg.category);

                    Rect catArrow = new Rect(catRect.xMax - IconSize - 2f, catRect.y + (CategoryHeaderHeight - IconSize) * 0.5f, IconSize, IconSize);
                    GUI.color = Color.white;
                    Widgets.DrawTextureFitted(catArrow, catExpanded ? TexButton.Collapse : TexButton.Reveal, 1f);

                    if (Widgets.ButtonInvisible(catRect))
                    {
                        if (catExpanded)
                            expandedCategories.Remove(catKey);
                        else
                            expandedCategories.Add(catKey);
                        (catExpanded ? SoundDefOf.TabClose : SoundDefOf.TabOpen).PlayOneShotOnCamera();
                    }

                    curY += CategoryHeaderHeight + 1f;

                    if (!catExpanded) continue;

                    foreach (CodexEntryDef entry in cg.entries)
                    {
                        Rect entryRect = new Rect(20f, curY, viewRect.width - 20f, EntryRowHeight);

                        // Selection highlight
                        if (selectedEntry == entry)
                            Widgets.DrawBoxSolid(entryRect, SelectedEntryColor);
                        else if (Mouse.IsOver(entryRect))
                            Widgets.DrawBoxSolid(entryRect, HoverColor);

                        // Icon + label
                        float textX = entryRect.x + margin;
                        if (entry.Icon is object)
                        {
                            Rect iconRect = new Rect(entryRect.x + 4f, entryRect.y + (EntryRowHeight - 16f) * 0.5f, 16f, 16f);
                            GUI.DrawTexture(iconRect, entry.Icon);
                            textX = iconRect.xMax + 4f;
                        }

                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.MiddleLeft;
                        GUI.color = selectedEntry == entry ? Color.white : new Color(0.9f, 0.9f, 0.9f);
                        Widgets.Label(new Rect(textX, entryRect.y, entryRect.xMax - textX - 4f, entryRect.height), entry.LabelCap);
                        ResetText();

                        if (Widgets.ButtonInvisible(entryRect))
                        {
                            SelectEntry(entry);
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }

                        curY += EntryRowHeight;
                    }
                }
            }

            Widgets.EndScrollView();
            ResetText();
        }

        private float CalculateLeftPaneHeight()
        {
            float total = 0f;
            foreach (ModGroup mg in modGroups)
            {
                total += GroupHeaderHeight + 2f;
                if (!expandedMods.Contains(mg.modId)) continue;

                foreach (CategoryGroup cg in mg.categories)
                {
                    total += CategoryHeaderHeight + 1f;
                    if (expandedCategories.Contains(CatKey(mg.modId, cg.category)))
                        total += cg.entries.Count * EntryRowHeight;
                }
            }
            return total;
        }

        // ── Right Pane: selected entry detail ──
        private void DrawRightPane(Rect rect)
        {
            if (selectedEntry is null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "FCCodexSelectEntry".Translate());
                ResetText();
                return;
            }

            // Calculate content height first, then scroll
            float contentWidth = rect.width - 16f;
            float contentHeight = CalculateRightPaneHeight(contentWidth);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

            Widgets.BeginScrollView(rect, ref rightScroll, viewRect);
            float curY = 0f;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Rect titleRect = new Rect(0f, curY, contentWidth, 30f);
            Widgets.Label(titleRect, selectedEntry.LabelCap);
            ResetText();
            curY += 32f;

            // Category + Mod badge
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.UpperLeft;
            string meta = selectedEntry.category + "  \u2022  " + selectedEntry.ModName;
            Widgets.Label(new Rect(0f, curY, contentWidth, 20f), meta);
            ResetText();
            curY += 22f;

            // Divider
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(0f, curY, contentWidth);
            GUI.color = Color.white;
            curY += margin;

            // Image carousel (above description)
            List<Texture2D> images = selectedEntry.Images;
            if (images.Count > 0)
            {
                curY = DrawImageCarousel(curY, contentWidth, images);
                curY += margin;
            }

            // Description (body text from Def.description)
            if (!selectedEntry.description.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                Rect descRect = new Rect(0f, curY, contentWidth, 100f);
                Widgets.LabelCacheHeight(ref descRect, selectedEntry.description);
                curY += descRect.height + margin;
                ResetText();
            }

            // Dynamic content
            ICodexDynamicProvider provider = selectedEntry.DynamicProvider;
            if (provider is object)
            {
                FactionFC faction = FactionCache.FactionComp;
                if (faction is object)
                {
                    string dynamic = null;
                    try
                    {
                        dynamic = provider.GetDynamicContent(faction);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error($"CodexWindow: dynamic provider for '{selectedEntry.defName}' threw: {ex}");
                    }

                    if (!dynamic.NullOrEmpty())
                    {
                        // Section header
                        Text.Font = GameFont.Small;
                        GUI.color = new Color(0.7f, 0.9f, 0.7f);
                        Widgets.Label(new Rect(0f, curY, contentWidth, 20f), "FCCodexLiveData".Translate());
                        ResetText();
                        curY += 22f;

                        // Measure height, draw background, then draw text
                        Text.Font = GameFont.Small;
                        float dynHeight = Text.CalcHeight(dynamic, contentWidth);
                        Rect bgRect = new Rect(-4f, curY - 2f, contentWidth + 8f, dynHeight + 4f);
                        Widgets.DrawBoxSolid(bgRect, DynamicContentBg);
                        Rect dynRect = new Rect(0f, curY, contentWidth, dynHeight);
                        Widgets.Label(dynRect, dynamic);
                        ResetText();
                        curY += dynHeight + margin;
                    }
                }
            }

            // See Also links
            if (!selectedEntry.seeAlso.NullOrEmpty())
            {
                curY += margin;
                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, curY, contentWidth, 20f), "FCCodexSeeAlso".Translate());
                ResetText();
                curY += 22f;

                foreach (string refName in selectedEntry.seeAlso)
                {
                    CodexEntryDef linked = DefDatabase<CodexEntryDef>.GetNamedSilentFail(refName);
                    if (linked is null) continue;

                    Rect linkRect = new Rect(margin, curY, contentWidth - margin, SeeAlsoButtonHeight);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = SeeAlsoColor;
                    Widgets.Label(linkRect, "\u2192 " + linked.LabelCap);

                    if (Mouse.IsOver(linkRect))
                        Widgets.DrawHighlight(linkRect);

                    if (Widgets.ButtonInvisible(linkRect))
                    {
                        SelectEntry(linked);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }

                    ResetText();
                    curY += SeeAlsoButtonHeight;
                }
            }

            Widgets.EndScrollView();
            ResetText();
        }

        private float DrawImageCarousel(float startY, float width, List<Texture2D> images)
        {
            float curY = startY;
            currentImageIndex = Mathf.Clamp(currentImageIndex, 0, images.Count - 1);
            Texture2D img = images[currentImageIndex];

            // Scale image to fit width while maintaining aspect ratio, capped at max height
            float aspect = (float)img.width / img.height;
            float drawWidth = width;
            float drawHeight = drawWidth / aspect;
            if (drawHeight > ImageMaxHeight)
            {
                drawHeight = ImageMaxHeight;
                drawWidth = drawHeight * aspect;
            }

            // Center the image
            float imgX = (width - drawWidth) * 0.5f;
            Rect imgRect = new Rect(imgX, curY, drawWidth, drawHeight);
            GUI.DrawTexture(imgRect, img, ScaleMode.ScaleToFit);
            curY += drawHeight + 4f;

            // Navigation (only if multiple images)
            if (images.Count > 1)
            {
                float navWidth = ImageNavButtonSize * 2 + 60f;
                float navX = (width - navWidth) * 0.5f;

                // Left arrow
                Rect leftBtn = new Rect(navX, curY, ImageNavButtonSize, ImageNavButtonSize);
                if (currentImageIndex > 0)
                {
                    if (Widgets.ButtonText(leftBtn, "<"))
                    {
                        currentImageIndex--;
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                }

                // Page indicator
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect pageRect = new Rect(leftBtn.xMax, curY, 60f, ImageNavButtonSize);
                Widgets.Label(pageRect, (currentImageIndex + 1) + " / " + images.Count);
                ResetText();

                // Right arrow
                Rect rightBtn = new Rect(pageRect.xMax, curY, ImageNavButtonSize, ImageNavButtonSize);
                if (currentImageIndex < images.Count - 1)
                {
                    if (Widgets.ButtonText(rightBtn, ">"))
                    {
                        currentImageIndex++;
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                }

                curY += ImageNavButtonSize + 2f;
            }

            return curY;
        }

        private float CalculateRightPaneHeight(float width)
        {
            if (selectedEntry is null) return 0f;

            float total = 32f + 22f + margin; // title + meta + divider

            // Images
            List<Texture2D> images = selectedEntry.Images;
            if (images.Count > 0)
            {
                Texture2D img = images[Mathf.Clamp(currentImageIndex, 0, images.Count - 1)];
                float aspect = (float)img.width / img.height;
                float drawHeight = Mathf.Min(width / aspect, ImageMaxHeight);
                total += drawHeight + 4f + margin;
                if (images.Count > 1)
                    total += ImageNavButtonSize + 2f;
            }

            // Description
            if (!selectedEntry.description.NullOrEmpty())
            {
                total += Text.CalcHeight(selectedEntry.description, width) + margin;
            }

            // Dynamic content (estimate — recalculated at render)
            if (selectedEntry.DynamicProvider is object)
            {
                total += 22f + 200f + margin; // header + estimated body
            }

            // See Also
            if (!selectedEntry.seeAlso.NullOrEmpty())
            {
                total += margin + 22f + selectedEntry.seeAlso.Count * SeeAlsoButtonHeight;
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
