using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static UnityEngine.GridBrushBase;

namespace FactionColonies
{
    public class CreateColonyWindowFc : Window
    {
        public sealed override Vector2 InitialSize => new Vector2(300f, 600f);

        public PlanetTile currentTileSelected = -1;
        public BiomeResourceDef currentBiomeSelected;
        public bool traitExpansionistReducedFee;
        public int timeToTravel = -1;

        public WorldSettlementDef currentSettlementType = WorldSettlementDefOf.WorldSettlementDefBase;

        private int settlementCreationCost = 0;
        private readonly FactionFC faction = null;

        private int SettlementCreationBaseCost => (int)(TraitUtilsFC.cycleTraits("createSettlementMultiplier", faction.Traits, Operation.Multiplication) *
                                                        (currentSettlementType.GetModExtension<SettlementTypeExtension>().getCreationCost() + (TraitUtilsFC.cycleTraits("createSettlementBaseCost", faction.Traits, Operation.Addition))));

        public CreateColonyWindowFc()
        {
            forcePause = false;
            draggable = false;
            preventCameraMotion = false;
            doCloseX = true;
            windowRect = new Rect(UI.screenWidth - InitialSize.x, (UI.screenHeight - InitialSize.y) / 2f - (UI.screenHeight/8f), InitialSize.x, InitialSize.y);
            faction = Find.World.GetComponent<FactionFC>();
        }



        //Pre-Opening
        public override void PreOpen()
        {

        }

        //Drawing
        public override void DoWindowContents(Rect inRect)
        {
            faction.roadBuilder.DrawPaths();

            GetTileData();

            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            CalculateSettlementCreationCost();
            
            //Draw Label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, 268, 40), "SettleANewColony".Translate());

            //hori line
            Widgets.DrawLineHorizontal(0, 40, 300);


            //Upper menu
            Widgets.DrawMenuSection(new Rect(5, 45, 258, 220));

            DrawLabelBox(new Rect(10, 50, 100, 100), (currentSettlementType.isConstructed ? "ConstructionTime".Translate() : "TravelTime".Translate()), timeToTravel.ToTimeString());
            DrawLabelBox(new Rect(153, 50, 100, 100), "InitialCost".Translate(), settlementCreationCost + " " + "Silver".Translate());


            //Lower Menu label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 270, 268, 40), "BaseProductionStats".Translate());


            //Lower menu
            Widgets.DrawMenuSection(new Rect(5, 310, 258, 220));


            //Draw production
            DrawProduction();
            DrawChooseSettlementTypeButton();
            DrawCreateSettlementButton();

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }


        private void GetTileData()
        {
            PlanetTile selectedTile = Find.WorldSelector.SelectedTile;
            if (selectedTile.Valid && selectedTile.tileId != currentTileSelected)
            {
                currentTileSelected = selectedTile.tileId;
            }
            else /* If a WorldObject is selected, then get the tile underneath it. */
            {
                WorldObject obj = Find.WorldSelector.SingleSelectedObject;
                if (obj != null && obj.Tile != null)
                {
                    currentTileSelected = obj.Tile;
                }
            }
            if (currentTileSelected == PlanetTile.Invalid)
            {
                return;
            }

            if (currentSettlementType.biomeResourceOverride != null)
            {
                currentBiomeSelected = currentSettlementType.biomeResourceOverride;
                //default biome
                if (!DefDatabase<BiomeResourceDef>.AllDefs.Contains(currentBiomeSelected))
                {
                    LogUtil.Error($"Settlement type {currentSettlementType.LabelCap} has an invalid override biome. Using default biome.");
                    currentBiomeSelected = BiomeResourceDefOf.defaultBiome;
                }
            }
            else
            {
                currentBiomeSelected = DefDatabase<BiomeResourceDef>.GetNamed(currentTileSelected.Tile.PrimaryBiome.defName, false);
                //default biome
                if (currentBiomeSelected == default(BiomeResourceDef))
                {
                    LogUtil.Warning($"Selected tile has biome {currentTileSelected.Tile.PrimaryBiome.LabelCap}, which is not defined for Empire settlements. Using default biome.");
                    currentBiomeSelected = BiomeResourceDefOf.defaultBiome;
                }
            }

            if (CanCreateSettlementHere(true))
            {
                currentTileSelected = currentSettlementType.getTileForSettlement(currentTileSelected);
                timeToTravel = currentSettlementType.getCreationTime(currentTileSelected);
            }
            else
            {
                timeToTravel = 0;
            }
        }

        private IEnumerable<FloatMenuOption> GetAvailableSettlementTypes()
        {
            var tiers = new List<WorldSettlementDef>();

            foreach (WorldSettlementDef settlementDef in DefDatabase<WorldSettlementDef>.AllDefs)
            {
                if (settlementDef.isUnlocked())
                {
                    yield return new FloatMenuOption(settlementDef.LabelCap, delegate
                    {
                        currentSettlementType = settlementDef;
                    });
                }
            }
        }

        private void CalculateSettlementCreationCost()
        {
            settlementCreationCost = SettlementCreationBaseCost;

            if (faction.hasPolicy(FCPolicyDefOf.isolationist)) settlementCreationCost *= 2;

            if (!faction.hasPolicy(FCPolicyDefOf.expansionist)) return;

            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
            {
                traitExpansionistReducedFee = false;
                settlementCreationCost = 0;
                return;
            }

            if (faction.traitExpansionistTickLastUsedSettlementFeeReduction == -1 || (faction.traitExpansionistBoolCanUseSettlementFeeReduction))
            {
                traitExpansionistReducedFee = true;
                settlementCreationCost /= 2;
                return;
            }

            traitExpansionistReducedFee = false;
        }
        
        private void DrawProduction()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //Production headers
            Widgets.Label(new Rect(40, 310, 60, 25), "Base".Translate());
            Widgets.Label(new Rect(110, 310, 60, 25), "Modifier".Translate());
            Widgets.Label(new Rect(180, 310, 60, 25), "Final".Translate());

            if (currentTileSelected != -1)
            {
                List<ResourceFC> resTypes = faction.FactionResources;
                List<ResourceTypeDef> biomeResourceTypes = currentBiomeSelected.getBiomeResourceTypes();
                List<ResourceTypeDef> settlementResourceTypes = currentSettlementType.getResourceDefs();

                for (int i = 0; i < resTypes.Count; i++)
                {
                    ResourceTypeDef titheType = resTypes[i].def;
                    int baseHeight = 15;
                    if (Widgets.ButtonImage(new Rect(20, 335 + i * (5 + baseHeight), baseHeight, baseHeight), resTypes[i].getIcon))
                    {
                        string label = resTypes[i].label;
                        Find.WindowStack.Add(new DescWindowFc("SettlementProductionOf".Translate() + ": " + label, label.CapitalizeFirst()));
                    }
                    ResourceBonuses biomeRes = currentBiomeSelected.getBiomeResource(titheType);
                    ResourceBonuses settleRes = currentSettlementType.getSettlementResource(titheType);

                    float xMod = 70f;
                    Rect baseRect = new Rect(40, 335 + i * (5 + baseHeight), 60, baseHeight + 2);

                    if (biomeRes == null || settleRes == null || !titheType.ResourceTypeAllowedByTech(faction.techLevel))
                    {
                        /* One of the following is true:
                         *  1. The biome does not support this resource type
                         *  2. The settlement type does not support this resource type
                         *  3. The resource type's research requirements have not been met
                         * So show it as producing nothing.
                         */
                        TaggedString na = "N/A".ApplyTag(TagType.Gray);
                        Widgets.Label(baseRect, na);
                        Widgets.Label(baseRect.CopyAndShift(xMod, 0f), na);
                        Widgets.Label(baseRect.CopyAndShift(xMod * 2f, 0f), na);
                    }
                    else
                    {
                        double baseProduction = biomeRes.additive + settleRes.additive + titheType.getExtensionAdditives(currentTileSelected);
                        double baseMultiplier = Math.Round(biomeRes.multiplier * settleRes.multiplier * titheType.getExtensionMultipliers(currentTileSelected),2);
                        double total = Math.Round(baseProduction * baseMultiplier, 2);

                        Widgets.Label(baseRect, (baseProduction).ToString());
                        Widgets.Label(baseRect.CopyAndShift(xMod, 0f), (baseMultiplier).ToString());
                        Widgets.Label(baseRect.CopyAndShift(xMod * 2f, 0f), (total).ToString());
                    }
                }
            }
        }

        private void DrawCreateSettlementButton()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            int buttonLength = 130;
            if (Widgets.ButtonText(new Rect((InitialSize.x - 32 - buttonLength) / 2f, 535, buttonLength, 32), "Settle".Translate() + ": (" + settlementCreationCost + ")")) //add inital cost
            {
                if (!CanCreateSettlementHere()) return;

                PaymentUtil.paySilver(settlementCreationCost);

                //create settle event
                FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.settleNewColony);
                evt.location = currentTileSelected;
                evt.timeTillTrigger = Find.TickManager.TicksGame + timeToTravel;
                evt.source = faction.capitalLocation;
                if (currentSettlementType.isConstructed)
                {
                    evt.customDescription = "ColonyConstruction".Translate(currentSettlementType.LabelCap);
                }
                faction.addEvent(evt);

                faction.settlementCaravansList.Add(evt.location.ToString());
                Messages.Message((currentSettlementType.isConstructed ? "ConstructionToLocation".Translate() : "CaravanSentToLocation".Translate()) + " " +
                                 (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString() + "!", MessageTypeDefOf.PositiveEvent);

                DoPostEventCreationTraitThings();
            }
        }
        private void DrawChooseSettlementTypeButton()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            int buttonLength = 130;
            if (Widgets.ButtonText(new Rect((InitialSize.x - 32 - buttonLength) / 2f, 535, buttonLength, 32), currentSettlementType.LabelCap))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                IEnumerable<FloatMenuOption> options = GetAvailableSettlementTypes();
                if (options != null)
                {
                    foreach (FloatMenuOption option in options)
                    {
                        list.Add(option);
                    }
                }
                FloatMenu menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);
            }
        }

        private bool CanCreateSettlementHere(bool silent = false)
        {
            StringBuilder reason = new StringBuilder();
            if (!WorldTileChecker.IsValidTileForNewSettlement(currentTileSelected, currentSettlementType, reason) || faction.checkSettlementCaravansList(currentTileSelected.ToString()) || !PlayerHasEnoughSilver(reason))
            {
                if (!silent)
                {
                    Messages.Message(reason.ToString(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }

        private void DoPostEventCreationTraitThings()
        {
            if (traitExpansionistReducedFee)
            {
                faction.traitExpansionistTickLastUsedSettlementFeeReduction = Find.TickManager.TicksGame;
                faction.traitExpansionistBoolCanUseSettlementFeeReduction = false;
            }
        }

        private bool PlayerHasEnoughSilver(StringBuilder reason)
        {
            if (PaymentUtil.getSilver() >= settlementCreationCost) return true;

            reason?.Append("NotEnoughSilverToSettle".Translate() + "!");
            return false;
        }

        public void DrawLabelBox(Rect rect, string text1, string text2)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            //Draw highlight
            Widgets.DrawHighlight(new Rect(rect.x, rect.y + rect.height /8, rect.width, rect.height / 4f));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, rect.height / 2f), text1);

            //divider
            Widgets.DrawLineHorizontal(rect.x + 5, rect.y + rect.height / 2, rect.width - 10);

            //Bottom Text - Gamers Rise Up
            Widgets.Label(new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2f), text2);
        }

        public enum OrbitalPlatformTier
        {
            None,
            Basic,
            Logistics,
            Advanced,
            Glitter
        }
    }
}