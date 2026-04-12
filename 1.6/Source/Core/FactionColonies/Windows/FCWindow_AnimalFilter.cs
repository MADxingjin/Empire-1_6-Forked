using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCWindow_AnimalFilter : Window
    {
        private FactionFC faction;
        private AnimalFilter filter;
        private List<PawnKindDef> allAnimals;
        private List<PawnKindDef> filteredAnimals;
        private string searchTerm = "";
        private Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(450f, 600f);

        private const float RowHeight = 30f;
        private const float SearchBarHeight = 28f;
        private const int margin = 5;
        private const int smallMargin = 3;
        private const int bigRowHeight = 26;

        public FCWindow_AnimalFilter()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;
            doCloseButton = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            faction = FactionCache.FactionComp;
            if (faction is null)
            {
                LogUtil.Error("Null FactionFC WorldComponent when opening FCWindow_AnimalFilter");
                Close();
                return;
            }

            filter = faction.animalFilter;
            if (filter is null)
            {
                LogUtil.Error("Null animalFilter when opening FCWindow_AnimalFilter");
                Close();
                return;
            }

            allAnimals = FactionCache.AllAnimalKindDefs
                .OrderBy(a => a.label ?? a.defName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            filteredAnimals = allAnimals;
        }

        public override void PostClose()
        {
            base.PostClose();
            filter.Validate();
            faction.xenotypeFilter?.RefreshPawnGroupMakers();
            FactionDefDescriptionPatch.Invalidate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            // Header
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect header = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(header, "AnimalSelection".Translate());
            Widgets.DrawLineHorizontal(header.x, header.yMax, header.width);

            // Sub-header: faction name
            Text.Font = GameFont.Small;
            Rect subHeader = new Rect(inRect.x, header.yMax, inRect.width, 26f);
            Widgets.Label(subHeader, FactionCache.PlayerColonyFaction.Name);

            // Search bar
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect searchRect = new Rect(inRect.x, subHeader.yMax + margin, inRect.width, SearchBarHeight);
            string newSearch = Widgets.TextField(searchRect, searchTerm);
            if (newSearch != searchTerm)
            {
                searchTerm = newSearch;
                RebuildFilteredList();
            }

            float bottomY = inRect.yMax - CloseButSize.y - margin;

            // Warnings and errors at bottom (drawn bottom-up)
            if (filter.AllowedCount == 0)
            {
                string errorText = "AnimalFilterNoneError".Translate();
                float textHeight = Text.CalcHeight(errorText, inRect.width - (smallMargin * 2));
                Rect errorBox = new Rect(inRect.x, bottomY - textHeight - (smallMargin * 2), inRect.width, textHeight + (smallMargin * 2));
                Rect errorLabel = new Rect(errorBox.x + smallMargin, errorBox.y + smallMargin, errorBox.width - (smallMargin * 2), textHeight);

                Widgets.DrawHighlight(errorBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(errorLabel, errorText.Colorize(Color.red));
                bottomY -= (errorBox.height + margin);
            }

            bool noCombat = filter.AllowedCombatAnimals.Count == 0 && filter.AllowedCount > 0;
            if (noCombat)
            {
                string warnText = "AnimalFilterNoCombatWarning".Translate();
                float textHeight = Text.CalcHeight(warnText, inRect.width - (smallMargin * 2));
                Rect warnBox = new Rect(inRect.x, bottomY - textHeight - (smallMargin * 2), inRect.width, textHeight + (smallMargin * 2));
                Rect warnLabel = new Rect(warnBox.x + smallMargin, warnBox.y + smallMargin, warnBox.width - (smallMargin * 2), textHeight);

                Widgets.DrawHighlight(warnBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(warnLabel, warnText.Colorize(Color.yellow));
                bottomY -= (warnBox.height + margin);
            }

            bool noPack = filter.AllowedPackAnimals.Count == 0 && filter.AllowedCount > 0;
            if (noPack)
            {
                string warnText = "AnimalFilterNoPackWarning".Translate();
                float textHeight = Text.CalcHeight(warnText, inRect.width - (smallMargin * 2));
                Rect warnBox = new Rect(inRect.x, bottomY - textHeight - (smallMargin * 2), inRect.width, textHeight + (smallMargin * 2));
                Rect warnLabel = new Rect(warnBox.x + smallMargin, warnBox.y + smallMargin, warnBox.width - (smallMargin * 2), textHeight);

                Widgets.DrawHighlight(warnBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(warnLabel, warnText.Colorize(Color.yellow));
                bottomY -= (warnBox.height + margin);
            }

            // Enable All / Disable All buttons
            Rect enableButton = new Rect(inRect.x, bottomY - bigRowHeight, inRect.width / 2f, bigRowHeight);
            Rect disableButton = new Rect(enableButton.xMax, enableButton.y, enableButton.width, enableButton.height);
            if (Widgets.ButtonText(enableButton, "AnimalEnableAll".Translate()))
            {
                filter.AllowAll();
            }
            if (Widgets.ButtonText(disableButton, "AnimalDisableAll".Translate()))
            {
                filter.DisallowAll();
            }
            bottomY -= (enableButton.height + margin);

            // Scrollable animal list
            float listTop = searchRect.yMax + margin;
            float listHeight = bottomY - listTop;
            Rect scrollOutRect = new Rect(inRect.x, listTop, inRect.width, listHeight);
            Widgets.DrawMenuSection(scrollOutRect);

            Rect innerRect = new Rect(scrollOutRect.x + 2, scrollOutRect.y + 2, scrollOutRect.width - 4, scrollOutRect.height - 4);
            float viewHeight = filteredAnimals.Count * RowHeight;
            float contentWidth = viewHeight > innerRect.height ? innerRect.width - 16f : innerRect.width;
            Rect scrollViewRect = new Rect(innerRect.x, innerRect.y, contentWidth, Math.Max(viewHeight, innerRect.height));

            Widgets.BeginScrollView(innerRect, ref scrollPos, scrollViewRect);

            Text.Font = GameFont.Small;
            for (int i = 0; i < filteredAnimals.Count; i++)
            {
                PawnKindDef animal = filteredAnimals[i];
                Rect row = new Rect(scrollViewRect.x, scrollViewRect.y + (i * RowHeight), scrollViewRect.width, RowHeight);

                if (i % 2 == 0)
                    Widgets.DrawHighlight(row);

                // Icon
                Rect iconRect = new Rect(row.x + margin, row.y, RowHeight, RowHeight);
                Widgets.ThingIcon(iconRect, animal.race);

                // Info button
                Rect infoRect = new Rect(iconRect.xMax, row.y + 2, RowHeight - 4, RowHeight - 4);
                Widgets.InfoCardButton(infoRect, animal.race);

                // Checkbox
                float checkboxSize = 24f;
                Rect checkRect = new Rect(row.xMax - margin - checkboxSize, row.y + (RowHeight - checkboxSize) / 2f, checkboxSize, checkboxSize);
                bool allowed = filter.IsAllowed(animal);
                bool prev = allowed;
                Widgets.Checkbox(checkRect.x, checkRect.y, ref allowed, checkboxSize);
                if (allowed != prev)
                {
                    filter.SetAllowed(animal, allowed);
                }

                // Label
                Rect labelRect = new Rect(infoRect.xMax + margin, row.y, checkRect.x - infoRect.xMax - (margin * 2), RowHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, animal.LabelCap);

                TooltipHandler.TipRegion(labelRect, animal.race.description);
            }

            if (filteredAnimals.Count == 0)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(innerRect, "fcNoAnimalsAvailable".Translate());
            }

            Widgets.EndScrollView();

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void RebuildFilteredList()
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                filteredAnimals = allAnimals;
            }
            else
            {
                filteredAnimals = allAnimals
                    .Where(a => (a.label ?? a.defName).IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }
    }
}
