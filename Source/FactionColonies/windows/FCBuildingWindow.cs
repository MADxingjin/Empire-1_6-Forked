﻿using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public enum BuildingFilter
    {
        All,
        Happiness,
        Food,
        Weapons,
        Apparel,
        Research,
        Medicine,
        Power,
        Military,
        Basetax,
        Workers
    }

    class FCBuildingWindow : Window
    {
        readonly WorldSettlementFC settlement;
        readonly int buildingSlot;
        readonly BuildingFCDef buildingDef;
        readonly List<BuildingFCDef> buildingList;
        readonly List<BuildingFCDef> filteredBuildingList;
        readonly FactionFC factionfc;
        readonly TaggedString buildingDesc;

        private static readonly int offset = 8;
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly int rowHeight = 90;

        private float fullScrollHeight = 90f;

        // Filter state
        //private BuildingFilter currentFilter = BuildingFilter.All;
        /* To deal with a variable number of resources (and variable resources in general), we use an int for
         * the filter. The value of the filter, and the corresponding label, are set in WorldObjectComp_SettlementBuildings
         */
        private int currentFilter = 0;
        private int filterSize = 0;
        private int filterRows = 2;
        private const int filterButtonsPerRow = 6;
        private static readonly int filterButtonHeight = 25;
        private static readonly int filterRowHeight = 30;
        
        // Dynamic rectangles that will be calculated based on window size
        Rect TopWindow;
        Rect TopIcon;
        Rect TopName;
        Rect TopDescription;
        Rect FilterArea;

        // Window size settings - add these fields
        public float buildingWindowWidth = 450f;
        public float buildingWindowHeight = 600f;
        
        // Static variables to remember window size during play session
        private static Vector2 savedWindowSize = new Vector2(450f, 600f);
        private static bool hasSavedSize = false;
        
        public override Vector2 InitialSize => new Vector2(
            FCSettings.buildingWindowWidth,
            FCSettings.buildingWindowHeight
        );
        
        // Override PreClose to save the current window size
        public override void PreClose()
        {
            base.PreClose();
            
            // Save the current window size to settings
            FCSettings.buildingWindowWidth = windowRect.width;
            FCSettings.buildingWindowHeight = windowRect.height;
            
            // Write the settings to disk
            LoadedModManager.GetMod<FactionColoniesMod>().WriteSettings();
        }
        
        // Calculate dynamic layout based on current window size
        private void CalculateLayout(Rect inRect)
        {
            float descHeight = Math.Max(64, Text.CalcHeight(buildingDesc.RawText, inRect.width - 110));
            float topWindowHeight = Math.Max(120f, descHeight + 45); // 20% of window height, minimum 120px
            
            TopWindow = new Rect(0, 0, inRect.width, topWindowHeight);
            TopName = new Rect(15, 15, inRect.width - 30, 30);
            TopIcon = new Rect(15, TopName.yMax + 5, 64, 64);
            TopDescription = new Rect(95, TopIcon.y + 5, inRect.width - 115, descHeight);
            FilterArea = new Rect(5, topWindowHeight + 5, inRect.width - 10, filterRowHeight * filterRows);

            CalculateScrollHeight(inRect);
        }
        private void CalculateScrollHeight(Rect inRect)
        {
            float width = inRect.width - 96f;
            fullScrollHeight = 0;
            for (int i = 0; i < filteredBuildingList.Count; i++)
            {
                BuildingFCDef building = filteredBuildingList[i];
                TaggedString buildingdesc = settlement.BuildingsComp.getBuildingDesc(building);
                // fancy math to size the description box to fit the description text
                GameFont tmp = Text.Font;
                Text.Font = GameFont.Tiny;
                float textHeight = Text.CalcHeight(buildingdesc.RawText, width);
                Text.Font = tmp;
                float descHeight = Math.Max(64, textHeight);
                fullScrollHeight += descHeight + 25f;
            }
        }

        private void DrawFilterButtons(Rect inRect)
        {
            // Define filter categories
            /*var filters = new[]
            {
                BuildingFilter.All,
                BuildingFilter.Happiness,
                BuildingFilter.Military,
                BuildingFilter.Basetax,
                BuildingFilter.Workers,
                BuildingFilter.Food,
                BuildingFilter.Weapons,
                BuildingFilter.Apparel,
                BuildingFilter.Research,
                BuildingFilter.Medicine,
                BuildingFilter.Power
            };*/

            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Text.Font = GameFont.Tiny;

            // Calculate button dimensions - 6 per row to fit all 11 filters in 2 rows
            float buttonWidth = (FilterArea.width - 10) / filterButtonsPerRow;
            float buttonHeight = filterButtonHeight;

            for (int i = 0; i < filterSize; i++)
            {
                int row = i / filterButtonsPerRow;
                int col = i % filterButtonsPerRow;
                
                Rect buttonRect = new Rect(
                    FilterArea.x + 5 + (col * buttonWidth),
                    FilterArea.y + (row * (buttonHeight + 5)),
                    buttonWidth - 5,
                    buttonHeight
                );

                bool isSelected = currentFilter == i;
                
                // Draw button background
                if (isSelected)
                {
                    // Draw selected state with blue background
                    Widgets.DrawBoxSolid(buttonRect, new Color(0.2f, 0.5f, 0.8f, 0.8f));
                    Widgets.DrawBox(buttonRect, 1);
                }
                else
                {
                    // Draw normal button background
                    if (Widgets.ButtonInvisible(buttonRect))
                    {
                        currentFilter = i;
                        ApplyFilter();
                    }
                    Widgets.DrawAtlas(buttonRect, Widgets.ButtonBGAtlas);
                }
                
                // Draw text manually with consistent centering
                Text.Anchor = TextAnchor.MiddleCenter;
                Color textColor = isSelected ? Color.white : Color.white;
                GUI.color = textColor;
                Widgets.Label(buttonRect, settlement.BuildingsComp.getLabelForFilter(i));
                GUI.color = Color.white;
                
                // Handle click for selected buttons
                if (isSelected && Widgets.ButtonInvisible(buttonRect))
                {
                    // Allow clicking selected button to deselect (go back to All)
                    currentFilter = 0;
                    ApplyFilter();
                }
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void ApplyFilter()
        {
            filteredBuildingList.Clear();
            
            foreach (var building in buildingList)
            {
                if (ShouldShowBuilding(building))
                {
                    filteredBuildingList.Add(building);
                }
            }
        }

        private bool ShouldShowBuilding(BuildingFCDef building)
        {
            return settlement.BuildingsComp.filterBuilding(currentFilter, building);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Calculate dynamic layout
            CalculateLayout(inRect);
            
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            
            // Draw filter buttons
            DrawFilterButtons(inRect);
            
            // Dynamic scroll area that adjusts to window size and accounts for filter area
            var scrollAreaTop = FilterArea.yMax + 5;
            var outRect = new Rect(0f, scrollAreaTop, inRect.width, inRect.height - scrollAreaTop);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, fullScrollHeight);
            
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            
            //Buildings
            for (int i = 0; i < filteredBuildingList.Count; i++)
            {
                BuildingFCDef building = filteredBuildingList[i];
                TaggedString buildingdesc = settlement.BuildingsComp.getBuildingDesc(building);
                // fancy math to size the description box to fit the description text
                float buildingDescWidth = ls.ColumnWidth - 80;
                GameFont tmp = Text.Font;
                Text.Font = GameFont.Tiny;
                float textHeight = Text.CalcHeight(buildingdesc.RawText, buildingDescWidth);
                Text.Font = tmp;
                float descHeight = Math.Max(64, textHeight);
                float thisRowHeight = 25f + descHeight;


                var newBuildingWindow = ls.GetRect(thisRowHeight);
                var newBuildingIcon = new Rect(newBuildingWindow.x + offset, newBuildingWindow.y + offset, 64, 64);
                var newBuildingLabel = new Rect(newBuildingWindow.x + 80, newBuildingWindow.y + 5,
                    buildingDescWidth, 20);
                var newBuildingDesc = new Rect(newBuildingWindow.x + 80, newBuildingWindow.y + 25,
                    buildingDescWidth, descHeight);

                if (Widgets.ButtonInvisible(newBuildingWindow))
                {
                    //If click on building
                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                    if (building == buildingDef)
                    {
                        //if the same building
                        list.Add(new FloatMenuOption("Destroy".Translate(), delegate
                        {
                            settlement.deconstructBuilding(buildingSlot);
                            Find.WindowStack.TryRemove(this);
                            Find.WindowStack.WindowOfType<SettlementWindowFc>().windowUpdateFc();
                        }));
                    }
                    else
                    {
                        //if not the same building
                        list.Add(new FloatMenuOption("Build".Translate(), delegate
                        {
                            if (settlement.BuildingsComp?.validConstructBuilding(building, buildingSlot) != true) return;
                            FCEvent tmpEvt = new FCEvent(true)
                            {
                                def = FCEventDefOf.constructBuilding,
                                source = settlement.Tile,
                                building = building,
                                buildingSlot = buildingSlot
                            };

                            int triggerTime = building.constructionDuration;
                            if (factionfc.hasPolicy(FCPolicyDefOf.isolationist))
                                triggerTime /= 2;

                            tmpEvt.timeTillTrigger = Find.TickManager.TicksGame + triggerTime;
                            Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);

                            PaymentUtil.paySilver(Convert.ToInt32(building.cost));
                            Messages.Message(building.label + " " + "WillBeConstructedIn".Translate() + " " + (tmpEvt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString(), MessageTypeDefOf.PositiveEvent);
                            settlement.BuildingsComp.startConstruction(building, buildingSlot, tmpEvt.timeTillTrigger);
                            Find.WindowStack.TryRemove(this);
                            Find.WindowStack.WindowOfType<SettlementWindowFc>().windowUpdateFc();
                        }));
                    }

                    FloatMenu menu = new FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }

                Widgets.DrawMenuSection(newBuildingWindow);
                Widgets.DrawMenuSection(newBuildingIcon);
                Widgets.DrawLightHighlight(newBuildingIcon);
                Widgets.ButtonImage(newBuildingIcon, building.Icon);

                Text.Font = GameFont.Small;
                Widgets.ButtonTextSubtle(newBuildingLabel, "");
                Widgets.Label(newBuildingLabel, "  " + building.LabelCap + " - " + "Cost".Translate() + ": " + building.cost);

                Text.Font = GameFont.Tiny;
                Widgets.Label(newBuildingDesc, settlement.BuildingsComp.getBuildingDesc(building));
            }

            ls.End();
            Widgets.EndScrollView();

            //Top Window - now using dynamic sizing
            Widgets.DrawMenuSection(TopWindow);
            Widgets.DrawHighlight(TopWindow);
            Widgets.DrawMenuSection(TopIcon);
            Widgets.DrawLightHighlight(TopIcon);

            // Dynamic border that adjusts to window width
            Widgets.DrawBox(new Rect(0, 0, inRect.width, TopWindow.height));
            Widgets.ButtonImage(TopIcon, buildingDef.Icon);

            Widgets.ButtonTextSubtle(TopName, "");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(TopName.x + 5, TopName.y, TopName.width, TopName.height), buildingDef.LabelCap);

            Widgets.DrawMenuSection(new Rect(TopDescription.x - 5, TopDescription.y - 5, TopDescription.width + 10, TopDescription.height));
            Text.Font = GameFont.Small;
            Widgets.Label(TopDescription, buildingDesc);
            
            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public FCBuildingWindow(WorldSettlementFC settlement, int buildingSlot)
        {
            factionfc = Find.World.GetComponent<FactionFC>();
            buildingList = new List<BuildingFCDef>();
            filteredBuildingList = new List<BuildingFCDef>();
            
            foreach (BuildingFCDef building in DefDatabase<BuildingFCDef>.AllDefsListForReading.Where(def => def.RequiredModsLoaded))
            {
                if(building.defName != "Empty" && building.defName != "Construction")
                {
                    //If not a building that shouldn't appear on the list
                    if (building.techLevel <= factionfc.techLevel)
                    {
                        //If building techlevel requirement is met
                        if (building.applicableBiomes.Count == 0 || building.applicableBiomes.Any() 
                            && building.applicableBiomes.Contains(settlement.biome)){
                            //If building meets the biome requirements
                            
                            // Check settlement type restrictions
                            bool meetsSettlementTypeRequirement = true;
                            if (building.settlementTypeBlockList?.Count > 0)
                            {
                                if (building.settlementTypeBlockList.Contains(settlement.settlementDef))
                                {
                                    meetsSettlementTypeRequirement = false;
                                }
                            }
                            if (building.settlementTypeAllowList?.Count > 0)
                            {
                                //If we have an allowlist, then the default restriction is false
                                meetsSettlementTypeRequirement = false;
                                if (building.settlementTypeAllowList.Contains(settlement.settlementDef))
                                {
                                    meetsSettlementTypeRequirement = true;
                                }
                            }
                            
                            if (meetsSettlementTypeRequirement)
                            {
                                buildingList.Add(building);
                            }
                        }
                    }
                }
            }

            buildingList.Sort(CompareUtil.CompareBuildingDef);

            // Initialize filtered list with all buildings
            filteredBuildingList.AddRange(buildingList);

            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;  // Enable window resizing

            this.settlement = settlement;
            this.buildingSlot = buildingSlot;
            buildingDef = settlement.BuildingsComp?.getBuildingInSlot(buildingSlot);
            /* If the buildingDef is "Construction", then find the building that's being constructed and list it in the description. */
            if (buildingDef == BuildingFCDefOf.Construction)
            {
                buildingDesc = "Empire_BuildingWindow_ConstructionDesc".Translate(settlement.BuildingsComp.Buildings[buildingSlot].underConstructionDef.label);
            }
            else
            {
                buildingDesc = settlement.BuildingsComp.getBuildingDesc(buildingDef);
            }

            filterSize = settlement.BuildingsComp.getFilterSize();
            filterRows = (int)Math.Ceiling((double)filterSize / (double)filterButtonsPerRow);
            fullScrollHeight = filteredBuildingList.Count * rowHeight;
        }
    }
}