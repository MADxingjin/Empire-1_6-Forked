using FactionColonies.util;
using LudeonTK;
using RimWorld;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    public static class MilitaryUtil
    {

        /// <summary>
        /// Internal method used to spawn a <paramref name="settlement"/>'s squad for military deployment
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="squad"></param>
        /// <param name="dropPosition"></param>
        /// <param name="DropPod"></param>
        private static void SpawnSquad(SettlementFC settlement, MercenarySquadFC squad, IntVec3 dropPosition, bool DropPod)
        {
            IncidentParms parms = new IncidentParms
            {
                target = Find.CurrentMap,
                faction = ColonyUtil.getPlayerColonyFaction(),
                podOpenDelay = 140,
                points = 999,
                raidArrivalModeForQuickMilitaryAid = true,
                raidNeverFleeIndividual = true,
                //raidForceOneIncap = true,
                raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
            };

            if (DropPod)
            {
                parms.spawnCenter = dropPosition;
                PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, squad.AllEquippedMercenaryPawns);
            }
            else
            {
                PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
                worker.TryResolveRaidSpawnCenter(parms);
                worker.Arrive(squad.AllEquippedMercenaryPawns, parms);
            }

            squad.AllEquippedMercenaryPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
            squad.isDeployed = true;
            squad.orderLocation = dropPosition;
            squad.timeDeployed = Find.TickManager.TicksGame;
            Find.LetterStack.ReceiveLetter("deploymentSuccessLabel".Translate(), "deploymentSuccessDesc".Translate(settlement.name, Find.CurrentMap.Parent.LabelCap), LetterDefOf.NeutralEvent, new LookTargets(squad.AllEquippedMercenaryPawns));

            settlement.SendMilitary(Find.CurrentMap.Index, Find.World.info.name, MilitaryJob.Deploy, 1, null);
            LordMaker.MakeNewLord(ColonyUtil.getPlayerColonyFaction(), new LordJob_DeployMilitary(dropPosition, squad), Find.CurrentMap, squad.AllEquippedMercenaryPawns);

            if (settlement.militarySquad != squad)
            {
                Find.World.GetComponent<FactionFC>().traitMilitaristicTickLastUsedExtraSquad = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// Deploys a <paramref name="settlement"/>'s main force, takes silver if there is an <paramref name="overrideSquad"/>
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="DropPod"></param>
        /// <param name="overrideSquad"></param>
        public static void CallinAlliedForces(SettlementFC settlement, bool DropPod, MercenarySquadFC overrideSquad = null)
        {
            MercenarySquadFC squad = overrideSquad ?? settlement.militarySquad;

            if (Find.CurrentMap.Parent is WorldSettlementFC)
            {
                Messages.Message("FCMilitaryTriedDeployingToSettlementFC".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            squad.updateSquadStats(settlement.settlementMilitaryLevel);
            squad.resetNeeds();

            IntVec3 dropPosition;
            DebugTool tool = new DebugTool("selectDeploymentPosition".Translate(), delegate
            {
                dropPosition = UI.MouseCell();
                Map curMap = Find.CurrentMap;

                if (!dropPosition.InBounds(curMap))
                {
                    Messages.Message("selectedPosOutOfBounds".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                if (dropPosition.CloseToEdge(curMap, 10))
                {
                    Messages.Message("selectedPosTooCloseToEdge".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                if (overrideSquad != null) PaymentUtil.paySilver((int)Math.Round(settlement.militarySquad.outfit.updateEquipmentTotalCost() * .2));
                SpawnSquad(settlement, squad, dropPosition, DropPod);
                DebugTools.curTool = null;
            });
            DebugTools.curTool = tool;

            //UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
        }

        /// <summary>
        /// Deploys the secondary military of the empire from a <paramref name="settlement"/> 
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="DropPod"></param>
        /// <param name="cost"></param>
        public static void CallinExtraForces(SettlementFC settlement, bool DropPod)
        {
            MercenarySquadFC squad = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.createMercenarySquad(settlement, true);
            squad.OutfitSquad(squad.settlement.militarySquad.outfit);

            CallinAlliedForces(settlement, DropPod, squad);
        }
        public static void FireSupport(SettlementFC settlement, MilitaryFireSupport support)
        {
            DebugTool tool = null;
            IntVec3 DropPosition;
            tool = new DebugTool("FCFireSupportSelectPosition".Translate(), delegate
            {
                float cost = support.returnTotalCost();
                if (PaymentUtil.getSilver() > cost)
                {
                    PaymentUtil.paySilver((int)Math.Round(cost));
                    DropPosition = UI.MouseCell();
                    IntVec3 spawnCenter = DropPosition;
                    Map map = Find.CurrentMap;
                    //Make new list
                    List<ThingDef> projectiles = new List<ThingDef>();
                    projectiles.AddRange(support.projectiles);
                    MilitaryFireSupport fireSupport = new MilitaryFireSupport("fireSupport", map, spawnCenter,
                        projectiles.Count() * 15, 600, support.accuracy, projectiles);
                    Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

                    Messages.Message("FCFireSupportNameWillBeFiredOnPosition".Translate(support.name), MessageTypeDefOf.ThreatSmall);
                    settlement.artilleryTimer = Find.TickManager.TicksGame + 60000;
                }
                else
                {
                    Messages.Message("FCFireSupportNoSilver".Translate(), MessageTypeDefOf.RejectInput);
                }


                DebugTools.curTool = null;
            }, delegate { GenDraw.DrawRadiusRing(UI.MouseCell(), support.accuracy, Color.red); });
            DebugTools.curTool = tool;
        }

        private static float plusOrMinusRandomAttackValue = 2;
        private static List<float> GetAttackPoints()
        {
            List<float> list = new List<float>();
            for (int i = -Convert.ToInt32(plusOrMinusRandomAttackValue * 10);
                i < plusOrMinusRandomAttackValue * 10;
                i++)
            {
                list.Add((i / 10));
            }

            return list;
        }

        public static float RandomAttackModifier()
        {
            float y = (from x in GetAttackPoints()
                       select x).RandomElementByWeight(x =>
                       new SimpleCurve
                               {new CurvePoint(0f, 1f), new CurvePoint(plusOrMinusRandomAttackValue, .1f)}
                           .Evaluate(Math.Abs(x) - 2));
            return y;
        }
    }
}
