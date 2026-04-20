using System;
using System.Collections.Generic;
using Verse;

namespace FactionColonies
{

    //Squad Class
    public class MilSquadFC : IExposable, ILoadReferenceable
    {
        public const int MaxSquadSize = 30;

        public int loadID = -1;
        public string name;
        private List<MilUnitFC> units = new List<MilUnitFC>();
        public IReadOnlyList<MilUnitFC> Units => units;
        public double equipmentTotalCost;
        public int tickChanged;

        public static void UpdateEquipmentTotalCostOfSquadsContaining(MilUnitFC unit)
        {
            FactionCache.FactionComp.militaryCustomizationUtil.squads.ForEach(delegate (MilSquadFC squad)
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
                SetLoadID();
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
                UpdateEquipmentTotalCost();
            }
        }

        public void SetLoadID()
        {
            loadID = FactionCache.FactionComp.NextSquadID;
        }

        private bool costDirty = true;

        public double GetEquipmentTotalCost()
        {
            if (costDirty)
            {
                UpdateEquipmentTotalCost();
                costDirty = false;
            }
            return equipmentTotalCost;
        }

        public int UpdateEquipmentTotalCost()
        {
            double totalCost = 0;
            foreach (MilUnitFC unit in units)
            {
                totalCost += unit.getTotalCost;
            }

            equipmentTotalCost = totalCost;
            return (int)equipmentTotalCost;
        }

        public void NewSquad()
        {
            units = new List<MilUnitFC>();
            for (int sq = 0; sq < MaxSquadSize; sq++)
            {
                units.Add(FactionCache.FactionComp.militaryCustomizationUtil.blankUnit);
            }

            UpdateEquipmentTotalCost();
        }

        public void ChangeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
            costDirty = true;
        }

        public void SetUnit(int index, MilUnitFC unit)
        {
            units[index] = unit;
            ChangeTick();
        }

        public void AddUnit(MilUnitFC unit)
        {
            units.Add(unit);
            ChangeTick();
        }

        public int FindUnitIndex(Predicate<MilUnitFC> predicate) => units.FindIndex(predicate);

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

        public void DeleteSquad()
        {
            FactionCache.FactionComp.militaryCustomizationUtil.squads.Remove(this);
        }

        public string GetUniqueLoadID()
        {
            return $"MilSquadFC_{loadID}";
        }

    }
}