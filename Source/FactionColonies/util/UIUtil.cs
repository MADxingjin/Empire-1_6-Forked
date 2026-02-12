using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public static class UIUtil
    {
        public static float getTotalHeight(Rect inputBox)
        {
            return inputBox.y + inputBox.height;
        }
        /// <summary>
        /// A modified version of TooltipHandler.TipRegionByKey() to use with text that isn't meant to be translated,
        /// or (more properly) that has already been translated.
        /// </summary>
        /// <param name="rect">The rect to show the tooltip for.</param>
        /// <param name="text">The tooltip text.</param>
        public static void TipRegionByText(Rect rect, string text)
        {
            if (Mouse.IsOver(rect) || DebugViewSettings.drawTooltipEdges)
            {
                TooltipHandler.TipRegion(rect, text);
            }
        }

        public static void DrawProgressBar(Rect rect, float progress)
        {
            Rect baseRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect progressRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            Widgets.DrawBoxSolid(baseRect, Color.black);
            Widgets.DrawBoxSolid(progressRect, Color.cyan);
        }

        public static void NumericFieldIncrement(Rect rect, ref int increment, ref string buffer, int min = 0, int max = int.MaxValue)
        {
            TextAnchor originalAnchor = Text.Anchor;
            GameFont originalFont = Text.Font;

            float fieldWidth = Math.Min(rect.width / 5f, 40f);
            float buttonWidth = (rect.width - fieldWidth) / 4f;

            Rect mostDecrementBox = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Rect decrementBox = new Rect(mostDecrementBox.xMax, rect.y, buttonWidth, rect.height);
            Rect fieldBox = new Rect(decrementBox.xMax, rect.y, fieldWidth, rect.height);
            Rect incrementBox = new Rect(fieldBox.xMax, rect.y, buttonWidth, rect.height);
            Rect mostIncrementBox = new Rect(incrementBox.xMax, rect.y, buttonWidth, rect.height);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.TextFieldNumeric(fieldBox, ref increment, ref buffer, min, max);
            if (Widgets.ButtonText(mostDecrementBox, "-10"))
            {
                increment = Math.Max(increment - 10, min);
            }
            if (Widgets.ButtonText(decrementBox, "-1"))
            {
                increment = Math.Max(increment - 1, min);
            }
            if (Widgets.ButtonText(incrementBox, "+1"))
            {
                increment = Math.Min(increment + 1, max);
            }
            if (Widgets.ButtonText(mostIncrementBox, "+10"))
            {
                increment = Math.Min(increment + 10, max);
            }

            Text.Anchor = originalAnchor;
            Text.Font = originalFont;
        }
        public static void NumericFieldIncrementF(Rect rect, ref float increment, ref string buffer, float min = 0, float max = float.MaxValue)
        {
            TextAnchor originalAnchor = Text.Anchor;
            GameFont originalFont = Text.Font;

            float fieldWidth = Math.Min(rect.width / 5f, 40f);
            float buttonWidth = (rect.width - fieldWidth) / 4f;

            Rect mostDecrementBox = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Rect decrementBox = new Rect(mostDecrementBox.xMax, rect.y, buttonWidth, rect.height);
            Rect fieldBox = new Rect(decrementBox.xMax, rect.y, fieldWidth, rect.height);
            Rect incrementBox = new Rect(fieldBox.xMax, rect.y, buttonWidth, rect.height);
            Rect mostIncrementBox = new Rect(incrementBox.xMax, rect.y, buttonWidth, rect.height);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.TextFieldNumeric(fieldBox, ref increment, ref buffer, min, max);
            if (Widgets.ButtonText(mostDecrementBox, "-10"))
            {
                increment = Math.Max(increment - 10, min);
            }
            if (Widgets.ButtonText(decrementBox, "-1"))
            {
                increment = Math.Max(increment - 1, min);
            }
            if (Widgets.ButtonText(incrementBox, "+1"))
            {
                increment = Math.Min(increment + 1, max);
            }
            if (Widgets.ButtonText(mostIncrementBox, "+10"))
            {
                increment = Math.Min(increment + 10, max);
            }

            Text.Anchor = originalAnchor;
            Text.Font = originalFont;
        }
    }
}
