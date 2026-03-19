using FactionColonies.util;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    public static class DebugActionsMisc
    {
        [DebugAction("Mods", "Display Empire patch notes", allowedGameStates = AllowedGameStates.Entry)]
        public static void PatchNotesDisplayWindow() => Find.WindowStack.Add(new PatchNotesDisplayWindow());
    }

    class PatchNotesDisplayWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(1200f + (StandardMargin * 2), 595f + (StandardMargin * 2));

        private const float HeaderHeight = 45f;
        private const float TitleBarHeight = 30f;
        private const float Margin = 5f;
        private const float DividerPad = 15f;
        private const float BadgeWidth = 55f;
        private const float BadgeHeight = 22f;
        private const float DateWidth = 90f;
        private const float IconSize = 45f;
        private const float LinkButtonSize = 24f;

        private static readonly List<PatchNoteDef> patchNoteDefs = DefDatabase<PatchNoteDef>.AllDefsListForReading.ListFullCopy();

        private readonly string title = "FCPatchNotesWindowTitle".Translate();

        // Left panel state
        private HashSet<int> expandedDefs = new HashSet<int>();
        private Dictionary<int, float> expandedHeights = new Dictionary<int, float>();
        private int selectedDef = -1;
        private bool shouldRefreshHeight = true;
        private float scrollViewHeight = 0f;
        private Vector2 patchNoteScrollPos = new Vector2();

        // Right panel state
        private int displayedImage = -1;
        private Vector2 imageDescScrollPos = new Vector2();

        // Scrolling bug fix
        private bool firstRun = true;
        private bool fixDone = false;

        private Texture2D defaultImage;

        // Badge colors
        private static readonly Color BadgeColorMajor = new Color(0.85f, 0.65f, 0.13f);
        private static readonly Color BadgeColorMinor = new Color(0.3f, 0.5f, 0.9f);
        private static readonly Color BadgeColorHotfix = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color BadgeColorPatch = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color OrangeColor = Color.Lerp(Color.yellow, Color.red, 0.5f);

        public PatchNotesDisplayWindow()
        {
            patchNoteDefs.SortBy((def) => def.ReleaseDate, (def) => def.ToOldEmpireVersion);
            patchNoteDefs.Reverse();

            // Auto-expand unread entries
            for (int i = 0; i < patchNoteDefs.Count; i++)
            {
                if (patchNoteDefs[i].IsNewerThan(FCSettings.lastSeenVersionMajor,
                    FCSettings.lastSeenVersionMinor, FCSettings.lastSeenVersionPatch))
                {
                    expandedDefs.Add(i);
                }
            }

            // Default right panel to newest entry
            if (patchNoteDefs.Count > 0)
            {
                selectedDef = 0;
            }
        }

        public PatchNotesDisplayWindow(string title) : this() => this.title = title;

        public override void PostClose()
        {
            base.PostClose();
            if (patchNoteDefs.Count > 0)
            {
                PatchNoteDef latest = patchNoteDefs[0];
                FCSettings.lastSeenVersionMajor = latest.Major;
                FCSettings.lastSeenVersionMinor = latest.Minor;
                FCSettings.lastSeenVersionPatch = latest.Patch;
                LoadedModManager.GetMod<FactionColoniesMod>().WriteSettings();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Compute layout from inRect
            float dividerX = inRect.width * 0.55f;
            Rect titleRect = new Rect(inRect.x + Margin, inRect.y, inRect.width - Margin * 2, TitleBarHeight);
            float contentTop = inRect.y + TitleBarHeight + DividerPad;
            float contentHeight = inRect.height - TitleBarHeight - DividerPad;
            Rect leftPanel = new Rect(inRect.x + Margin, contentTop, dividerX - Margin * 2, contentHeight);
            Rect rightPanel = new Rect(dividerX + DividerPad, contentTop, inRect.width - dividerX - DividerPad - Margin, contentHeight);

            FixScrollingBug();
            CalculateScrollViewSize(leftPanel.width - 17f);
            DrawTitle(titleRect);
            DrawDividers(inRect, dividerX);
            DrawPatchNotes(leftPanel);
            DrawImageContent(rightPanel);
        }

        private void FixScrollingBug()
        {
            if (fixDone) return;

            if (!firstRun)
            {
                shouldRefreshHeight = true;
                fixDone = true;
            }
            else
            {
                firstRun = false;
            }
        }

        private void DrawTitle(Rect titleRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(titleRect, title);

            // Link buttons in title bar (right-aligned, before close button)
            if (patchNoteDefs.Count > 0)
            {
                PatchNoteDef anyDef = patchNoteDefs[0];
                float startX = titleRect.xMax - TitleBarHeight; // leave room for close button
                for (int i = anyDef.Links.Count - 1; i >= 0; i--)
                {
                    startX -= LinkButtonSize + Margin;
                    Rect btnRect = new Rect(startX, titleRect.y + 3f, LinkButtonSize, LinkButtonSize);
                    TooltipHandler.TipRegion(btnRect, anyDef.LinkButtonToolTips[i]);
                    if (Widgets.ButtonImage(btnRect, anyDef.LinkButtonImages[i]))
                    {
                        SteamUtility.OpenUrl(anyDef.Links[i]);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                }
            }

            // Close button
            if (Widgets.ButtonImage(titleRect.RightPartPixels(TitleBarHeight).ContractedBy(6f), TexButton.CloseXSmall))
            {
                Close();
            }

            ResetTextAndColor();
        }

        private void DrawDividers(Rect inRect, float dividerX)
        {
            GUI.color = Color.gray;
            float lineY = inRect.y + TitleBarHeight + (DividerPad * 0.5f) - 1f;
            Widgets.DrawLineHorizontal(inRect.x + Margin, lineY, inRect.width - Margin * 2);
            Widgets.DrawLineVertical(dividerX + DividerPad * 0.5f, lineY, inRect.height - TitleBarHeight - DividerPad * 0.5f);
            ResetTextAndColor();
        }

        private void DrawPatchNotes(Rect panelRect)
        {
            float scrollContentWidth = panelRect.width - 17f;
            Rect scrollViewRect = new Rect(0f, 0f, scrollContentWidth, scrollViewHeight);

            Widgets.BeginScrollView(panelRect, ref patchNoteScrollPos, scrollViewRect);

            float curY = 0f;

            for (int i = 0; i < patchNoteDefs.Count; i++)
            {
                PatchNoteDef def = patchNoteDefs[i];
                bool isExpanded = expandedDefs.Contains(i);
                bool isNew = def.IsNewerThan(FCSettings.lastSeenVersionMajor, FCSettings.lastSeenVersionMinor, FCSettings.lastSeenVersionPatch);
                bool isSelected = (i == selectedDef);

                // --- Header ---
                Rect headerRect = new Rect(0f, curY, scrollContentWidth, HeaderHeight);

                // Alternating highlight
                if (i % 2 == 0)
                    Widgets.DrawHighlight(headerRect);
                else
                    Widgets.DrawLightHighlight(headerRect);

                // Selection highlight
                if (isSelected)
                    Widgets.DrawHighlightSelected(headerRect);

                // New/unread border
                if (isNew)
                    GUI.color = Color.red;
                Widgets.DrawBox(headerRect);
                ResetTextAndColor();

                // Badge
                Rect badgeRect = new Rect(headerRect.x + Margin, headerRect.y + (HeaderHeight - BadgeHeight) * 0.5f, BadgeWidth, BadgeHeight);
                DrawTypeBadge(badgeRect, def.GetPatchNoteType);

                // Expand/collapse icon (rightmost)
                Rect iconRect = new Rect(headerRect.xMax - IconSize, headerRect.y, IconSize, HeaderHeight);
                Widgets.DrawTextureFitted(iconRect.ContractedBy(11f), isExpanded ? TexButton.Collapse : TexButton.Reveal, 1f);

                // Date (right-aligned, before icon)
                Rect dateRect = new Rect(iconRect.x - DateWidth - Margin, headerRect.y, DateWidth, HeaderHeight);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(dateRect, def.ReleaseDate.ToString("dd MMM yyyy"));
                ResetTextAndColor();

                // Title (between badge and date)
                float titleX = badgeRect.xMax + Margin;
                Rect titleRect = new Rect(titleX, headerRect.y, dateRect.x - titleX - Margin, HeaderHeight);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(titleRect, def.Title);
                ResetTextAndColor();

                // Click handling
                if (Widgets.ButtonInvisible(headerRect))
                {
                    if (isExpanded)
                    {
                        expandedDefs.Remove(i);
                        expandedHeights.Remove(i);
                        SoundDefOf.TabClose.PlayOneShotOnCamera();
                    }
                    else
                    {
                        expandedDefs.Add(i);
                        SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    }
                    selectedDef = i;
                    displayedImage = -1;
                    imageDescScrollPos = new Vector2();
                    shouldRefreshHeight = true;
                }

                curY += HeaderHeight + Margin;

                // --- Expanded body ---
                if (isExpanded)
                {
                    Text.Font = GameFont.Small;
                    string bodyText = def.CompletePatchNotesString;
                    float bodyWidth = scrollContentWidth - Margin * 4f;
                    Rect bodyRect = new Rect(Margin * 2f, curY, bodyWidth, 100f);
                    Widgets.LabelCacheHeight(ref bodyRect, bodyText);
                    expandedHeights[i] = bodyRect.height;
                    curY += bodyRect.height + Margin;
                    ResetTextAndColor();
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawTypeBadge(Rect rect, PatchNoteType type)
        {
            Color badgeColor;
            string badgeLabel;
            switch (type)
            {
                case PatchNoteType.Major:
                    badgeColor = BadgeColorMajor;
                    badgeLabel = "MAJOR";
                    break;
                case PatchNoteType.Minor:
                    badgeColor = BadgeColorMinor;
                    badgeLabel = "MINOR";
                    break;
                case PatchNoteType.Hotfix:
                    badgeColor = BadgeColorHotfix;
                    badgeLabel = "HOTFIX";
                    break;
                case PatchNoteType.Patch:
                    badgeColor = BadgeColorPatch;
                    badgeLabel = "PATCH";
                    break;
                default:
                    badgeColor = BadgeColorPatch;
                    badgeLabel = "???";
                    break;
            }

            Widgets.DrawBoxSolid(rect, badgeColor);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, badgeLabel);
            ResetTextAndColor();
        }

        private void DrawImageContent(Rect panelRect)
        {
            Widgets.DrawBox(panelRect);

            float imageHeight = panelRect.height * 0.5f;
            Rect imageArea = new Rect(panelRect.x + Margin, panelRect.y + Margin, panelRect.width - Margin * 2, imageHeight);
            Rect descArea = new Rect(panelRect.x + Margin, imageArea.yMax + Margin, panelRect.width - Margin * 2, panelRect.height - imageHeight - Margin * 3);

            Widgets.DrawLightHighlight(descArea);

            if (selectedDef == -1 || patchNoteDefs.Count == 0)
            {
                DrawImageContentMissing(imageArea, descArea, "FCSelectPatchNotes".Translate(), OrangeColor);
            }
            else
            {
                DrawImageContentOfDef(imageArea, descArea);
            }
        }

        private void DrawImageContentOfDef(Rect imageArea, Rect descArea)
        {
            PatchNoteDef def = patchNoteDefs[selectedDef];
            List<Texture2D> patchNoteImages = def.PatchNoteImages;

            if (patchNoteImages.NullOrEmpty())
            {
                DrawImageContentMissing(imageArea, descArea, "FCPatchNotesImagesMissing".Translate(), OrangeColor);
            }
            else
            {
                displayedImage = displayedImage == -1 ? 0 : displayedImage;
                Texture2D tex = patchNoteImages[displayedImage];
                GUI.DrawTexture(imageArea, tex, ScaleMode.ScaleToFit);

                DrawImageSelectors(imageArea, patchNoteImages.Count - 1);

                // Tooltip for zoom
                Rect tooltipRect = new Rect(imageArea);
                if (displayedImage == 0)
                {
                    tooltipRect.x += 50f;
                    tooltipRect.width -= 50f;
                }
                if (displayedImage == patchNoteImages.Count - 1)
                {
                    tooltipRect.width -= 50f;
                }
                TooltipHandler.TipRegion(tooltipRect, "FCPatchNotesImageZoomTooltip".Translate());
                if (Widgets.ButtonInvisible(tooltipRect))
                {
                    Find.WindowStack.Add(new ImageViewerForPatchNoteDefs(def, displayedImage));
                }

                // Description
                Text.Font = GameFont.Small;
                Widgets.LabelScrollable(descArea.ContractedBy(Margin), def.PatchNoteImageDescriptions[displayedImage], ref imageDescScrollPos);
            }

            ResetTextAndColor();
        }

        private void DrawImageSelectors(Rect imageArea, int max)
        {
            Rect lastBtn = new Rect(imageArea.x, imageArea.y, 50f, imageArea.height);
            Rect nextBtn = new Rect(imageArea.xMax - 50f, imageArea.y, 50f, imageArea.height);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;

            DrawImageSelector(nextBtn, ">", () => displayedImage < max, () => displayedImage++);
            DrawImageSelector(lastBtn, "<", () => displayedImage > 0, () => displayedImage--);

            ResetTextAndColor();
        }

        private void DrawImageSelector(Rect buttonRect, string buttonLabel, Func<bool> predicate, Action action)
        {
            if (!predicate()) return;

            Color guiColor = Color.black;
            guiColor.a = Mouse.IsOver(buttonRect) ? 0.8f : 0.3f;
            Widgets.DrawBoxSolid(buttonRect, guiColor);

            GUI.color = Color.white;
            if (!Mouse.IsOver(buttonRect))
            {
                Color faded = GUI.color;
                faded.a = 0.3f;
                GUI.color = faded;
            }

            if (Widgets.ButtonInvisible(buttonRect))
            {
                action();
                SoundDefOf.Click.PlayOneShotOnCamera();
                imageDescScrollPos = new Vector2();
            }

            Widgets.Label(buttonRect, buttonLabel);
            ResetTextAndColor();
        }

        private void DrawImageContentMissing(Rect imageArea, Rect descArea, string reason, Color reasonColor)
        {
            if (DefaultImage != null)
            {
                GUI.DrawTexture(imageArea, DefaultImage, ScaleMode.ScaleToFit);
                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Small;
                GUI.color = reasonColor;
                Widgets.Label(descArea.ContractedBy(Margin), reason);
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                GUI.color = reasonColor;
                Widgets.DrawBoxSolid(imageArea, Color.black);
                Widgets.Label(imageArea, reason);
            }

            displayedImage = -1;
            ResetTextAndColor();
        }

        private void CalculateScrollViewSize(float contentWidth)
        {
            if (!shouldRefreshHeight) return;
            shouldRefreshHeight = false;

            float total = 0f;
            for (int i = 0; i < patchNoteDefs.Count; i++)
            {
                total += HeaderHeight + Margin;
                float bodyH;
                if (expandedDefs.Contains(i))
                {
                    if (expandedHeights.TryGetValue(i, out bodyH))
                        total += bodyH + Margin;
                    else
                        total += 200f + Margin; // estimate for not-yet-measured
                }
            }

            scrollViewHeight = total;
        }

        private void ResetTextAndColor()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private Texture2D DefaultImage
        {
            get
            {
                if (defaultImage == null)
                {
                    defaultImage = ContentFinder<Texture2D>.Get("UI/Banners/Empire", false) ??
                                  ContentFinder<Texture2D>.Get("GUI/questionmark", false);
                }
                return defaultImage;
            }
        }
    }
}
