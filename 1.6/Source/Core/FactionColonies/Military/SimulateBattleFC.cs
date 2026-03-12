using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class SimulateBattleFc
    {
        public static BattleResult FightBattle(militaryForce MFA, militaryForce MFB, IRandProvider rand = null)
        {
            var result = new BattleResult();
            try
            {
                BattleModifierRegistry.InvokeModifyForce(MFA, true);
                BattleModifierRegistry.InvokeModifyForce(MFB, false);

                // Defender advantage: defenders are inherently harder to dislodge
                MFB.forceRemaining = Math.Round(MFB.forceRemaining * FCSettings.defenderAdvantage);

                result.attackerInitialForce = MFA.forceRemaining;
                result.defenderInitialForce = MFB.forceRemaining;
                result.roundLog = new List<bool>();

                LogUtil.Message("SimulateBattleFc.FightBattle: Starting battle");
                while (MFA.forceRemaining > 0 && MFB.forceRemaining > 0)
                {
                    double prevDefender = MFB.forceRemaining;
                    FightRound(MFA, MFB, rand);
                    // If defender lost force this round, attacker won the round
                    result.roundLog.Add(MFB.forceRemaining < prevDefender);
                }

                result.attackerRemainingForce = MFA.forceRemaining;
                result.defenderRemainingForce = MFB.forceRemaining;
                result.totalRounds = result.roundLog.Count;

                if (MFA.forceRemaining <= 0)
                {
                    LogUtil.Message("SimulateBattleFc.FightBattle: Defending Force has won.");
                    result.winner = BattleWinner.Defender;
                }
                else
                {
                    LogUtil.Message("SimulateBattleFc.FightBattle: Attacking Force has won.");
                    result.winner = BattleWinner.Attacker;
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"An exception occurred while resolving combat in Empire {Environment.NewLine}[{e}]");
                result.winner = BattleWinner.Error;
            }

            return result;
        }

        public static void FightRound(militaryForce MFA, militaryForce MFB, IRandProvider rand = null)
        {
            rand = rand ?? new RimWorldRandProvider();
            var randA = (rand.Range(0, 20) * MFA.militaryEfficiency);
            var randB = (rand.Range(0, 20) * MFB.militaryEfficiency);
            // LogUtil.Message("A Begin: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B begin");
            // LogUtil.Message("A Rolled: " + randA.ToString() + " : " + randB.ToString() + " B rolled");

            if (randA > randB)
            {
                MFB.forceRemaining -= 1;
            }
            else
            {
                MFA.forceRemaining -= 1;
            }
            //LogUtil.Message("A Remain: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B remain");
        }
    }


    public class militaryForce : IExposable
    {
        public double militaryLevel;
        public double militaryEfficiency;
        public double forceRemaining;
        public int random;
        public WorldSettlementFC homeSettlement;
        public Faction homeFaction;

        /// <summary>forceRemaining with defender advantage applied (for display).</summary>
        public double DefensivePower => Math.Round(forceRemaining * FCSettings.defenderAdvantage);

        public void ExposeData()
        {
            Scribe_Values.Look(ref militaryLevel, "militaryLevel");
            Scribe_Values.Look(ref militaryEfficiency, "militaryEfficiency");
            Scribe_Values.Look(ref forceRemaining, "forceRemaining");
            Scribe_Values.Look(ref random, "random");
            Scribe_References.Look(ref homeSettlement, "homeSettlement");
            Scribe_References.Look(ref homeFaction, "homeFaction");
        }

        public militaryForce()
        {
        }

        public militaryForce(double militaryLevel, double militaryEfficiency, WorldSettlementFC homeSettlement, Faction homeFaction)
        {
            this.militaryLevel = militaryLevel;
            this.militaryEfficiency = militaryEfficiency;
            this.homeSettlement = homeSettlement;
            this.homeFaction = homeFaction;
            forceRemaining = Math.Max(1, Math.Round(militaryLevel * militaryEfficiency));
        }

        public static militaryForce CreateMilitaryForceFromSettlement(WorldSettlementFC settlement, bool isAttacking = false, militaryForce homeDefendingForce = null)
        {
            FactionFC faction = FactionCache.FactionComp;
            double homeForceLevel = 0;
            if (homeDefendingForce != null)
            {
                homeForceLevel = homeDefendingForce.militaryLevel;
            }

            double militaryLevel = settlement.settlementMilitaryLevel + homeForceLevel;
            double efficiency = settlement.GetStatValue(FCStatDefOf.militaryCombatEfficiency);
            if (isAttacking)
            {
                militaryLevel += faction.GetStatValue(FCStatDefOf.militaryLevelBonusAttacking);
                efficiency *= faction.GetStatValue(FCStatDefOf.militaryEfficiencyBonusAttacking);
            }
            else
            {
                militaryLevel += faction.GetStatValue(FCStatDefOf.militaryLevelBonusDefending);
                efficiency *= faction.GetStatValue(FCStatDefOf.militaryEfficiencyBonusDefending);
            }
            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, settlement, FactionCache.PlayerColonyFaction);
            return returnForce;
            //create and return force.
        }

        public static void GetMilitaryLevelAndEfficiencyFromTechLevel(TechLevel techlevel, out double militaryLevel, out double efficiency)
        {
            switch (techlevel)
            {
                case TechLevel.Undefined:
                    militaryLevel = 1;
                    efficiency = .5;
                    break;
                case TechLevel.Animal:
                    militaryLevel = 1;
                    efficiency = .5;
                    break;
                case TechLevel.Neolithic:
                    militaryLevel = 2;
                    efficiency = 1;
                    break;
                case TechLevel.Medieval:
                    militaryLevel = 3;
                    efficiency = 1.2;
                    break;
                case TechLevel.Industrial:
                    militaryLevel = 5;
                    efficiency = 1.2;
                    break;
                case TechLevel.Spacer:
                    militaryLevel = 6;
                    efficiency = 1.3;
                    break;
                case TechLevel.Ultra:
                    militaryLevel = 7;
                    efficiency = 1.3;
                    break;
                case TechLevel.Archotech:
                    militaryLevel = 9;
                    efficiency = 1.5;
                    break;
                default:
                    militaryLevel = 1;
                    efficiency = 1;
                    LogUtil.Message("Defaulted GetMilitaryLevelAndEfficiencyFromTechLevel switch case");
                    break;
            }
        }

        public static militaryForce CreateMilitaryForceFromEnemySettlement(Settlement settlement)
        {
            double militaryLevel = 0;
            double efficiency = 0;

            GetMilitaryLevelAndEfficiencyFromTechLevel(settlement.Faction.def.techLevel, out militaryLevel, out efficiency);

            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, null, settlement.Faction);
            return returnForce;
        }

        public static militaryForce CreateMilitaryForceFromFaction(Faction faction, bool handicap)
        {
            double militaryLevel = 1;
            double efficiency = 1;
            if (faction != null && faction.def != null)
            {
                GetMilitaryLevelAndEfficiencyFromTechLevel(faction.def.techLevel, out militaryLevel, out efficiency);

                if (faction.def.defName == "Insect")
                {
                    militaryLevel = 4;
                    efficiency = 1.2;
                }
            }

            double value = militaryLevel + MilitaryUtil.RandomAttackModifier();
            value = Math.Max(value, 1);

            FactionFC factionComp = FactionCache.FactionComp;

            // Apply Empire Threat Level scaling
            value *= ThreatScalingUtil.ComputeEmpireThreatLevel(factionComp);

            // Apply storyteller-curve adaptation
            if (factionComp.threatAdaptation != null)
            {
                value *= factionComp.threatAdaptation.ThreatFactor;
            }

            if (handicap)
            {
                value = Math.Min(value, ThreatScalingUtil.ComputeHandicapCap(factionComp));
            }

            militaryForce returnForce = new militaryForce(value, efficiency, null, faction);
            return returnForce;
        }
    }

    public class MilitaryUtilFC
    {
        public static void AttackPlayerSettlement(militaryForce attackingForce, WorldSettlementFC settlement, Faction enemyFaction)
        {
            FactionFC factionfc = FactionCache.FactionComp;

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.settlementBeingAttacked);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 60000;
            tmp.location = settlement.Tile;
            tmp.hasDestination = true;
            tmp.customDescription = "settlementAboutToBeAttacked".Translate(settlement.Name, enemyFaction.Name);
            tmp.militaryForceDefending = militaryForce.CreateMilitaryForceFromSettlement(settlement);
            tmp.militaryForceDefendingFaction = FactionCache.PlayerColonyFaction;
            tmp.militaryForceAttacking = attackingForce;
            tmp.militaryForceAttackingFaction = enemyFaction;
            tmp.settlementFCDefending = settlement;

            WorldSettlementFC highest = null;

            foreach (WorldSettlementFC settlementCompare in factionfc.settlements)
            {
                if (settlementCompare.MilitaryComp != null &&
                    settlementCompare.MilitaryComp.autoDefend && !settlementCompare.MilitaryComp.militaryBusy &&
                    !settlementCompare.MilitaryComp.isUnderAttack &&
                    settlementCompare.settlementMilitaryLevel > settlement.settlementMilitaryLevel &&
                    DefenseValidatorRegistry.CanDefend(settlementCompare, settlement) &&
                    (highest == null || settlementCompare.settlementMilitaryLevel > highest.settlementMilitaryLevel))
                {
                    highest = settlementCompare;
                }
            }

            // Also check external auto-defenders (e.g., defensive outposts)
            IAutoDefender bestExternalDefender = AutoDefenderRegistry.FindBestDefender(settlement.Tile, settlement.settlementMilitaryLevel);

            if (highest != null)
            {
                int externalLevel = bestExternalDefender != null ? bestExternalDefender.MilitaryLevel : 0;
                if (highest.settlementMilitaryLevel >= externalLevel)
                {
                    ChangeDefendingMilitaryForce(tmp, highest);
                }
                else
                {
                    tmp.militaryForceDefending = bestExternalDefender.CreateDefendingForce();
                    tmp.externalDefenderSource = bestExternalDefender.WorldObject;
                    bestExternalDefender.OnDefenseStarted();
                }
            }
            else if (bestExternalDefender != null)
            {
                tmp.militaryForceDefending = bestExternalDefender.CreateDefendingForce();
                tmp.externalDefenderSource = bestExternalDefender.WorldObject;
                bestExternalDefender.OnDefenseStarted();
            }

            if (settlement.MilitaryComp != null)
            {
                settlement.MilitaryComp.defenderForce = tmp.militaryForceDefending;
                settlement.MilitaryComp.attackerForce = tmp.militaryForceAttacking;

                FactionCache.FactionComp.AddEvent(tmp);

                tmp.customDescription += "\n\n" + "settlementAttackEstimate".Translate(
                    tmp.militaryForceAttacking.forceRemaining,
                    tmp.militaryForceDefending.DefensivePower);
                if (FCSettings.battleMode == BattleMode.Hybrid)
                    tmp.customDescription += "\n\n" + "settlementAttackHybridHint".Translate();
                settlement.MilitaryComp.isUnderAttack = true;

                Find.LetterStack.ReceiveLetter("settlementInDanger".Translate(), tmp.customDescription,
                    LetterDefOf.ThreatBig, new LookTargets(Find.WorldObjects.WorldObjectAt<WorldSettlementFC>(settlement.Tile)));
            }
            else
            {
                LogUtil.Warning($"Attempted to attack settlement {settlement.Name} without a MilitaryComp");
            }
        }

        /// <summary>
        /// Attacks an external <see cref="IRaidTarget"/> registered via <see cref="RaidTargetRegistry"/>.
        /// Creates a <c>settlementBeingAttacked</c> event with the same 24-hour warning as settlement raids.
        /// Auto-defend logic checks both Empire settlements and <see cref="AutoDefenderRegistry"/> entries.
        /// </summary>
        public static void AttackRaidTarget(militaryForce attackingForce, IRaidTarget target, Faction enemyFaction)
        {
            FactionFC factionfc = FactionCache.FactionComp;

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.settlementBeingAttacked);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            tmp.location = target.Tile;
            tmp.hasDestination = true;
            tmp.customDescription = "settlementAboutToBeAttacked".Translate(target.Name, enemyFaction.Name);

            // Create a default defending force from the target's military level
            double defLevel = Math.Max(1, target.MilitaryLevel);
            double defEfficiency = 1.0;
            defEfficiency *= factionfc.GetStatValue(FCStatDefOf.militaryEfficiencyBonusDefending);
            defLevel += factionfc.GetStatValue(FCStatDefOf.militaryLevelBonusDefending);
            tmp.militaryForceDefending = new militaryForce(defLevel, defEfficiency, null, FactionCache.PlayerColonyFaction);
            tmp.militaryForceDefendingFaction = FactionCache.PlayerColonyFaction;
            tmp.militaryForceAttacking = attackingForce;
            tmp.militaryForceAttackingFaction = enemyFaction;
            tmp.settlementFCDefending = target.WorldObject;

            // Check Empire settlements for auto-defend
            WorldSettlementFC highestSettlement = null;
            foreach (WorldSettlementFC settlementCompare in factionfc.settlements)
            {
                if (settlementCompare.MilitaryComp != null &&
                    settlementCompare.MilitaryComp.autoDefend && !settlementCompare.MilitaryComp.militaryBusy &&
                    !settlementCompare.MilitaryComp.isUnderAttack &&
                    settlementCompare.settlementMilitaryLevel > target.MilitaryLevel &&
                    (highestSettlement == null || settlementCompare.settlementMilitaryLevel > highestSettlement.settlementMilitaryLevel))
                {
                    highestSettlement = settlementCompare;
                }
            }

            // Check external auto-defenders
            IAutoDefender bestExternalDefender = AutoDefenderRegistry.FindBestDefender(target.Tile, target.MilitaryLevel);

            // Pick the stronger defender (Empire settlement vs external)
            int externalLevel = bestExternalDefender != null ? bestExternalDefender.MilitaryLevel : 0;

            if (highestSettlement != null && highestSettlement.settlementMilitaryLevel >= externalLevel)
            {
                // Empire settlement defends — assign its force and mark it as busy
                tmp.militaryForceDefending = militaryForce.CreateMilitaryForceFromSettlement(highestSettlement);
                highestSettlement.MilitaryComp?.SendMilitary(target.Tile, MilitaryJobDefOf.DefendFriendlySettlement, -1, enemyFaction);
            }
            else if (bestExternalDefender != null)
            {
                tmp.militaryForceDefending = bestExternalDefender.CreateDefendingForce();
                tmp.externalDefenderSource = bestExternalDefender.WorldObject;
                bestExternalDefender.OnDefenseStarted();
            }

            target.IsUnderAttack = true;
            factionfc.AddEvent(tmp);

            tmp.customDescription += "\n\n" + "settlementAttackEstimate".Translate(
                tmp.militaryForceAttacking.forceRemaining,
                tmp.militaryForceDefending.DefensivePower);

            Find.LetterStack.ReceiveLetter("settlementInDanger".Translate(), tmp.customDescription,
                LetterDefOf.ThreatBig, new LookTargets(target.WorldObject));
        }

        public static void ChangeDefendingMilitaryForce(FCEvent evt, WorldSettlementFC settlementOfMilitaryForce)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            militaryForce tmpMilitaryForce = null;
            WorldSettlementFC homeSettlement = factionfc.ReturnSettlementByLocation(evt.location);
            if (evt.militaryForceDefending.homeSettlement != null
                && settlementOfMilitaryForce == evt.militaryForceDefending.homeSettlement)
            {
                Messages.Message("militaryAlreadyDefendingSettlement".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            WorldSettlementFC target = Find.World.worldObjects.WorldObjectAt<WorldSettlementFC>(evt.location);

            if (evt.militaryForceDefending.homeSettlement != null
                && evt.militaryForceDefending.homeSettlement != factionfc.ReturnSettlementByLocation(evt.location))
            {
                //if the forces defending aren't the forces belonging to the settlement
                evt.militaryForceDefending.homeSettlement.MilitaryComp?.ReturnMilitary(false);
            }
            else if (evt.externalDefenderSource != null)
            {
                IAutoDefender autoDefender = AutoDefenderRegistry.FindByWorldObject(evt.externalDefenderSource);
                autoDefender?.OnDefenseReplaced();
                evt.externalDefenderSource = null;
            }

            if (settlementOfMilitaryForce != homeSettlement)
            {
                tmpMilitaryForce =
                    militaryForce.CreateMilitaryForceFromSettlement(
                        factionfc.ReturnSettlementByLocation(evt.location), true);
            }

            factionfc.militaryTargets.Remove(evt.location);
            evt.militaryForceDefending =
                militaryForce.CreateMilitaryForceFromSettlement(settlementOfMilitaryForce,
                    homeDefendingForce: tmpMilitaryForce);

            if (target.MilitaryComp == null)
            {
                LogUtil.Warning($"ChangeDefendingMilitaryForce: target settlement {target?.Name} has no MilitaryComp. Aborting.");
                return;
            }
            target.MilitaryComp.defenderForce = evt.militaryForceDefending;

            if (settlementOfMilitaryForce == homeSettlement)
            {
                //if home settlement is reseting to defense
                Messages.Message("defendingMilitaryReset".Translate(), MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                //if settlement is foreign
                settlementOfMilitaryForce.MilitaryComp?.SendMilitary(evt.settlementFCDefending.Tile, MilitaryJobDefOf.DefendFriendlySettlement, -1, evt.militaryForceAttackingFaction);
                Find.LetterStack.ReceiveLetter("Military Action", "ForeignMilitarySwitch"
                    .Translate(settlementOfMilitaryForce.Name,
                        factionfc.ReturnSettlementByLocation(evt.location).Name,
                        evt.militaryForceDefending.militaryLevel), LetterDefOf.NeutralEvent);
            }
        }

        public static void ChangeDefendingToExternalForce(FCEvent evt, IAutoDefender defender)
        {
            FactionFC factionfc = FactionCache.FactionComp;

            if (evt.externalDefenderSource != null && evt.externalDefenderSource == defender.WorldObject)
            {
                Messages.Message("militaryAlreadyDefendingSettlement".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            // Clean up current defender
            if (evt.militaryForceDefending.homeSettlement != null
                && evt.militaryForceDefending.homeSettlement != factionfc.ReturnSettlementByLocation(evt.location))
            {
                evt.militaryForceDefending.homeSettlement.MilitaryComp?.ReturnMilitary(false);
            }
            else if (evt.externalDefenderSource != null)
            {
                IAutoDefender old = AutoDefenderRegistry.FindByWorldObject(evt.externalDefenderSource);
                old?.OnDefenseReplaced();
            }

            // Assign new external defender
            factionfc.militaryTargets.Remove(evt.location);
            evt.militaryForceDefending = defender.CreateDefendingForce();
            evt.externalDefenderSource = defender.WorldObject;
            defender.OnDefenseStarted();

            WorldSettlementFC target = Find.World.worldObjects.WorldObjectAt<WorldSettlementFC>(evt.location);
            if (target?.MilitaryComp != null)
            {
                target.MilitaryComp.defenderForce = evt.militaryForceDefending;
            }

            Messages.Message("externalDefenderAssigned".Translate(defender.WorldObject.LabelCap),
                MessageTypeDefOf.NeutralEvent);
        }

        public static militaryForce ReturnDefendingMilitaryForce(FCEvent evt)
        {
            return evt.militaryForceDefending;
        }

        public static FCEvent ReturnMilitaryEventByLocation(PlanetTile location)
        {
            return FactionCache.FactionComp.events.FirstOrDefault(evt => evt.def == FCEventDefOf.settlementBeingAttacked && evt.location == location);
        }
    }

    public class RelationsUtilFC
    {
        public static void AttackFaction(Faction faction)
        {
            //LogUtil.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            Find.FactionManager.OfPlayer.TryAffectGoodwillWith(faction, -50);
            TrySetRelationKind(Find.FactionManager.OfPlayer, faction, FactionRelationKind.Hostile);
            ResetPlayerColonyRelations();
            //LogUtil.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            //FactionColonies.getPlayerColonyFaction().TryAffectGoodwillWith(faction, -50)
        }

        public static void ResetPlayerColonyRelations()
        {
            Faction PCFaction = FactionCache.PlayerColonyFaction;
            foreach (Faction faction in Find.FactionManager.AllFactionsInViewOrder)
            {
                if (faction != Find.FactionManager.OfPlayer && faction != PCFaction)
                {
                    //if not player faction or player colony faction
                    PCFaction.TryAffectGoodwillWith(faction,
                        (Find.FactionManager.OfPlayer.RelationWith(faction).baseGoodwill -
                         PCFaction.RelationWith(faction).baseGoodwill));
                    TrySetRelationKind(PCFaction, faction, Find.FactionManager.OfPlayer.RelationKindWith(faction));
                    //LogUtil.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony " + PCFaction.RelationWith(faction).goodwill);
                }
            }
        }

        private static bool TrySetRelationKind(Faction self, Faction other, FactionRelationKind kind, bool canSendLetter = true)
        {
            FactionRelation factionRelation = self.RelationWith(other);
            if (factionRelation.kind == kind)
            {
                return true;
            }
            if (!self.HasGoodwill)
            {
                self.SetRelationDirect(other, kind, canSendLetter);
                return true;
            }
            switch (kind)
            {
                case FactionRelationKind.Hostile:
                    self.TryAffectGoodwillWith(other, -75 - factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Hostile;
                case FactionRelationKind.Neutral:
                    self.TryAffectGoodwillWith(other, -factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Neutral;
                case FactionRelationKind.Ally:
                    self.TryAffectGoodwillWith(other, 75 - factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Ally;
                default:
                    throw new NotSupportedException(kind.ToString());
            }
        }
    }
}