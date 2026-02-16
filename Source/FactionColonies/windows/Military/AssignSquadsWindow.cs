using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class AssignSquadsWindow : MilitaryWindow
    {
        private MilitaryCustomizationUtil util;
        private FactionFC faction;

        public float settlementMaxScroll;
        public int settlementHeight;
        public int settlementYSpacing;
        public int settlementWindowHeight = 500;

        public AssignSquadsWindow(MilitaryCustomizationUtil util, FactionFC faction)
        {
            this.util = util;
            this.faction = faction;
            settlementHeight = 120;
            settlementYSpacing = 5;
            settlementMaxScroll =
                (FactionCache.FactionComp.settlements.Count * (settlementYSpacing + settlementHeight) -
                 settlementWindowHeight);
        }

        public override void DrawTab(Rect rect)
        {
            Rect SettlementBox = new Rect(5, 45, 535, settlementHeight);
            Rect SettlementName = new Rect(SettlementBox.x + 5, SettlementBox.y + 5, 250, 25);
            Rect MilitaryLevel = new Rect(SettlementName.x, SettlementName.y + 30, 250, 25);
            Rect AssignedSquad = new Rect(MilitaryLevel.x, MilitaryLevel.y + 30, 250, 25);
            Rect isBusy = new Rect(AssignedSquad.x, AssignedSquad.y + 30, 250, 25);

            Rect buttonSetSquad = new Rect(SettlementBox.x + SettlementBox.width - 265, SettlementBox.y + 5, 100, 25);
            Rect buttonViewSquad = new Rect(buttonSetSquad.x, buttonSetSquad.y + 3 + buttonSetSquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonDeploySquad = new Rect(buttonViewSquad.x, buttonViewSquad.y + 3 + buttonViewSquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonResetPawns = new Rect(buttonDeploySquad.x, buttonDeploySquad.y + 3 + buttonDeploySquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonOrderFireSupport = new Rect(buttonSetSquad.x + 125 + 5, SettlementBox.y + 5, 125, 25);


            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;


            int count = 0;
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                if (settlement.MilitaryComp == null)
                {
                    continue;
                }
                WorldObjectComp_SettlementMilitary MilitaryComp = settlement.MilitaryComp;

                Text.Font = GameFont.Small;

                Widgets.DrawMenuSection(new Rect(SettlementBox.x,
                    SettlementBox.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                    SettlementBox.width, SettlementBox.height));

                //click on settlement name
                if (Widgets.ButtonTextSubtle(
                    new Rect(SettlementName.x,
                        SettlementName.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        SettlementName.width, SettlementName.height), settlement.Name))
                {
                    Find.WindowStack.Add(new SettlementWindowFc(settlement));
                }

                Widgets.Label(
                    new Rect(MilitaryLevel.x,
                        MilitaryLevel.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        MilitaryLevel.width, MilitaryLevel.height * 2),
                    "Mil Level: " + settlement.settlementMilitaryLevel + " - Max Squad Cost: " +
                    MilitaryCustomizationUtil.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel));
                if (MilitaryComp.militarySquad != null)
                {
                    if (MilitaryComp.militarySquad.outfit != null)
                    {
                        Widgets.Label(
                            new Rect(AssignedSquad.x,
                                AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                                AssignedSquad.width, AssignedSquad.height),
                            "Assigned Squad: " +
                            MilitaryComp.militarySquad.outfit.name); //settlement.militarySquad.name);
                    }
                    else
                    {
                        Widgets.Label(
                            new Rect(AssignedSquad.x,
                                AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                                AssignedSquad.width, AssignedSquad.height),
                            "No assigned Squad"); //settlement.militarySquad.name);
                    }
                }
                else
                {
                    Widgets.Label(
                        new Rect(AssignedSquad.x,
                            AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                            AssignedSquad.width, AssignedSquad.height), "No assigned Squad");
                }


                Widgets.Label(
                    new Rect(isBusy.x, isBusy.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        isBusy.width, isBusy.height), "Available: " + (!MilitaryComp.isMilitaryBusySilent()));

                Text.Font = GameFont.Tiny;

                //Set Squad Button
                if (Widgets.ButtonText(
                    new Rect(buttonSetSquad.x,
                        buttonSetSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonSetSquad.width, buttonSetSquad.height), "Set Squad"))
                {
                    //check null
                    if (util.squads == null)
                    {
                        util.resetSquads();
                    }

                    List<FloatMenuOption> squads = new List<FloatMenuOption>();

                    squads.AddRange(util.squads.Select(squad => new FloatMenuOption(squad.name + " - Total Equipment Cost: " + squad.equipmentTotalCost, delegate
                        {
                            //Unit is selected
                            util.attemptToAssignSquad(settlement, squad);
                        })));

                    if (!squads.Any())
                    {
                        squads.Add(new FloatMenuOption("No Available Squads", null));
                    }

                    FloatMenu selection = new Searchable_FloatMenu(squads);
                    Find.WindowStack.Add(selection);
                }

                //View Squad
                if (Widgets.ButtonText(
                    new Rect(buttonViewSquad.x,
                        buttonViewSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonViewSquad.width, buttonViewSquad.height), "View Squad"))
                {
                    Messages.Message("This is currently not implemented.", MessageTypeDefOf.RejectInput);
                }


                //Deploy Squad
                if (Widgets.ButtonText(
                    new Rect(buttonDeploySquad.x,
                        buttonDeploySquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonDeploySquad.width, buttonDeploySquad.height), "Deploy Squad"))
                {
                    if (!MilitaryComp.isMilitaryBusy(true) && MilitaryComp.isMilitarySquadValid())
                    {
                        Find.WindowStack.Add(new FloatMenu(DeploymentOptions(settlement)));
                    }
                    else if (MilitaryComp.isMilitaryBusy(true) && MilitaryComp.isMilitarySquadValid() && faction.hasPolicy(FCPolicyDefOf.militaristic))
                    {
                        if ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) <= Find.TickManager.TicksGame)
                        {
                            int cost = (int)Math.Round(MilitaryComp.militarySquad.outfit.updateEquipmentTotalCost() *.2);
                            List<FloatMenuOption> options = new List<FloatMenuOption>();

                            options.Add(new FloatMenuOption("Deploy Secondary Squad - $" + cost + " silver",
                                delegate
                                {
                                    if (PaymentUtil.getSilver() >= cost)
                                    {
                                        List<FloatMenuOption> deploymentOptions = new List<FloatMenuOption>();

                                        deploymentOptions.Add(new FloatMenuOption("Walk into map", delegate
                                        {
                                            MilitaryUtil.CallinExtraForces(settlement, false);
                                            Find.WindowStack.currentlyDrawnWindow.Close();
                                        }));
                                        //check if medieval only
                                        if (!FCSettings.medievalTechOnly &&
                                            (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false))
                                        {
                                            deploymentOptions.Add(new FloatMenuOption("Drop-Pod", delegate
                                            {
                                                MilitaryUtil.CallinExtraForces(settlement, true);
                                                Find.WindowStack.currentlyDrawnWindow.Close();
                                            }));
                                        }

                                        Find.WindowStack.Add(new FloatMenu(deploymentOptions));
                                    }
                                    else
                                    {
                                        Messages.Message("NotEnoughSilverToDeploySquad".Translate(),
                                            MessageTypeDefOf.RejectInput);
                                    }
                                }));

                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                        else
                        {
                            Messages.Message("XDaysToRedeploy".Translate(Math.Round(
                                    ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) -
                                     Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        MilitaryComp.isMilitaryBusy();
                    }
                }

                //Reset Squad
                if (Widgets.ButtonText(
                    new Rect(buttonResetPawns.x,
                        buttonResetPawns.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonResetPawns.width, buttonResetPawns.height), "Reset Pawns"))
                {
                    FloatMenuOption confirm = new FloatMenuOption("Are you sure? Click to confirm", delegate
                    {
                        if (MilitaryComp.militarySquad != null)
                        {
                            Messages.Message("Pawns have been regenerated for the squad",
                                MessageTypeDefOf.NeutralEvent);
                            MilitaryComp.militarySquad.initiateSquad();
                        }
                        else
                        {
                            Messages.Message("There is no pawns to reset. Assign a squad first.",
                                MessageTypeDefOf.RejectInput);
                        }
                    });

                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(confirm);
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                //Order Fire Support
                if (Widgets.ButtonText(
                    new Rect(buttonOrderFireSupport.x,
                        buttonOrderFireSupport.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonOrderFireSupport.width, buttonOrderFireSupport.height), "Order Fire Support"))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();


                    foreach (MilitaryFireSupport support in util.fireSupportDefs)
                    {
                        float cost = support.returnTotalCost();
                        FloatMenuOption option = new FloatMenuOption(support.name + " - $" + cost, delegate
                        {
                            if (support.returnTotalCost() <=
                                MilitaryCustomizationUtil.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel))
                            {
                                //TODO: to follow the new modular design of buildings and buildingcomps/defs, the fire support code should
                                //      really be refactored to be more modular. Save that for later, though
                                if (settlement.BuildingsComp?.hasBuilding(BuildingFCDefOf.artilleryOutpost) == true)
                                {
                                    if (MilitaryComp.artilleryTimer <= Find.TickManager.TicksGame)
                                    {
                                        if (PaymentUtil.getSilver() >= cost)
                                        {
                                            MilitaryUtil.FireSupport(settlement, support);
                                            Find.WindowStack.TryRemove(typeof(MilitaryCustomizationWindowFc));
                                        }
                                        else
                                        {
                                            Messages.Message(
                                                "You lack the required amount of silver to use that firesupport option!",
                                                MessageTypeDefOf.RejectInput);
                                        }
                                    }
                                    else
                                    {
                                        Messages.Message(
                                            "That firesupport option is on cooldown for another " +
                                            (MilitaryComp.artilleryTimer - Find.TickManager.TicksGame)
                                            .ToStringTicksToDays(), MessageTypeDefOf.RejectInput);
                                    }
                                }
                                else
                                {
                                    Messages.Message(
                                        "The settlement requires an artillery outpost to be built to use that firesupport option",
                                        MessageTypeDefOf.RejectInput);
                                }
                            }
                            else
                            {
                                Messages.Message(
                                    "The settlement requires a higher military level to use that fire support!",
                                    MessageTypeDefOf.RejectInput);
                            }
                        });
                        list.Add(option);
                    }

                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("No fire supports currently made. Make one", delegate { }));
                    }

                    FloatMenu menu = new Searchable_FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }

                count++;
            }

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y, settlementMaxScroll);
            }
        }

        private FloatMenuOption DropPodDeploymentOption(WorldSettlementFC settlement)
        {
            bool medievalOnly = FCSettings.medievalTechOnly;
            if (!medievalOnly && (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false))
            {
                return new FloatMenuOption("dropPodDeploymentOption".Translate(),
                    delegate { MilitaryUtil.CallinAlliedForces(settlement, true); });
            }

            return new FloatMenuOption(
                "dropPodDeploymentOption".Translate() + (medievalOnly
                    ? "dropPodDeploymentOptionUnavailableReasonMedieval".Translate()
                    : "dropPodDeploymentOptionUnavailableReasonTech".Translate(
                        DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.label ??
                        "errorDropPodResearchCouldNotBeFound".Translate())), null);
        }

        private List<FloatMenuOption> DeploymentOptions(WorldSettlementFC settlement) => new List<FloatMenuOption>
        {
            new FloatMenuOption("walkIntoMapDeploymentOption".Translate(), delegate 
            { 
                MilitaryUtil.CallinAlliedForces(settlement, false); 
            }), DropPodDeploymentOption(settlement)
        };
    }
}