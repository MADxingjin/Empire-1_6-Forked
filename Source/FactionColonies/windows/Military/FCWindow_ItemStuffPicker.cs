using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// A modal picker window that shows items on the left and stuff materials
    /// on the right. Used for weapon and apparel selection in the unit designer.
    /// </summary>
    public class FCWindow_ItemStuffPicker : Window
    {
        private readonly List<ThingDef> items;
        private readonly Action<ThingDef, ThingDef> onConfirm;
        private readonly Action onUnequip;
        private readonly string titleKey;

        private ThingDef selectedItem;
        private ThingDef selectedStuff;
        private List<ThingDef> currentStuffs = new List<ThingDef>();

        private string itemSearchTerm = "";
        private string stuffSearchTerm = "";

        private Vector2 itemScrollPos;
        private Vector2 stuffScrollPos;

        private const float RowHeight = 30f;
        private const float IconSize = 24f;
        private const float SearchBarHeight = 28f;
        private const float ButtonHeight = 35f;
        private const float PanelGap = 10f;
        private const float margin = 5f;

        public override Vector2 InitialSize => new Vector2(700f, 550f);

        public FCWindow_ItemStuffPicker(
            List<ThingDef> items,
            Action<ThingDef, ThingDef> onConfirm,
            Action onUnequip = null,
            string titleKey = "fcPickItem",
            ThingDef initialItem = null,
            ThingDef initialStuff = null)
        {
            this.items = items;
            this.onConfirm = onConfirm;
            this.onUnequip = onUnequip;
            this.titleKey = titleKey;

            if (initialItem != null)
            {
                selectedItem = initialItem;
                selectedStuff = initialStuff;
                if (initialItem.MadeFromStuff)
                {
                    currentStuffs.AddRange(GenStuff.AllowedStuffsFor(initialItem));
                    currentStuffs.SortBy(s => s.label);
                }
            }

            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            absorbInputAroundWindow = true;
            resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), titleKey.Translate());

            float contentTop = 40f;
            float summaryHeight = 25f;
            float contentHeight = inRect.height - contentTop - ButtonHeight - summaryHeight - 20f;
            float panelWidth = (inRect.width - PanelGap) / 2f;

            // Left panel: Items
            Rect itemPanelRect = new Rect(0, contentTop, panelWidth, contentHeight);
            DrawItemPanel(itemPanelRect);

            // Right panel: Stuff
            Rect stuffPanelRect = new Rect(panelWidth + PanelGap, contentTop, panelWidth, contentHeight);
            if (selectedItem != null && selectedItem.MadeFromStuff)
            {
                DrawStuffPanel(stuffPanelRect);
            }
            else if (selectedItem != null)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(stuffPanelRect, "fcNoMaterialNeeded".Translate());
            }

            // Selection summary
            Rect summaryBox = new Rect(inRect.x, contentTop + contentHeight + 5f, inRect.width, summaryHeight);
            Rect summaryRect = new Rect(summaryBox.x + margin, contentTop + contentHeight + 5f, inRect.width - (margin*2), summaryHeight);
            Widgets.DrawHighlight(summaryBox);
            DrawSummary(summaryRect);

            // Bottom buttons
            Rect buttonBar = new Rect(0, inRect.height - ButtonHeight - 5f, inRect.width, ButtonHeight);
            DrawButtons(buttonBar);

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void DrawItemPanel(Rect panelRect)
        {
            Text.Font = GameFont.Small;

            // Title bar
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect titleBox = new Rect(panelRect.x, panelRect.y, panelRect.width, SearchBarHeight);
            Widgets.DrawHighlight(titleBox);
            Widgets.Label(titleBox, "Item".Translate());

            Text.Anchor = TextAnchor.MiddleLeft;
            // Search bar
            Rect searchRect = new Rect(panelRect.x, titleBox.yMax + margin, panelRect.width, SearchBarHeight);
            itemSearchTerm = Widgets.TextField(searchRect, itemSearchTerm);

            // Scroll view
            Rect scrollOutRect = new Rect(panelRect.x, searchRect.yMax + 5f,
                panelRect.width, panelRect.height - (SearchBarHeight*2) - (margin*2));
            Widgets.DrawMenuSection(scrollOutRect);

            List<ThingDef> filtered = string.IsNullOrEmpty(itemSearchTerm)
                ? items
                : items.Where(t => t.label.IndexOf(itemSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            float viewHeight = filtered.Count * RowHeight;
            float scrollMargin = viewHeight > scrollOutRect.height ? 16f : 0f;
            Rect scrollViewRect = new Rect(scrollOutRect.x, scrollOutRect.y, scrollOutRect.width - scrollMargin, Mathf.Max(viewHeight, scrollOutRect.height));

            Widgets.BeginScrollView(scrollOutRect, ref itemScrollPos, scrollViewRect);

            for (int i = 0; i < filtered.Count; i++)
            {
                ThingDef item = filtered[i];
                Rect row = new Rect(scrollViewRect.x, scrollViewRect.y + (i * RowHeight), scrollViewRect.width, RowHeight);

                if (item == selectedItem)
                    Widgets.DrawHighlightSelected(row);
                else if (i % 2 == 0)
                    Widgets.DrawHighlight(row);

                // Icon
                Rect iconRect = new Rect(row.x + 2f, row.y + 3f, IconSize, IconSize);
                Widgets.ThingIcon(iconRect, item);

                // Label + cost
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect labelRect = new Rect(iconRect.xMax + 5f, row.y, row.width - IconSize - 40f, RowHeight);
                Widgets.Label(labelRect, item.LabelCap + " - " + item.BaseMarketValue.ToString("F0"));

                // Click to select
                if (Widgets.ButtonInvisible(row))
                {
                    selectedItem = item;

                    currentStuffs.Clear();
                    if (item.MadeFromStuff)
                    {
                        currentStuffs.AddRange(GenStuff.AllowedStuffsFor(item));
                        currentStuffs.SortBy(s => s.label);
                    }

                    // Keep selectedStuff if it's still valid for the new item
                    if (selectedStuff != null && (currentStuffs.Count == 0 || !currentStuffs.Contains(selectedStuff)))
                    {
                        selectedStuff = null;
                    }
                }

                // Info button
                Rect infoRect = new Rect(row.xMax - 26f, row.y + 3f, IconSize, IconSize);
                if (Widgets.ButtonImage(infoRect, TexButton.Info))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(item));
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawStuffPanel(Rect panelRect)
        {
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleCenter;
            Rect titleBox = new Rect(panelRect.x, panelRect.y, panelRect.width, SearchBarHeight);
            Widgets.DrawHighlight(titleBox);
            Widgets.Label(titleBox, "Stuff".Translate());

            Text.Anchor = TextAnchor.MiddleLeft;
            // Search bar
            Rect searchRect = new Rect(panelRect.x, titleBox.yMax + margin, panelRect.width, SearchBarHeight);
            stuffSearchTerm = Widgets.TextField(searchRect, stuffSearchTerm);

            // Scroll view
            Rect scrollOutRect = new Rect(panelRect.x, searchRect.yMax + 5f,
                panelRect.width, panelRect.height - (SearchBarHeight * 2) - (margin * 2));
            Widgets.DrawMenuSection(scrollOutRect);

            List<ThingDef> filtered = string.IsNullOrEmpty(stuffSearchTerm)
                ? currentStuffs
                : currentStuffs.Where(s => s.label.IndexOf(stuffSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            float viewHeight = filtered.Count * RowHeight;
            float scrollMargin = viewHeight > scrollOutRect.height ? 16f : 0f;
            Rect scrollViewRect = new Rect(scrollOutRect.x, scrollOutRect.y, scrollOutRect.width - scrollMargin, Mathf.Max(viewHeight, scrollOutRect.height));

            Widgets.BeginScrollView(scrollOutRect, ref stuffScrollPos, scrollViewRect);

            for (int i = 0; i < filtered.Count; i++)
            {
                ThingDef stuff = filtered[i];
                Rect row = new Rect(scrollViewRect.x, scrollViewRect.y + (i * RowHeight), scrollViewRect.width, RowHeight);

                if (stuff == selectedStuff)
                    Widgets.DrawHighlightSelected(row);
                else if (i % 2 == 0)
                    Widgets.DrawHighlight(row);

                // Icon
                Rect iconRect = new Rect(row.x + 2f, row.y + 3f, IconSize, IconSize);
                Widgets.ThingIcon(iconRect, stuff);

                // Label + calculated value
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                float totalValue = StatWorker_MarketValue.CalculatedBaseMarketValue(selectedItem, stuff);
                Rect labelRect = new Rect(iconRect.xMax + 5f, row.y, row.width - IconSize - 10f, RowHeight);
                Widgets.Label(labelRect, stuff.LabelCap + " - " + totalValue.ToString("F0"));

                // Click to select
                if (Widgets.ButtonInvisible(row))
                {
                    selectedStuff = stuff;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawSummary(Rect rect)
        {
            if (selectedItem == null) return;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string itemName = selectedItem.LabelCap;
            if (selectedStuff != null)
                itemName += " (" + selectedStuff.LabelCap + ")";

            float cost = selectedStuff != null
                ? StatWorker_MarketValue.CalculatedBaseMarketValue(selectedItem, selectedStuff)
                : selectedItem.BaseMarketValue;

            Widgets.Label(rect, "fcPickerSummary".Translate(itemName, cost.ToString("F0")));
        }

        private void DrawButtons(Rect bar)
        {
            float buttonWidth = 120f;

            // Unequip (left)
            if (onUnequip != null)
            {
                Rect unequipRect = new Rect(bar.x, bar.y, buttonWidth, bar.height);
                if (Widgets.ButtonText(unequipRect, "unitActionUnequipThing".Translate()))
                {
                    onUnequip();
                    Close();
                }
            }

            // Cancel (right)
            Rect cancelRect = new Rect(bar.xMax - buttonWidth, bar.y, buttonWidth, bar.height);
            if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
            {
                Close();
            }

            // Confirm (left of cancel)
            bool canConfirm = selectedItem != null && (!selectedItem.MadeFromStuff || selectedStuff != null);
            Rect confirmRect = new Rect(cancelRect.x - buttonWidth - 10f, bar.y, buttonWidth, bar.height);
            if (Widgets.ButtonText(confirmRect, "FCConfirm".Translate(), active: canConfirm))
            {
                if (canConfirm)
                {
                    onConfirm(selectedItem, selectedStuff);
                    Close();
                }
            }
        }
    }
}
