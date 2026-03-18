using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies.util
{
    /// <summary>
    /// Manages cloning of Empire's template PawnKindDefs for each enabled race and tech level.
    /// Templates are defined in XML with race=Human and cloned at runtime with the target race set.
    /// </summary>
    static class PawnKindTemplateUtil
    {
        private static Dictionary<RaceTechKey, List<PawnKindDef>> cloneCache = new Dictionary<RaceTechKey, List<PawnKindDef>>();
        private static Dictionary<ThingDef, RaceGearData> raceGearCache = new Dictionary<ThingDef, RaceGearData>();

        private struct RaceGearData
        {
            public List<string> apparelTags;
            public List<string> weaponTags;
            public FloatRange apparelMoney;
            public FloatRange weaponMoney;
        }

        private struct RaceTechKey
        {
            public ThingDef race;
            public TechLevel techLevel;

            public RaceTechKey(ThingDef race, TechLevel techLevel)
            {
                this.race = race;
                this.techLevel = techLevel;
            }

            public override int GetHashCode()
            {
                return Gen.HashCombineInt(race.GetHashCode(), (int)techLevel);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is RaceTechKey other)) return false;
                return race == other.race && techLevel == other.techLevel;
            }
        }

        private static PawnKindDef[] Templates
        {
            get
            {
                return new PawnKindDef[]
                {
                    PColonyPawnKindDefOf.PColony_Fighter,
                    PColonyPawnKindDefOf.PColony_Elite,
                    PColonyPawnKindDefOf.PColony_Leader,
                    PColonyPawnKindDefOf.PColony_Trader,
                    PColonyPawnKindDefOf.PColony_Guard,
                    PColonyPawnKindDefOf.PColony_Villager
                };
            }
        }

        /// <summary>
        /// Gets or creates cloned template PawnKindDefs for the given race and tech level.
        /// Clones are cached and reused until <see cref="InvalidateCache"/> is called.
        /// </summary>
        public static List<PawnKindDef> GetOrCreateClonesForRace(ThingDef race, TechLevel techLevel)
        {
            RaceTechKey key = new RaceTechKey(race, techLevel);
            if (cloneCache.TryGetValue(key, out List<PawnKindDef> cached))
            {
                return cached;
            }

            List<PawnKindDef> clones = new List<PawnKindDef>();
            foreach (PawnKindDef template in Templates)
            {
                PawnKindDef clone = template.ShallowClone();
                clone.race = race;
                clone.defName = template.defName + "_" + race.defName;
                clone.xenotypeSet = null;
                clone.useFactionXenotypes = false;

                // Copy tag lists so we can mutate them without affecting the template
                if (template.weaponTags != null)
                    clone.weaponTags = new List<string>(template.weaponTags);
                if (template.apparelTags != null)
                    clone.apparelTags = new List<string>(template.apparelTags);

                ApplyTechLevelScaling(clone, techLevel);

                if (race != ThingDefOf.Human)
                {
                    ApplyRaceGearOverrides(clone, race);
                }

                clones.Add(clone);
            }

            cloneCache[key] = clones;
            return clones;
        }

        /// <summary>
        /// Returns the Fighter template clone for a given race at the current Empire tech level.
        /// </summary>
        public static PawnKindDef GetFighterForRace(ThingDef race)
        {
            TechLevel techLevel = FactionCache.FactionComp != null ? FactionCache.FactionComp.techLevel : TechLevel.Industrial;
            List<PawnKindDef> clones = GetOrCreateClonesForRace(race, techLevel);
            // Fighter is the first template in the array
            return clones.Count > 0 ? clones[0] : PColonyPawnKindDefOf.PColony_Fighter;
        }

        /// <summary>
        /// Clears the clone cache. Must be called when race weights or tech level change.
        /// </summary>
        public static void InvalidateCache()
        {
            cloneCache.Clear();
            raceGearCache.Clear();
        }

        /// <summary>
        /// Adjusts weapon/apparel money and tags on a cloned PawnKindDef based on tech level.
        /// Templates are defined at Industrial level; this scales relative to that baseline.
        /// </summary>
        private static void ApplyTechLevelScaling(PawnKindDef clone, TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Neolithic:
                    ScaleMoneyRanges(clone, 0.3f, 0.4f);
                    ReplaceWeaponTags(clone, new List<string> { "NeolithicMeleeBasic", "NeolithicRangedBasic" });
                    ReplaceApparelTags(clone, new List<string> { "Neolithic" });
                    break;

                case TechLevel.Medieval:
                    ScaleMoneyRanges(clone, 0.6f, 0.6f);
                    ReplaceWeaponTags(clone, new List<string> { "MedievalMeleeBasic", "MedievalMeleeDecent" });
                    ReplaceApparelTags(clone, new List<string> { "IndustrialBasic" });
                    break;

                case TechLevel.Industrial:
                    // Base level — no changes needed
                    break;

                case TechLevel.Spacer:
                    ScaleMoneyRanges(clone, 1.5f, 1.4f);
                    AddTagIfAbsent(clone.weaponTags, "SpacerGun");
                    AddTagIfAbsent(clone.apparelTags, "SpacerMilitary");
                    break;

                case TechLevel.Ultra:
                case TechLevel.Archotech:
                    ScaleMoneyRanges(clone, 2.0f, 1.8f);
                    AddTagIfAbsent(clone.weaponTags, "SpacerGun");
                    AddTagIfAbsent(clone.weaponTags, "UltratechMelee");
                    AddTagIfAbsent(clone.apparelTags, "SpacerMilitary");
                    break;
            }
        }

        private static void ScaleMoneyRanges(PawnKindDef clone, float weaponMult, float apparelMult)
        {
            clone.weaponMoney = new FloatRange(clone.weaponMoney.min * weaponMult, clone.weaponMoney.max * weaponMult);
            clone.apparelMoney = new FloatRange(clone.apparelMoney.min * apparelMult, clone.apparelMoney.max * apparelMult);
        }

        private static void ReplaceWeaponTags(PawnKindDef clone, List<string> newTags)
        {
            if (clone.weaponTags == null)
                clone.weaponTags = new List<string>();
            else
                clone.weaponTags.Clear();
            clone.weaponTags.AddRange(newTags);
        }

        private static void ReplaceApparelTags(PawnKindDef clone, List<string> newTags)
        {
            if (clone.apparelTags == null)
                clone.apparelTags = new List<string>();
            else
                clone.apparelTags.Clear();
            clone.apparelTags.AddRange(newTags);
        }

        private static void AddTagIfAbsent(List<string> tags, string tag)
        {
            if (tags != null && !tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        /// <summary>
        /// For non-Human HAR races, replaces the clone's apparel/weapon tags with tags harvested
        /// from the race's own PawnKindDefs, and floor-clamps budgets to ensure the race's gear is affordable.
        /// </summary>
        private static void ApplyRaceGearOverrides(PawnKindDef clone, ThingDef race)
        {
            RaceGearData gearData = GetOrHarvestRaceGearData(race);

            // Replace apparel tags with the race's own tags so PawnApparelGenerator can find matching apparel.
            // If no tags were found, set to null so the generator skips tag filtering entirely
            // and lets HAR's race restrictions handle it.
            if (gearData.apparelTags != null && gearData.apparelTags.Count > 0)
            {
                clone.apparelTags = new List<string>(gearData.apparelTags);
            }
            else
            {
                clone.apparelTags = null;
            }

            // Replace weapon tags only if the race defines its own; otherwise keep Empire's tags
            if (gearData.weaponTags != null && gearData.weaponTags.Count > 0)
            {
                clone.weaponTags = new List<string>(gearData.weaponTags);
            }

            // Floor-clamp budgets: take the max of Empire's (possibly tech-scaled) budget and the race's average.
            // This ensures alien gear (often more expensive) is affordable while preserving tech-level scaling boosts.
            clone.apparelMoney = new FloatRange(
                Math.Max(clone.apparelMoney.min, gearData.apparelMoney.min),
                Math.Max(clone.apparelMoney.max, gearData.apparelMoney.max));
            clone.weaponMoney = new FloatRange(
                Math.Max(clone.weaponMoney.min, gearData.weaponMoney.min),
                Math.Max(clone.weaponMoney.max, gearData.weaponMoney.max));
        }

        /// <summary>
        /// Scans all PawnKindDefs for the given race and collects their apparel/weapon tags and budget ranges.
        /// Results are cached per race.
        /// </summary>
        private static RaceGearData GetOrHarvestRaceGearData(ThingDef race)
        {
            if (raceGearCache.TryGetValue(race, out RaceGearData cached))
            {
                return cached;
            }

            HashSet<string> apparelTags = new HashSet<string>();
            HashSet<string> weaponTags = new HashSet<string>();
            float apparelMoneyMinSum = 0f, apparelMoneyMaxSum = 0f;
            float weaponMoneyMinSum = 0f, weaponMoneyMaxSum = 0f;
            int apparelBudgetCount = 0;
            int weaponBudgetCount = 0;

            foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (def.race != race) continue;
                // Skip Empire's own clones to avoid circular contamination
                if (def.defName.StartsWith("PColony_")) continue;

                if (def.apparelTags != null)
                {
                    foreach (string tag in def.apparelTags)
                    {
                        apparelTags.Add(tag);
                    }
                }
                if (def.weaponTags != null)
                {
                    foreach (string tag in def.weaponTags)
                    {
                        weaponTags.Add(tag);
                    }
                }

                if (def.apparelMoney.max > 0)
                {
                    apparelMoneyMinSum += def.apparelMoney.min;
                    apparelMoneyMaxSum += def.apparelMoney.max;
                    apparelBudgetCount++;
                }
                if (def.weaponMoney.max > 0)
                {
                    weaponMoneyMinSum += def.weaponMoney.min;
                    weaponMoneyMaxSum += def.weaponMoney.max;
                    weaponBudgetCount++;
                }
            }

            RaceGearData data = new RaceGearData
            {
                apparelTags = apparelTags.Count > 0 ? apparelTags.ToList() : null,
                weaponTags = weaponTags.Count > 0 ? weaponTags.ToList() : null,
                apparelMoney = apparelBudgetCount > 0
                    ? new FloatRange(apparelMoneyMinSum / apparelBudgetCount, apparelMoneyMaxSum / apparelBudgetCount)
                    : new FloatRange(0, 0),
                weaponMoney = weaponBudgetCount > 0
                    ? new FloatRange(weaponMoneyMinSum / weaponBudgetCount, weaponMoneyMaxSum / weaponBudgetCount)
                    : new FloatRange(0, 0)
            };

            raceGearCache[race] = data;
            return data;
        }
    }
}
