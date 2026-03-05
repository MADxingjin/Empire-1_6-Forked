using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Militaristic : FCPolicyBehavior
    {
        private CooldownAbility extraSquadCooldown = new CooldownAbility
        {
            cooldownTicks = GenDate.TicksPerDay * 5
        };

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            settlement.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
        }

        public override double ModifyStat(FCStatDef stat, double currentValue, WorldSettlementFC settlement)
        {
            // Military building upkeep discount: -100 for buildings with military stats
            if (stat == FCStatDefOf.militaryBuildingUpkeepDiscount)
                return currentValue - 100;
            return currentValue;
        }

        public override string GetStatDescription(FCStatDef stat, WorldSettlementFC settlement)
        {
            if (stat == FCStatDefOf.militaryBuildingUpkeepDiscount)
                return TextUtil.colorizeAdditiveBonus(-100, invert: true) + " - " + policy.def.LabelCap + "\n";
            return null;
        }

        public override void OnSquadDeployed(FactionFC faction, WorldSettlementFC settlement, bool isExtraSquad)
        {
            if (isExtraSquad)
                extraSquadCooldown.Use();
        }

        public override IEnumerable<FloatMenuOption> GetExtraDeploymentOptions(
            FactionFC faction, WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp)
        {
            if (!extraSquadCooldown.IsReady)
            {
                Messages.Message("XDaysToRedeploy".Translate(
                    Math.Round(extraSquadCooldown.DaysRemaining, 1)), MessageTypeDefOf.RejectInput);
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

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref extraSquadCooldown, "extraSquadCooldown");
            extraSquadCooldown = extraSquadCooldown ?? new CooldownAbility { cooldownTicks = GenDate.TicksPerDay * 5 };
        }
    }
}
