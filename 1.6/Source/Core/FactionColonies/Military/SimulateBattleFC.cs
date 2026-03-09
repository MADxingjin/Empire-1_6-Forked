using System;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class SimulateBattleFc
    {
        public static int FightBattle(militaryForce MFA, militaryForce MFB, IRandProvider rand = null)
        {
            int result = 0;
            try
            {
                BattleModifierRegistry.InvokeModifyForce(MFA, true);
                BattleModifierRegistry.InvokeModifyForce(MFB, false);

                LogUtil.Message("SimulateBattleFc.FightBattle: Starting battle");
                while (MFA.forceRemaining > 0 && MFB.forceRemaining > 0)
                {
                    // One number should always be reduced to 0
                    FightRound(MFA, MFB, rand);
                }

                if (MFA.forceRemaining <= 0)
                {
                    LogUtil.Message("SimulateBattleFc.FightBattle: Defending Force has won.");
                    //b is winner
                    result = 1;
                }

                else
                {
                    LogUtil.Message("SimulateBattleFc.FightBattle: Attacking Force has won.");
                    //a is winner
                    result = 0;
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"An exception occurred while resolving combat in Empire {Environment.NewLine}[{e}]");
                result = -1;
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
            forceRemaining = Math.Round(militaryLevel * militaryEfficiency);
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

                if (faction.def.defName == "VFEI_Insect")
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
                    (highest == null || settlementCompare.settlementMilitaryLevel > highest.settlementMilitaryLevel))
                {
                    highest = settlementCompare;
                }
            }

            if (highest != null)
            {
                ChangeDefendingMilitaryForce(tmp, highest);
            }

            if (settlement.MilitaryComp != null)
            {
                settlement.MilitaryComp.defenderForce = tmp.militaryForceDefending;
                settlement.MilitaryComp.attackerForce = tmp.militaryForceAttacking;

                FactionCache.FactionComp.AddEvent(tmp);

                tmp.customDescription += "\n\nThe estimated attacking force's power is: " +
                                         tmp.militaryForceAttacking.forceRemaining;
                settlement.MilitaryComp.isUnderAttack = true;

                Find.LetterStack.ReceiveLetter("settlementInDanger".Translate(), tmp.customDescription,
                    LetterDefOf.ThreatBig, new LookTargets(Find.WorldObjects.WorldObjectAt<WorldSettlementFC>(settlement.Tile)));
            }
            else
            {
                LogUtil.Warning($"Attempted to attack settlement {settlement.Name} without a MilitaryComp");
            }
        }

        public static void ChangeDefendingMilitaryForce(FCEvent evt, WorldSettlementFC settlementOfMilitaryForce)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            militaryForce tmpMilitaryForce = null;
            WorldSettlementFC homeSettlement = factionfc.ReturnSettlementByLocation(evt.location);
            if (settlementOfMilitaryForce == evt.militaryForceDefending.homeSettlement)
            {
                Messages.Message("militaryAlreadyDefendingSettlement".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            WorldSettlementFC target = Find.World.worldObjects.WorldObjectAt<WorldSettlementFC>(evt.location);

            if (evt.militaryForceDefending.homeSettlement != factionfc.ReturnSettlementByLocation(evt.location))
            {
                //if the forces defending aren't the forces belonging to the settlement
                evt.militaryForceDefending.homeSettlement.MilitaryComp?.ReturnMilitary(false);
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