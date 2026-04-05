using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FactionColonies.util;
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

        [DebugAction("Empire", "Reload Saved Military", allowedGameStates = AllowedGameStates.Playing)]
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
                    LogUtil.Error($"Failed to load unit at path {path} due to exception: {e.Message}");
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
        public PawnKindDef animal;
        public PawnKindDef pawnKind;
        public List<SavedThing> weapons;
        public List<SavedThing> apparel;
        public XenotypeDef xenotype;
        public string customXenotypeName;
        public ThingDef preferredAmmo;

        // Set during load when defs fail to resolve (e.g. mod removed). Not serialized.
        public bool isDegraded;
        public List<string> missingDefs;

        public SavedUnitFC() { }

        public SavedUnitFC(MilUnitFC unit)
        {
            name = unit.name;
            weapons = new List<SavedThing>(unit.weapons);
            apparel = new List<SavedThing>(unit.apparel);
            animal = unit.animal;
            pawnKind = unit.pawnKind;
            xenotype = unit.xenotype;
            customXenotypeName = unit.customXenotypeName;
            preferredAmmo = unit.preferredAmmo;
        }

        public MilUnitFC CreateMilUnit()
        {
            PawnKindDef resolvedKind = pawnKind;
            if (pawnKind != null && FactionCache.FactionComp.xenotypeFilter.GetRaceWeight(pawnKind.race) <= 0)
            {
                resolvedKind = FactionCache.PlayerColonyFaction.RandomPawnKind();
            }

            if (resolvedKind == null)
            {
                resolvedKind = FactionCache.PlayerColonyFaction?.RandomPawnKind()
                    ?? PawnKindDefOf.Colonist;
                LogUtil.Warning($"Saved unit '{name}' has no pawnKind (mod removed?), "
                    + $"using {resolvedKind.defName}");
            }

            MilUnitFC unit = new MilUnitFC(false)
            {
                name = name,
                animal = animal,
                pawnKind = resolvedKind,
                xenotype = xenotype,
                customXenotypeName = customXenotypeName,
                preferredAmmo = preferredAmmo,
                weapons = weapons?.Where(w => w.thing != null).ToList() ?? new List<SavedThing>(),
                apparel = apparel?.Where(a => a.thing != null).ToList() ?? new List<SavedThing>()
            };

            unit.ChangeTick();
            unit.UpdateEquipmentTotalCost();

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
            // Capture XML parent before any collection loading can shift the cursor.
            XmlNode xmlParent = (Scribe.mode == LoadSaveMode.LoadingVars)
                ? Scribe.loader.curXmlParent : null;

            Scribe_Values.Look(ref name, "name");
            Scribe_Defs.Look(ref animal, "animal");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Defs.Look(ref xenotype, "xenotype");
            Scribe_Values.Look(ref customXenotypeName, "customXenotypeName");
            Scribe_Defs.Look(ref preferredAmmo, "preferredAmmo");
            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Deep);
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Deep);

            if (xmlParent != null)
            {
                ValidateAfterLoad(xmlParent);
            }
        }

        private void ValidateAfterLoad(XmlNode xmlParent)
        {
            List<string> missing = new List<string>();

            CheckDef(xmlParent, "pawnKind", pawnKind, missing);
            CheckDef(xmlParent, "animal", animal, missing);
            CheckDef(xmlParent, "xenotype", xenotype, missing);
            CheckDef(xmlParent, "preferredAmmo", preferredAmmo, missing);

            int nullWeapons = weapons?.Count(w => w.thing == null) ?? 0;
            int nullApparel = apparel?.Count(a => a.thing == null) ?? 0;
            if (nullWeapons > 0) missing.Add($"{nullWeapons} weapon(s)");
            if (nullApparel > 0) missing.Add($"{nullApparel} apparel item(s)");

            if (missing.Count > 0)
            {
                isDegraded = true;
                missingDefs = missing;
                LogUtil.Warning($"Saved unit '{name}' references missing defs (unloaded mod?): "
                    + string.Join(", ", missing));
            }
        }

        private static void CheckDef(XmlNode parent, string label, Def resolved, List<string> missing)
        {
            XmlNode node = parent?[label];
            if (node == null) return;
            string raw = node.InnerText;
            if (string.IsNullOrEmpty(raw) || raw == "null") return;
            if (resolved == null)
            {
                missing.Add($"{label}={raw}");
            }
        }
    }

    public class SavedSquadFC : IExposable
    {
        public string name;
        public List<SavedUnitFC> unitTemplates = new List<SavedUnitFC>();
        public List<int> units = new List<int>(30);
        public bool IsDegraded => unitTemplates != null && unitTemplates.Any(u => u.isDegraded);

        public SavedSquadFC() { }

        public SavedSquadFC(MilSquadFC squad)
        {
            name = squad.name;

            // Dont store blank units
            var squadTemplates = squad.units.Distinct().Where(u => !u.isBlank).ToList();

            unitTemplates = squadTemplates.Select(unit => new SavedUnitFC(unit)).ToList();
            units = squad.units.Select(unit => squadTemplates.IndexOf(unit)).ToList();
        }

        public MilSquadFC CreateMilSquad()
        {
            MilSquadFC squad = new MilSquadFC(true);
            squad.name = name;

            FactionFC fc = FactionCache.FactionComp;

            var milUnits = unitTemplates.Select(unit => unit.CreateMilUnit()).ToList();

            foreach (int i in units)
            {
                if (i == -1)
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
            Scribe_Collections.Look(ref unitTemplates, "unitTemplates", LookMode.Deep);
            Scribe_Collections.Look(ref units, "units", LookMode.Value);
        }
    }

    public struct SavedThing : IExposable
    {
        public ThingDef thing;
        public ThingDef stuff;
        public QualityCategory? quality; // null = not specified (future feature)
        public Color color;
        public bool hasColor;

        public SavedThing(Thing t)
        {
            thing = t.def;
            stuff = t.Stuff;
            quality = t.TryGetQuality(out QualityCategory q) ? q : (QualityCategory?)null;
            color = Color.white;
            hasColor = false;
        }

        public SavedThing(ThingDef thing, ThingDef stuff)
        {
            this.thing = thing;
            this.stuff = stuff;
            this.quality = null;
            this.color = Color.white;
            this.hasColor = false;
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
            thing != null ? CraftUtil.ThingValue(thing, stuff, quality ?? QualityCategory.Normal) : 0f;

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

            // Apparel color override
            Scribe_Values.Look(ref hasColor, "hasColor", false);
            if (hasColor)
            {
                Scribe_Values.Look(ref color, "color", Color.white);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars && !hasColor)
            {
                color = Color.white;
            }
        }
    }
}
