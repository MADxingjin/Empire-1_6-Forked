using FactionColonies.util;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FactionCustomizeWindowFc : Window
    {
        private const float fullwidth = 500f;
        private const float fullheight = 415f;
        private const float margin = 8f;
        private const float smallMargin = 4f;
        private const float cardGap = 8f;
        private const float cardPadding = 8f;
        private const float swatchSize = 30f;
        private const float colorsRowHeight = 105f;
        private const float rowHeight = 60f;
        private const float bottomRowHeight = 80f;

        public override Vector2 InitialSize => new Vector2(fullwidth, fullheight);

        private FactionFC faction;

        private string tempName;
        private string tempTitle;
        private Texture2D tempFactionIcon;
        private string tempFactionIconPath;

        private Color tempPrimaryColor;
        private bool tempHasPrimaryColor;
        private Color tempSecondaryColor;
        private bool tempHasSecondaryColor;

        public FactionCustomizeWindowFc(FactionFC faction)
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            this.faction = faction;

            tempName = faction.name;
            tempTitle = faction.title;
            tempFactionIcon = faction.factionIcon;
            tempFactionIconPath = faction.factionIconPath;

            tempPrimaryColor = faction.factionColorPrimary;
            tempHasPrimaryColor = faction.hasFactionColor;
            tempSecondaryColor = faction.factionColorSecondary;
            tempHasSecondaryColor = faction.hasFactionColorSecondary;
        }

        private void ApplyChanges()
        {
            if (tempName.NullOrEmpty()) tempName = "PlayerFaction".Translate();

            faction.title = tempTitle;
            faction.name = tempName;
            faction.factionIconPath = tempFactionIconPath;
            faction.factionIcon = tempFactionIcon;

            faction.factionColorPrimary = tempPrimaryColor;
            faction.hasFactionColor = tempHasPrimaryColor;
            faction.factionColorSecondary = tempSecondaryColor;
            faction.hasFactionColorSecondary = tempHasSecondaryColor;

            Faction fact = FactionCache.PlayerColonyFaction;
            if (fact != null)
            {
                fact.Name = tempName;
                faction.UpdateFactionIcon(ref fact, "FactionIcons/" + tempFactionIconPath);

                if (tempHasPrimaryColor)
                    fact.color = tempPrimaryColor;
                else
                    fact.color = null;
            }
            else
            {
                LogUtil.Error("PlayerColonyFaction is null - cannot sync faction name/icon!");
            }

            if (faction.militaryCustomizationUtil?.units != null)
            {
                foreach (MilUnitFC unit in faction.militaryCustomizationUtil.units)
                    unit.MarkEquipmentDirty();
            }
        }

        public override void OnAcceptKeyPressed()
        {
            ApplyChanges();
            base.OnAcceptKeyPressed();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            float y = inRect.y;

            // 1. Header
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 36f), "CustomizeFaction".Translate());
            y += 36f + smallMargin;
            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
            y += margin;

            // 2. Name / Title Row
            float halfWidth = (inRect.width - cardGap) / 2f;
            Rect nameCard = new Rect(inRect.x, y, halfWidth, rowHeight);
            Rect titleCard = new Rect(inRect.x + halfWidth + cardGap, y, halfWidth, rowHeight);
            DrawNameCard(nameCard);
            DrawTitleCard(titleCard);
            y += rowHeight + cardGap;

            // 3. Icon + Colors Row
            float iconCardWidth = 80f;
            Rect iconCard = new Rect(inRect.x, y, iconCardWidth, colorsRowHeight);
            Rect colorsCard = new Rect(inRect.x + iconCardWidth + cardGap, y, inRect.width - iconCardWidth - cardGap, colorsRowHeight);
            DrawIconCard(iconCard);
            DrawColorsCard(colorsCard);
            y += colorsRowHeight + cardGap;

            // 4. Xenotypes + Policies Row
            Rect xenoCard = new Rect(inRect.x, y, halfWidth, bottomRowHeight);
            Rect policyCard = new Rect(inRect.x + halfWidth + cardGap, y, halfWidth, bottomRowHeight);
            DrawXenotypesCard(xenoCard);
            DrawPoliciesCard(policyCard);
            y += bottomRowHeight + cardGap;

            // 5. Confirm Button
            float confirmWidth = 200f;
            float confirmHeight = 30f;
            Rect confirmRect = new Rect((inRect.width - confirmWidth) / 2f, inRect.yMax - confirmHeight - margin, confirmWidth, confirmHeight);
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(confirmRect, "ConfirmChanges".Translate()))
            {
                ApplyChanges();
                Find.WindowStack.TryRemove(this);
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void DrawNameCard(Rect rect)
        {
            DrawCard(rect);
            DrawCardLabel(rect, "FactionName".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect fieldRect = new Rect(rect.x + cardPadding, rect.y + cardPadding + 18f, rect.width - cardPadding * 2, 28f);
            tempName = Widgets.TextField(fieldRect, tempName);
        }

        private void DrawTitleCard(Rect rect)
        {
            DrawCard(rect);
            DrawCardLabel(rect, "FactionTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect fieldRect = new Rect(rect.x + cardPadding, rect.y + cardPadding + 18f, rect.width - cardPadding * 2, 28f);
            tempTitle = Widgets.TextField(fieldRect, tempTitle);
        }

        private void DrawIconCard(Rect rect)
        {
            DrawCard(rect);
            DrawCardLabel(rect, "fcInsignia".Translate());
            float btnSize = 36f;
            Rect btnRect = new Rect(rect.x + (rect.width - btnSize) / 2f, rect.y + cardPadding + 20f, btnSize, btnSize);
            if (Widgets.ButtonImage(btnRect, tempFactionIcon))
            {
                List<FloatMenuOption> list = TexLoad.factionIcons.Select(texture => new FloatMenuOption(texture.name, delegate
                {
                    tempFactionIcon = texture;
                    tempFactionIconPath = texture.name;
                }, texture, Color.white)).ToList();

                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        private void DrawColorsCard(Rect rect)
        {
            DrawCard(rect);
            DrawCardLabel(rect, "fcColors".Translate());

            float contentY = rect.y + cardPadding + 18f;
            float swatchGroupX = rect.x + cardPadding;

            // Primary swatch
            Rect primarySwatchRect = new Rect(swatchGroupX, contentY, swatchSize, swatchSize);
            DrawColorSwatch(primarySwatchRect, tempPrimaryColor, tempHasPrimaryColor);
            if (Widgets.ButtonInvisible(primarySwatchRect))
            {
                OpenFactionColorPicker(true);
            }

            // Primary label + action
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Rect primaryLabelRect = new Rect(primarySwatchRect.x - 10f, primarySwatchRect.yMax + 2f, swatchSize + 20f, 18f);
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(primaryLabelRect, "fcPrimaryColor".Translate());
            GUI.color = Color.white;

            if (tempHasPrimaryColor)
            {
                Rect clearRect = new Rect(primarySwatchRect.x, primaryLabelRect.yMax, swatchSize, 18f);
                GUI.color = new Color(0.72f, 0.53f, 0.04f);
                if (Widgets.ButtonText(clearRect, "Clear", drawBackground: false))
                {
                    tempHasPrimaryColor = false;
                    tempPrimaryColor = Color.white;
                    // Clearing primary also clears secondary
                    tempHasSecondaryColor = false;
                    tempSecondaryColor = Color.white;
                }
                GUI.color = Color.white;
            }
            else
            {
                Rect notSetRect = new Rect(primarySwatchRect.x - 8f, primaryLabelRect.yMax, swatchSize + 16f, 18f);
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(notSetRect, "fcColorNotSet".Translate());
                GUI.color = Color.white;
            }

            TooltipHandler.TipRegion(primarySwatchRect, "fcPrimaryColorDesc".Translate());

            // Secondary swatch
            float secondaryX = swatchGroupX + swatchSize + 24f;
            Rect secondarySwatchRect = new Rect(secondaryX, contentY, swatchSize, swatchSize);
            bool secondaryDisabled = !tempHasPrimaryColor;

            DrawColorSwatch(secondarySwatchRect, tempSecondaryColor, tempHasSecondaryColor);
            if (secondaryDisabled)
            {
                // Gray overlay on disabled secondary (drawn after swatch so checkerboard is clean)
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                GUI.DrawTexture(secondarySwatchRect, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }

            if (!secondaryDisabled && Widgets.ButtonInvisible(secondarySwatchRect))
            {
                OpenFactionColorPicker(false);
            }

            // Secondary label + action
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Rect secondaryLabelRect = new Rect(secondarySwatchRect.x - 12f, secondarySwatchRect.yMax + 2f, swatchSize + 24f, 18f);
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(secondaryLabelRect, "fcSecondaryColor".Translate());
            GUI.color = Color.white;

            if (tempHasSecondaryColor)
            {
                Rect clearRect = new Rect(secondarySwatchRect.x, secondaryLabelRect.yMax, swatchSize, 18f);
                GUI.color = new Color(0.72f, 0.53f, 0.04f);
                if (Widgets.ButtonText(clearRect, "Clear", drawBackground: false))
                {
                    tempHasSecondaryColor = false;
                    tempSecondaryColor = Color.white;
                }
                GUI.color = Color.white;
            }
            else
            {
                Rect notSetRect = new Rect(secondarySwatchRect.x - 8f, secondaryLabelRect.yMax, swatchSize + 16f, 18f);
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(notSetRect, "fcColorNotSet".Translate());
                GUI.color = Color.white;
            }

            if (secondaryDisabled)
            {
                TooltipHandler.TipRegion(secondarySwatchRect, "fcSetPrimaryFirst".Translate());
            }
            else
            {
                TooltipHandler.TipRegion(secondarySwatchRect, "fcSecondaryColorDesc".Translate());
            }

            // Info box when both are unset (to the right of swatches)
            if (!tempHasPrimaryColor && !tempHasSecondaryColor)
            {
                float infoX = secondaryX + swatchSize + 16f;
                float infoWidth = rect.xMax - cardPadding - infoX;
                if (infoWidth > 40f)
                {
                    Rect infoRect = new Rect(infoX, contentY, infoWidth, swatchSize + 16f);
                    Widgets.DrawBoxSolid(infoRect, new Color(0.18f, 0.14f, 0.10f, 0.6f));
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = new Color(0.72f, 0.53f, 0.04f);
                    Widgets.Label(infoRect.ContractedBy(4f), "fcColorsRandomInfo".Translate());
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawColorSwatch(Rect rect, Color color, bool isSet)
        {
            if (isSet)
            {
                // Solid color with white border
                Widgets.DrawBoxSolidWithOutline(rect, color, Color.white, 2);

                // Green checkmark badge (top-right)
                float badgeSize = 12f;
                Rect badge = new Rect(rect.xMax - badgeSize + 3f, rect.y - 3f, badgeSize, badgeSize);
                Widgets.DrawBoxSolid(badge, new Color(0.2f, 0.7f, 0.2f));
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.white;
                Widgets.Label(badge, "\u2713");
            }
            else
            {
                // Checkerboard with gray border
                GUI.DrawTexture(rect, TexLoad.checkerboard);
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                Widgets.DrawBox(rect, 2);
                GUI.color = Color.white;
            }
        }

        private void DrawXenotypesCard(Rect rect)
        {
            DrawCard(rect);
            DrawCardLabel(rect, "AllowedXenotypes".Translate());
            float btnWidth = rect.width - cardPadding * 2;
            float btnHeight = 28f;
            Rect btnRect = new Rect(rect.x + cardPadding, rect.y + (rect.height - btnHeight) / 2f + 8f, btnWidth, btnHeight);
            if (UIUtil.ButtonFlat(btnRect, "fcConfigure".Translate()))
            {
                Find.WindowStack.Add(new FCCustomizeXenotypesWindow());
            }
        }

        private void DrawPoliciesCard(Rect rect)
        {
            DrawCard(rect);

            bool policiesSelected = faction.policies.Count >= FCSettings.maxPolicyCount;
            float contentY;

            if (policiesSelected)
            {
                // No card label when policies are selected — icons are self-explanatory
                contentY = rect.y + (rect.height - 50f) / 2f;
                // Show selected policy icons
                float totalIconWidth = faction.policies.Count * 32f + (faction.policies.Count - 1) * 5f;
                float startX = rect.x + (rect.width - totalIconWidth) / 2f;

                for (int i = 0; i < faction.policies.Count; i++)
                {
                    FCPolicy policy = faction.policies[i];
                    Rect iconRect = new Rect(startX + i * (32f + 5f), contentY, 32f, 32f);
                    Widgets.DrawBoxSolid(iconRect, new Color(0.15f, 0.15f, 0.15f));

                    Texture2D icon = policy.def.IconLight;
                    if (icon != null)
                    {
                        GUI.DrawTexture(iconRect.ContractedBy(2f), icon);
                    }
                    TooltipHandler.TipRegion(iconRect, policy.def.PolicyText());
                }

                // Combined label below
                string policyNames = string.Join(" \u00b7 ", faction.policies.Select(p => p.def.LabelCap.ToString()));
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                Widgets.Label(new Rect(rect.x + cardPadding, contentY + 34f, rect.width - cardPadding * 2, 16f), policyNames);
                GUI.color = Color.white;
            }
            else
            {
                DrawCardLabel(rect, "FCSelectPolicies".Translate());
                contentY = rect.y + cardPadding + 18f;

                // Show placeholder slots
                float slotSize = 28f;
                int slotCount = FCSettings.maxPolicyCount;
                float totalSlotWidth = slotCount * slotSize + (slotCount - 1) * 5f;
                float startX = rect.x + (rect.width - totalSlotWidth) / 2f;

                for (int i = 0; i < slotCount; i++)
                {
                    Rect slotRect = new Rect(startX + i * (slotSize + 5f), contentY, slotSize, slotSize);
                    GUI.color = new Color(0.3f, 0.3f, 0.3f);
                    Widgets.DrawBox(slotRect, 2);
                    GUI.color = new Color(0.4f, 0.4f, 0.4f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(slotRect, "?");
                    GUI.color = Color.white;
                }

                // "Click to select" label
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(new Rect(rect.x + cardPadding, contentY + slotSize + 4f, rect.width - cardPadding * 2, 16f), "fcClickToSelect".Translate());
                GUI.color = Color.white;

                // Clicking anywhere in the card opens the policy window
                if (Widgets.ButtonInvisible(rect))
                {
                    Find.WindowStack.Add(new FactionCustomizePoliciesWindowFC(faction));
                }
            }
        }

        private void OpenFactionColorPicker(bool primary)
        {
            Color current = primary
                ? (tempHasPrimaryColor ? tempPrimaryColor : Color.white)
                : (tempHasSecondaryColor ? tempSecondaryColor : Color.white);

            string header = primary
                ? "fcChoosePrimaryColor".Translate()
                : "fcChooseSecondaryColor".Translate();

            Find.WindowStack.Add(new FCWindow_ColorPicker(
                header,
                current,
                delegate(Color color)
                {
                    if (primary)
                    {
                        tempPrimaryColor = color;
                        tempHasPrimaryColor = true;
                    }
                    else
                    {
                        tempSecondaryColor = color;
                        tempHasSecondaryColor = true;
                    }
                }
            ));
        }

        private static void DrawCard(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
        }

        private static void DrawCardLabel(Rect card, string label)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(card.x + cardPadding, card.y + cardPadding, card.width - cardPadding * 2, 16f), label.ToUpper());
        }
    }
}
