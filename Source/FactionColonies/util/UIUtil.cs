using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

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

        public static bool InfoCardButton(float x, float y, float width, float height, Def def)
        {
            Rect infoRect = new Rect(x, y, width, height);
            return InfoCardButton(infoRect, def);
        }
        public static bool InfoCardButton(Rect button, Def def)
        {
            if (CustomInfoCardButtonWorker(button))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(def));
                return true;
            }
            return false;
        }
        private static bool CustomInfoCardButtonWorker(Rect rect)
        {
            MouseoverSounds.DoRegion(rect);
            TooltipHandler.TipRegionByKey(rect, "DefInfoTip");
            bool result = Widgets.ButtonImage(rect, TexButton.Info, GUI.color);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            return result;
        }
    }
}
