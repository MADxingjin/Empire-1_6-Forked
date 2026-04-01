using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    public static class UIUtil
    {
        public static void DrawProgressBar(Rect rect, float progress)
        {
            DrawProgressBarColors(rect, progress, Color.black, Color.cyan);
        }
        public static void DrawProgressBarColors(Rect rect, float progress, Color background, Color bar)
        {
            Rect baseRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect progressRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            Widgets.DrawBoxSolid(baseRect, background);
            Widgets.DrawBoxSolid(progressRect, bar);
        }

        public static void DrawPawnPortrait(Rect rect, Pawn pawn, float cameraZoom = 1f)
        {
            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South, cameraZoom: cameraZoom);
            GUI.DrawTexture(rect, portrait);
        }

        public static bool ButtonFlat(Rect rect, string label, Color? labelColor = null, bool disabled = false, bool highlighted = false)
        {
            return ButtonFlatIcon(rect, label, null, labelColor, disabled, highlighted);
        }

        public static bool ButtonFlatIcon(Rect rect, string label, Texture2D icon = null,
            Color? labelColor = null, bool disabled = false, bool highlighted = false)
        {
            bool hovered = !disabled && Mouse.IsOver(rect);
            float normalBg = highlighted ? 0.15f : 0.22f;
            float hoverBg = highlighted ? 0.28f : 0.35f;
            float bg = hovered ? hoverBg : normalBg;
            Widgets.DrawBoxSolid(rect, new Color(bg, bg, bg));

            float iconSpace = 0f;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + (rect.height - 16f) / 2f, 16f, 16f), icon);
                iconSpace = 20f;
            }

            TextAnchor prevAnchor = Text.Anchor;
            bool prevWordWrap = Text.WordWrap;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Color prevColor = GUI.color;
            GUI.color = disabled ? Color.gray : (labelColor ?? Color.white);
            Widgets.Label(new Rect(rect.x + iconSpace, rect.y, rect.width - iconSpace, rect.height), label);
            GUI.color = prevColor;
            Text.Anchor = prevAnchor;
            Text.WordWrap = prevWordWrap;

            if (!disabled && Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                return true;
            }
            return false;
        }

        public static void DrawTabDecoratorHorizontalTop(Rect tab, Rect boundingBox, Color color)
        {
            DrawTabDecoratorHorizontalTop(tab, boundingBox.x, boundingBox.xMax, color);
        }
        public static void DrawTabDecoratorHorizontalTop(Rect tab, float leftx, float rightx, Color color)
        {
            Color origColor = GUI.color;
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(leftx, tab.yMax, tab.x - leftx);
            Widgets.DrawLineVertical(tab.x, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.x, tab.y, tab.width);
            Widgets.DrawLineVertical(tab.xMax, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.xMax, tab.yMax, rightx - tab.xMax);
            GUI.color = origColor;
        }
        public static void DrawTabDecoratorVerticalLeft(Rect tab, Rect boundingBox, Color color)
        {
            DrawTabDecoratorVerticalLeft(tab, boundingBox.y, boundingBox.yMax, color);
        }
        public static void DrawTabDecoratorVerticalLeft(Rect tab, float upy, float downy, Color color)
        {
            Color origColor = GUI.color;
            GUI.color = color;
            Widgets.DrawLineVertical(tab.xMax, upy, tab.y - upy);
            Widgets.DrawLineHorizontal(tab.x, tab.y, tab.width);
            Widgets.DrawLineVertical(tab.x, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.x, tab.yMax, tab.width);
            Widgets.DrawLineVertical(tab.xMax, tab.yMax, downy - tab.yMax);
            GUI.color = origColor;
        }

        public static int GetModifier => 1 * (Event.current.shift ? 5 : 1) * (Event.current.control ? 10 : 1);

        /// <summary>
        /// Draws a row of tabs with automatic multi-row overflow using a custom button drawer.
        /// Returns the selected tab index (changed if a tab was clicked).
        /// <paramref name="contentRect"/> is set to the usable area below the tab rows,
        /// bordered on sides and bottom.
        /// </summary>
        public static int DrawTabRow(Rect boundingBox, List<string> tabLabels, int selectedTab,
            out Rect contentRect, Func<Rect, string, bool, bool> buttonDrawer,
            float tabHeight = 20f, float minTabWidth = 100f)
        {
            int tabCount = tabLabels.Count;
            int rows = Math.Max(1, Mathf.CeilToInt(tabCount * minTabWidth / boundingBox.width));
            int basePerRow = Mathf.FloorToInt((float)tabCount / rows);

            float totalTabHeight = rows * tabHeight;
            float contentTop = boundingBox.y + totalTabHeight;

            // Track which row the selected tab lands in, and its rect
            Rect chosenRect = new Rect();
            int selectedRow = -1;
            int tabIndex = 0;
            int result = selectedTab;

            for (int row = 0; row < rows; row++)
            {
                // First row gets the remainder
                int tabsThisRow = (row == 0) ? tabCount - (rows - 1) * basePerRow : basePerRow;
                float rowTabWidth = boundingBox.width / tabsThisRow;
                float rowY = boundingBox.y + row * tabHeight;

                for (int col = 0; col < tabsThisRow; col++)
                {
                    Rect tabRect = new Rect(boundingBox.x + col * rowTabWidth, rowY, rowTabWidth, tabHeight);
                    string label = tabLabels[tabIndex];
                    bool isSelected = tabIndex == selectedTab;

                    // Tooltip for truncated labels
                    if (Text.CalcSize(label).x > tabRect.width - 8f)
                    {
                        TooltipHandler.TipRegion(tabRect, label);
                    }

                    if (buttonDrawer(tabRect, label, isSelected))
                    {
                        result = tabIndex;
                    }

                    if (isSelected)
                    {
                        chosenRect = tabRect;
                        selectedRow = row;
                    }

                    tabIndex++;
                }
            }

            // Border drawing
            Color origColor = GUI.color;
            GUI.color = Color.gray;

            if (selectedRow == rows - 1)
            {
                // Selected tab is in the bottom row — draw notch border
                DrawTabDecoratorHorizontalTop(chosenRect, boundingBox.x, boundingBox.xMax, Color.gray);
            }
            else
            {
                // Selected tab is in an upper row — straight line across content top
                Widgets.DrawLineHorizontal(boundingBox.x, contentTop, boundingBox.width);
                GUI.color = Color.gray;
                Widgets.DrawBox(chosenRect);
                GUI.color = Color.gray;
            }

            // Content box sides and bottom
            Widgets.DrawLineVertical(boundingBox.x, contentTop, boundingBox.height - totalTabHeight);
            Widgets.DrawLineVertical(boundingBox.xMax, contentTop, boundingBox.height - totalTabHeight);
            Widgets.DrawLineHorizontal(boundingBox.x, boundingBox.yMax - 1, boundingBox.width);

            GUI.color = origColor;

            contentRect = new Rect(boundingBox.x, contentTop, boundingBox.width, boundingBox.height - totalTabHeight);
            return result;
        }

        /// <summary>Draws tabs using <see cref="ButtonFlat"/> with highlighted selection. Default overload.</summary>
        public static int DrawTabRow(Rect boundingBox, List<string> tabLabels, int selectedTab,
            out Rect contentRect, float tabHeight = 20f, float minTabWidth = 100f)
        {
            return DrawTabRow(boundingBox, tabLabels, selectedTab, out contentRect,
                (r, l, sel) => ButtonFlat(r, l, highlighted: sel), tabHeight, minTabWidth);
        }

        /// <summary>Draws tabs using <see cref="Widgets.ButtonText"/>.</summary>
        public static int DrawTabRowButtonText(Rect boundingBox, List<string> tabLabels, int selectedTab,
            out Rect contentRect, float tabHeight = 20f, float minTabWidth = 100f)
        {
            return DrawTabRow(boundingBox, tabLabels, selectedTab, out contentRect,
                (r, l, sel) => Widgets.ButtonText(r, l), tabHeight, minTabWidth);
        }

        /// <summary>Draws tabs using <see cref="ButtonFlat"/> with optional label color.</summary>
        public static int DrawTabRowButtonFlat(Rect boundingBox, List<string> tabLabels, int selectedTab,
            out Rect contentRect, Color? labelColor = null,
            float tabHeight = 20f, float minTabWidth = 100f)
        {
            return DrawTabRow(boundingBox, tabLabels, selectedTab, out contentRect,
                (r, l, sel) => ButtonFlat(r, l, labelColor: labelColor, highlighted: sel),
                tabHeight, minTabWidth);
        }

        /// <summary>Draws tabs using <see cref="ButtonFlatIcon"/> with optional icon and label color.</summary>
        public static int DrawTabRowButtonFlatIcon(Rect boundingBox, List<string> tabLabels, int selectedTab,
            out Rect contentRect, Texture2D icon = null, Color? labelColor = null,
            float tabHeight = 20f, float minTabWidth = 100f)
        {
            return DrawTabRow(boundingBox, tabLabels, selectedTab, out contentRect,
                (r, l, sel) => ButtonFlatIcon(r, l, icon, labelColor: labelColor, highlighted: sel),
                tabHeight, minTabWidth);
        }
    }
}
