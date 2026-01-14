using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies
{
    /// <summary>
    /// A WorldObjectComp class for use with BuildingFCDefs. A building can specify a WorldObjectComp_SettlementBuilding through defmodextensions.
    /// This comp class still needs to be attached to the WorldSettlementFC world object; the functions provided here simply allow for any one-time
    /// special processing that a building might require.
    /// </summary>
    public abstract class WorldObjectComp_SettlementBuilding : WorldObjectComp
    {
        /// <summary>
        /// Handles any special processing when the building is first constructed.
        /// NOTE: this function is called AFTER the building is added to the building array.
        /// </summary>
        public virtual void OnConstruct(int buildingSlot)
        {
        }
        /// <summary>
        /// Handles any special processing when the building is deconstructed.
        /// NOTE: this function is called BEFORE the building is actually removed from the building array.
        /// </summary>
        public virtual void OnDeconstruct(int buildingSlot)
        {
        }
    }
}
