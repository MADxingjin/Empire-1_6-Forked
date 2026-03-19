using FactionColonies.util;
using LudeonTK;
using RimWorld;
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
        public override Vector2 InitialSize => new Vector2(750f + (StandardMargin * 2), 750f + (StandardMargin * 2));

        private const float HeaderHeight = 45f;
        private const float TitleBarHeight = 30f;
        private const float Margin = 5f;
        private const float DividerPad = 15f;
        private const float BadgeWidth = 55f;
        private const float BadgeHeight = 22f;
        private const float DateWidth = 90f;
        private const float IconSize = 45f;
        private const float LinkButtonSize = 24f;
        private const float BannerHeight = 120f;

        private static readonly List<PatchNoteDef> patchNoteDefs = DefDatabase<PatchNoteDef>.AllDefsListForReading.ListFullCopy();

        private Texture2D bannerImage;

        private readonly string title = "FCPatchNotesWindowTitle".Translate();

        // Scroll state
        private HashSet<int> expandedDefs = new HashSet<int>();
        private Dictionary<int, float> expandedHeights = new Dictionary<int, float>();
        private bool shouldRefreshHeight = true;
        private float scrollViewHeight = 0f;
        private Vector2 patchNoteScrollPos = new Vector2();

        // Scrolling bug fix
        private bool firstRun = true;
        private bool fixDone = false;

        // Badge colors
        private static readonly Color BadgeColorMajor = new Color(0.85f, 0.65f, 0.13f);
        private static readonly Color BadgeColorMinor = new Color(0.3f, 0.5f, 0.9f);
        private static readonly Color BadgeColorHotfix = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color BadgeColorPatch = new Color(0.5f, 0.5f, 0.5f);

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
            Rect titleRect = new Rect(inRect.x + Margin, inRect.y, inRect.width - Margin * 2, TitleBarHeight);
            float bannerTop = inRect.y + TitleBarHeight + DividerPad;
            Rect bannerRect = new Rect(inRect.x + Margin, bannerTop, inRect.width - Margin * 2, BannerHeight);
            float contentTop = bannerTop + BannerHeight + Margin;
            float contentHeight = inRect.height - (contentTop - inRect.y);
            Rect contentPanel = new Rect(inRect.x + Margin, contentTop, inRect.width - Margin * 2, contentHeight);

            FixScrollingBug();
            CalculateScrollViewSize();
            DrawTitle(titleRect);
            DrawHorizontalDivider(inRect);
            DrawBanner(bannerRect);
            DrawPatchNotes(contentPanel);
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
                float startX = titleRect.xMax - TitleBarHeight;
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

        private void DrawHorizontalDivider(Rect inRect)
        {
            GUI.color = Color.gray;
            float lineY = inRect.y + TitleBarHeight + (DividerPad * 0.5f) - 1f;
            Widgets.DrawLineHorizontal(inRect.x + Margin, lineY, inRect.width - Margin * 2);
            ResetTextAndColor();
        }

        private void DrawBanner(Rect bannerRect)
        {
            if (bannerImage == null)
            {
                bannerImage = ContentFinder<Texture2D>.Get("UI/Banners/Empire", false);
            }
            if (bannerImage != null)
            {
                GUI.DrawTexture(bannerRect, bannerImage, ScaleMode.ScaleToFit);
            }
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

                // --- Header ---
                Rect headerRect = new Rect(0f, curY, scrollContentWidth, HeaderHeight);

                // Alternating highlight
                if (i % 2 == 0)
                    Widgets.DrawHighlight(headerRect);
                else
                    Widgets.DrawLightHighlight(headerRect);

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
                Rect titleLabelRect = new Rect(titleX, headerRect.y, dateRect.x - titleX - Margin, HeaderHeight);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(titleLabelRect, def.ShortTitle);
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
                    shouldRefreshHeight = true;
                }

                curY += HeaderHeight + Margin;

                // --- Expanded body ---
                if (isExpanded)
                {
                    Text.Font = GameFont.Small;
                    string bodyText = def.CompactBodyString;
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

        private void CalculateScrollViewSize()
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
                        total += 200f + Margin;
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
    }
}
