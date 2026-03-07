using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    
    //Squad Class
    public class MilSquadFC : IExposable, ILoadReferenceable
    {
        public int loadID = -1;
        public string name;
        public List<MilUnitFC> units = new List<MilUnitFC>();
        public double equipmentTotalCost;
        public int tickChanged;

        public static void UpdateEquipmentTotalCostOfSquadsContaining(MilUnitFC unit)
        {
            FactionCache.FactionComp.militaryCustomizationUtil.squads.ForEach(delegate(MilSquadFC squad)
            {
                if (squad.units.Contains(unit))
                {
                    squad.ChangeTick();
                }
            });
        }

        public MilSquadFC()
        {
        }

        public MilSquadFC(bool newSquad)
        {
            if (newSquad)
            {
                setLoadID();
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref units, "units", LookMode.Reference);
            Scribe_Values.Look(ref equipmentTotalCost, "equipmentTotalCost", -1);
            Scribe_Values.Look(ref tickChanged, "tickChanged");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                updateEquipmentTotalCost();
            }
        }

        public void setLoadID()
        {
            loadID = FactionCache.FactionComp.NextSquadID;
        }

        private bool costDirty = true;

        public double GetEquipmentTotalCost()
        {
            if (costDirty)
            {
                updateEquipmentTotalCost();
                costDirty = false;
            }
            return equipmentTotalCost;
        }

        public int updateEquipmentTotalCost()
        {
            double totalCost = 0;
            foreach (MilUnitFC unit in units)
            {
                totalCost += unit.getTotalCost;
            }

            equipmentTotalCost = totalCost;
            return (int) equipmentTotalCost;
        }

        public void newSquad()
        {
            units = new List<MilUnitFC>();
            for (int sq = 0; sq < 30; sq++)
            {
                units.Add(FactionCache.FactionComp.militaryCustomizationUtil.blankUnit);
            }

            updateEquipmentTotalCost();
        }

        public void ChangeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
            costDirty = true;
        }

        public int getLatestChanged
        {
            get
            {
                int latestChange;
                latestChange = tickChanged;
                foreach (MilUnitFC unit in units)
                {
                    latestChange = Math.Max(unit.tickChanged, latestChange);
                }

                return latestChange;
            }
        }

        public void deleteSquad()
        {
            FactionCache.FactionComp.militaryCustomizationUtil.squads.Remove(this);
        }

        public string GetUniqueLoadID()
        {
            return $"MilSquadFC_{loadID}";
        }

    }
}