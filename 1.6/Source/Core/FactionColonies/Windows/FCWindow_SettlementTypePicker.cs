using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCWindow_SettlementTypePicker : Window
    {
        private readonly Action<WorldSettlementDef> onSelect;
        private readonly List<WorldSettlementDef> allTypes;
        private Vector2 scrollPos;

        private const float margin = 5f;
        private const float TitleHeight = 35f;
        private const float NameHeight = 22f;
        private const float ResourceIconSize = 18f;
        private const float ResourceIconGap = 3f;
        private const float ResourceRowHeight = 22f;
        private const float AccentBarWidth = 4f;
        private const float LockReasonHeight = 18f;
        private const float RowPadding = 6f;
        private const float SeparatorHeight = 1f;

        public override Vector2 InitialSize => new Vector2(480f, 550f);

        public FCWindow_SettlementTypePicker(Action<WorldSettlementDef> onSelect)
        {
            this.onSelect = onSelect;
            draggable = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = false;

            allTypes = DefDatabase<WorldSettlementDef>.AllDefs
                .Where(d => d.available)
                .OrderBy(d => d.IsUnlocked() ? 0 : 1)
                .ThenBy(d => d.LabelCap.ToString())
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(0, 0, inRect.width, TitleHeight), "FCPickSettlementType".Translate());

            // Scroll view
            float listTop = TitleHeight + margin;
            float listHeight = inRect.height - listTop;
            Rect scrollOutRect = new Rect(0, listTop, inRect.width, listHeight);

            float contentWidth = scrollOutRect.width - 16f;
            float totalHeight = 0f;
            foreach (WorldSettlementDef def in allTypes)
            {
                totalHeight += GetRowHeight(def, contentWidth) + SeparatorHeight;
            }

            Rect scrollViewRect = new Rect(0, 0, contentWidth, Mathf.Max(totalHeight, listHeight));
            Widgets.BeginScrollView(scrollOutRect, ref scrollPos, scrollViewRect);

            float curY = 0f;
            for (int i = 0; i < allTypes.Count; i++)
            {
                WorldSettlementDef def = allTypes[i];
                float rowHeight = GetRowHeight(def, contentWidth);
                Rect rowRect = new Rect(0, curY, contentWidth, rowHeight);

                if (rowRect.yMax >= scrollPos.y && rowRect.y <= scrollPos.y + listHeight)
                {
                    DrawSettlementTypeRow(rowRect, def, i);
                }

                curY += rowHeight;

                // Separator line
                if (i < allTypes.Count - 1)
                {
                    GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    Widgets.DrawLineHorizontal(AccentBarWidth + margin, curY, contentWidth - AccentBarWidth - margin * 2);
                    GUI.color = Color.white;
                    curY += SeparatorHeight;
                }
            }

            Widgets.EndScrollView();

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private float GetRowHeight(WorldSettlementDef def, float width)
        {
            float contentWidth = width - AccentBarWidth - margin * 3;

            // Description height
            Text.Font = GameFont.Tiny;
            float descHeight = Text.CalcHeight(def.description, contentWidth);

            float height = RowPadding + NameHeight + descHeight + margin;

            // Resource icons row
            if (def.resources != null && def.resources.Count > 0)
            {
                height += ResourceRowHeight;
            }

            // Lock reason
            string lockedReason;
            if (!def.IsUnlocked(out lockedReason))
            {
                Text.Font = GameFont.Tiny;
                height += Text.CalcHeight(lockedReason, contentWidth) + margin;
            }

            height += RowPadding;
            return height;
        }

        private void DrawSettlementTypeRow(Rect rect, WorldSettlementDef def, int index)
        {
            string lockedReason;
            bool unlocked = def.IsUnlocked(out lockedReason);

            // Background
            if (unlocked && Mouse.IsOver(rect))
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (index % 2 == 0)
            {
                Widgets.DrawHighlight(rect);
            }

            // Accent bar
            if (def.accentColor.HasValue)
            {
                GUI.color = def.accentColor.Value;
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, AccentBarWidth, rect.height), def.accentColor.Value);
                GUI.color = Color.white;
            }

            float xOffset = rect.x + AccentBarWidth + margin;
            float contentWidth = rect.width - AccentBarWidth - margin * 3;
            float curY = rect.y + RowPadding;

            if (!unlocked)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
            }

            // Name
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(xOffset, curY, contentWidth, NameHeight), def.LabelCap);
            curY += NameHeight;

            // Description
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            float descHeight = Text.CalcHeight(def.description, contentWidth);
            Widgets.Label(new Rect(xOffset, curY, contentWidth, descHeight), def.description);
            curY += descHeight + margin;

            // Resource icons
            if (def.resources != null && def.resources.Count > 0)
            {
                DrawResourceRow(new Rect(xOffset, curY, contentWidth, ResourceRowHeight), def);
                curY += ResourceRowHeight;
            }

            GUI.color = Color.white;

            // Lock reason (drawn in red, after resetting GUI.color)
            if (!unlocked && lockedReason != null)
            {
                curY += margin;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = new Color(0.8f, 0.2f, 0.2f);
                float reasonHeight = Text.CalcHeight(lockedReason, contentWidth);
                Widgets.Label(new Rect(xOffset, curY, contentWidth, reasonHeight), lockedReason);
                GUI.color = Color.white;
            }

            // Click handling
            if (unlocked && Widgets.ButtonInvisible(rect))
            {
                onSelect(def);
                Close();
            }
            else if (!unlocked)
            {
                TooltipHandler.TipRegion(rect, lockedReason);
            }
        }

        private void DrawResourceRow(Rect rect, WorldSettlementDef def)
        {
            float xCursor = rect.x;

            // "Resources:" label
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            float labelWidth = Text.CalcSize("FCSettlementResources".Translate()).x + margin;
            Widgets.Label(new Rect(xCursor, rect.y, labelWidth, rect.height), "FCSettlementResources".Translate());
            xCursor += labelWidth;

            foreach (ResourceAvailability ra in def.resources)
            {
                if (ra.resourceDef == null) continue;

                // Icon
                Rect iconRect = new Rect(xCursor, rect.y + (rect.height - ResourceIconSize) / 2f, ResourceIconSize, ResourceIconSize);
                GUI.DrawTexture(iconRect, ra.resourceDef.Icon);
                TooltipHandler.TipRegion(iconRect, GetResourceTooltip(ra));
                xCursor += ResourceIconSize;

                // Bonus text
                string bonusText = GetBonusText(ra);
                if (bonusText != null)
                {
                    Text.Font = GameFont.Tiny;
                    float bonusWidth = Text.CalcSize(bonusText).x + 2f;
                    Widgets.Label(new Rect(xCursor, rect.y, bonusWidth, rect.height), bonusText);
                    xCursor += bonusWidth;
                }

                xCursor += ResourceIconGap;
            }
        }

        private static string GetResourceTooltip(ResourceAvailability ra)
        {
            string tooltip = ra.resourceDef.LabelCap;
            if (ra.additive != 0)
                tooltip += "\n+" + ra.additive;
            if (ra.multiplier != 1)
                tooltip += "\nx" + ra.multiplier;
            return tooltip;
        }

        private static string GetBonusText(ResourceAvailability ra)
        {
            if (ra.additive != 0 && ra.multiplier != 1)
                return "+" + ra.additive + " x" + ra.multiplier;
            if (ra.additive != 0)
                return "+" + ra.additive;
            if (ra.multiplier != 1)
                return "x" + ra.multiplier;
            return null;
        }
    }
}
