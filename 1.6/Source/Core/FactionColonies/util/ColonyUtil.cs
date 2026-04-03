using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace FactionColonies.util
{
    public static class ColonyUtil
    {
        public static WorldSettlementFC CreatePlayerColonySettlement(PlanetTile tile, WorldSettlementDef settlementType)
        {
            if (settlementType == null)
            {
                LogUtil.Error($"Tried to create a settlement with null WorldSettlementDef! Using default WorldSettlementDef.");
                settlementType = WorldSettlementDefOf.WorldSettlementDef_Surface;
            }

            /* Do any pre-settlement-creation demanded of the settlement type */
            settlementType.GetSettlementTypeExtension().PreCreation(ref tile, ref settlementType);

            LogUtil.Message($"Creating settlement of type {settlementType.defName}");
            Faction faction = FactionCache.PlayerColonyFaction;

            FactionFC worldcomp = FactionCache.FactionComp;
            if (!worldcomp.settlements.Any())
            {
                FactionCache.FactionComp.timeStart = Find.TickManager.TicksGame;
            }

            WorldSettlementFC settlement = (WorldSettlementFC)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldSettlementDef>.GetNamed(settlementType.defName));
            settlement.PostPostMake(tile);

            settlement.SetFaction(faction);
            Find.WorldObjects.Add(settlement);

            worldcomp.AddSettlement(settlement);
            worldcomp.roadBuilder.FlagUpdateRoadQueues();

            /* Do any post-settlement-creation demanded of the settlement type */
            settlementType.GetSettlementTypeExtension().PostCreation(settlement);

            LifecycleRegistry.InvokeOnSettlementCreated(settlement);

            Find.LetterStack.ReceiveLetter("FCSettlementFormed".Translate(),
                "SettleEventCompletedDesc".Translate(settlement.Name, settlementType.LabelCap, tile.Tile.PrimaryBiome.LabelCap),
                LetterDefOf.PositiveEvent);

            return settlement;
        }

        public static void RemovePlayerSettlement(WorldSettlementFC settlement)
        {
            settlement.settlementDef.GetSettlementTypeExtension()?.PreDestruction(settlement);
            settlement.PrepareDestroy();
            FactionFC faction = FactionCache.FactionComp;
            LifecycleRegistry.InvokeOnSettlementRemoved(settlement);
            faction.settlements.Remove(settlement);

            // Clean up any pending bills for the destroyed settlement
            for (int i = faction.Bills.Count - 1; i >= 0; i--)
            {
                if (faction.Bills[i].settlement == settlement)
                {
                    LogUtil.Message("RemovePlayerSettlement: removing orphaned bill (loadID=" + faction.Bills[i].loadID + ") for destroyed settlement " + settlement.Name);
                    faction.Bills.RemoveAt(i);
                }
            }

            faction.DirtyFactionProfitCache();
            faction.DirtyAveragesCache();
            faction.roadBuilder.FlagUpdateRoadQueues();
            Messages.Message("SettlementRemoved".Translate(settlement.Name), MessageTypeDefOf.NegativeEvent);

            Find.WorldObjects.Remove(Find.World.worldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed(settlement.def.defName), settlement.Tile));

            //clear military events
            settlement.MilitaryComp?.ReturnMilitary(false);

            HashSet<FCEvent> toRemove = new HashSet<FCEvent>();

            foreach (FCEvent evt in faction.events)
            {
                //military event removal
                if (evt.def == FCEventDefOf.captureEnemySettlement || evt.def == FCEventDefOf.raidEnemySettlement)
                {
                    if (evt.militaryForceAttacking.homeSettlement == settlement)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def == FCEventDefOf.settlementBeingAttacked)
                {
                    if (evt.militaryForceDefending.homeSettlement == settlement)
                    {
                        if (evt.settlementFCDefending == settlement)
                        {
                            toRemove.Add(evt);
                        }
                        else if (evt.settlementFCDefending is WorldSettlementFC targetSettlement)
                        {
                            //if not defending settlement, reset to target's own defense
                            MilitaryUtilFC.ChangeDefendingMilitaryForce(evt, targetSettlement);
                        }
                        else
                        {
                            // External raid target (outpost etc.) — defender removed, remove event
                            toRemove.Add(evt);
                            IRaidTarget raidTarget = RaidTargetRegistry.FindByWorldObject(evt.settlementFCDefending);
                            if (raidTarget != null) raidTarget.IsUnderAttack = false;
                        }
                    }
                    else
                    {
                        //if force belongs to other settlement
                        evt.militaryForceDefending.homeSettlement.MilitaryComp?.CooldownMilitaryFinal();

                        toRemove.Add(evt);
                    }
                }


                //settlement event removal
                if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy ||
                    evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
                {
                    if (evt.source == settlement.Tile)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def.isRandomEvent && evt.settlementTraitLocations.Count > 0)
                {
                    if (evt.settlementTraitLocations.Contains(settlement))
                    {
                        evt.settlementTraitLocations.Remove(settlement);
                        if (evt.settlementTraitLocations.Count == 0)
                        {
                            toRemove.Add(evt);
                        }
                    }
                }

                // Let extensions cancel their own custom events
                if (!toRemove.Contains(evt))
                {
                    FCEventHandlerExtension handler = evt.def.GetModExtension<FCEventHandlerExtension>();
                    if (handler != null && handler.ShouldCancelOnSettlementRemoval(evt, settlement))
                    {
                        toRemove.Add(evt);
                    }
                }
            }

            foreach (FCEvent evt in toRemove)
            {
                faction.events.Remove(evt);
            }
            if (toRemove.Count > 0)
            {
                faction.eventsVersion++;
                faction.InvalidateFactionStatCache();
            }
        }
        public static Faction CreatePlayerColonyFaction()
        {
            FactionFC worldcomp = FactionCache.FactionComp;
            if (worldcomp == null)
            {
                LogUtil.Error("FactionFC world component is missing! Cannot create player colony faction.");
                return null;
            }
            LogUtil.Message("Creating new player faction");
            worldcomp.SetCapital();

            FactionDef facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction
            {
                def = facDef
            };
            faction.def.techLevel = Faction.OfPlayer.def.techLevel;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
            faction.Name = "PlayerColony".Translate();
            faction.def.classicIdeo = Faction.OfPlayer.def.classicIdeo;
            faction.ideos = Faction.OfPlayer.ideos;

            worldcomp.DirtyTechLevelCache();
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }
            // Set starting goodwill to Player
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 200);

            // Generate Leader
            if (faction.leader == null || faction.leader.Dead)
            {
                CreatePlayerFactionLeader(faction);
            }

            Find.FactionManager.Add(faction);
            worldcomp.OnCreation();
            return faction;
        }

        public static bool CreatePlayerFactionLeader(Faction faction)
        {
            bool success = true;
            if (!faction.TryGenerateNewLeader())
            {
                LogUtil.Warning("TryGenerateNewLeader failed. Falling back to manual generation.");
                PawnKindDef fallbackKind = faction.RandomPawnKind();
                LogUtil.Message($"Fallback pawnkind: {fallbackKind?.defName ?? "null"}");
                faction.leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: fallbackKind,
                faction: faction, context: PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false, worldPawnFactionDoesntMatter: false));
                if (faction.leader == null)
                {
                    LogUtil.Warning("Fallback leader generation also failed!");
                    success = false;
                }
                else
                {
                    if (!Find.WorldPawns.Contains(faction.leader))
                    {
                        Find.WorldPawns.PassToWorld(faction.leader, PawnDiscardDecideMode.KeepForever);
                    }
                    LogUtil.Message($"Created leader {faction.leader.Name} ({faction.leader.ThingID}), " +
                                    $"pawnKind: {faction.leader.kindDef?.defName ?? "null"}, " +
                                    $"title: {faction.LeaderTitle}, " +
                                    $"ideo: {faction.leader.Ideo?.name ?? "none"}, " +
                                    $"faction: {faction.Name}");
                }
            }
            else
            {
                LogUtil.Message($"TryGenerateNewLeader succeeded. Leader: {faction.leader?.Name} ({faction.leader?.ThingID}), " +
                                $"pawnKind: {faction.leader?.kindDef?.defName ?? "null"}, " +
                                $"title: {faction.LeaderTitle}, " +
                                $"ideo: {faction.leader?.Ideo?.name ?? "none"}");
            }

            return success;
        }
    }
}
