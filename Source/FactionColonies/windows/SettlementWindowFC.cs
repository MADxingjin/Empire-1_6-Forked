using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace FactionColonies
{
    public sealed class SettlementWindowFc : Window
    {
        public override Vector2 InitialSize
        {
            get { return new Vector2(1270f, 645f); }
        }


        //UI STUFF
        public const int ScrollSpacing = 45;
        public const int ScrollHeight = 315;

        private const int margin = 5;
        private const int smallMargin = 3;

        //time variables
        private int uiUpdateTimer;
        private int scroll;
        private int maxScroll;
        private FactionFC factionfc;

        // Building UI values
        private const int buildingSpacing = 15;
        private const int buildingBoxSide = 72;

        private const int constructionListItemLabelHeight = 15;
        private const int constructionListProgressBarHeight = 10;
        private const int constructionListItemHeight = (smallMargin * 4) + (constructionListItemLabelHeight * 2) + constructionListProgressBarHeight; // 4 * smallMargin + 2 * label height + progress bar height
        private const int constructionListIconHeight = constructionListItemLabelHeight * 2 + smallMargin;

        private const int buildingSpacingFromSide = 15; // (494 - (spacing + boxSide) * elementsPerRow) / 2;

        private const int scrollSpacing = 17;

        private const int buildingPanelWidth = buildingSpacingFromSide * 2 + buildingBoxSide * 2 + buildingSpacing;// + scrollSpacing;

        // UI State
        private bool constructionOpen = true;

        public void windowUpdateFc()
        {
            // Only update description, don't recalculate production unless needed
            settlement.updateDescription();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            settlement.updateDescription();
            // Don't recalculate production on UI open - this overwrites saved values
            // settlement.updateProfitAndProduction();
            maxScroll = (settlement.Resources.Count * ScrollSpacing) - ScrollHeight;
            //settlement.update description
            factionfc = Find.World.GetComponent<FactionFC>();
        }

        public void UiUpdate()
        {
            if (uiUpdateTimer == 0)
            {
                uiUpdateTimer = FCSettings.updateUiTimer;
                windowUpdateFc();
            }
            else
            {
                uiUpdateTimer -= 1;
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            UiUpdate();
        }

        private readonly List<string> stats = new List<string>(5) 
        {
            "FCMilitaryLevel".Translate(),
            "FCHappiness".Translate(),
            "FCLoyality".Translate(),
            "FCUnrest".Translate(),
            "FCProsperity".Translate()
        };

        private readonly List<string> buttons = new List<string>(5)
        {
            "DeleteSettlement".Translate(), 
            "UpgradeTown".Translate(), 
            "FCSpecialActions".Translate(),
            "PrisonersMenu".Translate(), 
            "Military".Translate(),
            "example button 6",
            "example button 7"
        };

        private WorldSettlementFC settlement; //Don't expose

        public SettlementWindowFc(WorldSettlementFC settlement)
        {
            if (settlement == null)
            {
                Close();
            }

            this.settlement = settlement;
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
        }


        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            float validWidth = InitialSize.x - (Margin * 2);
            float validHeight = InitialSize.y - (Margin * 2);

            //WIP notes: change these functions to accept a rect, that forms the bounds of that segment of the UI
            Rect leftBox = new Rect(0, 0, buildingPanelWidth, validHeight);
            Rect centerBox = new Rect(leftBox.xMax + margin, 0, (validWidth * 0.75f) - buildingPanelWidth, validHeight);
            Rect rightBox = new Rect(centerBox.xMax + margin, 0, (validWidth / 4) - (margin * 2), validHeight);

            DrawLeftInfo(leftBox);
            //Widgets.DrawLineVertical(leftBox.xMax + margin, 0, validHeight); // x = 530, y = 0, length = 564
            DrawCenterInfo(centerBox);
            //Widgets.DrawLineVertical(centerBox.xMax + margin, 0, validHeight); // x = 530, y = 0, length = 564
            DrawRightInfo(rightBox);

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void DrawCenterInfo(Rect boundingBox)
        {
            Rect infobox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 360); //originally: 520 width, 340 height
            DrawBasicInfobox(infobox);

            Rect miscOverview = new Rect(infobox.x, infobox.yMax + margin, (infobox.width * 0.75f), boundingBox.height - (infobox.height + (margin * 2)));
            Rect mainButtons = new Rect(miscOverview.xMax + margin, miscOverview.y, (infobox.width * 0.25f) - margin, miscOverview.height);

            DrawMiscOverview(miscOverview);
            DrawMainButtons(mainButtons);
        }

        private void DrawBasicInfobox(Rect boundingBox)
        {
            /* Settlement name on top */
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect nameRect = new Rect(boundingBox.x + margin, boundingBox.y + margin, boundingBox.width - (margin * 2 + 20), 30);
            Widgets.Label(nameRect, settlement.Name);
            //Draw name settings button
            Rect configRect = new Rect(nameRect.xMax + margin, boundingBox.y + margin, 20, 20);
            if (Widgets.ButtonImage(configRect, TexLoad.iconCustomize))
            {
                //if click faction customize button
                Find.WindowStack.Add(new SettlementCustomizeWindowFc(settlement));
            }
            Rect infoBox = new Rect(boundingBox.x, nameRect.yMax + margin, boundingBox.width, boundingBox.height - (nameRect.height + margin*2));
            // box with level, settlement type, location description
            Widgets.DrawBox(infoBox);

            /* Town level */
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect levelBoundingBox = new Rect(infoBox.x + margin, infoBox.y + margin, 60, 60);
            //gotta love aligning text
            Rect levelBox = new Rect(levelBoundingBox.x + 25, levelBoundingBox.y + 14, 30, 30);
            Widgets.DrawShadowAround(levelBox);
            Widgets.DrawHighlight(levelBoundingBox);
            Widgets.DrawBox(levelBoundingBox);
            Widgets.Label(levelBox, settlement.settlementLevel.ToString());

            // Drawn settlement type, basic description (from def), and location text
            Text.Font = GameFont.Small;
            float rightSideWidth = boundingBox.width - (margin * 3 + levelBoundingBox.width);
            Rect typeBox = new Rect(levelBoundingBox.xMax + margin, levelBoundingBox.y, rightSideWidth, levelBoundingBox.height / 2);
            Rect typeTextBox = new Rect(typeBox.x + margin, typeBox.y, typeBox.width - (margin * 2), typeBox.height);
            Rect basicDescBox = new Rect(typeBox.x, typeBox.yMax, rightSideWidth * 0.4f, levelBoundingBox.height / 2);
            Rect basicDescTextBox = new Rect(basicDescBox.x + margin, basicDescBox.y, basicDescBox.width - (margin * 2), basicDescBox.height);
            Rect locBox = new Rect(basicDescBox.xMax + margin, typeBox.yMax, (rightSideWidth * 0.6f) - margin, levelBoundingBox.height / 2);
            Rect locTextBox = new Rect(locBox.x + margin, locBox.y, locBox.width - (margin * 2), locBox.height);
            Widgets.DrawHighlight(typeBox);
            Widgets.Label(typeTextBox, settlement.settlementDef.LabelCap); //returnSettlement().title);
            //Widgets.DrawLineHorizontal(typeBox.x, typeBox.yMax, typeBox.width);
            Text.Font = GameFont.Tiny;
            Widgets.Label(basicDescTextBox, settlement.settlementDef.description);
            Widgets.DrawLineVertical(basicDescBox.xMax, basicDescBox.y + margin, basicDescBox.height - (margin * 2));
            //TODO: localize this. LabelCap and description can be localized through def injection, but locationText is derived differently
            Widgets.Label(locTextBox, settlement.locationText);

            //TODO: programmatic way to determine width and height
            Rect statsBox = new Rect(levelBoundingBox.x, levelBoundingBox.yMax + (margin * 3), 125, infoBox.height - (levelBoundingBox.height + margin * 5));
            DrawSettlementStats(statsBox);

            Rect descBox = new Rect(statsBox.xMax + margin * 2, statsBox.y, infoBox.width - (statsBox.width + margin * 4), statsBox.height);
            DrawDescription(descBox); // x = 150, y = 80, length = 370, size = 220
        }

        //125 wide, 215ish tall
        private void DrawSettlementStats(Rect boundingBox)
        {
            float statBoxHeight = (boundingBox.height - (4 * margin)) / 5;
            float statSize = Math.Min(30f, statBoxHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            for (int i = 0; i < stats.Count(); i++)
            {
                Rect statBox = new Rect(boundingBox.x, boundingBox.y + (statBoxHeight + margin) * i, boundingBox.width, statBoxHeight);
                Widgets.DrawMenuSection(statBox);
                Rect buttonBox = new Rect(statBox.x + margin, statBox.y + margin, statSize + 4, statSize + 4);
                Rect labelBox = new Rect(buttonBox.xMax + margin, buttonBox.y, statBox.width - (buttonBox.width + margin * 2), buttonBox.height);
                if (stats[i] == "militaryLevel")
                {
                    //TODO: it's kinda silly that these are buttons. Turn them into mouseover text instead?
                    if (Widgets.ButtonImage(buttonBox, TexLoad.iconMilitary))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementMilitaryLevelDesc".Translate(),
                            "SettlementMilitaryLevel".Translate()));
                    }

                    Widgets.Label(labelBox, settlement.settlementMilitaryLevel.ToString());
                }

                if (stats[i] == "happiness")
                {
                    if (Widgets.ButtonImage(buttonBox, TexLoad.iconHappiness))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementHappinessDesc".Translate(),
                            "SettlementHappiness".Translate()));
                    }

                    Widgets.Label(labelBox, settlement.happiness + "%");
                }

                if (stats[i] == "loyalty")
                {
                    if (Widgets.ButtonImage(buttonBox, TexLoad.iconLoyalty))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementLoyaltyDesc".Translate(),
                            "SettlementLoyalty".Translate()));
                    }

                    Widgets.Label(labelBox, settlement.loyalty + "%");
                }

                if (stats[i] == "unrest")
                {
                    if (Widgets.ButtonImage(buttonBox, TexLoad.iconUnrest))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementUnrestDesc".Translate(),
                            "SettlementUnrest".Translate()));
                    }

                    Widgets.Label(labelBox, settlement.unrest + "%");
                }

                if (stats[i] != "prosperity") continue;
                if (Widgets.ButtonImage(buttonBox, TexLoad.iconProsperity))
                {
                    Find.WindowStack.Add(new DescWindowFc("SettlementProsperityDesc".Translate(),
                        "SettlementProsperity".Translate()));
                }

                Widgets.Label(labelBox, settlement.prosperity + "%");
            }
        }

        private void DrawDescription(Rect boundingBox)
        {
            //Widgets.Label(new Rect(x, y - 20, 100, 30), "Description".Translate());
            Widgets.DrawMenuSection(boundingBox);

            Rect textBox = new Rect(boundingBox.x + margin, boundingBox.y + margin, boundingBox.width - (margin * 2), boundingBox.height - (margin * 2));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(textBox, settlement.description);
        }

        //TODO
        private void DrawMiscOverview(Rect boundingBox)
        {
            Widgets.DrawMenuSection(boundingBox);
            Widgets.Label(boundingBox, "insert misc overview here");
        }

        private void DrawMainButtons(Rect boundingBox)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            float size = (boundingBox.height - ((buttons.Count() - 1) * margin)) / (buttons.Count());
            for (int i = 0; i < buttons.Count(); i++)
            {
                Rect buttonRect = new Rect(boundingBox.x, boundingBox.y + ((size + margin) * i), boundingBox.width, size);
                if (Widgets.ButtonText(buttonRect, buttons[i]))
                {
                    //If click a button button
                    if (buttons[i] == "UpgradeTown".Translate())
                    {
                        //if click upgrade town button
                        Find.WindowStack.Add(new SettlementUpgradeWindowFc(settlement));
                    }

                    if (buttons[i] == "AreYouSureRemove".Translate())
                    {
                        //if click to delete colony
                        Find.WindowStack.TryRemove(this);
                        ColonyUtil.removePlayerSettlement(settlement);
                    }

                    if (buttons[i] == "DeleteSettlement".Translate())
                    {
                        //if click town log button
                        buttons[i] = "AreYouSureRemove".Translate();
                    }

                    if (buttons[i] == "FCSpecialActions".Translate())
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>
                        {
                            //Add to all
                            new FloatMenuOption("GoToLocation".Translate(), delegate
                            {
                                Find.WindowStack.TryRemove(this);
                                settlement.goTo();
                            })
                        };


                        if (factionfc.hasPolicy(FCPolicyDefOf.authoritarian))
                            list.Add(new FloatMenuOption("FCBuyLoyalty".Translate(),
                                delegate { Find.WindowStack.Add(new FCWindow_Pay_Silver_Loyalty(settlement)); }));

                        if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian))
                            list.Add(new FloatMenuOption("FCGiveTaxBreak".Translate(), delegate
                            {
                                if (settlement.trait_Egalitarian_TaxBreak_Enabled == false)
                                {
                                    Find.WindowStack.Add(new FCWindow_Confirm_TaxBreak(settlement));
                                }
                                else
                                    Messages.Message(
                                        "FCAlreadyGivingTaxBreak".Translate(Math.Round(
                                            (settlement.trait_Egalitarian_TaxBreak_Tick +
                                                GenDate.TicksPerDay * 10 -
                                                Find.TickManager.TicksGame) / (double)GenDate.TicksPerDay, 1)),
                                        MessageTypeDefOf.RejectInput);
                            }));

                        if (list.Count() == 0)
                            list.Add(new FloatMenuOption("No special actions to take", delegate { }));
                        Find.WindowStack.Add(new FloatMenu(list));
                    }

                    if (buttons[i] == "PrisonersMenu".Translate())
                    {
                        Find.WindowStack.Add(new FCPrisonerMenu(settlement));
                    }

                    if (buttons[i] == "Military".Translate() && settlement.MilitaryComp != null)
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>
                        {
                            new FloatMenuOption(
                            "ToggleAutoDefend".Translate(settlement.MilitaryComp.autoDefend.ToString()),
                            delegate
                            {
                                settlement.MilitaryComp.autoDefend = !settlement.MilitaryComp.autoDefend;
                                //Messages.Message("autoDefendWarning".Translate(), MessageTypeDefOf.CautionInput);
                            })
                        };

                        if (settlement.MilitaryComp.isUnderAttack)
                        {
                            FCEvent evt = MilitaryUtilFC.returnMilitaryEventByLocation(settlement.Tile);

                            list.Add(new FloatMenuOption(
                                "SettlementDefendingInformation".Translate(
                                    evt.militaryForceDefending.homeSettlement.Name,
                                    evt.militaryForceDefending.militaryLevel), null, MenuOptionPriority.High));
                            list.Add(new FloatMenuOption("ChangeDefendingForce".Translate(), delegate
                            {
                                List<FloatMenuOption> settlementList = new List<FloatMenuOption>();
                                WorldSettlementFC homeSettlement = settlement;

                                settlementList.Add(new FloatMenuOption(
                                    "ResetToHomeSettlement".Translate(homeSettlement.settlementMilitaryLevel),
                                    delegate { MilitaryUtilFC.changeDefendingMilitaryForce(evt, homeSettlement); },
                                    MenuOptionPriority.High));

                                foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                                {
                                    if (settlement.MilitaryComp.isMilitaryValid() && settlement != homeSettlement)
                                    {
                                        //if military is valid to use.

                                        settlementList.Add(new FloatMenuOption(
                                            settlement.Name + " " + "ShortMilitary".Translate() + " " +
                                            settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() +
                                            ": " + (!settlement.MilitaryComp.isMilitaryBusySilent()).ToString(), delegate
                                            {
                                                if (settlement.MilitaryComp.isMilitaryBusy())
                                                {
                                                    //military is busy
                                                }
                                                else
                                                {
                                                    MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
                                                }
                                            }
                                        ));
                                    }
                                }

                                if (settlementList.Count == 0)
                                {
                                    settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                }

                                Find.WindowStack.Add(new Searchable_FloatMenu(settlementList) { vanishIfMouseDistant = true });


                                //set to raid settlement here
                            }));

                            Find.WindowStack.Add(new FloatMenu(list));
                        }
                        else
                        {
                            list.Add(new FloatMenuOption("SettlementNotBeingAttacked".Translate(), null));
                            Find.WindowStack.Add(new FloatMenu(list));
                        }
                    }
                }
            }
        }

        /* Left side overview */
        private void DrawLeftInfo(Rect boundingBox)
        {
            if (settlement.BuildingsComp == null)
            {
                Widgets.Label(boundingBox, "no buildings");
                return;
            }
            //getUnderConstructionBuildings() uses caching, so we don't re-compute the list every time we call the function. So calling it here should be fine.
            int numUnderConstruction = settlement.BuildingsComp.getUnderConstructionBuildings().Count + (settlement.isUpgrading ? 1 : 0);
            Rect constructionBox;
            if (numUnderConstruction == 0)
            {
                // There is no active construction
                constructionBox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 30);
            }
            else if (constructionOpen == true)
            {
                // There is active construction, and the production box IS open
                constructionBox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 60 + (margin * 5) + (constructionListItemHeight * 3));
            }
            else
            {
                // There is active construction, but the production box is NOT open
                constructionBox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 60 + (margin * 2));
            }
            DrawConstructionBox(constructionBox, numUnderConstruction, settlement.BuildingsComp.getUnderConstructionBuildings());

            Rect facilitiesBox = new Rect(boundingBox.x, constructionBox.yMax + margin, boundingBox.width, boundingBox.height - (constructionBox.height + margin));
            DrawFacilities(facilitiesBox);
        }
        private Vector2 scrollVectorBuildings = new Vector2();
        public void DrawFacilities(Rect boundingBox)
        {
            if (settlement.BuildingsComp == null)
            {
                // can't draw what doesn't exist
                return;
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Rect labelTextBox = new Rect(boundingBox.x + margin, boundingBox.y + margin, boundingBox.width - (margin * 2), 30);
            Widgets.Label(labelTextBox, "BuildingUpgrades".Translate());

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;

            int elementsPerRow = 2; // (int)((boundingBox.width - (buildingSpacingFromSide * 2)) / (buildingBoxSide + buildingSpacing));
            float buildingBoxHeight = boundingBox.height - (labelTextBox.height + margin * 2);
            float totalHeight = Mathf.Ceil(((float)settlement.BuildingsComp.Buildings.Count / (float)elementsPerRow)) * (buildingBoxSide + buildingSpacing);

            int row;
            int column;

            Rect box = new Rect(0 + buildingSpacingFromSide, 0, buildingBoxSide, buildingBoxSide);
            Rect buildingIcon = new Rect(4 + box.x, 4 + box.y, buildingBoxSide - 8, buildingBoxSide - 8);

            Rect nBox;
            Rect nBuilding;

            Rect buildingBox = new Rect(boundingBox.x, labelTextBox.yMax + margin, boundingBox.width, buildingBoxHeight);

            Rect viewRect = new Rect(buildingBox.x, labelTextBox.yMax + margin, boundingBox.width, totalHeight);

            Widgets.BeginScrollView(buildingBox, ref scrollVectorBuildings, viewRect, false);


            int i = 0;

            foreach (BuildingFC buildingfc in settlement.BuildingsComp.Buildings)
            {
                BuildingFCDef building = buildingfc.def;
                //Update Variables for List
                row = (int)Math.Floor(i / (double)elementsPerRow);
                column = i % elementsPerRow;

                nBox = new Rect(
                    new Vector2(box.x + buildingBox.x + ((box.width + buildingSpacing) * column),
                                box.y + viewRect.y + ((box.height + buildingSpacing) * row)),
                    box.size);
                nBuilding = new Rect(
                    new Vector2(buildingIcon.x + buildingBox.x + ((box.width + buildingSpacing) * column),
                                buildingIcon.y + viewRect.y + ((box.height + buildingSpacing) * row)),
                    buildingIcon.size);


                //Actual UI Code
                Widgets.DrawMenuSection(nBox);
                if (i < settlement.BuildingsComp.NumBuildingSlots)
                {
                    if (Widgets.ButtonImage(nBuilding, building.Icon))
                    {
                        // Check if this is an actual built building (not Empty or Construction)
                        if (building.defName != "Empty" && building.defName != "Construction")
                        {
                            // Show demolish menu for built buildings
                            int demolishCost = (int)Math.Round(building.cost * 0.5);
                            int buildingSlot = i;

                            List<FloatMenuOption> list = new List<FloatMenuOption>
                            {
                                new FloatMenuOption("FCDemolish".Translate() + " (" + "Cost".Translate() + ": " + demolishCost + " " + "Silver".Translate() + ")", delegate
                                {
                                    // Show confirmation dialog
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                        "FCDemolishConfirmation".Translate(building.LabelCap, demolishCost),
                                        delegate
                                        {
                                            // Check if player has enough silver
                                            if (PaymentUtil.getSilver() < demolishCost)
                                            {
                                                Messages.Message("FCNotEnoughSilverDemolish".Translate(), MessageTypeDefOf.RejectInput);
                                                return;
                                            }
                                            
                                            // Pay the demolition cost
                                            PaymentUtil.paySilver(demolishCost);
                                            
                                            // Deconstruct the building
                                            settlement.deconstructBuilding(buildingSlot);
                                            
                                            // Update the window
                                            windowUpdateFc();

                                            Messages.Message("FCBuildingDemolished".Translate(building.LabelCap), MessageTypeDefOf.PositiveEvent);
                                        }
                                    ));
                                }),
                                new FloatMenuOption("FCChangeBuildingButton".Translate(), delegate
                                {
                                    Find.WindowStack.Add(new FCBuildingWindow(settlement, buildingSlot));
                                })
                            };

                            Find.WindowStack.Add(new FloatMenu(list));
                        }
                        else
                        {
                            // Empty or Construction slot - open building window to build
                            Find.WindowStack.Add(new FCBuildingWindow(settlement, i));
                        }
                    }
                }
                else
                {
                    if (Widgets.ButtonImage(nBuilding, TexLoad.buildingLocked))
                    {
                        Messages.Message("That Building is locked", MessageTypeDefOf.RejectInput);
                    }
                }
                //Optional Label
                //Widgets.Label(nBuilding, building.LabelCap);

                i++;
            }
            Widgets.EndScrollView();
            Widgets.DrawBox(boundingBox);
        }
        private Vector2 scrollVectorConstruction = new Vector2();
        private void DrawConstructionBox(Rect boundingBox, int numConstruction, List<BuildingFC> construction)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            /* Draw the construction header */
            Rect conHeader = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 30);
            Rect conHeaderText = new Rect(conHeader.x, conHeader.y + margin, conHeader.width, conHeader.height - (margin*2));
            Widgets.DrawHighlight(conHeader);
            Widgets.Label(conHeaderText, "numUnderConstruction".Translate(numConstruction));

            if (numConstruction > 0)
            {
                Rect conButton = new Rect(boundingBox.x + margin, conHeader.yMax + margin, boundingBox.width - (margin * 2), 30);

                if (constructionOpen)
                {
                    if (Widgets.ButtonText(conButton, "closeConstructionBox".Translate()))
                    {
                        constructionOpen = false;
                    }

                    /* Scroll view time, baby */
                    float listHeight = boundingBox.height - (conButton.height + conHeader.height + (margin * 3));
                    float totalHeight = (constructionListItemHeight * numConstruction) + (margin * (numConstruction - 1));
                    Rect listBox = new Rect(boundingBox.x, conButton.yMax + margin, boundingBox.width, listHeight);
                    Rect viewRect = new Rect(listBox.x, listBox.y, listBox.width, totalHeight);

                    Widgets.BeginScrollView(listBox, ref scrollVectorConstruction, viewRect, false);

                    float initialY = viewRect.y;
                    Text.Anchor = TextAnchor.MiddleLeft;

                    if (settlement.isUpgrading)
                    {
                        float progress = (float)(Find.TickManager.TicksGame - settlement.startUpgradeTick) / (float)(settlement.finishUpgradeTick - settlement.startUpgradeTick);
                        Rect upgradeRect = new Rect(viewRect.x + margin,
                                                    viewRect.y,
                                                    viewRect.width - (margin * 2),
                                                    constructionListItemHeight);
                        DrawConstructionInfoBox(upgradeRect, null, "settlementupgrading".Translate(),
                                                "completiontimer".Translate((settlement.finishUpgradeTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(allowSeconds: false, shortForm: true)),
                                                progress);

                        initialY = upgradeRect.yMax + margin;
                    }

                    for (int i = 0; i < construction.Count; i++)
                    {
                        float progress = (float)(Find.TickManager.TicksGame - construction[i].startedTick) / (float)(construction[i].completionTick - construction[i].startedTick);
                        Rect upgradeRect = new Rect(viewRect.x + margin,
                                                    initialY + (i * (constructionListItemHeight + margin)),
                                                    viewRect.width - (margin * 2),
                                                    constructionListItemHeight);
                        DrawConstructionInfoBox(upgradeRect, construction[i].underConstructionDef.Icon, construction[i].underConstructionDef.LabelCap,
                                                "completiontimer".Translate((construction[i].completionTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(allowSeconds: false, shortForm: true)),
                                                progress);
                    }


                    Widgets.EndScrollView();
                }
                else
                {
                    if (Widgets.ButtonText(conButton, "openConstructionBox".Translate()))
                    {
                        constructionOpen = true;
                    }
                }

            }

            Widgets.DrawBox(boundingBox);
        }
        private void DrawConstructionInfoBox(Rect boundingBox, Texture2D icon, string label, string time, float progress)
        {
            Text.Font = GameFont.Tiny;
            float elementHeight = (boundingBox.height - (smallMargin * 4f)) / 3f;
            float iconHeight = (boundingBox.height - (smallMargin * 3f)) * (2f/3f);
            float labelX = icon == null ? boundingBox.x : boundingBox.x + constructionListIconHeight + smallMargin;
            Rect iconBox = new Rect(boundingBox.x + smallMargin,
                                    boundingBox.y + smallMargin,
                                    constructionListIconHeight,
                                    constructionListIconHeight);
            Rect labelBox = new Rect(labelX + smallMargin*2,
                                     boundingBox.y + smallMargin,
                                     boundingBox.xMax - (labelX + smallMargin*3),
                                     constructionListItemLabelHeight);
            Rect labelHighlight = new Rect(labelX + smallMargin,
                                           boundingBox.y + smallMargin,
                                           boundingBox.xMax - (labelX + smallMargin * 2),
                                           constructionListItemLabelHeight);
            Rect timeBox = new Rect(labelBox.x,
                                    labelBox.yMax + smallMargin,
                                    labelBox.width,
                                    constructionListItemLabelHeight);
            Rect progressRect = new Rect(boundingBox.x + smallMargin,
                                         timeBox.yMax + smallMargin,
                                         boundingBox.width - (smallMargin * 2),
                                         constructionListProgressBarHeight);

            Widgets.DrawBox(boundingBox);
            if (icon != null)
            {
                Widgets.ButtonImage(iconBox, icon);
            }
            Widgets.DrawHighlight(labelHighlight);
            Widgets.Label(labelBox, label);
            Widgets.Label(timeBox, time);
            UIUtil.DrawProgressBar(progressRect, progress);
        }

        private void DrawRightInfo(Rect boundingBox)
        {
            Widgets.Label(boundingBox, "production box");
            //ProDuctTion
            /*DrawProductionHeader(550, 0); //x = 550, y = 0

            DrawEconomicStats(687, 0, 139, 15); //x=687, y = 0, length = 139, size = 15
            //lowerProDucTion
            Widgets.DrawLineHorizontal(601, 80, 422); //x=601, y=80, length=422
            DrawProductionHeaderLower(550, 80, 5); //x=550, y=90, spacing=5*/

            Widgets.DrawBox(boundingBox);
        }

        /// <summary>
        /// Transforms the given bool <paramref name="var"/> into it's keyed translation
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        private string IsAllowedTranslation(bool var)
        {
            if (var) return "FCIsAllowed".Translate();
            return "FCIsNotAllowed".Translate();
        }

        /// <summary>
        /// Handles the tithe cutomization FloatMenuOptions for any given <paramref name="resource"/> with <paramref name="resourceType"/>
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceType"></param>
        private void TitheCustomizationClicked(ResourceFC resource)
        {
            //if click faction customize button
            if (resource.filter == null)
            {
                resource.filter = new ThingFilter();
                resource.resetThingFilter();
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("FCTitheEnableAll".Translate(),
                delegate
                {
                    resource.resetThingFilter();
                    resource.returnLowestCost();
                }),
                new FloatMenuOption("FCTitheDisableAll".Translate(),
                delegate
                {
                    resource.filter.SetDisallowAll();
                    resource.returnLowestCost();
                })
            };

            List<ThingDef> things = resource.generateThingDefList();//PaymentUtil.debugGenerateTithe(resourceType, settlement);

            foreach (ThingDef thing in things)
            {
                FloatMenuOption option = new FloatMenuOption("FCTitheSingleOption".Translate(thing.LabelCap, thing.BaseMarketValue, IsAllowedTranslation(resource.filter.Allows(thing))), null, thing);

                //Seperated because the label needs to be modified on press
                option.action = delegate
                {
                    resource.filter.SetAllow(thing, !resource.filter.Allows(thing));
                    resource.returnLowestCost();
                    option.Label = "FCTitheSingleOption".Translate(thing.LabelCap, thing.BaseMarketValue, IsAllowedTranslation(resource.filter.Allows(thing)));
                    SoundDefOf.Click.PlayOneShotOnCamera();
                };

                options.Add(option);
            }

            Find.WindowStack.Add(new Searchable_FloatMenu(options, true));
        }

        /// <summary>
        /// If <paramref name="isClicked"/>, changes the <paramref name="resource"/>'s tithe status and updates some necessary things
        /// </summary>
        /// <param name="isClicked"></param>
        /// <param name="resource"></param>
        private void DoTitheCheckboxAction(bool isClicked, ResourceFC resource)
        {
            if (isClicked)
            {
                resource.isTitheBool = resource.isTithe;
                settlement.updateProfitAndProduction();
                windowUpdateFc();
            }
        }

        /// <summary>
        /// Draws a <paramref name="resource"/>'s description window
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceType"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="spacing"></param>
        private void DoResourceDescriptionButton(ResourceFC resource, int displayIndex, int x, int y, int spacing)
        {
            if (Widgets.ButtonImage(new Rect(x + 45, scroll + y + 75 + (int)displayIndex * (45 + spacing), 30, 30), resource.def.Icon))
            {
                Find.WindowStack.Add(new DescWindowFc("SettlementProductionOf".Translate() + ": " + resource.def.LabelCap, resource.def.LabelCap));
            }
        }

        /// <summary>
        /// Increases the amount of workers in a settlement. Decreases if <paramref name="negative"/> is true. Modifies the amount based on if shift/ctrl are held
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="negative"></param>
        private void IncreaseWorkers(ResourceFC resource, bool negative = false)
        {
            if (settlement.MilitaryComp?.isUnderAttack == true)
            {
                Messages.Message("SettlementUnderAttack".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            //if clicked to lower amount of workers
            settlement.increaseWorkers(resource, (negative ? -1 : 1) * Modifiers.GetModifier);
            windowUpdateFc();
        }

        private bool ShouldTitheBeLockedForResouceType(ResourceTypeDef t) => t.isPoolResource;

        private void DrawResources(int x, int y, int spacing)
        {
            // Get the appropriate resource types based on settlement type
            List<ResourceFC> availableResources = settlement.Resources;

            for (int i = 0; i < availableResources.Count; i++)
            {
                ResourceFC resource = availableResources[i];
                if (resource == null) continue;
                
                float rectY = scroll + y + 70 + i * (45 + spacing);

                //Don't draw if outside view
                if (i * ScrollSpacing + scroll < 0) continue;

                bool titheDisabled = false;
                if (ShouldTitheBeLockedForResouceType(resource.def))
                {
                    titheDisabled = true;
                }
                else if (Widgets.ButtonImage(new Rect(x - 15, scroll + y + 65 + i * (45 + spacing) + 8, 20, 20), TexLoad.iconCustomize))
                {
                    TitheCustomizationClicked(resource);
                }

                Widgets.Checkbox(new Vector2(x + 8, scroll + y + 65 + i * (45 + spacing) + 8), ref resource.isTithe, 24, titheDisabled);
                DoTitheCheckboxAction(resource.isTithe != resource.isTitheBool, resource);
                //TODO: this function used to use the resourceType enum as a sort of index for mathing out the display. Make sure that switching to 'i' actually works
                DoResourceDescriptionButton(resource, i, x, y, spacing);

                //Production Efficiency
                Widgets.DrawBox(new Rect(x + 80, rectY, 100, 20));
                Widgets.FillableBar(new Rect(x + 80, rectY, 100, 20), (float)Math.Min(resource.productionBase, 1.0));
                Widgets.Label(new Rect(x + 80, scroll + y + 90 + i * (45 + spacing), 100, 20), "Workers".Translate() + ": " + resource.assignedWorkers);
                if (Widgets.ButtonText(new Rect(x + 80, scroll + y + 90 + i * (45 + spacing), 20, 20), "<")) IncreaseWorkers(resource, true);
                if (Widgets.ButtonText(new Rect(x + 160, scroll + y + 90 + i * (45 + spacing), 20, 20), ">")) IncreaseWorkers(resource);

                //Base Production
                Rect baseProd = new Rect(x + 195, rectY, 45, 40);
                Widgets.Label(baseProd,
                    TextUtil.FloorStat(resource.productionBase));

                //Final Modifier
                Widgets.Label(new Rect(x + 250, rectY, 50, 40),
                    TextUtil.FloorStat(resource.productionMult));

                //Final Base
                Widgets.Label(new Rect(x + 310, rectY, 45, 40),
                    (TextUtil.FloorStat(resource.totalProduction)));

                //Est Income
                Widgets.Label(new Rect(x + 365, rectY, 45, 40),
                    (TextUtil.FloorStat(resource.totalProduction * FCSettings.silverPerResource)));

                //Tithe Percentage
                resource.returnTaxPercentage();
                string taxPercentage = TextUtil.FloorStat(resource.taxPercentage) + "%";
                Widgets.Label(new Rect(x + 420, rectY, 45, 40), taxPercentage);
            }

        }

        public void DrawProductionHeaderLower(int x, int y, int spacing)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Small;

            //Assigned workers
            Widgets.Label(new Rect(x, y, 410, 30), string.Format("{0}: {1}/{2}/{3}", "AssignedWorkers".Translate(), settlement.getTotalWorkers(), settlement.workersMax, settlement.workersUltraMax));

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;

            //Item Headers
            Widgets.DrawHighlight(new Rect(x, y + 30, 40, 40));
            Widgets.Label(new Rect(x, y + 30, 40, 40), "IsTithe".Translate() + "?");

            Widgets.DrawHighlight(new Rect(x + 80, y + 30, 100, 40));
            Widgets.Label(new Rect(x + 80, y + 30, 100, 40), "ProductionEfficiency".Translate());

            Widgets.DrawHighlight(new Rect(x + 195, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 195, y + 30, 45, 40), "Base".Translate());

            Widgets.DrawHighlight(new Rect(x + 250, y + 30, 50, 40));
            Widgets.Label(new Rect(x + 250, y + 30, 50, 40), "Modifier".Translate());

            Widgets.DrawHighlight(new Rect(x + 310, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 310, y + 30, 45, 40), "Final".Translate());

            Widgets.DrawHighlight(new Rect(x + 365, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 365, y + 30, 45, 40), "EstimatedProfit".Translate());

            Widgets.DrawHighlight(new Rect(x + 420, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 420, y + 30, 45, 40), "TaxPercentage".Translate());

            DrawResources(x, y, spacing);

            //Scroll window for resources
            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y);
            }
        }



        public void DrawProductionHeader(int x, int y)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double egalitarianTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                egalitarianTaxBoost = Math.Floor(settlement.happiness / 10);
                if (settlement.trait_Egalitarian_TaxBreak_Enabled)
                {
                    egalitarianTaxBoost -= 30;
                }
            }

            double isolationistTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistTaxBoost = 10;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x, y, 400, 30), "Production".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x + 5, y + 60, 150, 20),
                "TaxBase".Translate() + ": " + (((100 + egalitarianTaxBoost + isolationistTaxBoost) +
                                                 TraitUtilsFC.cycleTraits("taxBasePercentage", settlement.Traits, Operation.Addition))).ToString() + "%");
        }

        public void DrawEconomicStats(int x, int y, int length, int size)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(new Rect(x, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " " + "Income".Translate());
            Widgets.Label(new Rect(x, size + 3, length, size), settlement.totalIncome.ToString());
            Widgets.Label(new Rect(x, size * 2 + 6, length, size), "FCUpkeep".Translate());
            Widgets.Label(new Rect(x, size * 3 + 9, length, size), settlement.totalUpkeep.ToString());
            Widgets.Label(new Rect(x + length + 10, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " " + "Profit".Translate());
            Widgets.Label(new Rect(x + length + 10, size + 3, length, size), settlement.totalProfit.ToString());
            Widgets.Label(new Rect(x + length + 10, size * 2 + 6, length, size), "CostPerWorker".Translate());
            Widgets.Label(new Rect(x + length + 10, size * 3 + 9, length, size), settlement.workerCost.ToString());
        }


        private void scrollWindow(float num)
        {
            if (scroll - num * 5 < -1 * maxScroll)
            {
                scroll = -1 * maxScroll;
            }
            else if (scroll - num * 5 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int) Event.current.delta.y * 5;
            }

            Event.current.Use();
        }
    }
}