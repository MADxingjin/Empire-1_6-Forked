using RimWorld;
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
        /// The identity value for this stat's aggregation: 0 for Additive, 1 for Multiplicative.
        /// </summary>
        public double IdentityValue => aggregation == FCStatAggregation.Multiplicative ? 1.0 : 0.0;

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

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string err in base.ConfigErrors())
                yield return err;

            if (linkedResource != null)
            {
                if (linkedResource.productionAdditiveStat == this && aggregation != FCStatAggregation.Additive)
                    yield return defName + ": linked as productionAdditiveStat on " + linkedResource.defName + " but aggregation is not Additive";
                if (linkedResource.productionMultiplierStat == this && aggregation != FCStatAggregation.Multiplicative)
                    yield return defName + ": linked as productionMultiplierStat on " + linkedResource.defName + " but aggregation is not Multiplicative";
            }
        }
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
        /// Returns true if this modifier represents a beneficial effect.
        /// Accounts for inverted stats where lower values are better.
        /// </summary>
        public bool IsBeneficial()
        {
            if (stat == null) return false;
            if (stat.aggregation == FCStatAggregation.Multiplicative)
                return stat.invertedForDisplay ? value < 1.0 : value > 1.0;
            return stat.invertedForDisplay ? value < 0.0 : value > 0.0;
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
                            desc += "RTDproductionAdditive".Translate(TextUtil.ColorizeAdditiveBonus(mod.value), mod.stat.linkedResource.LabelCap) + "\n";
                        else
                            desc += "RTDproductionMultiplier".Translate(TextUtil.ColorizeMultiplierBonus(mod.value), mod.stat.linkedResource.LabelCap) + "\n";
                    }
                    else
                    {
                        if (mod.stat.descriptionKey.NullOrEmpty()) continue;
                        if (mod.stat.aggregation == FCStatAggregation.Additive)
                            desc += mod.stat.descriptionKey.Translate(TextUtil.ColorizeAdditiveBonus(mod.value, mod.stat.invertedForDisplay)) + "\n";
                        else
                            desc += mod.stat.descriptionKey.Translate(TextUtil.ColorizeMultiplierBonus(mod.value, mod.stat.invertedForDisplay)) + "\n";
                    }
                }
            }
            return desc.Trim();
        }
    }

    [DefOf]
    public class FCStatDefOf
    {
        // ── Military ──────────────────────────────────────────────
        public static FCStatDef militaryBaseLevel;
        public static FCStatDef militaryCombatEfficiency;
        public static FCStatDef militaryLevelBonusDefending;
        public static FCStatDef militaryLevelBonusAttacking;
        public static FCStatDef militaryEfficiencyBonusAttacking;
        public static FCStatDef militaryEfficiencyBonusDefending;
        public static FCStatDef militaryCooldownOffset;
        public static FCStatDef raidCooldownOffset;
        public static FCStatDef deadPawnCooldownOffset;

        // ── Threat Scaling ───────────────────────────────────────
        public static FCStatDef threatScalingBase;
        public static FCStatDef threatScalingMultiplier;

        // ── Battle Penalties ──────────────────────────────────────
        public static FCStatDef battleProsperityLossMultiplier;
        public static FCStatDef battleHappinessLossMultiplier;
        public static FCStatDef battleLoyaltyLossMultiplier;

        // ── Economy ───────────────────────────────────────────────
        public static FCStatDef taxBasePercentage;
        public static FCStatDef taxBaseRandomModifier;
        public static FCStatDef taxBonusFlat;
        public static FCStatDef titheValueMultiplier;
        public static FCStatDef lootMultiplier;
        public static FCStatDef settlementCostMultiplier;
        public static FCStatDef buildTimeMultiplier;
        public static FCStatDef createSettlementBaseCost;
        public static FCStatDef createSettlementMultiplier;
        public static FCStatDef researchContributionMultiplier;

        // ── Workers ───────────────────────────────────────────────
        public static FCStatDef workerBaseCost;
        public static FCStatDef workerBaseMax;
        public static FCStatDef workerBaseOverMax;
        public static FCStatDef extraWorkersSoftcap;
        public static FCStatDef overMaxWorkersAdjustment;

        // ── Prosperity ────────────────────────────────────────────
        public static FCStatDef prosperityBaseRecovery;
        public static FCStatDef prosperityLostBase;

        // ── Happiness (base) ──────────────────────────────────────
        public static FCStatDef happinessLostBase;
        public static FCStatDef happinessGainedBase;

        // ── Happiness (multipliers) ───────────────────────────────
        public static FCStatDef happinessLostMultiplier;
        public static FCStatDef happinessGainedMultiplier;

        // ── Loyalty (base) ────────────────────────────────────────
        public static FCStatDef loyaltyLostBase;
        public static FCStatDef loyaltyGainedBase;

        // ── Loyalty (multipliers) ─────────────────────────────────
        public static FCStatDef loyaltyLostMultiplier;
        public static FCStatDef loyaltyGainedMultiplier;

        // ── Unrest (base) ─────────────────────────────────────────
        public static FCStatDef unrestLostBase;
        public static FCStatDef unrestGainedBase;

        // ── Unrest (multipliers) ──────────────────────────────────
        public static FCStatDef unrestLostMultiplier;
        public static FCStatDef unrestGainedMultiplier;

        static FCStatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCStatDefOf));
        }
    }
}
