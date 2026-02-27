using FactionColonies.util;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    [StaticConstructorOnStartup]
    public static class FactionColoniesMilitary
    {
        private static List<SavedUnitFC> savedUnits = new List<SavedUnitFC>();
        private static List<SavedSquadFC> savedSquads = new List<SavedSquadFC>();
        public static IEnumerable<SavedSquadFC> SavedSquads => savedSquads;
        public static IEnumerable<SavedUnitFC> SavedUnits => savedUnits;

        public static string EmpireConfigFolderPath;
        public static string EmpireMilitaryUnitFolder;
        public static string EmpireMilitarySquadFolder;

        static FactionColoniesMilitary()
        {
            EmpireConfigFolderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "Empire");
            EmpireMilitarySquadFolder = Path.Combine(EmpireConfigFolderPath, "Squads");
            EmpireMilitaryUnitFolder = Path.Combine(EmpireConfigFolderPath, "Units");
            if (!Directory.Exists(EmpireConfigFolderPath) ||
                !Directory.Exists(EmpireMilitarySquadFolder) ||
                !Directory.Exists(EmpireMilitaryUnitFolder))
            {
                Directory.CreateDirectory(EmpireConfigFolderPath);
                Directory.CreateDirectory(EmpireMilitarySquadFolder);
                Directory.CreateDirectory(EmpireMilitaryUnitFolder);
            }

            Read();
        }

        public static SavedSquadFC GetSquad(string name) => savedSquads.FirstOrFallback(s => s.name == name);
        public static SavedUnitFC GetUnit(string name) => savedUnits.FirstOrFallback(u => u.name == name);

        public static void RemoveSquad(string name)
        {
            savedSquads.RemoveAll(squad => squad.name == name);
            File.Delete(GetSquadPath(name));
        }
        
        public static void RemoveSquad(SavedSquadFC squad)
        {
            savedSquads.Remove(squad);
            File.Delete(GetSquadPath(squad.name));
        }

        public static void RemoveUnit(string name)
        {
            savedSquads.RemoveAll(unit => unit.name == name);
            File.Delete(GetUnitPath(name));
        }
        
        public static void RemoveUnit(SavedUnitFC unit)
        {
            savedUnits.Remove(unit);
            File.Delete(GetUnitPath(unit.name));
        }

        [DebugAction("Empire", "Reload Saved Military")]
        public static void Read()
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
                throw new Exception("Empire - Attempt to load saved military while scribe is active");
            
            savedSquads.Clear();
            savedUnits.Clear();
            foreach (string path in Directory.EnumerateFiles(EmpireMilitarySquadFolder))
            {
                try
                {
                    SavedSquadFC squad = new SavedSquadFC();
                    Scribe.loader.InitLoading(path);
                    squad.ExposeData();
                    savedSquads.Add(squad);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"Failed to load squad at path {path} due to exception: {e.Message}");
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
            }

            foreach (string path in Directory.EnumerateFiles(EmpireMilitaryUnitFolder))
            {
                try
                {
                    SavedUnitFC unit = new SavedUnitFC();
                    Scribe.loader.InitLoading(path);
                    unit.ExposeData();
                    savedUnits.Add(unit);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"Failed to load squad at path {path} due to exception: {e.Message}");
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
            }
        }

        public static string GetUnitPath(string name) => Path.Combine(EmpireMilitaryUnitFolder, $"{name}.xml");
        public static string GetSquadPath(string name) => Path.Combine(EmpireMilitarySquadFolder, $"{name}.xml");

        public static void SaveSquad(SavedSquadFC squad)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                throw new Exception("Empire - Attempt to save squad while scribe is active");
            }

            string path = GetSquadPath(squad.name);
            try
            {
                Scribe.saver.InitSaving(path, "squad");
                int version = 0;
                Scribe_Values.Look(ref version, "version");
                squad.ExposeData();
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed to save squad {squad.name} {e}");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }

            savedSquads.RemoveAll(s => s.name == squad.name);
            savedSquads.Add(squad);
        }

        public static void SaveUnit(SavedUnitFC unit)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                throw new Exception("Empire - Attempt to save unit while scribe is active");
            }

            string path = GetUnitPath(unit.name);
            try
            {
                Scribe.saver.InitSaving(path, "unit");
                int version = 0;
                Scribe_Values.Look(ref version, "version");
                unit.ExposeData();
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed to save unit {unit.name} {e}");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
            savedUnits.RemoveAll(u => u.name == unit.name);
            savedUnits.Add(unit);
        }

        public static void SaveAllUnits() => savedUnits.ForEach(SaveUnit);
        public static void SaveAllSquads() => savedSquads.ForEach(SaveSquad);
    }

    public class SavedUnitFC : IExposable
    {
        public string name;
        public bool isTrader;
        public bool isCivilian;
        public PawnKindDef animal;
        public PawnKindDef pawnKind;
        public List<SavedThing> weapons;
        public List<SavedThing> apparel;
        public XenotypeDef xenotype;

        public SavedUnitFC() {}

        public SavedUnitFC(MilUnitFC unit)
        {
            name = unit.name;
            weapons = new List<SavedThing>(unit.weapons);
            apparel = new List<SavedThing>(unit.apparel);
            isTrader = unit.isTrader;
            isCivilian = unit.isCivilian;
            animal = unit.animal;
            pawnKind = unit.pawnKind;
            xenotype = unit.xenotype;
        }

        public MilUnitFC CreateMilUnit()
        {
            PawnKindDef resolvedKind = pawnKind;
            if (pawnKind != null && !FactionCache.FactionComp.raceFilter.Allows(pawnKind.race))
            {
                resolvedKind = FactionCache.PlayerColonyFaction.RandomPawnKind();
            }

            MilUnitFC unit = new MilUnitFC(false)
            {
                name = name,
                isCivilian = isCivilian,
                isTrader = isTrader,
                animal = animal,
                pawnKind = resolvedKind,
                xenotype = xenotype,
                weapons = weapons?.Where(w => w.thing != null).ToList() ?? new List<SavedThing>(),
                apparel = apparel?.Where(a => a.thing != null).ToList() ?? new List<SavedThing>()
            };

            unit.changeTick();
            unit.updateEquipmentTotalCost();

            return unit;
        }

        public MilUnitFC Import()
        {
            FactionFC fc = FactionCache.FactionComp;
            MilUnitFC unit = this.CreateMilUnit();
            fc.militaryCustomizationUtil.units.Add(unit);
            return unit;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref isTrader, "isTrader");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Defs.Look(ref animal, "animal");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Deep);
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Deep);
        }
    }

    public class SavedSquadFC : IExposable
    {
        public string name;
        public List<SavedUnitFC> unitTemplates = new List<SavedUnitFC>();
        public List<int> units = new List<int>(30);
        public bool isTraderCaravan;
        public bool isCivilian;
        public SavedSquadFC() {}

        public SavedSquadFC(MilSquadFC squad)
        {
            name = squad.name;
            isTraderCaravan = squad.isTraderCaravan;
            isCivilian = squad.isCivilian;

            // Dont store blank units
            var squadTemplates = squad.units.Distinct().Where(u => !u.isBlank).ToList();
            
            unitTemplates = squadTemplates.Select(unit => new SavedUnitFC(unit)).ToList();
            units = squad.units.Select(unit => squadTemplates.IndexOf(unit)).ToList();
        }

        public MilSquadFC CreateMilSquad()
        {
            MilSquadFC squad = new MilSquadFC(true);
            squad.name = name;
            squad.isCivilian = isCivilian;
            squad.isTraderCaravan = isTraderCaravan;

            FactionFC fc = FactionCache.FactionComp;

            var milUnits = unitTemplates.Select(unit => unit.CreateMilUnit()).ToList();

            foreach (int i in units)
            {
                if(i == -1)
                    squad.units.Add(fc.militaryCustomizationUtil.blankUnit);
                else
                    squad.units.Add(milUnits[i]);
            }

            return squad;
        }
        public MilSquadFC Import()
        {
            FactionFC fc = FactionCache.FactionComp;
            MilSquadFC squad = this.CreateMilSquad();
            foreach (MilUnitFC unit in squad.units.Distinct().Where(unit => !unit.isBlank))
            {
                fc.militaryCustomizationUtil.units.Add(unit);
            }
            fc.militaryCustomizationUtil.squads.Add(squad);
            return squad;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Values.Look(ref isTraderCaravan, "isTraderCaravan");
            Scribe_Collections.Look(ref unitTemplates, "unitTemplates", LookMode.Deep);
            Scribe_Collections.Look(ref units, "units", LookMode.Value);
        }
    }
    
    public struct SavedThing : IExposable
    {
        public ThingDef thing;
        public ThingDef stuff;
        public QualityCategory? quality; // null = not specified (future feature)

        public SavedThing(Thing t)
        {
            thing = t.def;
            stuff = t.Stuff;
            quality = t.TryGetQuality(out QualityCategory q) ? q : (QualityCategory?)null;
        }

        public SavedThing(ThingDef thing, ThingDef stuff)
        {
            this.thing = thing;
            this.stuff = stuff;
            this.quality = null;
        }

        public Thing CreateThing()
        {
            if (thing == null) return null;
            Thing t = ThingMaker.MakeThing(thing, stuff);
            if (quality.HasValue)
                t.TryGetComp<CompQuality>()?.SetQuality(quality.Value, null);
            return t;
        }

        public float MarketValue =>
            thing != null ? StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff) : 0f;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thing, "thing");
            Scribe_Defs.Look(ref stuff, "stuff");
            // quality is nullable — save only if set
            QualityCategory qualityVal = quality ?? QualityCategory.Normal;
            bool hasQuality = quality.HasValue;
            Scribe_Values.Look(ref hasQuality, "hasQuality", false);
            if (hasQuality)
            {
                Scribe_Values.Look(ref qualityVal, "quality", QualityCategory.Normal);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                quality = hasQuality ? qualityVal : (QualityCategory?)null;
            }
        }
    }
}
