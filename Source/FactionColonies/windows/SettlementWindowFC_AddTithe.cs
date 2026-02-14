using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class SettlementWindowFC_AddTithe : Window
    {
        private WorldSettlementFC settlement;
        private ResourceFC resource;
        private Vector2 scrollBar = new Vector2();

        private ThingDef selectedThing = null;
        private QualityCategory selectedQuality = QualityCategory.Normal;
        private ThingDef selectedStuff = null;

        private const int margin = 5;
        private const int smallMargin = 3;
        private const int rowHeight = 23;
        private const int scrollSpacing = 16;

        public override Vector2 InitialSize
        {
            get { return new Vector2(450f, 500f); }
        }

        public SettlementWindowFC_AddTithe(WorldSettlementFC settlement, ResourceFC resource)
        {
            if (resource == null || settlement == null)
            {
                Close();
            }
            this.settlement = settlement;
            this.resource = resource;
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;
        }

        public override void DoWindowContents(Rect boundingBox)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            bool thingHasQuality = false;
            bool thingIsStuffable = false;
            float selectionPanelHeight = 0;
            if (selectedThing != null)
            {
                thingHasQuality = CraftUtil.thingHasQuality(selectedThing);
                thingIsStuffable = CraftUtil.thingIsStuffable(selectedThing);
                selectionPanelHeight = (rowHeight * 2) + smallMargin + margin;
                if (thingHasQuality)
                {
                    selectionPanelHeight += rowHeight + smallMargin;
                }
                if (thingIsStuffable)
                {
                    selectionPanelHeight += rowHeight + smallMargin;
                }
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect header = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 35f);
            Widgets.Label(header, "AddTitheItemHeader".Translate());
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

            List<ThingDef> allThings = resource.generateThingDefList();
            Rect drawBox = new Rect(boundingBox.x, iconBox.yMax + margin, boundingBox.width, boundingBox.yMax - iconBox.yMax - margin - selectionPanelHeight);
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
                Rect addButton = new Rect(row.xMax - margin - 65f, row.y, 65f, rowHeight);
                Rect valueLabel = new Rect(addButton.x - margin - 60f, addButton.y, 60f, rowHeight);
                Rect label = new Rect(info.xMax + margin, row.y, valueLabel.x - info.xMax - (margin * 2), rowHeight);

                if (selectedThing == iThing)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(icon, new GUIContent(iThing.uiIcon));
                if (Widgets.ButtonText(addButton, "Select".Translate()))
                {
                    selectedThing = iThing;
                    selectedQuality = QualityCategory.Normal;
                    selectedStuff = null;
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(label, iThing.LabelCap);
                Widgets.Label(valueLabel, "$" + iThing.BaseMarketValue.ToString());
                //Widgets.InfoCardButton(icon.xMax, row.y+1, iThing);
                UIUtil.InfoCardButton(info, iThing);
            }

            Widgets.EndScrollView();

            if (selectedThing != null)
            {
                Rect selectionPanel = new Rect(boundingBox.x, boundingBox.yMax - selectionPanelHeight, boundingBox.width, selectionPanelHeight);
                Widgets.DrawLineHorizontal(selectionPanel.x, selectionPanel.y, selectionPanel.width);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                float panelY = selectionPanel.y + margin;
                Rect selectedLabel = new Rect(selectionPanel.x + margin, panelY, selectionPanel.width - (margin * 2), rowHeight);
                Widgets.Label(selectedLabel, "Selected".Translate() + ": " + selectedThing.LabelCap);
                panelY += rowHeight + smallMargin;

                if (thingHasQuality)
                {
                    QualityCategory maxQuality = QualityCategory.Legendary;
                    resource.canSetTitheQuality(out maxQuality);
                    List<QualityCategory> categoryList = resource.getValidTitheQualities(maxQuality);

                    Rect qualityLabel = new Rect(selectionPanel.x + margin, panelY, 60f, rowHeight);
                    Rect qualityButton = new Rect(qualityLabel.xMax + smallMargin, panelY, 120f, rowHeight);
                    Widgets.Label(qualityLabel, "Quality".Translate() + ":");
                    if (Widgets.ButtonText(qualityButton, TextUtil.GetQualityLabelCap(selectedQuality)))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (QualityCategory cat in categoryList)
                        {
                            options.Add(new FloatMenuOption(TextUtil.GetQualityLabelCap(cat), delegate
                            {
                                selectedQuality = cat;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    panelY += rowHeight + smallMargin;
                }

                if (thingIsStuffable)
                {
                    List<ThingDef> stuffList = resource.getStuffListForThingDef(selectedThing);

                    Rect stuffLabel = new Rect(selectionPanel.x + margin, panelY, 60f, rowHeight);
                    Rect stuffButton = new Rect(stuffLabel.xMax + smallMargin, panelY, 120f, rowHeight);
                    Widgets.Label(stuffLabel, "Stuff".Translate() + ":");
                    string stuffButtonText = selectedStuff?.LabelCap ?? "SelectStuff".Translate();
                    if (Widgets.ButtonText(stuffButton, stuffButtonText))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (ThingDef stuff in stuffList)
                        {
                            options.Add(new FloatMenuOption(stuff.LabelCap, delegate
                            {
                                selectedStuff = stuff;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    panelY += rowHeight + smallMargin;
                }

                float buttonWidth = (selectionPanel.width - (margin * 3)) / 2f;
                Rect cancelButton = new Rect(selectionPanel.x + margin, panelY, buttonWidth, rowHeight);
                Rect confirmButton = new Rect(cancelButton.xMax + margin, panelY, buttonWidth, rowHeight);

                if (Widgets.ButtonText(cancelButton, "Cancel".Translate()))
                {
                    selectedThing = null;
                }

                bool canConfirm = true;
                string confirmTooltip = "";

                if (thingIsStuffable && selectedStuff == null)
                {
                    canConfirm = false;
                    confirmTooltip = "MustSelectStuff".Translate();
                }

                ThingQualityTuple tuple = new ThingQualityTuple
                {
                    thingDef = selectedThing,
                    quality = thingHasQuality ? selectedQuality : QualityCategory.Normal,
                    stuffDef = thingIsStuffable ? selectedStuff : null
                };
                if (canConfirm && resource.hasTitheListKey(tuple))
                {
                    canConfirm = false;
                    confirmTooltip = "ItemAlreadyInTitheList".Translate();
                }

                if (!canConfirm)
                {
                    GUI.color = Color.gray;
                }
                if (Widgets.ButtonText(confirmButton, "Confirm".Translate()))
                {
                    if (canConfirm)
                    {
                        if (resource.canAffordThingAmount(tuple, 1))
                        {
                            resource.addToTitheList(tuple, 1);
                        }
                        else
                        {
                            resource.addToTitheList(tuple, 0);
                        }
                        selectedThing = null;
                        Close();
                    }
                }
                if (!canConfirm)
                {
                    GUI.color = Color.white;
                    UIUtil.TipRegionByText(confirmButton, confirmTooltip);
                }
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
    }
}
