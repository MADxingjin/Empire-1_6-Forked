using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class WorldObjectCompProperties_SettlementShuttles : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementShuttles()
        {
            compClass = typeof(WorldObjectComp_SettlementShuttles);
        }
        public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
            {
                yield return parentDef.defName + " has WorldObjectCompProperties_SettlementShuttles but it's not MapParent.";
            }
        }
    }

    public class WorldObjectComp_SettlementShuttles : WorldObjectComp
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (parent is WorldSettlementFC worldsettlement)
            {
                if (worldsettlement.settlement.buildings.Contains(BuildingFCDefOf.shuttlePort))
                {
                    yield return RequestShuttleAction(worldsettlement);
                    yield return RequestShuttleForCaravanAction(worldsettlement);
                }
            }
        }

        private Command RequestShuttleAction(WorldSettlementFC worldsettlement)
        {
            Command_Action requestShuttle = new Command_Action
            {
                defaultLabel = "shuttlePortCallShuttleLabel".Translate(),
                defaultDesc = "shuttlePortCallShuttleDesc".Translate(worldsettlement.shuttleUsesRemaining, ShuttleSender.cost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),
                action = delegate
                {
                    Find.WorldSelector.ClearSelection();
                    var sender = new ShuttleSender(worldsettlement.Tile, worldsettlement);
                    Find.WorldTargeter.BeginTargeting(sender.PerformActionWithTarget, true,
                        CompLaunchable.TargeterMouseAttachment, false, sender.DrawWorldRadiusRing,
                        sender.DisplayTargetInformation, sender.ChoseWorldTarget);
                }
            };
            if (worldsettlement.shuttleUsesRemaining < ShuttleSender.cost)
            {
                requestShuttle.Disable("notEnoughShuttleUsesRemaining".Translate());
            }

            return requestShuttle;
        }

        private Command RequestShuttleForCaravanAction(WorldSettlementFC worldsettlement)
        {
            Command_Action requestShuttleForCaravan = new Command_Action
            {
                defaultLabel = "shuttlePortCallShuttleForCaravanLabel".Translate(),
                defaultDesc = "shuttlePortCallShuttleDesc".Translate(worldsettlement.shuttleUsesRemaining, ShuttleSender.cost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),

                action = delegate
                {
                    var caravans = Find.World.worldObjects.Caravans.Where(caravan => caravan.Faction == Faction.OfPlayer).ToList();
                    var options = new List<FloatMenuOption>();

                    caravans.ForEach(caravan => options.Add(new FloatMenuOption(caravan.Label, delegate
                    {
                        var sender = new ShuttleSenderCaravan(caravan.Tile, caravan, worldsettlement);

                        CameraJumper.TryJump(caravan);
                        Find.WorldSelector.ClearSelection();
                        var tile = caravan.Tile;
                        Find.WorldTargeter.BeginTargeting(sender.ChoseWorldTarget, true,
                            CompLaunchable.TargeterMouseAttachment, false,
                            delegate { GenDraw.DrawWorldRadiusRing(tile, ShuttleSender.ShuttleRange); },
                            target => sender.TargetingLabelGetter(target, tile, ShuttleSender.ShuttleRange,
                                Gen.YieldSingle(caravan), sender.Launch));
                    })));

                    if (options.Count == 0) options.Add(new FloatMenuOption("noCaravansToSendShuttleTo".Translate(), null));

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
            if (worldsettlement.shuttleUsesRemaining < ShuttleSender.cost)
            {
                requestShuttleForCaravan.Disable("noShuttleUsesRemaining".Translate());
            }

            return requestShuttleForCaravan;
        }
    }
}
