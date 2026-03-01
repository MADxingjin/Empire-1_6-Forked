using System;
using System.Collections.Generic;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    public class FireSupportWindow : MilitaryWindow
    {
        private WorldSettlementFC settlementPointReference;
        private MilitaryFireSupport selectedSupport;
        private MilitaryCustomizationUtil util;
        private float fireSupportMaxScroll;

        public FireSupportWindow(MilitaryCustomizationUtil util)
        {
            this.util = util;
            selectedText = "Select a fire support";

            util.checkMilitaryUtilForErrors();
        }

        public override void DrawTab(Rect rect)
        {
            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            
            float projectileBoxHeight = 30;
            Rect SelectionBar = new Rect(5, 45, 200, 30);
            Rect createSupportButton = new Rect(5, SelectionBar.y + SelectionBar.height + 10, 200, 30);
            Rect nameTextField = new Rect(5, createSupportButton.y + createSupportButton.height + 10, 250, 30);
            Rect floatRangeAccuracyLabel = new Rect(nameTextField.x, nameTextField.y + nameTextField.height + 5,
                nameTextField.width, (float) (nameTextField.height * 1.5));
            Rect floatRangeAccuracy = new Rect(floatRangeAccuracyLabel.x,
                floatRangeAccuracyLabel.y + floatRangeAccuracyLabel.height + 5, floatRangeAccuracyLabel.width,
                floatRangeAccuracyLabel.height);


            Rect UnitStandBase = new Rect(140, 200, 50, 30);
            Rect TotalCost = new Rect(325, 50, 450, 20);
            Rect numberProjectiles = new Rect(TotalCost.x, TotalCost.y + TotalCost.height + 5, TotalCost.width,
                TotalCost.height);
            Rect duration = new Rect(numberProjectiles.x, numberProjectiles.y + numberProjectiles.height + 5,
                numberProjectiles.width, numberProjectiles.height);

            Rect ResetButton = new Rect(700 - 2, 100, 100, 30);
            Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5,
                ResetButton.width,
                ResetButton.height);
            Rect PointRefButton = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5,
                DeleteButton.width,
                DeleteButton.height);


            //Up here to make sure it goes behind other layers
            if (selectedSupport != null)
            {
                DrawFireSupportBox(10, 260, 30);
            }

            Widgets.DrawMenuSection(new Rect(0, 45, 800, 225));

            // --- Create New Fire Support Button ---
            if (Widgets.ButtonText(createSupportButton, "FCCreateNewFireSupport".Translate()))
            {
                MilitaryFireSupport newFireSupport = new MilitaryFireSupport();
                newFireSupport.name = "New Fire Support " + (util.fireSupportDefs.Count + 1);
                newFireSupport.setLoadID();
                newFireSupport.projectiles = new List<ThingDef>();
                selectedText = newFireSupport.name;
                selectedSupport = newFireSupport;
                util.fireSupportDefs.Add(newFireSupport);
            }

            // --- Fire Support Selection Dropdown ---
            if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black) && util.fireSupportDefs.Count > 0)
            {
                List<FloatMenuOption> supports = new List<FloatMenuOption>();

                foreach (MilitaryFireSupport support in util.fireSupportDefs)
                {
                    supports.Add(new FloatMenuOption(support.name, delegate
                    {
                        selectedText = support.name;
                        selectedSupport = support;
                    }));
                }

                FloatMenu selection = new Searchable_FloatMenu(supports);
                Find.WindowStack.Add(selection);
            }


            //if firesupport is selected
            if (selectedSupport != null)
            {
                //Need to adjust
                fireSupportMaxScroll =
                    selectedSupport.projectiles.Count * projectileBoxHeight - 10 * projectileBoxHeight;

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;


                if (settlementPointReference != null)
                {
                    Widgets.Label(TotalCost,
                        "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " +
                        MilitaryCustomizationUtil.calculateMilitaryLevelPoints(settlementPointReference
                            .settlementMilitaryLevel) +
                        " (Max Cost)");
                }
                else
                {
                    Widgets.Label(TotalCost,
                        "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " +
                        "No Reference");
                }

                Widgets.Label(numberProjectiles,
                    "Number of Projectiles: " + selectedSupport.projectiles.Count);
                Widgets.Label(duration,
                    "Duration of fire support: " + Math.Round(selectedSupport.projectiles.Count * .25, 2) +
                    " seconds");
                Widgets.Label(floatRangeAccuracyLabel,
                    selectedSupport.accuracy +
                    " = Accuracy of fire support (In tiles radius): Affecting cost by : " +
                    selectedSupport.returnAccuracyCostPercentage() + "%");
                selectedSupport.accuracy = Widgets.HorizontalSlider(floatRangeAccuracy,
                    selectedSupport.accuracy,
                    Math.Max(3, (15 - FactionCache.FactionComp.returnHighestMilitaryLevel())), 30,
                    roundTo: 1);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;


                //Unit Name
                selectedSupport.name = Widgets.TextField(nameTextField, selectedSupport.name);

                if (Widgets.ButtonText(ResetButton, "FCResetToDefault".Translate()))
                {
                    selectedSupport.projectiles = new List<ThingDef>();
                }

                if (Widgets.ButtonText(DeleteButton, "FCDeleteSupport".Translate()))
                {
                    selectedSupport.delete();
                    util.checkMilitaryUtilForErrors();
                    selectedSupport = null;
                    selectedText = "FCCreateNewFireSupport".Translate();

                    //Reset Text anchor and font
                    Text.Font = fontBefore;
                    Text.Anchor = anchorBefore;
                    return;
                }

                if (Widgets.ButtonText(PointRefButton, "FCSetPointRef".Translate()))
                {
                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                    foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
                    {
                        settlementList.Add(new FloatMenuOption(
                            settlement.Name + "FCMilitaryLevelLabel".Translate() + settlement.settlementMilitaryLevel,
                            delegate
                            {
                                //set points
                                settlementPointReference = settlement;
                            }));
                    }

                    if (!settlementList.Any())
                    {
                        settlementList.Add(new FloatMenuOption("FCNoValidSettlements".Translate(), null));
                    }

                    FloatMenu floatMenu = new FloatMenu(settlementList);
                    Find.WindowStack.Add(floatMenu);
                }

                //Reset Text anchor and font
                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y, fireSupportMaxScroll);
            }

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
        
        public void DrawFireSupportBox(float x, float y, float rowHeight)
        {
            //Set Text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;


            for (int i = 0; i <= selectedSupport.projectiles.Count; i++)
            {
                //Declare Rects
                Rect text = new Rect(x + 2, y + 2 + i * rowHeight + scroll, rowHeight - 4, rowHeight - 4);
                Rect cost = deriveRectRow(text, 2, 0, 150);
                Rect icon = deriveRectRow(cost, 2, 0, 250);
                //Rect name = deriveRectRow(icon, 2, 0, 150);
                Rect options = deriveRectRow(icon, 2, 0, 74);
                Rect upArrow = deriveRectRow(options, 12, 0, rowHeight - 4, rowHeight - 4);
                Rect downArrow = deriveRectRow(upArrow, 4);
                Rect delete = deriveRectRow(downArrow, 12);
                //Create outside box last to encapsulate entirety
                Rect box = new Rect(x, y + i * rowHeight + scroll, delete.x + delete.width + 4 - x, rowHeight);


                Widgets.DrawHighlight(box);
                Widgets.DrawMenuSection(box);

                if (i == selectedSupport.projectiles.Count)
                {
                    //If on last row
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(text, i.ToString());
                    if (Widgets.ButtonTextSubtle(icon, "FCAddNewProjectile".Translate()))
                    {
                        //if creating new projectile
                        List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                        foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                        {
                            thingOptions.Add(new FloatMenuOption(
                                def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                delegate 
                                {
                                    selectedSupport.projectiles.Add(def);
                                    SoundDefOf.Click.PlayOneShotOnCamera();
                                }, def));
                        }

                        if (!thingOptions.Any())
                        {
                            thingOptions.Add(
                                new FloatMenuOption("FCNoProjectilesFound".Translate(), delegate { }));
                        }

                        Find.WindowStack.Add(new Searchable_FloatMenu(thingOptions, true));
                    }
                }
                else
                {
                    //if on row with projectile
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(text, i.ToString());
                    if (Widgets.ButtonTextSubtle(icon, ""))
                    {
                        List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                        foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                        {
                            int k = i;
                            thingOptions.Add(new FloatMenuOption(
                                def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                delegate { selectedSupport.projectiles[k] = def; }, def));
                        }

                        if (!thingOptions.Any())
                        {
                            thingOptions.Add(
                                new FloatMenuOption("FCNoProjectilesFound".Translate(), delegate { }));
                        }

                        Find.WindowStack.Add(new FloatMenu(thingOptions));
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cost,
                        "$ " + (Math.Round(selectedSupport.projectiles[i].BaseMarketValue * 1.5,
                            2))); //ADD in future mod setting for firesupport cost

                    Widgets.DefLabelWithIcon(icon, selectedSupport.projectiles[i]);
                    if (Widgets.ButtonTextSubtle(options, "FCFireSupportOptions".Translate()))
                    {
                        //If clicked options button
                        int k = i;
                        List<FloatMenuOption> listOptions = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("FCInsertProjectileAbove".Translate(), delegate
                            {
                                List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                                foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                                {
                                    thingOptions.Add(new FloatMenuOption(
                                        def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                        delegate
                                        {
                                            selectedSupport.projectiles.Insert(k, def);
                                        }, def));
                                }

                                if (!thingOptions.Any())
                                {
                                    thingOptions.Add(new FloatMenuOption("FCNoProjectilesFound".Translate(),
                                        delegate { }));
                                }

                                Find.WindowStack.Add(new FloatMenu(thingOptions));
                            }),
                            new FloatMenuOption("FCDuplicate".Translate(), delegate
                            {
                                ThingDef tempDef = selectedSupport.projectiles[k];
                                List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();

                                thingOptions.Add(new FloatMenuOption("1x", delegate
                                {
                                    for (int l = 0; l < 1; l++)
                                    {
                                        if (k == selectedSupport.projectiles.Count - 1)
                                        {
                                            selectedSupport.projectiles.Add(tempDef);
                                        }
                                        else
                                        {
                                            selectedSupport.projectiles.Insert(k + 1, tempDef);
                                        }
                                    }
                                }));
                                thingOptions.Add(new FloatMenuOption("5x", delegate
                                {
                                    for (int l = 0; l < 5; l++)
                                    {
                                        if (k == selectedSupport.projectiles.Count - 1)
                                        {
                                            selectedSupport.projectiles.Add(tempDef);
                                        }
                                        else
                                        {
                                            selectedSupport.projectiles.Insert(k + 1, tempDef);
                                        }
                                    }
                                }));
                                thingOptions.Add(new FloatMenuOption("10x", delegate
                                {
                                    for (int l = 0; l < 10; l++)
                                    {
                                        if (k == selectedSupport.projectiles.Count - 1)
                                        {
                                            selectedSupport.projectiles.Add(tempDef);
                                        }
                                        else
                                        {
                                            selectedSupport.projectiles.Insert(k + 1, tempDef);
                                        }
                                    }
                                }));
                                thingOptions.Add(new FloatMenuOption("20x", delegate
                                {
                                    for (int l = 0; l < 20; l++)
                                    {
                                        if (k == selectedSupport.projectiles.Count - 1)
                                        {
                                            selectedSupport.projectiles.Add(tempDef);
                                        }
                                        else
                                        {
                                            selectedSupport.projectiles.Insert(k + 1, tempDef);
                                        }
                                    }
                                }));
                                thingOptions.Add(new FloatMenuOption("50x", delegate
                                {
                                    for (int l = 0; l < 50; l++)
                                    {
                                        if (k == selectedSupport.projectiles.Count - 1)
                                        {
                                            selectedSupport.projectiles.Add(tempDef);
                                        }
                                        else
                                        {
                                            selectedSupport.projectiles.Insert(k + 1, tempDef);
                                        }
                                    }
                                }));
                                Find.WindowStack.Add(new FloatMenu(thingOptions));
                            })
                        };
                        Find.WindowStack.Add(new FloatMenu(listOptions));
                    }

                    if (Widgets.ButtonTextSubtle(upArrow, ""))
                    {
                        //if click up arrow button
                        if (i != 0)
                        {
                            ThingDef temp = selectedSupport.projectiles[i];
                            selectedSupport.projectiles[i] = selectedSupport.projectiles[i - 1];
                            selectedSupport.projectiles[i - 1] = temp;
                        }
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(upArrow, "^");
                    if (Widgets.ButtonTextSubtle(downArrow, ""))
                    {
                        //if click down arrow button
                        if (i != selectedSupport.projectiles.Count - 1)
                        {
                            ThingDef temp = selectedSupport.projectiles[i];
                            selectedSupport.projectiles[i] = selectedSupport.projectiles[i + 1];
                            selectedSupport.projectiles[i + 1] = temp;
                        }
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(downArrow, "v");
                    if (Widgets.ButtonTextSubtle(delete, ""))
                    {
                        //if click delete  button
                        selectedSupport.projectiles.RemoveAt(i);
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(delete, "X");
                }
            }
            
            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
        
        public Rect deriveRectRow(Rect rect, float x, float y = 0, float width = 0, float height = 0)
        {
            float inputWidth;
            float inputHeight;
            if (width == 0)
            {
                inputWidth = rect.width;
            }
            else
            {
                inputWidth = width;
            }

            if (height == 0)
            {
                inputHeight = rect.height;
            }
            else
            {
                inputHeight = height;
            }

            Rect newRect = new Rect(rect.x + rect.width + x, rect.y + y, inputWidth, inputHeight);
            return newRect;
        }
    }
}