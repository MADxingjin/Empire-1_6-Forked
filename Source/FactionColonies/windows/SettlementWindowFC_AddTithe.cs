using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private QualityCategory? selectedQuality = null;
        private ThingDef selectedStuff = null;
        private List<ThingDef> currentStuffs = new List<ThingDef>();

        private const int margin = 5;
        private const int smallMargin = 3;
        private const int rowHeight = 23;
        private const int scrollSpacing = 16;
        private const float SearchBarHeight = 28f;
        private const float choiceBox = 45f;

        private string thingSearchTerm = "";
        private string stuffSearchTerm = "";

        public override Vector2 InitialSize
        {
            get { return new Vector2(780f, 500f); }
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
                selectionPanelHeight = rowHeight + smallMargin + margin;
                if (thingHasQuality)
                {
                    selectionPanelHeight += rowHeight + smallMargin;
                }

                selectionPanelHeight += choiceBox;
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

            Rect drawBox = new Rect(boundingBox.x, iconBox.yMax + margin, boundingBox.width, boundingBox.yMax - iconBox.yMax - margin - selectionPanelHeight);
            Rect leftPanel = new Rect(boundingBox.x, iconBox.yMax + margin, (boundingBox.width - margin) / 2f, boundingBox.yMax - iconBox.yMax - margin - selectionPanelHeight);
            Rect rightPanel = new Rect(leftPanel.xMax + margin, leftPanel.y, leftPanel.width, leftPanel.height);
            DrawLeftPanel(leftPanel);

            if (selectedThing is null || currentStuffs is null || currentStuffs.Count == 0)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rightPanel, "fcNoMaterialNeeded".Translate());
            }
            else
            {
                DrawRightPanel(rightPanel);
            }

            if (selectedThing != null)
            {
                Rect selectionPanel = new Rect(boundingBox.x, boundingBox.yMax - selectionPanelHeight, boundingBox.width, selectionPanelHeight);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                float panelY = selectionPanel.y + margin;

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

                Text.Anchor = TextAnchor.MiddleCenter;
                Rect selectedLabel = new Rect(selectionPanel.x + margin, panelY, selectionPanel.width - (margin * 2), choiceBox);
                Widgets.DrawHighlight(selectedLabel);
                string stuffStr = selectedStuff is null ? "" : $"{selectedStuff.LabelCap} ";
                string qualityStr = (thingHasQuality && !(selectedQuality is null)) ? $" ({TextUtil.GetQualityLabelCap(selectedQuality)})" : "";
                string totalCost;
                if (!(selectedThing is null) && (!thingIsStuffable || !(selectedStuff is null)) && (!thingHasQuality || !(selectedQuality is null)))
                {
                    totalCost = $"${resource.titheThingValue(selectedThing, selectedStuff, selectedQuality ?? QualityCategory.Normal)}";
                }
                else
                {
                    totalCost = "  ";
                }
                Widgets.Label(selectedLabel, $"{stuffStr}{selectedThing.LabelCap}{qualityStr}\n{totalCost}");
                panelY += selectedLabel.height + margin;

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
                    quality = thingHasQuality ? selectedQuality ?? QualityCategory.Normal : QualityCategory.Normal,
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
        private void DrawLeftPanel(Rect boundingBox)
        {
            // Search bar
            Rect searchRect = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, SearchBarHeight);
            thingSearchTerm = Widgets.TextField(searchRect, thingSearchTerm);

            List<ThingDef> thingsList = string.IsNullOrEmpty(thingSearchTerm)
                ? resource.generateThingDefList()
                : resource.generateThingDefList().Where(t => t.label.IndexOf(thingSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Actual scrollbox
            Rect drawBox = new Rect(boundingBox.x, searchRect.yMax + margin, boundingBox.width, boundingBox.height - margin - searchRect.height);
            Rect outerListBox = new Rect(drawBox.x + 2, drawBox.y + 2, drawBox.width - 4, drawBox.height - 4);
            float listHeight = thingsList.Count * rowHeight;
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

            for (int i = 0; i < thingsList.Count; i++)
            {
                ThingDef iThing = thingsList[i];
                Rect row = new Rect(innerScrollBox.x, innerScrollBox.y + (i * rowHeight), innerScrollBox.width, rowHeight);
                Rect icon = new Rect(row.x + margin, row.y, rowHeight, rowHeight);
                Rect info = new Rect(icon.xMax, row.y + 2, rowHeight - 4, rowHeight - 4);
                Rect valueLabel = new Rect(row.xMax - margin - 70f, row.y, 60f, rowHeight);
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
                if (Widgets.ButtonInvisible(row))
                {
                    selectedThing = iThing;

                    currentStuffs.Clear();
                    if (iThing.MadeFromStuff)
                    {
                        currentStuffs.AddRange(resource.getStuffListForThingDef(iThing));
                        currentStuffs.SortBy(s => s.label);
                    }

                    // Keep selectedStuff if it's still valid for the new item
                    if (selectedStuff != null && (currentStuffs.Count == 0 || !currentStuffs.Contains(selectedStuff)))
                    {
                        selectedStuff = null;
                    }
                    if (selectedQuality != null && !CraftUtil.thingHasQuality(iThing))
                    {
                        selectedQuality = null;
                    }
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(label, iThing.LabelCap);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(valueLabel, $"${Math.Round(iThing.BaseMarketValue)}");
                Text.Anchor = TextAnchor.MiddleLeft;
                UIUtil.InfoCardButton(info, iThing);
            }

            Widgets.EndScrollView();
        }
        private void DrawRightPanel(Rect boundingBox)
        {
            // Search bar
            Rect searchRect = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, SearchBarHeight);
            stuffSearchTerm = Widgets.TextField(searchRect, stuffSearchTerm);

            List<ThingDef> stuffList = string.IsNullOrEmpty(stuffSearchTerm)
                ? currentStuffs
                : currentStuffs.Where(t => t.label.IndexOf(stuffSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Actual scrollbox
            Rect drawBox = new Rect(boundingBox.x, searchRect.yMax + margin, boundingBox.width, boundingBox.height - margin - searchRect.height);
            Rect outerListBox = new Rect(drawBox.x + 2, drawBox.y + 2, drawBox.width - 4, drawBox.height - 4);
            float listHeight = stuffList.Count * rowHeight;
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

            for (int i = 0; i < stuffList.Count; i++)
            {
                ThingDef iStuff = stuffList[i];
                Rect row = new Rect(innerScrollBox.x, innerScrollBox.y + (i * rowHeight), innerScrollBox.width, rowHeight);
                Rect icon = new Rect(row.x + margin, row.y, rowHeight, rowHeight);
                Rect info = new Rect(icon.xMax, row.y + 2, rowHeight - 4, rowHeight - 4);
                Rect valueLabel = new Rect(row.xMax - margin - 70f, row.y, 60f, rowHeight);
                Rect label = new Rect(info.xMax + margin, row.y, valueLabel.x - info.xMax - (margin * 2), rowHeight);

                if (selectedStuff == iStuff)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(icon, new GUIContent(iStuff.uiIcon));
                if (Widgets.ButtonInvisible(row))
                {
                    selectedStuff = iStuff;
                }
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(label, iStuff.LabelCap);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(valueLabel, $"${Math.Round(StatWorker_MarketValue.CalculatedBaseMarketValue(selectedThing, iStuff))}");
                Text.Anchor = TextAnchor.MiddleLeft;
                UIUtil.InfoCardButton(info, iStuff);
            }

            Widgets.EndScrollView();
        }
    }
}
