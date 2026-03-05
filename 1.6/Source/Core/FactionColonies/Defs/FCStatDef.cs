using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    public enum FCStatAggregation : byte
    {
        Additive,
        Multiplicative
    }

    /// <summary>
    /// Defines a named stat that policies, buildings, and events can modify.
    /// Referenced by defName in XML, resolved at load time — typos become XML errors at startup.
    /// </summary>
    public class FCStatDef : Def
    {
        /// <summary>
        /// The default/identity value when no modifiers apply.
        /// For additive stats this should be 0. For multiplicative stats this should be 1.
        /// </summary>
        public double defaultValue;

        /// <summary>
        /// How multiple modifiers combine: Additive sums values, Multiplicative multiplies them.
        /// </summary>
        public FCStatAggregation aggregation = FCStatAggregation.Additive;

        /// <summary>
        /// Whether this stat applies at the settlement level (propagated to settlements).
        /// If false, it's faction-level only.
        /// </summary>
        public bool appliesToSettlements = true;

        /// <summary>
        /// Translation key for description display (e.g., "FCTraitDesc_MilitaryLevel").
        /// </summary>
        public string descriptionKey;

        /// <summary>
        /// If true, lower values are "better" for UI coloring purposes (e.g., costs, losses).
        /// </summary>
        public bool invertedForDisplay;

        /// <summary>
        /// If non-null, this stat is a resource production stat linked to this ResourceTypeDef.
        /// Used for description formatting (resource name + icon instead of generic descriptionKey).
        /// </summary>
        public ResourceTypeDef linkedResource;
    }

    /// <summary>
    /// A single stat modifier entry. Appears in lists on FCPolicyDef, BuildingFCDef, FCEventDef, etc.
    /// </summary>
    public class FCStatModifier
    {
        public FCStatDef stat;
        public double value;

        /// <summary>
        /// Yields ConfigError strings for any stat modifier with a null stat reference (unresolved defName in XML).
        /// </summary>
        public static IEnumerable<string> ConfigErrors(List<FCStatModifier> modifiers, string ownerDefName)
        {
            if (modifiers == null) yield break;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].stat == null)
                    yield return $"{ownerDefName}: statModifiers[{i}] has null stat (unresolved defName?)";
            }
        }

        /// <summary>
        /// Builds a human-readable description string from a list of stat modifiers.
        /// Resource-linked stats use resource-specific translation keys; other stats use descriptionKey.
        /// </summary>
        public static TaggedString GetDescription(List<FCStatModifier> modifiers)
        {
            TaggedString desc = "";
            if (modifiers?.Count > 0)
            {
                foreach (FCStatModifier mod in modifiers)
                {
                    if (mod.stat == null) continue;

                    if (mod.stat.linkedResource != null)
                    {
                        if (mod.stat.aggregation == FCStatAggregation.Additive)
                            desc += "RTDproductionAdditive".Translate(TextUtil.colorizeAdditiveBonus(mod.value), mod.stat.linkedResource.LabelCap) + "\n";
                        else
                            desc += "RTDproductionMultiplier".Translate(TextUtil.colorizeMultiplierBonus(mod.value), mod.stat.linkedResource.LabelCap) + "\n";
                    }
                    else
                    {
                        if (mod.stat.descriptionKey.NullOrEmpty()) continue;
                        if (mod.stat.aggregation == FCStatAggregation.Additive)
                            desc += mod.stat.descriptionKey.Translate(TextUtil.colorizeAdditiveBonus(mod.value, mod.stat.invertedForDisplay)) + "\n";
                        else
                            desc += mod.stat.descriptionKey.Translate(TextUtil.colorizeMultiplierBonus(mod.value, mod.stat.invertedForDisplay)) + "\n";
                    }
                }
            }
            return desc.Trim();
        }
    }
}
