using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FactionCustomizeWindowFc : Window
    {
        private const float fullwidth = 838f;
        private const float fullheight = 538f;
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

        float circleX = 500;
        float circleY = 20;
        float circleR = 80;

        string alertText = "";

        bool traitsChosen;

        int numberTraitsSelected;
        bool boolMilitaristic;
        bool boolPacifist;
        bool boolAuthoritarian;
        bool boolEgalitarian;
        bool boolIsolationist;
        bool boolExpansionist;
        bool boolTechnocrat;
        bool boolFeudal;
        bool boolSlaver;

        string policyText = "";


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

            numberTraitsSelected = faction.policies.Count();

            if (numberTraitsSelected != 0)
            {
                foreach (FCPolicy policy in faction.policies)
                {
                    switch (policy.def.defName)
                    {
                        case "militaristic":
                            boolMilitaristic = true;
                            break;
                        case "pacifist":
                            boolPacifist = true;
                            break;
                        case "authoritarian":
                            boolAuthoritarian = true;
                            break;
                        case "egalitarian":
                            boolEgalitarian = true;
                            break;
                        case "isolationist":
                            boolIsolationist = true;
                            break;
                        case "expansionist":
                            boolExpansionist = true;
                            break;
                        case "technocratic":
                            boolTechnocrat = true;
                            break;
                        case "feudal":
                            boolFeudal = true;
                            break;
                        case "slaver":
                            boolSlaver = true;
                            break;
                    }
                }
            }

            if (numberTraitsSelected == 2)
            {
                traitsChosen = true;
            }
            else
            {
                traitsChosen = false;
                faction.policies = new List<FCPolicy>();
            }
        }

        public override void OnAcceptKeyPressed()
        {
            base.OnAcceptKeyPressed();
            faction.title = title;
            faction.name = name;
            FactionCache.PlayerColonyFaction.Name = name;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //setup all the rects
            Rect labelFaction = new Rect(0, 0, 200, 40);

            float headerHeight = labelFaction.yMax + margin;

            Rect labelFactionName = new Rect(0, headerHeight + (margin*3), 100, 30);
            Rect textfieldName = new Rect(105, labelFactionName.y, 250, 30);

            Rect labelFactionTitle = new Rect(0, labelFactionName.yMax + margin, 100, 30);
            Rect textfieldTitle = new Rect(105, labelFactionTitle.y, 250, 30);

            Rect labelFactionIcon = new Rect(0, labelFactionTitle.yMax + margin, 100, 30);
            Rect buttonIcon = new Rect(105, labelFactionIcon.y, 30, 30);

            Rect buttonAllowedRaces = new Rect(25, labelFactionIcon.yMax + margin, 200, 40);

            Rect labelTraits = new Rect(0, 235, 200, 40);
            Rect buttonTrait1 = new Rect(25, 260, 200, 40);
            Rect buttonTrait2 = new Rect(25, 300, 200, 40);

            Rect labelPickTrait = new Rect(400, headerHeight + (margin*3), 400, 60);

            Rect menusectionTrait = new Rect(400, 200, 400, 300);

            Rect buttonConfirm = new Rect(130, 450, 200, 30);

            double traitRotationArc = (double)2 / (double)9; // amount of traits
            float traitButtonXrot = 60f;
            float traitButtonYrot = 60f;
            float traitButtonXbase = 600f;
            float traitButtonYbase = labelPickTrait.yMax + margin + traitButtonXrot;
            float traitButtonSize = 30f;
            float traitButtonAreaWidth = 2 * traitButtonXrot + traitButtonSize;
            float traitButtonAreaHeight = 2 * traitButtonYrot + traitButtonSize;

            Rect buttonMilitaristic = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 0 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 0 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonAuthoritarian = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 1 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 1 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonIsolationist = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 2 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 2 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonFeudal = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 3 * Math.PI)),
                                            (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 3 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonPacifist = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 4 * Math.PI)),
                                            (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 4 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonEgalitarian = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 5 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 5 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonExpansionist = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 6 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 6 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonTechnocrat = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 7 * Math.PI)),
                                                (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 7 * Math.PI)), traitButtonSize, traitButtonSize);

            Rect buttonSlaver = new Rect((float)(traitButtonXbase + traitButtonXrot * Math.Cos(traitRotationArc * 8 * Math.PI)),
                                            (float)(traitButtonYbase + traitButtonYrot * Math.Sin(traitRotationArc * 8 * Math.PI)), traitButtonSize, traitButtonSize);


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
                Faction fact = FactionCache.PlayerColonyFaction;
                faction.title = title;
                faction.name = name;
                fact.Name = name;
                faction.name = name;
                faction.factionIconPath = tempFactionIconPath;
                faction.factionIcon = tempFactionIcon;

                faction.updateFactionIcon(ref fact, "FactionIcons/" + tempFactionIconPath);


                if (!traitsChosen)
                {
                    //check each trait bool. If true and does not exist already, add to factionfc
                    if (boolMilitaristic)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.militaristic));
                    if (boolPacifist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.pacifist));
                    if (boolAuthoritarian)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.authoritarian));
                    if (boolEgalitarian)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.egalitarian));
                    if (boolIsolationist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.isolationist));
                    if (boolExpansionist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.expansionist));
                    if (boolTechnocrat)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.technocratic));
                    if (boolFeudal)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.feudal));
                    if (boolSlaver)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.slaver));
                }

                Find.WindowStack.TryRemove(this);
            }


            if (!traitsChosen)
                switch (faction.policies.Count())
                {
                    case 0:
                    case 1:
                        alertText = "FCSelectTraits0".Translate();
                        break;
                    case 2:
                        alertText = "FCSelectTraits2".Translate();
                        break;
                }
            else
            {
                alertText = "FCTraitsChosen".Translate();
            }


            Widgets.Label(labelPickTrait, alertText);


            Texture2D icon = TexLoad.iconLoyalty;
            if (boolMilitaristic)
                icon = FCPolicyDefOf.militaristic.IconLight;
            else
                icon = FCPolicyDefOf.militaristic.IconDark;
            if (buttonMilitaristic.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonMilitaristic, returnPolicyText(FCPolicyDefOf.militaristic));
            }

            if (Widgets.ButtonImage(buttonMilitaristic, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolMilitaristic)
                {
                    //Continue
                    if (boolPacifist == false)
                    {
                        boolMilitaristic = !boolMilitaristic;
                        if (boolMilitaristic)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.militaristic);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolAuthoritarian)
                icon = FCPolicyDefOf.authoritarian.IconLight;
            else
                icon = FCPolicyDefOf.authoritarian.IconDark;
            if (buttonAuthoritarian.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonAuthoritarian, returnPolicyText(FCPolicyDefOf.authoritarian));
            }

            if (Widgets.ButtonImage(buttonAuthoritarian, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolAuthoritarian)
                {
                    //Continue
                    if (boolEgalitarian == false)
                    {
                        boolAuthoritarian = !boolAuthoritarian;
                        if (boolAuthoritarian)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.authoritarian);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolIsolationist)
                icon = FCPolicyDefOf.isolationist.IconLight;
            else
                icon = FCPolicyDefOf.isolationist.IconDark;
            if (buttonIsolationist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonIsolationist, returnPolicyText(FCPolicyDefOf.isolationist));
            }

            if (Widgets.ButtonImage(buttonIsolationist, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolIsolationist)
                {
                    //Continue
                    if (boolExpansionist == false)
                    {
                        boolIsolationist = !boolIsolationist;
                        if (boolIsolationist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.isolationist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolFeudal)
                icon = FCPolicyDefOf.feudal.IconLight;
            else
                icon = FCPolicyDefOf.feudal.IconDark;
            if (buttonFeudal.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonFeudal, returnPolicyText(FCPolicyDefOf.feudal));
            }

            if (Widgets.ButtonImage(buttonFeudal, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolFeudal)
                {
                    //Continue
                    if (boolTechnocrat == false)
                    {
                        boolFeudal = !boolFeudal;
                        if (boolFeudal)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.feudal);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolPacifist)
                icon = FCPolicyDefOf.pacifist.IconLight;
            else
                icon = FCPolicyDefOf.pacifist.IconDark;
            if (buttonPacifist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonPacifist, returnPolicyText(FCPolicyDefOf.pacifist));
            }

            if (Widgets.ButtonImage(buttonPacifist, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolPacifist)
                {
                    //Continue
                    if (boolMilitaristic == false)
                    {
                        boolPacifist = !boolPacifist;
                        if (boolPacifist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.pacifist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolEgalitarian)
                icon = FCPolicyDefOf.egalitarian.IconLight;
            else
                icon = FCPolicyDefOf.egalitarian.IconDark;
            if (buttonEgalitarian.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonEgalitarian, returnPolicyText(FCPolicyDefOf.egalitarian));
            }

            if (Widgets.ButtonImage(buttonEgalitarian, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolEgalitarian)
                {
                    //Continue
                    if (boolAuthoritarian == false)
                    {
                        boolEgalitarian = !boolEgalitarian;
                        if (boolEgalitarian)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.egalitarian);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolExpansionist)
                icon = FCPolicyDefOf.expansionist.IconLight;
            else
                icon = FCPolicyDefOf.expansionist.IconDark;
            if (buttonExpansionist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonExpansionist, returnPolicyText(FCPolicyDefOf.expansionist));
            }

            if (Widgets.ButtonImage(buttonExpansionist, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolExpansionist)
                {
                    //Continue
                    if (boolIsolationist == false)
                    {
                        boolExpansionist = !boolExpansionist;
                        if (boolExpansionist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.expansionist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolTechnocrat)
                icon = FCPolicyDefOf.technocratic.IconLight;
            else
                icon = FCPolicyDefOf.technocratic.IconDark;
            if (buttonTechnocrat.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonTechnocrat, returnPolicyText(FCPolicyDefOf.technocratic));
            }

            if (Widgets.ButtonImage(buttonTechnocrat, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolTechnocrat)
                {
                    //Continue
                    if (boolFeudal == false)
                    {
                        boolTechnocrat = !boolTechnocrat;
                        if (boolTechnocrat)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.technocratic);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            if (boolSlaver)
                icon = FCPolicyDefOf.slaver.IconLight;
            else
                icon = FCPolicyDefOf.slaver.IconDark;
            if (buttonSlaver.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonSlaver, returnPolicyText(FCPolicyDefOf.slaver));
            }

            if (Widgets.ButtonImage(buttonSlaver, icon) && !traitsChosen)
            {
                if (numberTraitsSelected <= 1 || boolSlaver)
                {
                    //Continue
                    if (boolTechnocrat == false)
                    {
                        boolSlaver = !boolSlaver;
                        if (boolSlaver)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.slaver);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            //Widgets.DrawMenuSection(menusectionTrait);
            //Widgets.Label(menusectionTrait, policyText);


            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }


        string returnPolicyText(FCPolicyDef def)
        {
            string str = "";

            str += def.LabelCap + "\n";

            foreach (string positive in def.positiveEffects)
            {
                str += "\n" + positive;
            }

            str += "\n==========";
            foreach (string negative in def.negativeEffects)
            {
                str += "\n" + negative;
            }

            return str;
        }
    }
}