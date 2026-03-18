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
        private const float fullheight = 370f;
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
        }

        private void ApplyChanges()
        {
            if (name.NullOrEmpty()) name = "PlayerFaction".Translate();

            faction.title = title;
            faction.name = name;
            faction.factionIconPath = tempFactionIconPath;
            faction.factionIcon = tempFactionIcon;

            Faction fact = FactionCache.PlayerColonyFaction;
            if (fact != null)
            {
                fact.Name = name;
                faction.UpdateFactionIcon(ref fact, "FactionIcons/" + tempFactionIconPath);
            }
            else
            {
                LogUtil.Error("PlayerColonyFaction is null - cannot sync faction name/icon!");
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

            Rect buttonAllowedRaces = new Rect(25, labelFactionIcon.yMax + margin, 200, 40);

            Rect labelPickTrait = new Rect(400, headerHeight + (margin * 3), 400, 60);

            Rect menusectionTrait = new Rect(400, 200, 400, 300);

            Rect buttonConfirm = new Rect((inRect.xMax - 200f) / 2f, 300, 200, 30);


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
    }
}