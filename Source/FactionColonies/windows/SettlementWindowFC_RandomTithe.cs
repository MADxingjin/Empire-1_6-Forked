using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    public class SettlementWindowFC_RandomTithe : Window
    {
        private ResourceFC resource;
        private WorldSettlementFC settlement;
        private Vector2 scrollBar = new Vector2();

        private const int margin = 5;
        private const int smallMargin = 3;
        private const int rowHeight = 23;
        private const int scrollSpacing = 16;
        public override Vector2 InitialSize
        {
            get { return new Vector2(450f, 500f); }
        }

        public SettlementWindowFC_RandomTithe(WorldSettlementFC settlement, ResourceFC resource)
        {
            if (resource == null || settlement == null)
            {
                Close();
            }
            this.settlement = settlement;
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;
            this.resource = resource;
        }


        public override void DoWindowContents(Rect boundingBox)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect header = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 35f);
            Widgets.Label(header, "RandomTitheSelectionHeader".Translate());
            Widgets.DrawLineHorizontal(header.x, header.yMax, header.width);

            Text.Font = GameFont.Small;
            Rect subHeader = new Rect(boundingBox.x, header.yMax, boundingBox.width, 30f);
            Widgets.Label(subHeader, settlement.Name);

            Rect iconBox = new Rect(boundingBox.x, subHeader.yMax, 30f, 30f);
            Rect labelHighlight = new Rect(iconBox.xMax + margin, iconBox.y, boundingBox.width - margin - iconBox.width, 30f);
            Rect labelText = new Rect(labelHighlight.x + smallMargin, labelHighlight.y + smallMargin, labelHighlight.width - (smallMargin * 2), labelHighlight.height - (smallMargin * 2));

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.DrawHighlight(iconBox);
            Widgets.Label(iconBox, new GUIContent(resource.def.Icon));
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawHighlight(labelHighlight);
            Widgets.Label(labelText, resource.def.LabelCap);

            /* Enable All / Disable All buttons */
            Rect enableAllBox = new Rect(boundingBox.x, iconBox.yMax + margin, boundingBox.width / 2f, 30f);
            Rect disableAllBox = new Rect(enableAllBox.xMax, enableAllBox.y, boundingBox.width / 2f, 30f);
            if(Widgets.ButtonText(enableAllBox, "FCTitheEnableAll".Translate()))
            {
                resource.setAllRandomTitheFilter();
            }
            if (Widgets.ButtonText(disableAllBox, "FCTitheDisableAll".Translate()))
            {
                resource.clearRandomTitheFilter();
            }

            List<ThingDef> allThings = resource.generateThingDefList();
            Rect drawBox = new Rect(boundingBox.x, enableAllBox.yMax + margin, boundingBox.width, boundingBox.yMax - enableAllBox.yMax - margin);
            Rect outerListBox = new Rect(drawBox.x + 2, drawBox.y + 2, drawBox.width - 4, drawBox.height - 4);
            float listHeight = allThings.Count * rowHeight;
            float width;
            if (listHeight > outerListBox.height)
            {
                width = outerListBox.width - scrollSpacing;
            }
            else
            {
                width = outerListBox.width;
            }
            Rect innerScrollBox = new Rect(outerListBox.x, outerListBox.y, width, listHeight);
            Widgets.DrawMenuSection(drawBox);

            Widgets.BeginScrollView(outerListBox, ref scrollBar, innerScrollBox);

            for (int i = 0; i < allThings.Count; i++)
            {
                ThingDef iThing = allThings[i];
                Rect row = new Rect(innerScrollBox.x, innerScrollBox.y + (i * rowHeight), innerScrollBox.width, rowHeight);
                Rect icon = new Rect(row.x + margin, row.y, rowHeight, rowHeight);
                Rect info = new Rect(icon.xMax, row.y + 2, rowHeight - 4, rowHeight - 4);
                Rect enableBox = new Rect(row.xMax - margin - 65f, row.y, 65f, rowHeight);
                Rect valueLabel = new Rect(enableBox.x - margin - 60f, enableBox.y, 60f, rowHeight);
                Rect label = new Rect(info.xMax + margin, row.y, valueLabel.x - info.xMax - (margin*2), rowHeight);

                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(icon, new GUIContent(iThing.uiIcon));
                if (Widgets.ButtonText(enableBox, IsAllowedTranslation(resource.getRandomTitheFilterAllow(iThing))))
                {
                    resource.setRandomTitheFilterAllow(iThing, !resource.getRandomTitheFilterAllow(iThing));
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(label, iThing.LabelCap);
                Widgets.Label(valueLabel, "$" + iThing.BaseMarketValue.ToString());
                //Widgets.InfoCardButton(icon.xMax, row.y+1, iThing);
                UIUtil.InfoCardButton(info, iThing);
            }

            Widgets.EndScrollView();


            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        /// <summary>
        /// Transforms the given bool <paramref name="var"/> into it's keyed translation
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        private string IsAllowedTranslation(bool var)
        {
            if (var) return "FCIsAllowed".Translate();
            return "FCIsNotAllowed".Translate();
        }
    }
}
