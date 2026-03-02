using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class MainTabWindow_Colony : MainTabWindow
    {
        private const float margin = 5f;
        private const float smallMargin = 3f;
        private const float bigMargin = 8f;

        // ===== TAB STATE =====
        private enum EmpireTab { Overview, Bills, Events, Military }
        private EmpireTab curTab = EmpireTab.Overview;
        private List<TabRecord> tabs = new List<TabRecord>();

        // ===== WINDOW SIZE =====
        public override Vector2 InitialSize => new Vector2(1210f, 640f);

        // ===== DATA =====
        public bool selectingColonyFC;
        public FactionFC faction;

        // ===== UPDATE TIMER =====
        private int UIUpdateTimer;

        // ===== SCROLL POSITIONS =====
        private Vector2 settlementScroll;
        private Vector2 billsScroll;
        private Vector2 eventsScroll;

        // ===== MILITARY STATE =====
        private Vector2 militaryScroll;
        private MilitaryCustomizationUtil militaryUtil;

        // ===== LIFECYCLE =====

        public override void PreOpen()
        {
            base.PreOpen();
            faction = FactionCache.FactionComp;
            if (faction == null)
            {
                LogUtil.Error("WorldComp FactionFC is null - Something is wrong!");
                return;
            }

            faction.updateAverages();
            faction.updateTotalProfit();
            militaryUtil = faction.militaryCustomizationUtil;

            // Build tab list
            tabs.Clear();
            tabs.Add(new TabRecord("Overview".Translate(), delegate
            {
                curTab = EmpireTab.Overview;
                faction.updateTotalProfit();
            }, () => curTab == EmpireTab.Overview));

            tabs.Add(new TabRecord("Bills".Translate(), delegate
            {
                curTab = EmpireTab.Bills;
                billsScroll = Vector2.zero;
            }, () => curTab == EmpireTab.Bills));

            tabs.Add(new TabRecord("Events".Translate(), delegate
            {
                curTab = EmpireTab.Events;
                eventsScroll = Vector2.zero;
            }, () => curTab == EmpireTab.Events));

            tabs.Add(new TabRecord("Military".Translate(), delegate
            {
                curTab = EmpireTab.Military;
                militaryScroll = Vector2.zero;
            }, () => curTab == EmpireTab.Military));
        }

        public override void PostClose()
        {
            base.PostClose();
            selectingColonyFC = false;
            militaryUtil?.checkMilitaryUtilForErrors();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (UIUpdateTimer < Find.TickManager.TicksAbs)
            {
                UIUpdateTimer = Find.TickManager.TicksAbs + FCSettings.updateUiTimer;
                if (faction != null)
                    faction.updateAverages();
            }
        }

        // ===== MAIN DRAW =====

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Faction gfaction = FactionCache.PlayerColonyFaction;
            if (gfaction == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect btn = new Rect(inRect.x + inRect.width / 2f - 150f, inRect.y + inRect.height / 2f - 20f, 300f, 40f);
                if (Widgets.ButtonText(btn, "FCCreateNewFaction".Translate()))
                {
                    ColonyUtil.createPlayerColonyFaction();
                    faction = FactionCache.FactionComp;
                    if (faction != null)
                    {
                        faction.factionCreated = true;
                        Find.WindowStack.Add(new FactionCustomizeWindowFc(faction));
                        if (Find.CurrentMap.Parent != null &&
                            Find.WorldObjects.WorldObjectAt<WorldSettlementFC>(Find.CurrentMap.Parent.Tile) != null)
                        {
                            Messages.Message(
                                "SetAsFactionCapital".Translate(Find.WorldObjects.SettlementAt(Find.CurrentMap.Parent.Tile).Name),
                                MessageTypeDefOf.NeutralEvent);
                        }
                    }
                    else
                    {
                        LogUtil.Error("FactionFC world component is still null after creating new faction!");
                    }
                }
                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
                return;
            }

            // Content area sits below the tab strip
            Rect contentRect = new Rect(inRect.x, inRect.y + TabDrawer.TabHeight, inRect.width, inRect.height - TabDrawer.TabHeight);
            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabs);

            switch (curTab)
            {
                case EmpireTab.Overview:
                    DrawOverviewTab(contentRect);
                    break;
                case EmpireTab.Bills:
                    DrawBillsTab(contentRect);
                    break;
                case EmpireTab.Events:
                    DrawEventsTab(contentRect);
                    break;
                case EmpireTab.Military:
                    DrawMilitaryTab(contentRect);
                    break;
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        // ===== OVERVIEW TAB =====

        private void DrawOverviewTab(Rect rect)
        {
            const float leftWidth = 175f;
            const float rightWidth = 200f;
            const float panelGap = 10f;
            const float headerHeight = 63f;

            float bodyY = rect.y + margin + headerHeight + panelGap;
            float bodyH = rect.height - panelGap - headerHeight - margin;

            Rect headerPanel = new Rect(rect.x + margin, rect.y + margin, rect.width - (margin * 2), headerHeight);
            Rect leftPanel   = new Rect(rect.x + margin, bodyY, leftWidth, bodyH);
            Rect rightPanel  = new Rect(rect.xMax - margin - rightWidth, bodyY, rightWidth, bodyH);
            Rect centerPanel = new Rect(leftPanel.xMax + panelGap, bodyY, rightPanel.x - leftPanel.xMax - panelGap * 2, bodyH);

            DrawOverviewHeaderPanel(headerPanel);
            DrawOverviewLeftPanel(leftPanel);
            DrawOverviewCenterPanel(centerPanel);
            DrawOverviewRightPanel(rightPanel);

            Widgets.DrawLineVertical(leftPanel.xMax + (panelGap / 2), leftPanel.y, leftPanel.height - margin);
            Widgets.DrawLineVertical(centerPanel.xMax + (panelGap / 2), centerPanel.y, centerPanel.height - margin);
        }
        private void DrawOverviewHeaderPanel(Rect panel)
        {
            float iconSz = 55f;
            Rect iconRect = new Rect(panel.x, panel.y + (panel.height - iconSz) / 2f, iconSz, iconSz);
            Widgets.ButtonImage(iconRect, faction.factionIcon);

            float customizeBtnSize = 20f;
            Rect customizeBtn = new Rect(panel.xMax - customizeBtnSize, panel.y + margin, customizeBtnSize, customizeBtnSize);
            Rect labelBox = new Rect(iconRect.xMax + margin, panel.y, panel.width - iconSz - margin, 30f);
            Rect labelTextBox = new Rect(labelBox.x + margin, labelBox.y, labelBox.width - (margin * 2), labelBox.height);
            Rect titleBox = new Rect(labelBox.x, labelBox.yMax + margin, labelBox.width/2f, 22f);
            Rect foundingBox = new Rect(titleBox.xMax, titleBox.y, titleBox.width, titleBox.height);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawHighlight(labelBox);
            Widgets.Label(labelTextBox, faction.name ?? "");

            Text.Font = GameFont.Small;
            Widgets.Label(titleBox, faction.title ?? "");

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(foundingBox, "FCFoundedOn".Translate(faction.GetFoundingDate()));

            Widgets.DrawLineHorizontal(labelBox.x, titleBox.yMax + margin, panel.xMax - labelBox.x - margin);

            if (Widgets.ButtonImage(customizeBtn, TexLoad.iconCustomize))
            {
                if (FactionCache.PlayerColonyFaction != null)
                    Find.WindowStack.Add(new FactionCustomizeWindowFc(faction));
            }
        }
        private void DrawOverviewLeftPanel(Rect panel)
        {
            float y = panel.y;

            // --- XP Bar ---
            float xpH = 18f;
            Rect xpBar = new Rect(panel.x, y, panel.width, xpH);
            UIUtil.DrawProgressBarColors(xpBar, faction.factionXPCurrent / faction.factionXPGoal, Color.black, Color.green);
            Widgets.DrawShadowAround(xpBar);
            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(xpBar, Math.Round(faction.factionXPCurrent) + "/" + faction.factionXPGoal);
            y += xpH + margin;

            // Faction level
            Text.Font = GameFont.Small;
            Rect levelBox = new Rect(panel.x, y, panel.width, 20f);
            Widgets.DrawHighlight(levelBox);
            Widgets.Label(levelBox, "FCLevel".Translate(faction.factionLevel));
            y += levelBox.height + margin;

            // --- Stats ---
            Text.Anchor = TextAnchor.MiddleLeft;
            string[] statKeys = { "happiness", "loyalty", "unrest", "prosperity" };
            float statH   = 32f;

            foreach (string statKey in statKeys)
            {
                float iconSize = statH - (smallMargin * 2);
                Rect statBox = new Rect(panel.x, y, panel.width, statH);
                Rect iconBox = new Rect(statBox.x + smallMargin, y + smallMargin, iconSize, iconSize);
                Rect valueBox = new Rect(statBox.x + iconSize + bigMargin, y, statBox.width - iconSize - bigMargin, statH);
                Widgets.DrawMenuSection(statBox);
                Widgets.DrawHighlight(statBox);

                Texture2D icon;
                string value;
                string tooltip;

                switch (statKey)
                {
                    case "happiness":
                        icon      = TexLoad.iconHappiness;
                        value     = Convert.ToInt32(faction.averageHappiness) + "%";
                        tooltip = "FactionHappiness".Translate() + "\n-----\n" + "FactionHappinessDesc".Translate();
                        break;
                    case "loyalty":
                        icon      = TexLoad.iconLoyalty;
                        value     = Convert.ToInt32(faction.averageLoyalty) + "%";
                        tooltip = "FactionLoyalty".Translate() + "\n-----\n" + "FactionLoyaltyDesc".Translate();
                        break;
                    case "unrest":
                        icon      = TexLoad.iconUnrest;
                        value     = Convert.ToInt32(faction.averageUnrest) + "%";
                        tooltip = "FactionUnrest".Translate() + "\n-----\n" + "FactionUnrestDesc".Translate();
                        break;
                    default: // prosperity
                        icon      = TexLoad.iconProsperity;
                        value     = Convert.ToInt32(faction.averageProsperity) + "%";
                        tooltip = "FactionProsperity".Translate() + "\n-----\n" + "FactionProsperityDesc".Translate();
                        break;
                }

                Widgets.Label(iconBox, new GUIContent(icon));

                Text.Font   = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(valueBox, value);

                UIUtil.TipRegionByText(statBox, tooltip);

                y += statH + smallMargin;
            }

            y += margin - smallMargin;

            // --- Policies ---
            float policySize = 40f;
            if (faction.policies.Count == FCSettings.maxPolicyCount)
            {
                float leftX = panel.x + (panel.width - (policySize * faction.policies.Count) - (margin * (faction.policies.Count - 1)))/2f;
                for (int i = 0; i < faction.policies.Count; i++)
                {
                    Rect policyBox = new Rect(leftX + (i * (policySize + margin)), y, policySize, policySize);
                    Widgets.ButtonImage(policyBox, faction.policies[i].def.IconLight);
                    UIUtil.TipRegionByText(policyBox, faction.policies[i].def.PolicyText());
                }
            }
            else
            {
                Text.Font   = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect buttonRect = new Rect(panel.x, y, panel.width, policySize);
                if (Widgets.ButtonText(buttonRect, "FCSelectPolicies".Translate()))
                {
                    Find.WindowStack.Add(new FactionCustomizePoliciesWindowFC(faction));
                }
            }
            y += policySize + margin;

            // --- Traits ---
            Widgets.DrawLineHorizontal(panel.x + margin, y, panel.width - (margin * 2));
            y += margin;
            float traitH = 30f;

            bool hasOpenSlots = false;
            for (int slot = 0; slot < 5; slot++)
            {
                FCPolicy current = faction.factionTraits[slot];
                bool isLocked = faction.factionLevel < (slot + 1);
                bool isOpen = !isLocked && current.def == FCPolicyDefOf.empty;
                if (isOpen) hasOpenSlots = true;

                Rect traitRect = new Rect(panel.x, y, panel.width, traitH);
                Rect labelRect = new Rect(traitRect.x + (bigMargin * 2), y, traitRect.width - (bigMargin * 2), traitRect.height);

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.DrawHighlight(traitRect);

                if (isLocked)
                {
                    Widgets.Label(labelRect, "FCTraitLockedUntilLevel".Translate(slot + 1).Colorize(Color.gray));
                }
                else if (current.def != FCPolicyDefOf.empty)
                {
                    Widgets.Label(labelRect, current.def.LabelCap);
                    UIUtil.TipRegionByText(traitRect, current.def.PolicyText());
                }
                else
                {
                    Widgets.Label(labelRect, "FCSelectANewTrait".Translate().Colorize(Color.yellow));
                }

                y += traitH + smallMargin;
            }

            if (hasOpenSlots)
            {
                Rect selectButton = new Rect(panel.x, y, panel.width, traitH);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonText(selectButton, "FCSelectTraits".Translate()))
                {
                    Find.WindowStack.Add(new FactionCustomizeTraitsWindowFC(faction));
                }
                y += traitH + smallMargin;
            }

            y += margin - smallMargin;
            Widgets.DrawLineHorizontal(panel.x + margin, y, panel.width - (margin * 2));

            y += margin;

            // --- Road Building ---
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect roadBox = new Rect(panel.x, y, panel.width, 26f);
            Rect roadLabel = new Rect(roadBox.x + margin, y, 120f, roadBox.height);
            Rect checkBox = new Rect(roadBox.xMax - 24f, y+1, 24f, 24f);
            Widgets.DrawMenuSection(roadBox);
            Widgets.Label(roadLabel, "FCBuildRoads".Translate());
            Widgets.DrawHighlight(checkBox);
            Widgets.Checkbox(checkBox.x, checkBox.y, ref faction.roadBuilder.roadBuildingEnabled);
        }

        private void DrawOverviewCenterPanel(Rect panel)
        {
            float x = panel.x;
            float y = panel.y;
            float width = panel.width;

            // --- Action Buttons ---
            Rect actionPanel = new Rect(x, y, width, 30f);
            DrawActionButtons(actionPanel);

            y += actionPanel.height + margin;

            // Seperator
            Widgets.DrawLineHorizontal(x + margin, y, panel.width - (margin * 2));
            y += margin;

            // --- Settlements Table ---
            float tableH = panel.yMax - y - margin;
            if (tableH > 0f)
                DrawSettlementsTable(new Rect(x, y, width, tableH));
        }
        private void DrawActionButtons(Rect panel)
        {
            int numButtons = 1 + (faction.hasPolicy(FCPolicyDefOf.technocratic) ? 1 : 0) + (faction.hasPolicy(FCPolicyDefOf.feudal) ? 1 : 0);
            // The "Create New Colony" button is more important than all the rest, so we'll make it as wide as two of the other buttons. Keep that in mind for the following math
            float calcButtonWidth = (panel.width - (margin * (numButtons - 1))) / (numButtons + 1);
            float y = panel.y;
            float x = panel.x;
            float height = panel.height;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            // TODO: would really like to generalize the faction policy code
            if (faction.hasPolicy(FCPolicyDefOf.technocratic))
            {
                Rect techButton = new Rect(x, y, calcButtonWidth, height);
                if (Widgets.ButtonText(techButton, "FCSendResearchItems".Translate()))
                {
                    if (Find.ColonistBar.GetColonistsInOrder().Count > 0)
                    {
                        Pawn playerNegotiator = Find.ColonistBar.GetColonistsInOrder()[0];
                        FCTrader_Research trader = new FCTrader_Research();
                        Find.WindowStack.Add(new Dialog_Trade(playerNegotiator, trader));
                    }
                    else
                    {
                        LogUtil.Error("Couldn't find any colonists to trade with");
                    }
                }
                x += techButton.width + margin;
            }

            if (faction.hasPolicy(FCPolicyDefOf.feudal))
            {
                Rect feudalButton = new Rect(x, y, calcButtonWidth, height);
                if (Widgets.ButtonText(feudalButton, "FCRequestMercenary".Translate()))
                {
                    if (faction.traitFeudalBoolCanUseMercenary)
                    {
                        faction.traitFeudalBoolCanUseMercenary = false;
                        faction.traitFeudalTickLastUsedMercenary = Find.TickManager.TicksGame;

                        PawnGenerationRequest request = FCPawnGenerator.WorkerOrMilitaryRequest();
                        request.ColonistRelationChanceFactor = 20f;
                        Pawn pawn = PawnGenerator.GeneratePawn(request);

                        IncidentParms parms = new IncidentParms
                        {
                            target = Find.CurrentMap,
                            faction = FactionCache.PlayerColonyFaction,
                            points = 999,
                            raidArrivalModeForQuickMilitaryAid = true,
                            raidNeverFleeIndividual = true,
                            raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                            raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
                        };
                        parms.raidArrivalModeForQuickMilitaryAid = true;

                        PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
                        worker.TryResolveRaidSpawnCenter(parms);
                        worker.Arrive(new List<Pawn> { pawn }, parms);

                        Find.LetterStack.ReceiveLetter(
                            "FCMercenaryJoined".Translate(),
                            "FCMercenaryJoinedText".Translate(pawn.NameFullColored),
                            LetterDefOf.PositiveEvent,
                            new LookTargets(pawn));
                        pawn.SetFaction(Faction.OfPlayer);
                    }
                    else
                    {
                        Messages.Message(
                            "FCActionMercenaryOnCooldown".Translate(
                                ((faction.traitFeudalTickLastUsedMercenary + GenDate.TicksPerSeason) - Find.TickManager.TicksGame).ToTimeString()),
                            MessageTypeDefOf.RejectInput);
                    }
                }
                x += feudalButton.width + margin;
            }

            Rect newColonyButton = new Rect(x, y, calcButtonWidth * 2, height);
            if (Widgets.ButtonText(newColonyButton, "CreateNewColony".Translate()))
            {
                Find.WindowStack.Add(new CreateColonyWindowFc());
                Find.World.renderer.wantedMode = WorldRenderMode.Planet;
                Messages.Message("SelectTile".Translate(), MessageTypeDefOf.NegativeEvent);
                Find.WindowStack.TryRemove(this);
            }
        }
        private Vector2 poolScrollbar = new Vector2();
        private void DrawOverviewRightPanel(Rect panel)
        {
            float x = panel.x;
            float y = panel.y;
            float width = panel.width;

            Rect profitBox = new Rect(x, y, width, 28f);
            Rect profitLabel = new Rect(profitBox.x, profitBox.y, (width - margin)/2f, profitBox.height);
            Rect profitNum = new Rect(profitLabel.xMax + margin, profitLabel.y, profitLabel.width, profitLabel.height);

            // --- Economic Stats ---
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.DrawHighlight(profitBox);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(profitLabel, "EstimatedProfit".Translate() + ": ");
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(profitNum, new GUIContent(Math.Round(faction.profit).ToString(), ThingDefOf.Silver.uiIcon));
            y += profitBox.height + margin;

            Rect taxBox = new Rect(x, y, width, 22f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(taxBox, "TimeTillTax".Translate() + ": " + Math.Max(0, faction.taxTimeDue - Find.TickManager.TicksGame).ToTimeString());
            y += taxBox.height + margin;

            // Seperator
            Widgets.DrawLineHorizontal(x + margin, y, width - (margin * 2));
            y += margin;

            // --- Resource Pools ---
            int numPools = faction.resourcePools.Count;
            if (numPools > 0)
            {
                float rowSize = 22f;
                float rowWidth = width;
                float sectionHeight = rowSize * Math.Min(numPools, 5);
                float totalHeight = rowSize * numPools;

                Rect poolHeader = new Rect(x, y, width, rowSize);
                Widgets.DrawHighlight(poolHeader);
                Widgets.Label(poolHeader, "FCResourcePools".Translate());
                y += poolHeader.height + margin;

                Rect poolListBox = new Rect(x, y, width, sectionHeight);
                Widgets.DrawMenuSection(poolListBox);
                if (totalHeight > sectionHeight)
                {
                    //scrollbox time, baby
                    // By default, Empire only has two resource pools (research, power). But now that we can add ~more~, I'm including this code to allow the UI to gracefully handle more pools
                    Rect scrollList = new Rect(x, y, width, totalHeight);
                    Widgets.BeginScrollView(poolListBox, ref poolScrollbar, scrollList);
                    rowWidth -= 16f; //width of the scrollbar
                }

                // draw the pools
                for (int i = 0; i < faction.resourcePools.Count; i++)
                {
                    ResourcePool pool = faction.resourcePools[i];
                    Rect row = new Rect(x, y + (i * rowSize), rowWidth, rowSize);
                    Rect icon = new Rect(row.x + 2f, row.y + 1, rowSize - 2, rowSize - 2);
                    Rect actions = new Rect(row.xMax - 80f, row.y + 1, 79f, rowSize - 2);
                    Rect amount = new Rect(icon.xMax + margin, row.y, actions.x - icon.xMax - (margin * 2), rowSize);

                    if (i % 2 == 0)
                    {
                        Widgets.DrawHighlight(row);
                    }

                    Widgets.ButtonImage(icon, pool.resource.Icon);
                    UIUtil.TipRegionByText(icon, pool.resource.LabelCap);

                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(amount, Math.Round(pool.pool).ToString());

                    bool changedGui = false;
                    IEnumerable<FloatMenuOption> options = pool.GetFactionMenuFloatMenuOptions();
                    if (options == null || options.Count() == 0)
                    {
                        GUI.color = Color.gray;
                        changedGui = true;
                    }
                    Text.Font = GameFont.Tiny;
                    if (Widgets.ButtonText(actions, "Actions".Translate(), active: !changedGui))
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (FloatMenuOption option in options)
                            list.Add(option);
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                    if (changedGui)
                    {
                        GUI.color = Color.white;
                    }
                }

                if (totalHeight > sectionHeight)
                {
                    Widgets.EndScrollView();
                }
                y += sectionHeight + margin;

                // Seperator
                Widgets.DrawLineHorizontal(x + margin, y, width - (margin * 2));
                y += margin;
            }

            // --- Resource Production ---
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect prodHeaderBox = new Rect(x, y, width, 22f);
            Widgets.DrawHighlight(prodHeaderBox);
            Widgets.Label(prodHeaderBox, "TotalProduction".Translate());
            y += prodHeaderBox.height + margin;

            float iconSize  = 30f;
            float rGap = 5f;
            int rPerRow = Mathf.Max(1, (int)Math.Floor(width / (iconSize + rGap)));
            int ri = 0;
            float resRowWidth = rPerRow * (iconSize + rGap) - rGap;
            float rowx = panel.x + (panel.width - resRowWidth) / 2f;

            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (ResourceDisplay resource in faction.FactionResources)
            {
                int col = ri % rPerRow;
                int row = ri / rPerRow;
                float rx = rowx + col * (iconSize + rGap);
                float ry = y + row * (iconSize + 16f);
                Rect icon = new Rect(rx, ry, iconSize, iconSize);

                Widgets.ButtonImage(icon, resource.Icon);
                UIUtil.TipRegionByText(icon, resource.label);

                Widgets.Label(new Rect(rx, ry + iconSize, iconSize, 15f), resource.amount.ToString());
                ri++;
            }
        }

        private void DrawSettlementsTable(Rect tableRect)
        {
            const float headerH = 25f;
            const float rowH = 25f;
            // Scrollable rows
            float contentH = faction.settlements.Count * rowH;
            Rect viewRect = new Rect(tableRect.x, tableRect.y + headerH, tableRect.width, tableRect.height - headerH);
            float scrollMargin = contentH > viewRect.height ? 16f : 0;
            Rect scrollRect = new Rect(0f, 0f, tableRect.width - scrollMargin, Mathf.Max(contentH, viewRect.height));
            // math time, baby. Don't you love UI coding?
            float levelw = 50f;
            float millevelw = 60f;
            float profitw = 60f;
            float workerw = 75f;
            float happyw = 65f;
            float loyalw = 60f;
            float unrestw = 55f;
            float foundw = 90f;
            float namew = tableRect.width - levelw - millevelw - profitw - workerw - happyw - loyalw - unrestw - foundw - scrollMargin;
            float[] colWidths = { namew, levelw, millevelw, profitw, workerw, happyw, loyalw, unrestw, foundw };
            string[] colLabels =
            {
                "FCSettlementTableName".Translate(),
                "FCSettlementTableLevel".Translate(),
                "FCSettlementTableMilLevel".Translate(),
                "FCSettlementTableProfit".Translate(),
                "FCSettlementTableWorkers".Translate(),
                "FCSettlementTableHappiness".Translate(),
                "FCSettlementTableLoyalty".Translate(),
                "FCSettlementTableUnrest".Translate(),
                "FCSettlementTableFounding".Translate()
            };
            Action[] colSorts =
            {
                () => faction.settlements.Sort(CompareUtil.CompareSettlementName),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementLevel),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementMilitaryLevel),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementProfit),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementFreeWorkers),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementHappiness),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementLoyalty),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementUnrest),
                () => faction.settlements.Sort(CompareUtil.CompareSettlementFoundingDate)
            };

            // Header
            Rect headerRow = new Rect(tableRect.x, tableRect.y, tableRect.width, headerH);
            Widgets.DrawMenuSection(headerRow);
            Widgets.DrawLightHighlight(headerRow);

            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            float hx = tableRect.x;
            for (int c = 0; c < colWidths.Length; c++)
            {
                Rect cell = new Rect(hx, tableRect.y, colWidths[c], headerH);
                Widgets.Label(cell, colLabels[c]);
                int capturedC = c;
                if (Widgets.ButtonInvisible(cell))
                    colSorts[capturedC]();
                hx += colWidths[c];
            }

            Widgets.BeginScrollView(viewRect, ref settlementScroll, scrollRect);

            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < faction.settlements.Count; i++)
            {
                WorldSettlementFC s = faction.settlements[i];
                float ry = i * rowH;

                if (i % 2 == 0)
                    Widgets.DrawHighlight(new Rect(0f, ry, scrollRect.width, rowH));

                float sx = 0f;

                Text.Anchor = TextAnchor.MiddleLeft;
                if (Widgets.ButtonTextSubtle(new Rect(sx, ry, colWidths[0], rowH), s.Name))
                    Find.WindowStack.Add(new SettlementWindowFc(s));
                sx += colWidths[0];

                /* Need to reset because ButtonTextSubtle changes both the Anchor and the Font without reseting them */
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(sx, ry, colWidths[1], rowH), s.settlementLevel.ToString());         sx += colWidths[1];
                Widgets.Label(new Rect(sx, ry, colWidths[2], rowH), s.settlementMilitaryLevel.ToString()); sx += colWidths[2];
                Widgets.Label(new Rect(sx, ry, colWidths[3], rowH), ((int)s.getTotalProfit()).ToString()); sx += colWidths[3];
                Widgets.Label(new Rect(sx, ry, colWidths[4], rowH), (s.workersUltraMax - s.getTotalWorkers()).ToString()); sx += colWidths[4];
                Widgets.Label(new Rect(sx, ry, colWidths[5], rowH), ((int)s.Happiness).ToString()); sx += colWidths[5];
                Widgets.Label(new Rect(sx, ry, colWidths[6], rowH), ((int)s.Loyalty).ToString());  sx += colWidths[6];
                Widgets.Label(new Rect(sx, ry, colWidths[7], rowH), ((int)s.Unrest).ToString()); sx += colWidths[7];
                Widgets.Label(new Rect(sx, ry, colWidths[8], rowH), s.GetFoundingDate(false));
            }

            Widgets.EndScrollView();
        }

        // ===== BILLS TAB =====

        private void DrawBillsTab(Rect rect)
        {
            List<BillFC> bills = faction.Bills;
            const float pad     = 8f;
            const float headerH = 30f;
            const float rowH    = 30f;

            float cName    = 280f;
            float cDue     = 130f;
            float cAmount  = 120f;
            float cTithe   = 110f;
            float cResolve = rect.width - cName - cDue - cAmount - cTithe - pad * 2f;

            float hx = rect.x + pad;
            float hy = rect.y + pad;

            // Header row
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.DrawMenuSection(new Rect(rect.x + pad, hy, rect.width - pad * 2f, headerH));

            Widgets.ButtonTextSubtle(new Rect(hx,                               hy, cName,    headerH), "Settlement".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName,                       hy, cDue,     headerH), "DueFC".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName + cDue,                hy, cAmount,  headerH), "Amount".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName + cDue + cAmount,      hy, cTithe,   headerH), "HasTithe".Translate());

            Rect autoBtn = new Rect(hx + cName + cDue + cAmount + cTithe, hy, cResolve - 30f, headerH);
            if (Widgets.ButtonTextSubtle(autoBtn, "FCAutoResolve".Translate()))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("FCAutoResolving".Translate(faction.autoResolveBills ? "Yes".Translate() : "No".Translate()), delegate
                {
                    faction.autoResolveBills = !faction.autoResolveBills;
                    if (faction.autoResolveBills)
                    {
                        Messages.Message("FCBillsAutoResolving".Translate(), MessageTypeDefOf.NeutralEvent);
                        PaymentUtil.autoresolveBills(bills);
                    }
                    else
                    {
                        Messages.Message("FCBillsNotAutoResolving".Translate(), MessageTypeDefOf.NeutralEvent);
                    }
                }));
                Find.WindowStack.Add(new FloatMenu(list));
            }
            Widgets.Checkbox(new Vector2(autoBtn.xMax + 3f, hy + 3f), ref faction.autoResolveBills, 24, true);

            // Scrollable bill list
            float listY = hy + headerH + 2f;
            float viewH = rect.yMax - listY - pad;
            Rect viewRect  = new Rect(rect.x + pad, listY, rect.width - pad * 2f, viewH);
            float contentH = bills.Count * rowH;
            Rect scrollRect = new Rect(0f, 0f, viewRect.width - 16f, Mathf.Max(contentH, viewH));

            Widgets.BeginScrollView(viewRect, ref billsScroll, scrollRect);

            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            bool billResolved = false;
            for (int i = 0; i < bills.Count; i++)
            {
                BillFC bill = bills[i];
                float ry = i * rowH;
                float rx = 0f;

                if (i % 2 == 0)
                    Widgets.DrawHighlight(new Rect(rx, ry, scrollRect.width, rowH));

                string settleName = bill.settlement != null ? bill.settlement.Name : "Null";
                if (Widgets.ButtonText(new Rect(rx, ry, cName, rowH), settleName))
                {
                    if (bill.settlement != null)
                        Find.WindowStack.Add(new SettlementWindowFc(bill.settlement));
                }
                rx += cName;

                Widgets.Label(new Rect(rx, ry, cDue,    rowH), (bill.dueTick - Find.TickManager.TicksGame).ToTimeString()); rx += cDue;
                Widgets.Label(new Rect(rx, ry, cAmount,  rowH), bill.taxes.silverAmount.ToString()); rx += cAmount;

                bool hasTithe = bill.taxes.itemTithes.Count > 0;
                Widgets.Checkbox(new Vector2(rx + cTithe / 2f - 12f, ry), ref hasTithe); rx += cTithe;

                if (Widgets.ButtonText(new Rect(rx, ry, cResolve, rowH), "ResolveBill".Translate()))
                {
                    if (!bill.attemptResolve())
                        Messages.Message("NotEnoughSilverOnMapToPayBill".Translate() + "!", MessageTypeDefOf.RejectInput);
                    billResolved = true;
                    break;
                }
            }

            Widgets.EndScrollView();
        }

        // ===== EVENTS TAB =====

        private void DrawEventsTab(Rect rect)
        {
            List<FCEvent> events = faction.events;
            const float pad     = 8f;
            const float headerH = 30f;
            const float rowH    = 30f;

            float cName = 350f;
            float cDesc = 150f;
            float cLoc  = 150f;
            float cTime = rect.width - cName - cDesc - cLoc - pad * 2f;

            float hx = rect.x + pad;
            float hy = rect.y + pad;

            // Header row
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.DrawMenuSection(new Rect(rect.x + pad, hy, rect.width - pad * 2f, headerH));

            Widgets.ButtonTextSubtle(new Rect(hx,                         hy, cName, headerH), "Name".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName,                  hy, cDesc, headerH), "Description".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName + cDesc,          hy, cLoc,  headerH), "Source".Translate());
            Widgets.ButtonTextSubtle(new Rect(hx + cName + cDesc + cLoc,   hy, cTime, headerH), "TimeRemaining".Translate());

            // Scrollable event list
            float listY    = hy + headerH + 2f;
            float viewH    = rect.yMax - listY - pad;
            Rect viewRect  = new Rect(rect.x + pad, listY, rect.width - pad * 2f, viewH);
            float contentH = events.Count * rowH;
            Rect scrollRect = new Rect(0f, 0f, viewRect.width - 16f, Mathf.Max(contentH, viewH));

            Widgets.BeginScrollView(viewRect, ref eventsScroll, scrollRect);

            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < events.Count; i++)
            {
                FCEvent evt = events[i];
                float ry = i * rowH;
                float rx = 0f;

                if (i % 2 == 0)
                    Widgets.DrawHighlight(new Rect(rx, ry, scrollRect.width, rowH));

                Widgets.Label(new Rect(rx, ry, cName, rowH), evt.def.label); rx += cName;

                if (Widgets.ButtonText(new Rect(rx, ry, cDesc, rowH), "FCDesc".Translate()))
                {
                    if (!evt.hasCustomDescription)
                    {
                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");
                        if (!settlementString.NullOrEmpty())
                            Find.WindowStack.Add(new DescWindowFc($"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}"));
                        else
                            Find.WindowStack.Add(new DescWindowFc(evt.def.desc));
                    }
                    else
                    {
                        Find.WindowStack.Add(new DescWindowFc(evt.customDescription));
                    }
                }
                rx += cDesc;

                if (Widgets.ButtonText(new Rect(rx, ry, cLoc, rowH), "Location".Translate().CapitalizeFirst()))
                {
                    if (evt.hasDestination)
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(faction.returnSettlementByLocation(evt.location)));
                    }
                    else if (evt.settlementTraitLocations.Count > 0)
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (WorldSettlementFC settlement in evt.settlementTraitLocations)
                        {
                            if (settlement != null)
                            {
                                WorldSettlementFC cap = settlement;
                                list.Add(new FloatMenuOption(settlement.Name, delegate
                                {
                                    Find.WindowStack.Add(new SettlementWindowFc(cap));
                                }));
                            }
                        }
                        if (list.Count == 0)
                            list.Add(new FloatMenuOption("None".Translate(), null));
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                    else if (evt.def == FCEventDefOf.taxColony && evt.source != -1)
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(faction.returnSettlementByLocation(evt.source)));
                    }
                }
                rx += cLoc;

                Widgets.Label(new Rect(rx, ry, cTime, rowH), (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString());
            }

            Widgets.EndScrollView();
        }

        // ===== MILITARY TAB =====

        private void DrawMilitaryTab(Rect rect)
        {
            float x = rect.x;
            float y = rect.y;
            float width = rect.width;

            // --- Create buttons (right-aligned) ---
            float buttonWidth = 187f;
            float buttonHeight = 35f;
            float bx = rect.xMax - buttonWidth * 3 - margin;

            Rect iconRect = new Rect(x + margin, y + margin, buttonHeight, buttonHeight);
            Widgets.ButtonImage(iconRect, faction.factionIcon);

            Rect labelBox = new Rect(iconRect.xMax + margin, y + margin, bx - iconRect.xMax - (margin * 2), buttonHeight);
            Rect labelTextBox = new Rect(labelBox.x + margin, labelBox.y, labelBox.width - (margin * 2), labelBox.height);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawHighlight(labelBox);
            Widgets.Label(labelTextBox, faction.name ?? "");

            // Only allow the creation of units, squads, and fire support if there is at least one settlement. Required due to the fact that
            //   the design units menu pulls the list of possible material stuffs from the list of things that settlements can produce.
            if (faction.settlements?.Count > 0)
            {
                if (Widgets.ButtonTextSubtle(new Rect(bx, y + margin, buttonWidth, buttonHeight), "FCMilitaryTableButtonCreateUnit".Translate()))
                    OpenMilitaryWindow(new DesignUnitsWindow(militaryUtil, faction), "FCMilitaryTableButtonCreateUnit".Translate());
                bx += buttonWidth;

                if (Widgets.ButtonTextSubtle(new Rect(bx, y + margin, buttonWidth, buttonHeight), "FCMilitaryTableButtonCreateSquad".Translate()))
                    OpenMilitaryWindow(new DesignSquadsWindow(militaryUtil), "FCMilitaryTableButtonCreateSquad".Translate());
                bx += buttonWidth;

                if (Widgets.ButtonTextSubtle(new Rect(bx, y + margin, buttonWidth, buttonHeight), "FCMilitaryTableButtonCreateFireSupport".Translate()))
                    OpenMilitaryWindow(new FireSupportWindow(militaryUtil), "FCMilitaryTableButtonCreateFireSupport".Translate());
            }

            y += buttonHeight + margin * 2;

            // --- Settlements Table ---
            float tableH = rect.yMax - y - margin;
            if (tableH > 0f)
                DrawMilitarySettlementsTable(new Rect(x + margin, y, width - (margin*2), tableH));
        }

        private void DrawMilitarySettlementsTable(Rect tableRect)
        {
            const float headerH = 45f;
            const float rowH = 25f;
            bool changedColor = false;

            // Build list of settlements with military comps
            List<WorldSettlementFC> settlements = faction.settlements.Where(s => s.MilitaryComp != null).ToList();

            float contentH = settlements.Count * rowH;
            Rect viewRect = new Rect(tableRect.x, tableRect.y + headerH, tableRect.width, tableRect.height - headerH);
            float scrollMargin = contentH > viewRect.height ? 16f : 0;
            Rect scrollRect = new Rect(0f, 0f, tableRect.width - scrollMargin, Mathf.Max(contentH, viewRect.height));

            // Column widths
            float milLvW = 60f;
            float maxCostW = 90f;
            float squadW = 150f;
            float availW = 90f;
            float underAttackW = 90f;
            float setSquadW = 90f;
            float deployW = 90f;
            float resetW = 90f;
            float fireSupW = 90f;
            float nameW = tableRect.width - milLvW - maxCostW - squadW - availW - underAttackW - setSquadW - deployW - resetW - fireSupW - scrollMargin;
            float[] colWidths = { nameW, milLvW, maxCostW, squadW, availW, underAttackW, setSquadW, deployW, resetW, fireSupW };
            string[] colLabels =
            {
                "FCSettlementTableName".Translate(),
                "FCSettlementTableMilLevel".Translate(),
                "FCMilitaryTableMilitaryBudget".Translate(),
                "FCMilitaryTableSquad".Translate(),
                "FCMilitaryTableAvailable".Translate(),
                "FCMilitaryTableUnderAttack".Translate(),
                "FCMilitaryTableSetSquad".Translate(),
                "FCMilitaryTableDeploySquad".Translate(),
                "FCMilitaryTableResetSquad".Translate(),
                "FCMilitaryTableFireSupport".Translate()
            };

            // --- Header ---
            Rect headerRow = new Rect(tableRect.x, tableRect.y, tableRect.width, headerH);
            Widgets.DrawMenuSection(headerRow);
            Widgets.DrawLightHighlight(headerRow);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            float hx = tableRect.x;
            for (int c = 0; c < colWidths.Length; c++)
            {
                Widgets.Label(new Rect(hx, tableRect.y, colWidths[c], headerH), colLabels[c]);
                hx += colWidths[c];
            }

            // --- Rows ---
            Widgets.BeginScrollView(viewRect, ref militaryScroll, scrollRect);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < settlements.Count; i++)
            {
                WorldSettlementFC settlement = settlements[i];
                WorldObjectComp_SettlementMilitary milComp = settlement.MilitaryComp;
                float ry = i * rowH;

                if (i % 2 == 0)
                    Widgets.DrawHighlight(new Rect(0f, ry, scrollRect.width, rowH));

                float sx = 0f;

                // Name
                Text.Anchor = TextAnchor.MiddleLeft;
                if (Widgets.ButtonTextSubtle(new Rect(sx, ry, colWidths[0], rowH), settlement.Name))
                    Find.WindowStack.Add(new SettlementWindowFc(settlement));
                sx += colWidths[0];

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;

                // Mil Level
                Widgets.Label(new Rect(sx, ry, colWidths[1], rowH), settlement.settlementMilitaryLevel.ToString());
                sx += colWidths[1];

                // Max Cost
                Widgets.Label(new Rect(sx, ry, colWidths[2], rowH), "$" + MilitaryCustomizationUtil.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel).ToString());
                sx += colWidths[2];

                // Squad
                string squadName = milComp.militarySquad?.outfit?.name ?? "None".Translate();
                Widgets.Label(new Rect(sx, ry, colWidths[3], rowH), squadName);
                sx += colWidths[3];

                // Available
                string availText = milComp.isMilitaryBusySilent() ? "No".Translate() : "Yes".Translate();
                Widgets.Label(new Rect(sx, ry, colWidths[4], rowH), availText);
                sx += colWidths[4];

                // Under attack
                string underAttack = milComp.isUnderAttack ? "Yes".Translate().Colorize(Color.red) : "No".Translate().Colorize(Color.white);
                Widgets.Label(new Rect(sx, ry, colWidths[5], rowH), underAttack);
                sx += colWidths[5];

                // Set Squad button
                if ((militaryUtil.squads?.Count ?? 0) == 0)
                {
                    GUI.color = Color.gray;
                    changedColor = true;
                }
                if (Widgets.ButtonText(new Rect(sx, ry, colWidths[5], rowH), "Set".Translate()))
                {
                    if (militaryUtil.squads == null) militaryUtil.resetSquads();

                    List<FloatMenuOption> squads = new List<FloatMenuOption>();
                    squads.AddRange(militaryUtil.squads.Select(squad => new FloatMenuOption(
                        squad.name + " - " + "Cost".Translate() + ": " + squad.GetEquipmentTotalCost(),
                        delegate { militaryUtil.attemptToAssignSquad(settlement, squad); })));

                    if (!squads.Any())
                        squads.Add(new FloatMenuOption("FCNoSquadAvailable".Translate(), null));

                    Find.WindowStack.Add(new Searchable_FloatMenu(squads));
                }
                if (changedColor)
                {
                    GUI.color = Color.white;
                    changedColor = false;
                }
                sx += colWidths[6];

                // Deploy button
                if (milComp.militarySquad?.outfit?.name is null)
                {
                    GUI.color = Color.gray;
                    changedColor = true;
                }
                if (Widgets.ButtonText(new Rect(sx, ry, colWidths[7], rowH), "Deploy".Translate()))
                {
                    HandleDeployClick(settlement, milComp);
                }
                if (changedColor)
                {
                    GUI.color = Color.white;
                    changedColor = false;
                }
                sx += colWidths[7];

                // Reset button
                if (milComp.militarySquad?.outfit?.name is null)
                {
                    GUI.color = Color.gray;
                    changedColor = true;
                }
                if (Widgets.ButtonText(new Rect(sx, ry, colWidths[8], rowH), "Reset".Translate()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("FCMilTableConfirm".Translate(), delegate
                        {
                            if (milComp.militarySquad != null)
                            {
                                Messages.Message("FCResetSquadPawns".Translate(), MessageTypeDefOf.NeutralEvent);
                                milComp.militarySquad.initiateSquad();
                            }
                            else
                            {
                                Messages.Message("FCResetSquadRejected".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        })
                    };
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                if (changedColor)
                {
                    GUI.color = Color.white;
                    changedColor = false;
                }
                sx += colWidths[8];

                // Fire Support button
                if (militaryUtil.fireSupportDefs.Count == 0 || settlement.BuildingsComp?.hasBuilding(BuildingFCDefOf.artilleryOutpost) == false)
                {
                    GUI.color = Color.gray;
                    changedColor = true;
                }
                if (Widgets.ButtonText(new Rect(sx, ry, colWidths[9], rowH), "FCMilitaryTableFireSupport".Translate()))
                {
                    HandleFireSupportClick(settlement, milComp);
                }
                if (changedColor)
                {
                    GUI.color = Color.white;
                    changedColor = false;
                }
            }

            Widgets.EndScrollView();
        }

        private void HandleDeployClick(WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp)
        {
            if (!milComp.isMilitaryBusy(true) && milComp.isMilitarySquadValid())
            {
                Find.WindowStack.Add(new FloatMenu(DeploymentOptions(settlement)));
            }
            else if (milComp.isMilitaryBusy(true) && milComp.isMilitarySquadValid() && faction.hasPolicy(FCPolicyDefOf.militaristic))
            {
                if ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) <= Find.TickManager.TicksGame)
                {
                    int cost = (int)Math.Round(milComp.militarySquad.outfit.updateEquipmentTotalCost() * .2);
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("FCDeploySecondarySquad".Translate(cost), delegate
                        {
                            if (PaymentUtil.getSilver() >= cost)
                            {
                                List<FloatMenuOption> deploymentOptions = new List<FloatMenuOption>
                                {
                                    new FloatMenuOption("walkIntoMapDeploymentOption".Translate(), delegate
                                    {
                                        MilitaryUtil.CallinExtraForces(settlement, false);
                                        Find.WindowStack.currentlyDrawnWindow.Close();
                                    })
                                };

                                if (!FCSettings.medievalTechOnly &&
                                    (FactionCache.TechTransportPods?.IsFinished ?? false))
                                {
                                    deploymentOptions.Add(new FloatMenuOption("dropPodDeploymentOption".Translate(), delegate
                                    {
                                        MilitaryUtil.CallinExtraForces(settlement, true);
                                        Find.WindowStack.currentlyDrawnWindow.Close();
                                    }));
                                }

                                Find.WindowStack.Add(new FloatMenu(deploymentOptions));
                            }
                            else
                            {
                                Messages.Message("NotEnoughSilverToDeploySquad".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        })
                    };
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
                milComp.isMilitaryBusy();
            }
        }

        private void HandleFireSupportClick(WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            foreach (MilitaryFireSupport support in militaryUtil.fireSupportDefs)
            {
                if (support.projectiles == null || support.projectiles.Count == 0)
                    continue;

                float cost = support.returnTotalCost();
                list.Add(new FloatMenuOption(support.name + " - $" + cost, delegate
                {
                    if (support.returnTotalCost() <=
                        MilitaryCustomizationUtil.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel))
                    {
                        if (settlement.BuildingsComp?.hasBuilding(BuildingFCDefOf.artilleryOutpost) == true)
                        {
                            if (milComp.artilleryTimer <= Find.TickManager.TicksGame)
                            {
                                if (PaymentUtil.getSilver() >= cost)
                                {
                                    MilitaryUtil.FireSupport(settlement, support);
                                }
                                else
                                {
                                    Messages.Message("FCNotEnoughSilverFireSupport".Translate(),
                                        MessageTypeDefOf.RejectInput);
                                }
                            }
                            else
                            {
                                Messages.Message("FCFireSupportCooldown".Translate(
                                    (milComp.artilleryTimer - Find.TickManager.TicksGame).ToStringTicksToDays()),
                                    MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            Messages.Message("FCRequiresArtilleryOutpost".Translate(),
                                MessageTypeDefOf.RejectInput);
                        }
                        Find.WindowStack.TryRemove(this);
                    }
                    else
                    {
                        Messages.Message("FCRequiresHigherMilLevel".Translate(),
                            MessageTypeDefOf.RejectInput);
                    }
                }));
            }

            if (!list.Any())
                list.Add(new FloatMenuOption("FCNoFireSupportsMade".Translate(), delegate { }));

            Find.WindowStack.Add(new Searchable_FloatMenu(list));
        }

        private List<FloatMenuOption> DeploymentOptions(WorldSettlementFC settlement) => new List<FloatMenuOption>
        {
            new FloatMenuOption("walkIntoMapDeploymentOption".Translate(), delegate
            {
                MilitaryUtil.CallinAlliedForces(settlement, false);
            }),
            DropPodDeploymentOption(settlement)
        };

        private FloatMenuOption DropPodDeploymentOption(WorldSettlementFC settlement)
        {
            bool medievalOnly = FCSettings.medievalTechOnly;
            if (!medievalOnly && (FactionCache.TechTransportPods?.IsFinished ?? false))
            {
                return new FloatMenuOption("dropPodDeploymentOption".Translate(),
                    delegate { MilitaryUtil.CallinAlliedForces(settlement, true); });
            }

            return new FloatMenuOption(
                "dropPodDeploymentOption".Translate() + (medievalOnly
                    ? "dropPodDeploymentOptionUnavailableReasonMedieval".Translate()
                    : "dropPodDeploymentOptionUnavailableReasonTech".Translate(
                        FactionCache.TechTransportPods?.label ??
                        "errorDropPodResearchCouldNotBeFound".Translate())), null);
        }

        private void OpenMilitaryWindow(MilitaryWindow content, string title)
        {
            Window toRemove = Find.WindowStack.Windows.FirstOrDefault(
                w => w is FCWindow_Military existing &&
                     existing.GetMilitaryWindow().GetType() == content.GetType());

            if (toRemove != null)
            {
                toRemove.Close();
            }

            Find.WindowStack.Add(new FCWindow_Military(content, title));
        }

    }
}
