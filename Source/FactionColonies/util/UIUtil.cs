using System;
using System.Collections.Generic;
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
    }
}
