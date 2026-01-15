using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// A special custom 'comp' class for use with BuildingFCDefs. A building can specify a SettlementBuildingComp through defmodextensions.
    /// When the building is constructed, its associated SettlementBuildingComp will be added to the settlement's WorldObjectComp_SettlementBuildings,
    /// which will handle all of the building processing.
    /// </summary>
    public abstract class SettlementBuildingComp : IExposable
    {
        public SettlementFC settlement;
        public List<int> buildingSlots = new List<int>();
        public bool CanDestroy => buildingSlots.Count == 0;
        public WorldObjectComp_SettlementBuildings parentComp => settlement.worldSettlement?.GetComponent<WorldObjectComp_SettlementBuildings>();
        public virtual void ExposeData()
        {
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Collections.Look(ref buildingSlots, "buildingSlots", LookMode.Deep);
        }

        /// <summary>
        /// A heavy-handed function to refresh the buildingSlots array. Should be called infrequently (currently only when saving).
        /// Meant to ensure that we don't have any memory leaks by leaving behind orphaned SettlementBuildingComps.
        /// </summary>
        public void RefreshBuildingSlots()
        {
            buildingSlots.Clear();
            for(int i = 0; i < settlement.buildings.Count; i++)
            {
                BuildingFCDef building = settlement.buildings[i];
                if (building.modExtensions != null)
                {
                    foreach (BuildingFCExtension ext in building.modExtensions)
                    {
                        if (ext.compClass != null)
                        {
                            SettlementBuildingComp comp = parentComp?.GetComponent(ext.compClass);

                            if (comp == this)
                            {
                                buildingSlots.Add(i);
                                continue;
                            }
                        }
                    }
                }
            }
        }

        public void RefreshBuildingSlotsWithErrorDetection()
        {
            int oldSlotCount = buildingSlots.Count;
            RefreshBuildingSlots();
            int newSlotCount = buildingSlots.Count;
            if (oldSlotCount != newSlotCount)
            {
                LogUtil.Warning($"Detected buildingSlot count discrepancy in SettlementBuildingComp {this.ToStringSafe()} | old count: {oldSlotCount} new count: {newSlotCount}");
            }
        }

        public virtual void Tick()
        {
        }
        /// <summary>
        /// Called when a building is constructed.
        /// This parent function should be called at the *beginning* of any subclass functions.
        /// </summary>
        /// <param name="buildingSlot"></param>
        public virtual void OnConstruct(int buildingSlot)
        {
            LogUtil.Message("Start of SettlementBuildingComp.OnConstruct");
            buildingSlots.Add(buildingSlot);
        }
        /// <summary>
        /// Called when a building is deconstructed.
        /// This parent function should be called at the *end* of any subclass functions.
        /// </summary>
        /// <param name="buildingSlot"></param>
        public virtual void OnDeconstruct(int buildingSlot)
        {
            LogUtil.Message("Start of SettlementBuildingComp.OnDeconstruct");
            buildingSlots.Remove(buildingSlot);
        }

        public virtual IEnumerable<Gizmo> GetGizmos()
        {
            return null;
        }
    }
}
