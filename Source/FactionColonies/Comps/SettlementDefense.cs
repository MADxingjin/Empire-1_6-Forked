using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public class WorldObjectCompProperties_SettlementDefense : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementDefense()
        {
            compClass = typeof(WorldObjectComp_SettlementDefense);
        }
        public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
            {
                yield return parentDef.defName + " has WorldObjectCompProperties_SettlementDefense but it's not MapParent.";
            }
        }
    }

    public class WorldObjectComp_SettlementDefense : WorldObjectComp
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (parent is WorldSettlementFC worldsettlement)
            {
                if (worldsettlement.settlement.isUnderAttack)
                {
                    yield return DefendColonyAction(worldsettlement);
                }
                if (worldsettlement.settlement.isUnderAttack && !worldsettlement.attackers.Any())
                {
                    FCEvent evt = MilitaryUtilFC.returnMilitaryEventByLocation(worldsettlement.settlement.mapLocation);
                    if (evt != null)
                    {
                        yield return ChangeDefenderAction(worldsettlement, evt);
                    }
                    else
                    {
                        LogUtil.Warning($"Settlment {worldsettlement.Name} is under attack, but found no valid associated event");
                    }
                }
            }
        }

        private Command DefendColonyAction(WorldSettlementFC worldsettlement)
        {
            Command_Action defendColony = new Command_Action
            {
                defaultLabel = "DefendColony".Translate(),
                defaultDesc = "DefendColonyDesc".Translate(),
                icon = TexLoad.iconMilitary,
                action = delegate
                {
                    worldsettlement.startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(worldsettlement.settlement.mapLocation), () => { });
                }
            };
            /* If auto-battle is enabled, then disable the button. We leave it visible, though, so that the player knows that this is an option if
             * they change their settings. (Once manual fighting becomes an option the player can use, at least) */
            AcceptanceReport canUse = CanDoManualFight();
            if (!canUse.Accepted)
            {
                defendColony.Disable(canUse.Reason);
            }

            return defendColony;
        }

        private Command ChangeDefenderAction(WorldSettlementFC worldsettlement, FCEvent evt)
        {
            Command_Action changeDefender = new Command_Action
            {
                defaultLabel = "DefendSettlement".Translate(),
                defaultDesc = "",
                icon = TexLoad.iconCustomize,
                action = delegate
                {
                    var list = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("SettlementDefendingInformation".Translate(evt.militaryForceDefending.homeSettlement.name,
                                                                                       evt.militaryForceDefending.militaryLevel),
                                            null, MenuOptionPriority.High),
                        new FloatMenuOption("ChangeDefendingForce".Translate(), () => ChangeDefendingForceAction(worldsettlement, evt))
                    };

                    var floatMenu = new FloatMenu(list)
                    {
                        vanishIfMouseDistant = true
                    };
                    Find.WindowStack.Add(floatMenu);
                }
            };

            return changeDefender;
        }

        private void ChangeDefendingForceAction(WorldSettlementFC worldsettlement, FCEvent evt)
        {
            var faction = Find.World.GetComponent<FactionFC>();
            var settlementList = new List<FloatMenuOption>
            {
                new FloatMenuOption
                (
                    "ResetToHomeSettlement".Translate(worldsettlement.settlement.settlementMilitaryLevel),
                    delegate { MilitaryUtilFC.changeDefendingMilitaryForce(evt, worldsettlement.settlement); },
                    MenuOptionPriority.High
                )
            };


            settlementList.AddRange
            (
                from foundSettlement in faction.settlements
                where foundSettlement.isMilitaryValid() && foundSettlement != worldsettlement.settlement
                select new FloatMenuOption
                (
                    FoundSettlementString(foundSettlement),
                    delegate
                    {
                        if (!foundSettlement.isMilitaryBusy())
                            MilitaryUtilFC.changeDefendingMilitaryForce(evt, foundSettlement);
                    }
                )
            );

            if (settlementList.Count == 0)
                settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));

            var floatMenu2 = new FloatMenu(settlementList)
            {
                vanishIfMouseDistant = true
            };
            Find.WindowStack.Add(floatMenu2);
        }

        private string FoundSettlementString(SettlementFC s)
        {
            return s.name + " " + "ShortMilitary".Translate() + " " + s.settlementMilitaryLevel +
                   " - " + "FCAvailable".Translate() + ": " + (!s.isMilitaryBusySilent()).ToString();
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (parent is WorldSettlementFC worldsettlement && worldsettlement.settlement.isUnderAttack)
            {
                yield return defendColonyCaravan(worldsettlement, caravan);
            }
        }

        private Command defendColonyCaravan(WorldSettlementFC worldsettlement, Caravan caravan)
        {
            Command_Action defendColonyCaravan = new Command_Action
            {
                defaultLabel = "DefendColony".Translate(),
                defaultDesc = "DefendColonyDesc".Translate(),
                icon = TexLoad.iconMilitary,
                action = () =>
                {
                    worldsettlement.startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(worldsettlement.settlement.mapLocation),
                                                 () => worldsettlement.CaravanDefend(caravan));
                }
            };
            /* If auto-battle is enabled, then disable the button. We leave it visible, though, so that the player knows that this is an option if
             * they change their settings. (Once manual fighting becomes an option the player can use, at least) */
            AcceptanceReport canUse = CanDoManualFight();
            if (!canUse.Accepted)
            {
                defendColonyCaravan.Disable(canUse.Reason);
            }

            return defendColonyCaravan;
        }

        private AcceptanceReport CanDoManualFight()
        {
            if (FCSettings.settlementsAutoBattle)
            {
                return new AcceptanceReport("autoBattleEnabledNoManualFight".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }
    }
}
