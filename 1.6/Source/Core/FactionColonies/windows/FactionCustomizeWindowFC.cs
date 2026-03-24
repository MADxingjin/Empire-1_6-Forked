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
        private const float fullwidth = 400f;
        private const float fullheight = 460f;
        private const float margin = 5f;
        public override Vector2 InitialSize => new Vector2(fullwidth, fullheight);

        //declare variables

        private FactionFC faction;

        public string desc;
        public string header;

        private string name;
        private string title;
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
            header = "CustomizeFaction".Translate();
            name = faction.name;
            title = faction.title;

            tempFactionIcon = faction.factionIcon;
            tempFactionIconPath = faction.factionIconPath;

            tempPrimaryColor = faction.factionColorPrimary;
            tempHasPrimaryColor = faction.hasFactionColor;
            tempSecondaryColor = faction.factionColorSecondary;
            tempHasSecondaryColor = faction.hasFactionColorSecondary;
        }

        private void ApplyChanges()
        {
            if (name.NullOrEmpty()) name = "PlayerFaction".Translate();

            faction.title = title;
            faction.name = name;
            faction.factionIconPath = tempFactionIconPath;
            faction.factionIcon = tempFactionIcon;

            faction.factionColorPrimary = tempPrimaryColor;
            faction.hasFactionColor = tempHasPrimaryColor;
            faction.factionColorSecondary = tempSecondaryColor;
            faction.hasFactionColorSecondary = tempHasSecondaryColor;

            Faction fact = FactionCache.PlayerColonyFaction;
            if (fact != null)
            {
                fact.Name = name;
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

            // Invalidate all unit previews so they pick up the new faction colors
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
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //setup all the rects
            Rect labelFaction = new Rect(0, 0, 200, 40);

            float headerHeight = labelFaction.yMax + margin;

            Rect labelFactionName = new Rect(0, headerHeight + (margin * 3), 100, 30);
            Rect textfieldName = new Rect(105, labelFactionName.y, 250, 30);

            Rect labelFactionTitle = new Rect(0, labelFactionName.yMax + margin, 100, 30);
            Rect textfieldTitle = new Rect(105, labelFactionTitle.y, 250, 30);

            Rect labelFactionIcon = new Rect(0, labelFactionTitle.yMax + margin, 100, 30);
            Rect buttonIcon = new Rect(105, labelFactionIcon.y, 30, 30);

            // Primary color row
            Rect labelPrimaryColor = new Rect(0, labelFactionIcon.yMax + margin, 100, 30);
            Rect swatchPrimary = new Rect(105, labelPrimaryColor.y + 4f, 22, 22);

            // Secondary color row
            Rect labelSecondaryColor = new Rect(0, labelPrimaryColor.yMax + margin, 100, 30);
            Rect swatchSecondary = new Rect(105, labelSecondaryColor.y + 4f, 22, 22);

            Rect buttonAllowedRaces = new Rect(25, labelSecondaryColor.yMax + margin, 200, 40);

            Rect labelPickTrait = new Rect(400, headerHeight + (margin * 3), 400, 60);

            Rect menusectionTrait = new Rect(400, 200, 400, 300);

            Rect buttonConfirm = new Rect((inRect.xMax - 200f) / 2f, inRect.height - 50f, 200, 30);


            //Settlement Tax Collection Header
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;

            Widgets.Label(labelFaction, header);
            Widgets.DrawLineHorizontal(labelFaction.x, labelFaction.yMax + margin, fullwidth - (Margin * 2));

            Text.Font = GameFont.Small;
            Widgets.Label(labelFactionName, "FactionName".Translate() + ":");
            name = Widgets.TextField(textfieldName, name);

            Widgets.Label(labelFactionTitle, "FactionTitle".Translate() + ":");
            title = Widgets.TextField(textfieldTitle, title);

            Widgets.Label(labelFactionIcon, "FactionIcon".Translate());
            if (Widgets.ButtonImage(buttonIcon, tempFactionIcon))
            {
                List<FloatMenuOption> list = TexLoad.factionIcons.Select(texture => new FloatMenuOption(texture.name, delegate
                    {
                        tempFactionIcon = texture;
                        tempFactionIconPath = texture.name;
                    }, texture, Color.white)).ToList();

                FloatMenu menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);
            }

            // Primary Color
            Widgets.Label(labelPrimaryColor, "fcPrimaryColor".Translate());
            Color primaryDisplay = tempHasPrimaryColor ? tempPrimaryColor : Color.white;
            Color primaryOutline = tempHasPrimaryColor ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Widgets.DrawBoxSolidWithOutline(swatchPrimary, primaryDisplay, primaryOutline);
            if (Widgets.ButtonInvisible(swatchPrimary))
            {
                OpenFactionColorPicker(true);
            }
            if (tempHasPrimaryColor)
            {
                Rect clearPrimary = new Rect(swatchPrimary.xMax + 6f, labelPrimaryColor.y + 3f, 50f, 24f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonText(clearPrimary, "Clear"))
                {
                    tempHasPrimaryColor = false;
                    tempPrimaryColor = Color.white;
                }
            }
            TooltipHandler.TipRegion(labelPrimaryColor, "fcPrimaryColorDesc".Translate());

            // Secondary Color
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelSecondaryColor, "fcSecondaryColor".Translate());
            Color secondaryDisplay = tempHasSecondaryColor ? tempSecondaryColor : Color.white;
            Color secondaryOutline = tempHasSecondaryColor ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Widgets.DrawBoxSolidWithOutline(swatchSecondary, secondaryDisplay, secondaryOutline);
            if (Widgets.ButtonInvisible(swatchSecondary))
            {
                OpenFactionColorPicker(false);
            }
            if (tempHasSecondaryColor)
            {
                Rect clearSecondary = new Rect(swatchSecondary.xMax + 6f, labelSecondaryColor.y + 3f, 50f, 24f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonText(clearSecondary, "Clear"))
                {
                    tempHasSecondaryColor = false;
                    tempSecondaryColor = Color.white;
                }
            }
            TooltipHandler.TipRegion(labelSecondaryColor, "fcSecondaryColorDesc".Translate());

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (Widgets.ButtonTextSubtle(buttonAllowedRaces, "AllowedXenotypes".Translate()))
            {
                Find.WindowStack.Add(new FCCustomizeXenotypesWindow());
            }

            if (Widgets.ButtonText(buttonConfirm, "ConfirmChanges".Translate()))
            {
                ApplyChanges();
                Find.WindowStack.TryRemove(this);
            }

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void OpenFactionColorPicker(bool primary)
        {
            List<Color> colors = new List<Color> { Color.white };
            foreach (ColorDef cd in DefDatabase<ColorDef>.AllDefsListForReading)
            {
                if (cd.colorType == ColorType.Ideo)
                    colors.Add(cd.color);
            }

            Color current = primary
                ? (tempHasPrimaryColor ? tempPrimaryColor : Color.white)
                : (tempHasSecondaryColor ? tempSecondaryColor : Color.white);

            string header = primary
                ? "fcChoosePrimaryColor".Translate()
                : "fcChooseSecondaryColor".Translate();

            Find.WindowStack.Add(new Dialog_ChooseColor(
                header,
                current,
                colors,
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
    }
}
