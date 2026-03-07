using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace FactionColonies
{
    public class FCOptionDef : Def
    {
        public FCOptionDef()
        {
        }

        public float baseChanceOfSuccess;
        public string affectingVariable = null;
        public int silverCost = 0;
        public FCEventDef parentEvent;
        public FCEventDef successEvent = null;
        public FCEventDef failEvent = null;
    }

    [DefOf]
    public class FCOptionDefOf
    {
        static FCOptionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCOptionDefOf));
        }
    }

    public class FCOptionWindow : Window
    {
        // Layout constants
        private const float WindowWidth = 520f;
        private const float Padding = 10f;
        private const float AccentBarHeight = 4f;
        private const float OptionSpacing = 8f;
        private const float OptionInnerPadding = 8f;
        private const float MinOptionHeight = 64f;
        private const float MetadataRowHeight = 20f;
        private const float StripeWidth = 3f;
        private const float SilverIconSize = 16f;
        private const float SettlementButtonHeight = 24f;
        private const float SettlementButtonSpacing = 4f;
        private const float MaxWindowHeight = 700f;

        public List<FCOptionDef> options = new List<FCOptionDef>();
        public string header;
        public string desc;
        public FCEvent parentEvent;

        private Color categoryColor;
        private List<WorldSettlementFC> affectedSettlements;
        private float cachedWindowHeight;
        private float cachedTitleHeight;
        private float cachedDescHeight;
        private float[] cachedOptionLabelHeights;

        public override Vector2 InitialSize
        {
            get { return new Vector2(WindowWidth, cachedWindowHeight > 0f ? cachedWindowHeight : 400f); }
        }

        public FCOptionWindow(FCEventDef evt, FCEvent parentEvent)
        {
            this.forcePause = !FCSettings.disableForcedPausingDuringEvents;
            this.draggable = true;
            this.doCloseX = false;
            this.preventCameraMotion = false;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.closeOnClickedOutside = false;

            this.header = evt.label;
            this.options = evt.options;
            this.desc = string.IsNullOrEmpty(evt.optionDescription) ? evt.desc : evt.optionDescription;
            this.parentEvent = parentEvent;

            // Category color
            if (parentEvent != null)
            {
                this.categoryColor = AccentUtil.GetEventCategoryColor(parentEvent);
            }
            else
            {
                FCEventCategoryDef cat = evt.category ?? FCEventCategoryDefOf.EC_Other;
                this.categoryColor = cat.color;
            }

            // Affected settlements
            this.affectedSettlements = new List<WorldSettlementFC>();
            if (parentEvent != null && parentEvent.settlementTraitLocations != null)
            {
                foreach (WorldSettlementFC s in parentEvent.settlementTraitLocations)
                {
                    if (s != null)
                    {
                        affectedSettlements.Add(s);
                    }
                }
            }

            // Measure layout
            MeasureLayout();
        }

        private void MeasureLayout()
        {
            float contentWidth = WindowWidth - (Margin * 2);
            float textWidth = contentWidth - (Padding * 2);

            Text.Font = GameFont.Medium;
            cachedTitleHeight = Text.CalcHeight(header, textWidth);

            Text.Font = GameFont.Small;
            cachedDescHeight = Text.CalcHeight(desc, textWidth);

            cachedOptionLabelHeights = new float[options.Count];
            float labelWidth = contentWidth - StripeWidth - (OptionInnerPadding * 2);
            Text.Font = GameFont.Small;
            for (int i = 0; i < options.Count; i++)
            {
                cachedOptionLabelHeights[i] = Text.CalcHeight(options[i].label, labelWidth);
            }

            // Sum up total height
            float y = AccentBarHeight + Padding;     // accent bar + top padding
            y += cachedTitleHeight;                    // title
            y += Padding;                             // gap
            y += cachedDescHeight;                     // description

            if (affectedSettlements.Count > 0)
            {
                y += 8f;                              // gap
                y += 16f;                             // "Affecting:" label
                y += 2f;                              // gap
                y += SettlementButtonHeight;           // settlement buttons row
            }

            y += Padding;                             // gap before separator
            y += 1f;                                  // separator
            y += Padding;                             // gap after separator

            for (int i = 0; i < options.Count; i++)
            {
                if (i > 0) y += OptionSpacing;
                float cardH = OptionInnerPadding + cachedOptionLabelHeights[i] + 6f + MetadataRowHeight + OptionInnerPadding;
                y += Math.Max(cardH, MinOptionHeight);
            }

            y += Padding;                             // bottom margin
            cachedWindowHeight = Math.Min(y + (Margin * 2), MaxWindowHeight);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            windowRect = new Rect(
                (UI.screenWidth - WindowWidth) / 2f,
                (UI.screenHeight - cachedWindowHeight) / 2f,
                WindowWidth,
                cachedWindowHeight
            );
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Color colorBefore = GUI.color;

            float contentWidth = inRect.width;
            float textWidth = contentWidth - (Padding * 2);
            float curY = inRect.y;

            // === Accent bar ===
            Widgets.DrawBoxSolid(new Rect(inRect.x, curY, contentWidth, AccentBarHeight), categoryColor);
            curY += AccentBarHeight + Padding;

            // === Title ===
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(inRect.x + Padding, curY, textWidth, cachedTitleHeight), header);
            curY += cachedTitleHeight + Padding;

            // === Description ===
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(new Rect(inRect.x + Padding, curY, textWidth, cachedDescHeight), desc);
            GUI.color = colorBefore;
            curY += cachedDescHeight;

            // === Affected settlements (clickable buttons) ===
            if (affectedSettlements.Count > 0)
            {
                curY += 8f;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                Widgets.Label(new Rect(inRect.x + Padding, curY, 60f, 16f), "FCEventAffecting".Translate());
                GUI.color = colorBefore;
                curY += 16f + 2f;

                float btnX = inRect.x + Padding;
                Text.Font = GameFont.Tiny;
                for (int i = 0; i < affectedSettlements.Count; i++)
                {
                    WorldSettlementFC settlement = affectedSettlements[i];
                    float btnWidth = Text.CalcSize(settlement.Name).x + 16f;
                    Rect btnRect = new Rect(btnX, curY, btnWidth, SettlementButtonHeight);

                    if (UIUtil.ButtonFlat(btnRect, settlement.Name, categoryColor))
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(settlement));
                    }

                    btnX += btnWidth + SettlementButtonSpacing;
                }
                curY += SettlementButtonHeight;
            }

            // === Separator line ===
            curY += Padding;
            Color dimCategoryColor = new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.3f);
            Widgets.DrawBoxSolid(new Rect(inRect.x + Padding, curY, textWidth, 1f), dimCategoryColor);
            curY += 1f + Padding;

            // === Option cards ===
            int currentSilver = PaymentUtil.GetSilver();

            for (int i = 0; i < options.Count; i++)
            {
                if (i > 0) curY += OptionSpacing;

                FCOptionDef opt = options[i];
                bool affordable = currentSilver >= opt.silverCost;
                bool isFree = opt.silverCost <= 0;

                // Card height
                float cardH = OptionInnerPadding + cachedOptionLabelHeights[i] + 6f + MetadataRowHeight + OptionInnerPadding;
                cardH = Math.Max(cardH, MinOptionHeight);

                Rect cardRect = new Rect(inRect.x, curY, contentWidth, cardH);

                // Card background
                float bgVal = affordable ? 0.18f : 0.12f;
                Widgets.DrawBoxSolid(cardRect, new Color(bgVal, bgVal, bgVal));

                // Left accent stripe
                Color stripeColor = affordable
                    ? categoryColor
                    : new Color(categoryColor.r * 0.4f, categoryColor.g * 0.4f, categoryColor.b * 0.4f);
                Widgets.DrawBoxSolid(new Rect(cardRect.x, cardRect.y, StripeWidth, cardRect.height), stripeColor);

                // Inner content
                float innerX = cardRect.x + StripeWidth + OptionInnerPadding;
                float innerW = cardRect.width - StripeWidth - (OptionInnerPadding * 2);
                float innerY = cardRect.y + OptionInnerPadding;

                // Option label text
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = affordable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(new Rect(innerX, innerY, innerW, cachedOptionLabelHeights[i]), opt.label);
                GUI.color = colorBefore;
                innerY += cachedOptionLabelHeights[i] + 6f;

                // Metadata row: success hint (left) + cost (right)
                Rect metaRect = new Rect(innerX, innerY, innerW, MetadataRowHeight);

                // Success hint
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                string successLabel;
                Color successColor;
                GetSuccessHint(opt.baseChanceOfSuccess, out successLabel, out successColor);
                if (!affordable) successColor = new Color(successColor.r * 0.5f, successColor.g * 0.5f, successColor.b * 0.5f);
                GUI.color = successColor;
                Widgets.Label(new Rect(metaRect.x, metaRect.y, metaRect.width * 0.6f, metaRect.height), successLabel);
                GUI.color = colorBefore;

                // Silver cost (right-aligned)
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;
                if (isFree)
                {
                    GUI.color = affordable ? AccentUtil.Income : new Color(0.3f, 0.5f, 0.3f);
                    Widgets.Label(metaRect, "FCEventOptionFree".Translate());
                    GUI.color = colorBefore;
                }
                else
                {
                    // Draw silver icon + cost text
                    GUI.color = affordable ? Color.white : AccentUtil.Expense;
                    string costStr = opt.silverCost.ToString();
                    float costTextW = Text.CalcSize(costStr).x;
                    Rect costTextRect = new Rect(metaRect.xMax - costTextW, metaRect.y, costTextW, metaRect.height);
                    Widgets.Label(costTextRect, costStr);

                    Rect iconRect = new Rect(
                        costTextRect.x - SilverIconSize - 2f,
                        metaRect.y + (metaRect.height - SilverIconSize) / 2f,
                        SilverIconSize, SilverIconSize
                    );
                    GUI.color = affordable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                    GUI.DrawTexture(iconRect, ThingDefOf.Silver.uiIcon);
                    GUI.color = colorBefore;

                    if (!affordable)
                    {
                        UIUtil.TipRegionByText(cardRect, "FCNotEnoughSilverOption".Translate());
                    }
                }

                // Hover effect
                if (Mouse.IsOver(cardRect) && affordable)
                {
                    Widgets.DrawBoxSolid(cardRect, new Color(1f, 1f, 1f, 0.04f));
                    Widgets.DrawBox(cardRect);
                }

                // Click handler
                if (Widgets.ButtonInvisible(cardRect))
                {
                    if (affordable)
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        PaymentUtil.PaySilver(opt.silverCost, PaymentUtil.Reason_EventOption);
                        FCEventMaker.CalculateSuccess(opt, parentEvent);
                        Find.WindowStack.TryRemove(this);
                    }
                    else
                    {
                        Messages.Message("FCNotEnoughSilverOption".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }

                curY += cardH;
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
            GUI.color = colorBefore;
        }

        private static void GetSuccessHint(float chance, out string label, out Color color)
        {
            if (chance >= 100f)
            {
                label = "FCEventSuccessGuaranteed".Translate();
                color = AccentUtil.StatGood;
            }
            else if (chance >= 75f)
            {
                label = "FCEventSuccessLikely".Translate();
                color = AccentUtil.StatGood;
            }
            else if (chance >= 40f)
            {
                label = "FCEventSuccessUncertain".Translate();
                color = AccentUtil.StatMedium;
            }
            else
            {
                label = "FCEventSuccessRisky".Translate();
                color = AccentUtil.StatBad;
            }
        }
    }
}
