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
    public class WorldObjectComp_SettlementBuildings : WorldObjectComp
    {
        List<SettlementBuildingComp> settlementBuildingComps = new List<SettlementBuildingComp>();
        public SettlementFC settlementfc => (parent as WorldSettlementFC)?.settlement;

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

        public static SettlementBuildingComp MakeSettlementBuildingComp(Type compClass, SettlementFC settlement)
        {
            SettlementBuildingComp comp = (SettlementBuildingComp)Activator.CreateInstance(compClass);
            comp.settlement = settlement;
            if (settlement == null)
            {
                Log.Error($"Created new SettlementBuildingComp {compClass} with null SettlementFC");
            }
            return comp;
        }
        /// <summary>
        /// Handles any special processing when the building is first constructed.
        /// NOTE: this function is called AFTER the building is added to the building array.
        /// </summary>
        public void OnConstruct(int buildingSlot)
        {
            if (parent is WorldSettlementFC && settlementfc?.buildings[buildingSlot].modExtensions != null)
            {
                foreach (BuildingFCExtension ext in settlementfc.buildings[buildingSlot].modExtensions)
                {
                    if (ext.compClass != null)
                    {
                        SettlementBuildingComp comp = GetComponent(ext.compClass);

                        if (comp == null)
                        {
                            comp = MakeSettlementBuildingComp(ext.compClass, settlementfc);
                            settlementBuildingComps.Add(comp);
                        }

                        comp.OnConstruct(buildingSlot);
                    }
                }
            }
        }
        /// <summary>
        /// Handles any special processing when the building is deconstructed.
        /// NOTE: this function is called BEFORE the building is actually removed from the building array.
        /// </summary>
        public void OnDeconstruct(int buildingSlot)
        {
            if (parent is WorldSettlementFC && settlementfc?.buildings[buildingSlot].modExtensions != null)
            {
                foreach (BuildingFCExtension ext in settlementfc.buildings[buildingSlot].modExtensions)
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
                List<SettlementBuildingComp> tmpComps = settlementBuildingComps;
                foreach(SettlementBuildingComp comp in tmpComps)
                {
                    comp.RefreshBuildingSlotsWithErrorDetection();
                    if (comp.CanDestroy)
                    {
                        settlementBuildingComps.Remove(comp);
                    }
                }
            }
            Scribe_Collections.Look(ref settlementBuildingComps, "settlementBuildingComps", LookMode.Deep);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            foreach (SettlementBuildingComp comp in settlementBuildingComps)
            {
                IEnumerable<Gizmo> gizmos = comp.GetGizmos();
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
