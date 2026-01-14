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
using static Verse.KeyPrefs;

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

    public class WorldObjectComp_SettlementShuttles : WorldObjectComp_SettlementBuilding
    {
        public int shuttleUsesRemaining = 0;
        public int totalShuttleUses = 0;
        public int lastShuttleUsesRefreshTick = 0;
        public const int shuttleRefreshInterval = GenDate.TicksPerDay * 5;
        public bool shuttlesActive => totalShuttleUses > 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shuttleUsesRemaining, "shuttleUsesRemaining", 0);
            Scribe_Values.Look(ref totalShuttleUses, "totalShuttleUses", 0);
            Scribe_Values.Look(ref lastShuttleUsesRefreshTick, "lastShuttleUsesRefreshTick", 0);
        }

        private void RefreshTotalShuttleUses(int buildingSlotToSkip = -1)
        {
            if (parent is WorldSettlementFC settlement)
            {
                totalShuttleUses = 0;
                for (int i = 0; i < settlement.settlement.buildings.Count; i++)
                {
                    if (i != buildingSlotToSkip)
                    {
                        BuildingFCExtension_Shuttles ext = settlement.settlement.buildings[i].GetModExtension<BuildingFCExtension_Shuttles>();
                        if (ext != null)
                        {
                            totalShuttleUses += ext.shuttleUses;
                        }
                    }
                }
            }
        }

        public override void OnConstruct(int buildingSlot)
        {
            LogUtil.Message("Start of WorldObjectComp_SettlementShuttles.OnConstruct");
            if (parent is WorldSettlementFC settlement)
            {
                int oldTotalUses = totalShuttleUses;
                RefreshTotalShuttleUses();
                shuttleUsesRemaining += (totalShuttleUses - oldTotalUses);
                if (shuttleUsesRemaining < 0)
                {
                    shuttleUsesRemaining = 0;
                }
            }
        }
        public override void OnDeconstruct(int buildingSlot)
        {
            LogUtil.Message("Start of WorldObjectComp_SettlementShuttles.OnDeconstruct");
            if (parent is WorldSettlementFC settlement)
            {
                RefreshTotalShuttleUses(buildingSlot);
                if (shuttleUsesRemaining > totalShuttleUses)
                {
                    shuttleUsesRemaining = totalShuttleUses;
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (shuttlesActive && lastShuttleUsesRefreshTick + shuttleRefreshInterval > Find.TickManager.TicksGame && parent is WorldSettlementFC settlement)
            {
                RefreshTotalShuttleUses();
                shuttleUsesRemaining = totalShuttleUses;
                lastShuttleUsesRefreshTick = Find.TickManager.TicksGame;
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (shuttlesActive && parent is WorldSettlementFC worldsettlement)
            {
                yield return RequestShuttleAction(worldsettlement);
                yield return RequestShuttleForCaravanAction(worldsettlement);
            }
        }

        private Command RequestShuttleAction(WorldSettlementFC worldsettlement)
        {
            Command_Action requestShuttle = new Command_Action
            {
                defaultLabel = "shuttlePortCallShuttleLabel".Translate(),
                defaultDesc = "shuttlePortCallShuttleDesc".Translate(shuttleUsesRemaining, ShuttleSender.cost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),
                action = delegate
                {
                    Find.WorldSelector.ClearSelection();
                    var sender = new ShuttleSender(worldsettlement.Tile, this);
                    Find.WorldTargeter.BeginTargeting(sender.PerformActionWithTarget, true,
                        CompLaunchable.TargeterMouseAttachment, false, sender.DrawWorldRadiusRing,
                        sender.DisplayTargetInformation, sender.ChoseWorldTarget);
                }
            };
            if (shuttleUsesRemaining < ShuttleSender.cost)
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
                defaultDesc = "shuttlePortCallShuttleDesc".Translate(shuttleUsesRemaining, ShuttleSender.cost),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),

                action = delegate
                {
                    var caravans = Find.World.worldObjects.Caravans.Where(caravan => caravan.Faction == Faction.OfPlayer).ToList();
                    var options = new List<FloatMenuOption>();

                    caravans.ForEach(caravan => options.Add(new FloatMenuOption(caravan.Label, delegate
                    {
                        var sender = new ShuttleSenderCaravan(caravan.Tile, caravan, this);

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
            if (shuttleUsesRemaining < ShuttleSender.cost)
            {
                requestShuttleForCaravan.Disable("noShuttleUsesRemaining".Translate());
            }

            return requestShuttleForCaravan;
        }
    }
}
