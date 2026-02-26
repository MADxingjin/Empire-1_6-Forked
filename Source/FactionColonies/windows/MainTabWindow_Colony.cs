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
        private enum EmpireTab { Overview, Bills, Events }
        private EmpireTab curTab = EmpireTab.Overview;
        private List<TabRecord> tabs = new List<TabRecord>();

        // ===== WINDOW SIZE =====
        public override Vector2 InitialSize => new Vector2(1110f, 640f);

        // ===== DATA =====
        public bool selectingColonyFC;
        public FactionFC faction;

        // ===== UPDATE TIMER =====
        private int UIUpdateTimer;

        // ===== SCROLL POSITIONS =====
        private Vector2 settlementScroll;
        private Vector2 billsScroll;
        private Vector2 eventsScroll;

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
        }

        public override void PostClose()
        {
            base.PostClose();
            selectingColonyFC = false;
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
                if (Widgets.ButtonText(btn, "Create New Faction"))
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
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        // ===== OVERVIEW TAB =====

        private void DrawOverviewTab(Rect rect)
        {
            const float leftWidth = 175f;//250f;
            const float panelGap = 10f;
            const float headerHeight = 63f;

            Rect headerPanel = new Rect(rect.x + margin, rect.y+margin, rect.width - (margin*2), headerHeight);
            Rect leftPanel  = new Rect(rect.x + margin,                        headerPanel.yMax + panelGap, leftWidth,                         rect.height - panelGap - headerHeight - margin);
            Rect rightPanel = new Rect(leftPanel.xMax + panelGap, headerPanel.yMax + panelGap, rect.xMax - leftPanel.xMax - panelGap - margin, rect.height - panelGap - headerHeight - margin);

            DrawOverviewHeaderPanel(headerPanel);
            DrawOverviewLeftPanel(leftPanel);
            DrawOverviewRightPanel(rightPanel);

            Widgets.DrawLineVertical(leftPanel.xMax + (panelGap / 2), leftPanel.y, leftPanel.height - (margin));
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
            List<FCPolicyDef> available = AvailableTraitsList();

            for (int slot = 0; slot < 5; slot++)
            {
                int capturedSlot = slot;
                FCPolicy current = faction.factionTraits[slot];
                Rect traitRect = new Rect(panel.x, y, panel.width, traitH);
                Rect labelRect = new Rect(traitRect.x + (bigMargin * 2), y, traitRect.width - (bigMargin * 2), traitRect.height);
                Color labelcolor = SlotLocked(slot + 1) ? Color.gray : Color.white;
                bool highlightLabel = CanChangeTrait(slot + 1, current);

                if (SlotLocked(slot + 1))
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.DrawHighlight(traitRect);
                    Widgets.Label(labelRect, ReturnTraitAvailability(slot + 1, current).Colorize(Color.gray));
                }
                else
                {
                    if (Widgets.ButtonTextSubtle(traitRect, ReturnTraitAvailability(slot + 1, current), labelColor: labelcolor, highlight: highlightLabel))
                    {
                        if (CanChangeTrait(slot + 1, current))
                        {
                            List<FloatMenuOption> list = new List<FloatMenuOption>();
                            foreach (FCPolicyDef trait in available)
                            {
                                FCPolicyDef capturedTrait = trait;
                                list.Add(new FloatMenuOption(trait.label,
                                    delegate
                                    {
                                        List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                        confirm.Add(new FloatMenuOption(
                                            "FCConfirmTrait".Translate(capturedTrait.label),
                                            delegate { faction.factionTraits[capturedSlot] = new FCPolicy(capturedTrait); }));
                                        Find.WindowStack.Add(new FloatMenu(confirm));
                                    },
                                    mouseoverGuiAction: delegate
                                    {
                                        TooltipHandler.TipRegion(
                                            new Rect(Event.current.mousePosition, new Vector2(200f, 200f)),
                                            capturedTrait.PolicyText());
                                    }));
                            }
                            Find.WindowStack.Add(new FloatMenu(list));
                        }
                    }
                }

                if (Mouse.IsOver(traitRect) && current.def != FCPolicyDefOf.empty)
                {
                    TooltipHandler.TipRegion(
                        new Rect(Event.current.mousePosition, new Vector2(200f, 200f)),
                        current.def.PolicyText());
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

        private void DrawOverviewRightPanel(Rect panel)
        {
            const float pad  = 6f;
            const float btnH = 30f;
            const float btnW = 130f;
            const float btnGap = 5f;

            float x = panel.x;
            float y = panel.y + pad;
            float w = panel.width;

            // --- Action Buttons ---
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            if (Widgets.ButtonText(new Rect(x, y, btnW, btnH), "Military".Translate()))
            {
                if (FactionCache.PlayerColonyFaction == null)
                    Messages.Message(new Message("NoFactionForMilitary".Translate(), MessageTypeDefOf.RejectInput));
                else
                    Find.WindowStack.Add(new MilitaryCustomizationWindowFc());
            }

            if (Widgets.ButtonText(new Rect(x + btnW + btnGap, y, btnW, btnH), "Actions".Translate()))
                DrawActionsFloatMenu();

            float createW = 160f;
            if (Widgets.ButtonText(new Rect(x + w - createW, y, createW, btnH), "CreateNewColony".Translate()))
            {
                Find.WindowStack.Add(new CreateColonyWindowFc());
                Find.World.renderer.wantedMode = WorldRenderMode.Planet;
                Messages.Message("SelectTile".Translate(), MessageTypeDefOf.NegativeEvent);
            }

            y += btnH + pad;

            // --- Economic Stats ---
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            float halfW = w / 2f;
            Widgets.Label(new Rect(x, y, halfW, 22f),
                "EstimatedProfit".Translate() + ": " + Convert.ToInt32(faction.profit) + " " + "Silver".Translate().ToLower());
            Widgets.Label(new Rect(x + halfW, y, halfW, 22f),
                "TimeTillTax".Translate() + ": " + Math.Max(0, faction.taxTimeDue - Find.TickManager.TicksGame).ToTimeString());
            y += 24f + pad;

            // --- Resource Production ---
            Text.Font   = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, w, 22f), "TotalProduction".Translate());
            y += 22f;

            float rSz  = 32f;
            float rGap = 5f;
            int rPerRow = Mathf.Max(1, (int)Math.Floor(w / (rSz + rGap)));
            int ri = 0;

            Text.Font   = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (ResourceDisplay resource in faction.FactionResources)
            {
                int col = ri % rPerRow;
                int row = ri / rPerRow;
                float rx = x + col * (rSz + rGap);
                float ry = y + row * (rSz + 14f);

                if (Widgets.ButtonImage(new Rect(rx, ry, rSz, rSz), resource.Icon))
                    Find.WindowStack.Add(new DescWindowFc("TotalFactionProduction".Translate() + ": " + resource.label, resource.label));

                Widgets.Label(new Rect(rx, ry + rSz, rSz, 14f), resource.amount.ToString());
                ri++;
            }

            int resourceRows = (ri == 0) ? 0 : (int)Math.Ceiling((double)ri / rPerRow);
            y += resourceRows * (rSz + 14f) + pad;

            // --- Settlements Table ---
            float tableH = panel.yMax - y - pad;
            if (tableH > 0f)
                DrawSettlementsTable(new Rect(x, y, w, tableH));
        }

        private void DrawSettlementsTable(Rect tableRect)
        {
            float[] colWidths = { 140f, 50f, 60f, 60f, 75f, 65f, 60f, 55f };
            string[] colLabels = { "Name", "Level", "Mil Level", "Profit", "Free Workers", "Happiness", "Loyalty", "Unrest" };
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
            };

            const float headerH = 25f;
            const float rowH    = 25f;

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

            // Scrollable rows
            float contentH = faction.settlements.Count * rowH;
            Rect viewRect  = new Rect(tableRect.x, tableRect.y + headerH, tableRect.width, tableRect.height - headerH);
            Rect scrollRect = new Rect(0f, 0f, tableRect.width - 16f, Mathf.Max(contentH, viewRect.height));

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

                if (Widgets.ButtonTextSubtle(new Rect(sx, ry, colWidths[0], rowH), s.Name))
                    Find.WindowStack.Add(new SettlementWindowFc(s));
                sx += colWidths[0];

                Widgets.Label(new Rect(sx, ry, colWidths[1], rowH), s.settlementLevel.ToString());         sx += colWidths[1];
                Widgets.Label(new Rect(sx, ry, colWidths[2], rowH), s.settlementMilitaryLevel.ToString()); sx += colWidths[2];
                Widgets.Label(new Rect(sx, ry, colWidths[3], rowH), ((int)s.getTotalProfit()).ToString()); sx += colWidths[3];
                Widgets.Label(new Rect(sx, ry, colWidths[4], rowH), (s.workersUltraMax - s.getTotalWorkers()).ToString()); sx += colWidths[4];
                Widgets.Label(new Rect(sx, ry, colWidths[5], rowH), ((int)s.Happiness).ToString()); sx += colWidths[5];
                Widgets.Label(new Rect(sx, ry, colWidths[6], rowH), ((int)s.Loyalty).ToString());  sx += colWidths[6];
                Widgets.Label(new Rect(sx, ry, colWidths[7], rowH), ((int)s.Unrest).ToString());
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
            if (Widgets.ButtonTextSubtle(autoBtn, "Auto-Resolve"))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("Auto-Resolving : " + faction.autoResolveBills, delegate
                {
                    faction.autoResolveBills = !faction.autoResolveBills;
                    if (faction.autoResolveBills)
                    {
                        Messages.Message("Bills are now autoresolving!", MessageTypeDefOf.NeutralEvent);
                        PaymentUtil.autoresolveBills(bills);
                    }
                    else
                    {
                        Messages.Message("Bills are now not autoresolving.", MessageTypeDefOf.NeutralEvent);
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

                if (Widgets.ButtonText(new Rect(rx, ry, cDesc, rowH), "Desc"))
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
                            list.Add(new FloatMenuOption("Null", null));
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

        // ===== HELPERS (from FCWindow_Overview) =====

        private string ReturnPolicyText(FCPolicyDef def)
        {
            string str = def.LabelCap + "\n";
            foreach (string pos in def.positiveEffects)
                str += "\n" + pos;
            str += "\n==========";
            foreach (string neg in def.negativeEffects)
                str += "\n" + neg;
            return str;
        }

        private string ReturnTraitAvailability(int slot, FCPolicy current)
        {
            if (current.def != FCPolicyDefOf.empty)
                return current.def.label;
            if (faction.factionLevel >= slot)
                return "FCSelectANewTrait".Translate();
            return "FCTraitLockedUntilLevel".Translate(slot);
        }

        private bool CanChangeTrait(int slot, FCPolicy current)
        {
            if (current.def != FCPolicyDefOf.empty)
                return false;
            return faction.factionLevel >= slot;
        }
        private bool SlotLocked(int slot)
        {
            return faction.factionLevel < slot;
        }

        private List<FCPolicyDef> AvailableTraitsList()
        {
            List<FCPolicyDef> list = new List<FCPolicyDef>();
            if (!faction.hasTrait(FCPolicyDefOf.resilient))      list.Add(FCPolicyDefOf.resilient);
            if (!faction.hasTrait(FCPolicyDefOf.raiders))        list.Add(FCPolicyDefOf.raiders);
            if (!faction.hasTrait(FCPolicyDefOf.defenseInDepth)) list.Add(FCPolicyDefOf.defenseInDepth);
            if (!faction.hasTrait(FCPolicyDefOf.industrious))    list.Add(FCPolicyDefOf.industrious);
            if (!faction.hasTrait(FCPolicyDefOf.roadBuilders))   list.Add(FCPolicyDefOf.roadBuilders);
            if (!faction.hasTrait(FCPolicyDefOf.mercantile))     list.Add(FCPolicyDefOf.mercantile);
            if (!faction.hasTrait(FCPolicyDefOf.innovative))     list.Add(FCPolicyDefOf.innovative);
            return list;
        }

        private void DrawActionsFloatMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            IEnumerable<FloatMenuOption> resourcePoolOptions = faction.GetFactionMenuResourcePoolFloatMenuOptions();
            if (resourcePoolOptions != null)
                foreach (FloatMenuOption option in resourcePoolOptions)
                    list.Add(option);

            list.Add(new FloatMenuOption("FCOpenPatchNotes".Translate(), () => DebugActionsMisc.PatchNotesDisplayWindow()));

            if (faction.hasPolicy(FCPolicyDefOf.technocratic))
            {
                list.Add(new FloatMenuOption("FCSendResearchItems".Translate(), delegate
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
                }));
            }

            if (faction.hasPolicy(FCPolicyDefOf.feudal))
            {
                list.Add(new FloatMenuOption("FCRequestMercenary".Translate(), delegate
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
                            target                          = Find.CurrentMap,
                            faction                         = FactionCache.PlayerColonyFaction,
                            points                          = 999,
                            raidArrivalModeForQuickMilitaryAid = true,
                            raidNeverFleeIndividual         = true,
                            raidArrivalMode                 = PawnsArrivalModeDefOf.CenterDrop,
                            raidStrategy                    = RaidStrategyDefOf.ImmediateAttackFriendly
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
                }));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }
    }
}
