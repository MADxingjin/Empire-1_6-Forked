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
    public class WorldObjectCompProperties_SettlementBuildings : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementBuildings()
        {
            compClass = typeof(WorldObjectComp_SettlementBuildings);
        }
        public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
            {
                yield return parentDef.defName + " has WorldObjectCompProperties_SettlementBuildings but it's not MapParent.";
            }
        }
    }
    /// <summary>
    /// A WorldObjectComp class for use with BuildingFCDefs. When a building is constructed, if it has a SettlementBuildingComp, then
    /// the comp is added to this comp's list and tracked.
    /// </summary>
    // TODO: use this comp to do *all* building tracking, instead of storing the buildings in the worldsettlementfc itself?
    public class WorldObjectComp_SettlementBuildings : WorldObjectComp
    {
        public int FC_MAX_BUILDINGS => (int)Math.Min(3 + Math.Floor(FCSettings.settlementMaxLevel / 2f), WorldSettlement.settlementDef.maxBuildingCount);
        private List<BuildingFC> buildings = new List<BuildingFC>();
        private List<SettlementBuildingComp> settlementBuildingComps = new List<SettlementBuildingComp>();

        public List<BuildingFC> Buildings => buildings;

        public int NumBuildingSlots => SettlementFormulas.CalculateBuildingSlots(WorldSettlement?.settlementLevel ?? 0, WorldSettlement.settlementDef.maxBuildingCount);

        private bool dirtyConstructionCache = true;
        private List<BuildingFC> constructionCache = new List<BuildingFC>();

        private WorldSettlementFC cachedWorldSettlementParent = null;
        public WorldSettlementFC WorldSettlement
        {
            get
            {
                if (cachedWorldSettlementParent != null)
                {
                    return cachedWorldSettlementParent;
                }
                if (parent is WorldSettlementFC ws)
                {
                    cachedWorldSettlementParent = ws;
                }
                else
                {
                    cachedWorldSettlementParent = null;
                    LogUtil.ErrorOnce($"WorldObjectComp_SettlementMilitary has a non-WorldSettlementFC parent", 93512107);
                }
                return cachedWorldSettlementParent;
            }
        }

        public string buildingID(int buildingSlot)
        {
            if (buildingSlot >= buildings.Count)
            {
                return "null";
            }
            return buildings[buildingSlot].def.defName + buildingSlot.ToString();
        }
        public bool hasBuilding(BuildingFCDef building)
        {
            foreach(BuildingFC bfc in buildings)
            {
                if (bfc.def == building)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Returns true if any currently-built building in this settlement
        /// lists the given building in its requiredBuildings.
        /// </summary>
        public bool IsBuildingRequiredByOther(BuildingFCDef building)
        {
            foreach (BuildingFC bfc in buildings)
            {
                if (bfc.def == BuildingFCDefOf.Empty || bfc.def == BuildingFCDefOf.Construction) continue;
                if (bfc.def.requiredBuildings != null && bfc.def.requiredBuildings.Contains(building))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Returns all currently-built buildings that directly require the given building.
        /// </summary>
        public List<BuildingFCDef> GetBuildingsDependingOn(BuildingFCDef building)
        {
            List<BuildingFCDef> result = new List<BuildingFCDef>();
            foreach (BuildingFC bfc in buildings)
            {
                if (bfc.def == BuildingFCDefOf.Empty || bfc.def == BuildingFCDefOf.Construction) continue;
                if (bfc.def.requiredBuildings != null && bfc.def.requiredBuildings.Contains(building))
                    result.Add(bfc.def);
            }
            return result;
        }
        public BuildingFCDef getBuildingInSlot(int buildingSlot)
        {
            if (buildingSlot >= buildings.Count)
            {
                return null;
            }
            return buildings[buildingSlot].def;
        }

        public SettlementBuildingComp GetComponent(Type type)
        {
            for (int i = 0; i < settlementBuildingComps.Count; i++)
            {
                if (type.IsInstanceOfType(settlementBuildingComps[i]))
                {
                    return settlementBuildingComps[i];
                }
            }
            return null;
        }

        public bool buildingSlotIsEmpty(int buildingSlot)
        {
            return buildings[buildingSlot].def.defName == BuildingFCDefOf.Empty.defName;
        }
        public bool buildingSlotIsConstruction(int buildingSlot)
        {
            return buildings[buildingSlot].def.defName == BuildingFCDefOf.Construction.defName;
        }
        public bool buildingSlotIsBuilding(int buildingSlot)
        {
            return !buildingSlotIsEmpty(buildingSlot) && !buildingSlotIsConstruction(buildingSlot);
        }
        public string buildingLabel(int buildingsSlot)
        {
            return buildings[buildingsSlot].def.LabelCap;
        }
        public List<BuildingFC> getUnderConstructionBuildings()
        {
            if (dirtyConstructionCache)
            {
                List<BuildingFC> list = new List<BuildingFC>();
                foreach (BuildingFC building in buildings)
                {
                    if (building.def == BuildingFCDefOf.Construction)
                    {
                        list.Add(building);
                    }
                }

                constructionCache = list;
                dirtyConstructionCache = false;
            }

            return constructionCache;
        }

        public void InitBuildings()
        {
            for (int i = 0; i < FC_MAX_BUILDINGS; i++)
            {
                buildings.Add(new BuildingFC
                {
                    def = BuildingFCDefOf.Empty,
                    startedTick = -1,
                    completionTick = Find.TickManager.TicksGame
                });
            }
        }
        public void ReinitBuildings()
        {
            LogUtil.Message($"Reinitializing buildings for settlement {WorldSettlement.Name}. Max buildings: {FC_MAX_BUILDINGS}. Current buildings count: {buildings.Count}");
            if (buildings.Count > FC_MAX_BUILDINGS)
            {
                /* Remove slots, starting at the end and working backwards */
                for (int i = buildings.Count-1; i >= FC_MAX_BUILDINGS && i >= 0; i--)
                {
                    DeconstructBuilding(i);
                    buildings.RemoveAt(i);
                }
            }
            else if (buildings.Count < FC_MAX_BUILDINGS)
            {
                for (int i = buildings.Count; i < FC_MAX_BUILDINGS; i++)
                {
                    buildings.Add(new BuildingFC
                    {
                        def = BuildingFCDefOf.Empty,
                        startedTick = -1,
                        completionTick = Find.TickManager.TicksGame
                    });
                }
            }
        }

        public static SettlementBuildingComp MakeSettlementBuildingComp(Type compClass, WorldSettlementFC settlement)
        {
            SettlementBuildingComp comp = (SettlementBuildingComp)Activator.CreateInstance(compClass);
            comp.settlement = settlement;
            if (settlement == null)
            {
                Log.Error($"Created new SettlementBuildingComp {compClass} with null SettlementFC");
            }
            return comp;
        }
        public bool validConstructBuilding(BuildingFCDef building, int buildingSlot)
        {
            bool valid = true;

            foreach (BuildingFC slot in buildings) //check if already a building of that type constructed
            {
                if (slot.def == building)
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (PaymentUtil.getSilver() < building.cost) //check if the player has enough money
            {
                valid = false;
                Messages.Message("NotEnoughSilverConstructBuilding".Translate() + "!", MessageTypeDefOf.RejectInput);
            }

            //TODO: rework construction. This info should really be held in this comp here, rather than in the events queue.
            //      maybe there can still be a "constructing building" event that refers to the SettlementBuilding comp, but
            //      the comp should be the source of truth, not the event
            foreach (FCEvent event1 in FactionCache.FactionComp.events) //check if construction would match any already-occuring events
            {
                if (WorldSettlement.MilitaryComp?.isUnderAttack == true)
                {
                    valid = false;
                    Messages.Message("SettlementUnderAttack".Translate(), MessageTypeDefOf.RejectInput);
                }
                if (event1.source == WorldSettlement.Tile && event1.building == building &&
                    event1.def.defName == "constructBuilding")
                {
                    valid = false;
                    Messages.Message("BuildingBeingBuiltAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }

                if (event1.source == WorldSettlement.Tile && event1.buildingSlot == buildingSlot &&
                    event1.def.defName == "constructBuilding"
                ) //check if there is already a building being constructed in that slot
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyConstructed".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (building.minhilliness != Hilliness.Undefined && building.minhilliness > WorldSettlement.Tile.Tile.hilliness)
            {
                valid = false;
                Messages.Message("BuildingInvalidEnvironment".Translate(), MessageTypeDefOf.RejectInput);
            }

            if (building.maxhilliness != Hilliness.Undefined && building.maxhilliness < WorldSettlement.Tile.Tile.hilliness)
            {
                valid = false;
                Messages.Message("BuildingInvalidEnvironment".Translate(), MessageTypeDefOf.RejectInput);
            }

            if (building.applicableBiomes.Count > 0)
            {
                bool match = building.applicableBiomes.Contains(WorldSettlement.biome);

                //if found no matches
                if (match == false)
                {
                    valid = false;
                    Messages.Message("BuildingInvalidEnvironment".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            // Check settlement type restrictions
            if (!building.CanBeBuiltForSettlementType(WorldSettlement.settlementDef))
            {
                valid = false;
                Messages.Message("BuildingInvalidSettlement".Translate(building.LabelCap, WorldSettlement.settlementDef.LabelCap), MessageTypeDefOf.RejectInput);
            }
            //TODO: rework based on def
            /*bool isOrbitalPlatform = ResourceUtils.IsOrbitalPlatform(settlement);
            switch (building.settlementTypeRestriction)
            {
                case SettlementTypeRestriction.SurfaceOnly:
                    if (isOrbitalPlatform)
                    {
                        valid = false;
                        Messages.Message("BuildingSurfaceOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    break;
                case SettlementTypeRestriction.OrbitalOnly:
                    if (!isOrbitalPlatform)
                    {
                        valid = false;
                        Messages.Message("BuildingOrbitalOnly".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    break;
            }*/

            return valid;
        }
        public void HandleOnConstructionComps(BuildingFCDef building, int buildingSlot)
        {
            addBuildingTrait(buildingSlot);

            if (buildings[buildingSlot].def.modExtensions?.Count > 0)
            {
                foreach (BuildingFCExtension ext in buildings[buildingSlot].def.modExtensions.OfType<BuildingFCExtension>())
                {
                    if (ext.compClass != null)
                    {
                        SettlementBuildingComp comp = GetComponent(ext.compClass);

                        if (comp == null)
                        {
                            comp = MakeSettlementBuildingComp(ext.compClass, WorldSettlement);
                            settlementBuildingComps.Add(comp);
                        }

                        comp.OnConstruct(buildingSlot);
                    }
                }
            }
        }
        public void startConstruction(BuildingFCDef building, int buildingSlot, int completionTick)
        {
            DeconstructBuilding(buildingSlot);

            LogUtil.Message($"Starting construction of building {building.defName} in slot {buildingSlot} in settlement {WorldSettlement.Name}. Completes on tick {completionTick}");
            dirtyConstructionCache = true;

            buildings[buildingSlot] = new BuildingFC
            {
                def = BuildingFCDefOf.Construction,
                underConstructionDef = building,
                startedTick = Find.TickManager.TicksGame,
                completionTick = completionTick
            };

            // The Construction def shouldn't have traits or modExtensions, I think. But just in case we decide to do something funky,
            //   we'll leave this code here.
            HandleOnConstructionComps(building, buildingSlot);
        }
        /// <summary>
        /// <para>Handles any special processing when a building is first constructed.</para>
        /// </summary>
        public void ConstructBuilding(BuildingFCDef building, int buildingSlot)
        {
            DeconstructBuilding(buildingSlot);

            LogUtil.Message($"Constructing building {building.defName} in slot {buildingSlot} in settlement {WorldSettlement.Name}");
            dirtyConstructionCache = true;

            buildings[buildingSlot] = new BuildingFC
            {
                def = building,
                startedTick = -1,
                completionTick = Find.TickManager.TicksGame
            };

            HandleOnConstructionComps(building, buildingSlot);
        }
        /// <summary>
        /// <para>Handles any special processing when a building is deconstructed.</para>
        /// </summary>
        public void DeconstructBuilding(int buildingSlot)
        {
            LogUtil.Message($"Deconstructing building {buildings[buildingSlot].def.defName} in slot {buildingSlot} in settlement {WorldSettlement?.Name ?? "nullsettlement"}");
            dirtyConstructionCache = true;

            removeBuildingTrait(buildingSlot);

            if (buildings[buildingSlot].def.modExtensions?.Count > 0)
            {
                foreach (BuildingFCExtension ext in buildings[buildingSlot].def.modExtensions.OfType<BuildingFCExtension>())
                {
                    if (ext.compClass != null)
                    {
                        SettlementBuildingComp comp = GetComponent(ext.compClass);

                        if (comp == null)
                        {
                            LogUtil.Error($"Found null comp for specificed compClass {ext.compClass} in OnDeconstruct. The comp should not be null yet.");
                        }
                        else
                        {
                            comp.OnDeconstruct(buildingSlot);

                            if (comp.CanDestroy)
                            {
                                settlementBuildingComps.Remove(comp);
                            }
                        }
                    }
                }
            }

            buildings[buildingSlot].def = BuildingFCDefOf.Empty;
        }
        public void addBuildingTrait(int buildingSlot)
        {
            if (buildings[buildingSlot].def.traits != null)
            {
                WorldSettlement.addTraits(buildings[buildingSlot].def.traits, buildingID(buildingSlot));
            }
            else if (!(buildings[buildingSlot].def == BuildingFCDefOf.Empty ||
                       buildings[buildingSlot].def == BuildingFCDefOf.Construction))
            {
                LogUtil.Warning($"Building {buildings[buildingSlot].def.defName} has no traits. Is this intentional?");
            }
        }
        public void removeBuildingTrait(int buildingSlot)
        {
            if (buildings[buildingSlot].def.traits != null)
            {
                WorldSettlement.removeTraits(buildings[buildingSlot].def.traits, buildingID(buildingSlot));
            }
            else if (!(buildings[buildingSlot].def == BuildingFCDefOf.Empty ||
                       buildings[buildingSlot].def == BuildingFCDefOf.Construction))
            {
                LogUtil.Warning($"Building {buildings[buildingSlot].def.defName} has no traits. Is this intentional?");
            }
        }
        /// <summary>
        /// Loops through all constructed buildings and applies their trait to the parent settlement.
        /// <para>Assumes that the parent settlement's trait list has already been cleared.</para>
        /// </summary>
        public void reapplyBuildingTraits()
        {
            for (int i = 0; i < FC_MAX_BUILDINGS; i++)
            {
                addBuildingTrait(i);
            }
        }

        public int getBuildingUpkeep(int buildingSlot)
        {
            return getBuildingUpkeep(getBuildingInSlot(buildingSlot));
        }
        public int getBuildingUpkeep(BuildingFCDef building)
        {
            if (building == null)
                return 0;

            double upkeep = building.upkeep;

            FactionFC faction = FactionCache.FactionComp;
            upkeep = faction.ApplyPolicyModifier(upkeep, (ext, val) => ext.ModifyBuildingUpkeep(val, building));

            upkeep += WorldSettlement?.buildingUpkeepModifier(building) ?? 0;

            return Math.Max((int)upkeep, 0);
        }

        public TaggedString getBuildingDesc(BuildingFCDef building)
        {
            TaggedString desc = building.desc + "\n";
            int buildingUpkeep = getBuildingUpkeep(building);
            if (buildingUpkeep > 0)
            {
                desc += "\n" + "FCBuildingUpkeep".Translate(buildingUpkeep.ToString());
            }

            desc += "\n" + building.AttributeDesc;

            return desc.Trim();
        }
        public TaggedString getBuildingDescFull(BuildingFCDef building)
        {
            TaggedString desc = building.LabelCap + "\n-----\n" + getBuildingDesc(building);
            return desc;
        }

        public int TotalUpkeep()
        {
            FactionFC faction = FactionCache.FactionComp;
            int upkeep = 0;
            foreach (BuildingFC building in buildings)
            {
                upkeep += Math.Max((int)faction.ApplyPolicyModifier((double)building.def.upkeep, (ext, val) => ext.ModifyBuildingUpkeep(val, building.def)), 0);
            }
            return upkeep;
        }
        /// <summary>
        /// Used by FCBuildingWindow to determine how many entries to the building filter there should be.
        /// <para>0 = All</para>
        /// <para>1 = Happiness</para>
        /// <para>2 = Basetax</para>
        /// <para>3 = Workers</para>
        /// <para>4 = Military (if the settlement has a MilitaryComp)</para>
        /// <para>5+ = each settlement resource in order</para>
        /// <para>If the settlement does not have a MilitaryComp, then resources will start at index 4 instead of 5.</para>
        /// </summary>
        /// <returns></returns>
        public int getFilterSize()
        {
            if (WorldSettlement.MilitaryComp != null)
            {
                return 5 + WorldSettlement.Resources.Count;
            }
            else
            {
                return 4 + WorldSettlement.Resources.Count;
            }
        }
        /// <summary>
        /// Used by FCBuildingWindow to determine what label to show for a given filter index.
        /// <para>0 = All</para>
        /// <para>1 = Happiness</para>
        /// <para>2 = Basetax</para>
        /// <para>3 = Workers</para>
        /// <para>4 = Military (if the settlement has a MilitaryComp)</para>
        /// <para>5+ = each settlement resource in order</para>
        /// <para>If the settlement does not have a MilitaryComp, then resources will start at index 4 instead of 5.</para>
        /// </summary>
        /// <returns></returns>
        public string getLabelForFilter(int i)
        {
            // edge-case protection
            if (i < 0)
                return null;
            else if (i == 0)
                return "BuildingFilterAll".Translate();
            else if (i == 1)
                return "BuildingFilterHappiness".Translate();
            else if (i == 2)
                return "BuildingFilterBasetax".Translate();
            else if (i == 3)
                return "BuildingFilterWorkers".Translate();
            else if (i == 4 && WorldSettlement.MilitaryComp != null)
                return "BuildingFilterMilitary".Translate();
            else
            {
                return WorldSettlement.getResourceByIndex(i - (WorldSettlement.MilitaryComp == null ? 4 : 5))?.label ?? "";
            }
        }

        /// <summary>
        /// Used by FCBuildingWindow to determine if a building should be filtered.
        /// <para>0 = All</para>
        /// <para>1 = Happiness</para>
        /// <para>2 = Basetax</para>
        /// <para>3 = Workers</para>
        /// <para>4 = Military (if the settlement has a MilitaryComp)</para>
        /// <para>5+ = each settlement resource in order</para>
        /// <para>If the settlement does not have a MilitaryComp, then resources will start at index 4 instead of 5.</para>
        /// </summary>
        /// <returns></returns>
        /* I feel like there has to be a better way to do this. But with a variable number of resources, we can't use an enum... */
        public bool filterBuilding(int i, BuildingFCDef building)
        {
            if (i == 0 || i < 0)
                return true;

            // Get the building's traits
            if (building.traits == null || building.traits.Count == 0)
                return false;

            foreach (FCTraitEffectDef traitDef in building.traits)
            {
                ResourceBonuses rtd;
                if (i == 1)
                {
                    if (traitDef.happinessLostBase != 0 || traitDef.happinessGainedBase != 0 ||
                        Math.Abs(traitDef.happinessLostMultiplier - 1.0) > 0.001 ||
                        Math.Abs(traitDef.happinessGainedMultiplier - 1.0) > 0.001)
                        return true;
                }
                else if (i == 2)
                {
                    if (traitDef.taxBasePercentage != 0 || traitDef.taxBaseRandomModifier != 0)
                        return true;
                }
                else if (i == 3)
                {
                    if (traitDef.workerBaseMax != 0 || traitDef.workerBaseOverMax != 0 || traitDef.workerBaseCost != 0)
                        return true;
                }
                else if (i == 4 && WorldSettlement.MilitaryComp != null)
                {
                    if (traitDef.militaryBaseLevel != 0 || Math.Abs(traitDef.militaryMultiplierCombatEfficiency - 1.0) > 0.001)
                        return true;
                }
                else
                {
                    rtd = traitDef.getTraitResource(WorldSettlement.getResourceByIndex(i - (WorldSettlement.MilitaryComp == null ? 4 : 5))?.def);
                    if (rtd != null && (rtd.additive != 0 || Math.Abs(rtd.multiplier - 1.0) > 0.001))
                        return true;
                }
            }

            return false;
        }

        public override void CompTick()
        {
            base.CompTick();

            foreach (SettlementBuildingComp comp in settlementBuildingComps)
            {
                comp.Tick();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                /* Look through the comps and see if any of them need destroying.
                 * They *should* be destroyed when the associated building is deconstructed. But just in case one gets orphaned somehow,
                 *   we'll destroy it here. Don't want any memory leaks, after all. */
                foreach(SettlementBuildingComp comp in settlementBuildingComps)
                {
                    comp.RefreshBuildingSlotsWithErrorDetection();
                }
                settlementBuildingComps.RemoveAll(comp => comp.CanDestroy);
                ReinitBuildings();
            }
            Scribe_Collections.Look(ref buildings, "buildings", LookMode.Deep);
            Scribe_Collections.Look(ref settlementBuildingComps, "settlementBuildingComps", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                settlementBuildingComps?.RemoveAll(c => c == null);
                ReinitBuildings();
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> gizmos = base.GetGizmos();
            if (gizmos != null)
            {
                foreach (Gizmo gizmo in gizmos)
                {
                    yield return gizmo;
                }
            }
            foreach (SettlementBuildingComp comp in settlementBuildingComps)
            {
                gizmos = comp.GetGizmos();
                if (gizmos == null)
                {
                    continue;
                }
                foreach (Gizmo gizmo in gizmos)
                {
                    yield return gizmo;
                }
            }
        }
    }
}
