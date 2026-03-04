using System;
using System.Collections.Generic;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Militaristic : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Militaristic();

        public override void ModifyMilitaryForce(ref double level, ref double efficiency, bool isAttacking)
        {
            if (isAttacking)
                efficiency *= 1.2;
        }

        public override double ModifyBuildingUpkeep(double upkeep, BuildingFCDef building)
        {
            if (building?.traits != null)
            {
                foreach (var trait in building.traits)
                {
                    if (trait.militaryBaseLevel > 0 || trait.militaryMultiplierCombatEfficiency > 1)
                        return Math.Max(0, upkeep - 100);
                }
            }
            return upkeep;
        }

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            settlement.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
        }

        public override int ModifyDeadPawnCooldownMultiplier(int multiplier) => multiplier - 2000;

        public override bool EnablesAction(FCActionType action) => action == FCActionType.DeployExtraSquad;

        public override void OnSquadDeployed(FactionFC faction, FCPolicy policy, WorldSettlementFC settlement, bool isExtraSquad)
        {
            if (isExtraSquad)
            {
                var state = policy.state as FCPolicyState_Militaristic;
                if (state != null)
                    state.tickLastUsedExtraSquad = Find.TickManager.TicksGame;
            }
        }

        public override IEnumerable<FloatMenuOption> GetExtraDeploymentOptions(FactionFC faction, FCPolicy policy, WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp)
        {
            var state = policy.state as FCPolicyState_Militaristic;
            if (state == null) yield break;

            if ((state.tickLastUsedExtraSquad + GenDate.TicksPerDay * 5) > Find.TickManager.TicksGame)
            {
                Messages.Message("XDaysToRedeploy".Translate(Math.Round(
                    ((state.tickLastUsedExtraSquad + GenDate.TicksPerDay * 5) -
                     Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
                yield break;
            }

            int cost = (int)Math.Round(milComp.militarySquad.outfit.updateEquipmentTotalCost() * .2);
            yield return new FloatMenuOption("FCDeploySecondarySquad".Translate(cost), delegate
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
            });
        }
    }
}
