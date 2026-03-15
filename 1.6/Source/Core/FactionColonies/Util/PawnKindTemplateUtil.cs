using System.Collections.Generic;
using RimWorld;
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
    }
}
