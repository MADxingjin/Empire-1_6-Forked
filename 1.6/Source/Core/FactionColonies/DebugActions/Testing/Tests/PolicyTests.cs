using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{
    public static class PolicyTestHelper
    {
        /// <summary>
        /// Snapshots the current faction policies and traits so they can be restored after a test.
        /// </summary>
        public static (List<FCPolicy> policies, List<FCPolicy> traits) SnapshotPolicies(FactionFC faction)
        {
            var savedPolicies = new List<FCPolicy>(faction.policies);
            var savedTraits = new List<FCPolicy>(faction.factionTraits);
            return (savedPolicies, savedTraits);
        }

        /// <summary>
        /// Restores faction policies and traits from a snapshot, calling OnRemoved on current behaviors.
        /// </summary>
        public static void RestorePolicies(FactionFC faction,
            (List<FCPolicy> policies, List<FCPolicy> traits) snapshot)
        {
            faction.RemoveAllPolicies(faction.policies);
            foreach (FCPolicy p in snapshot.policies)
                faction.policies.Add(p);

            for (int i = 0; i < faction.factionTraits.Count; i++)
            {
                if (faction.factionTraits[i]?.behavior != null)
                {
                    try { faction.factionTraits[i].behavior.OnRemoved(faction); }
                    catch (Exception ex) { LogUtil.Warning($"OnRemoved threw during test cleanup: {ex}"); }
                }
            }
            faction.factionTraits.Clear();
            foreach (FCPolicy t in snapshot.traits)
                faction.factionTraits.Add(t);

            faction.RebuildBehaviorCache();
        }

        /// <summary>
        /// Enacts a policy on the faction, adding it to the policies list and rebuilding the cache.
        /// </summary>
        public static FCPolicy EnactPolicy(FactionFC faction, FCPolicyDef def)
        {
            var policy = new FCPolicy(def);
            faction.policies.Add(policy);
            faction.RebuildBehaviorCache();
            return policy;
        }

        /// <summary>
        /// Sets a trait on the faction in the given slot, calling OnRemoved on any existing behavior.
        /// </summary>
        public static FCPolicy SetTrait(FactionFC faction, FCPolicyDef def, int slot)
        {
            if (faction.factionTraits[slot]?.behavior != null)
            {
                try { faction.factionTraits[slot].behavior.OnRemoved(faction); }
                catch (Exception ex) { LogUtil.Warning($"OnRemoved threw during test cleanup: {ex}"); }
            }
            var trait = new FCPolicy(def);
            faction.factionTraits[slot] = trait;
            faction.RebuildBehaviorCache();
            return trait;
        }

        /// <summary>
        /// Clears all policies and sets all traits to empty, rebuilding cache.
        /// </summary>
        public static void ClearAll(FactionFC faction)
        {
            faction.RemoveAllPolicies(faction.policies);
            for (int i = 0; i < faction.factionTraits.Count; i++)
            {
                if (faction.factionTraits[i]?.behavior != null)
                {
                    try { faction.factionTraits[i].behavior.OnRemoved(faction); }
                    catch (Exception ex) { LogUtil.Warning($"OnRemoved threw during test cleanup: {ex}"); }
                }
                faction.factionTraits[i] = new FCPolicy(FCPolicyDefOf.empty);
            }
            faction.RebuildBehaviorCache();
        }
    }

    public static class PolicyTests
    {
        private static FactionFC GetFaction()
        {
            return FactionCache.FactionComp;
        }

        // ============================
        // Stat Aggregation Tests
        // ============================

        [EmpireTest("Policy")]
        public static void StatAggregation_AdditiveDefault_IsZero()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                // taxBonusFlat is additive with IdentityValue 0
                double val = faction.GetFactionStatValue(FCStatDefOf.taxBonusFlat);
                TestAssert.AreEqual(0.0, val, 0.001, "Additive stat with no policies should be 0");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void StatAggregation_MultiplicativeDefault_IsOne()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                // happinessGainedMultiplier is multiplicative with IdentityValue 1
                double val = faction.GetFactionStatValue(FCStatDefOf.happinessGainedMultiplier);
                TestAssert.AreEqual(1.0, val, 0.001, "Multiplicative stat with no policies should be 1");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void StatAggregation_PolicyModifiers_Applied()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);

                // Find a policy with known statModifiers
                FCPolicyDef testDef = FCPolicyDefOf.militaristic;
                if (testDef.statModifiers.Count == 0)
                    TestAssert.Skip("militaristic has no XML stat modifiers");

                PolicyTestHelper.EnactPolicy(faction, testDef);

                // Check each stat modifier is reflected
                foreach (FCStatModifier mod in testDef.statModifiers)
                {
                    double expected;
                    if (mod.stat.aggregation == FCStatAggregation.Additive)
                        expected = mod.stat.IdentityValue + mod.value;
                    else
                        expected = mod.stat.IdentityValue * mod.value;

                    double actual = faction.GetFactionStatValue(mod.stat);
                    TestAssert.AreEqual(expected, actual, 0.001,
                        $"Stat {mod.stat.defName} should reflect policy modifier");
                }
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void StatAggregation_MultiplePolicies_Stack()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);

                // Find two policies that share an additive stat
                FCStatDef sharedStat = FCStatDefOf.militaryBaseLevel;
                var defs = DefDatabase<FCPolicyDef>.AllDefsListForReading
                    .Where(d => d.statModifiers.Any(m => m.stat == sharedStat
                        && sharedStat.aggregation == FCStatAggregation.Additive))
                    .Take(2).ToList();

                if (defs.Count < 2)
                    TestAssert.Skip("Not enough policies modifying militaryBaseLevel");

                PolicyTestHelper.EnactPolicy(faction, defs[0]);
                PolicyTestHelper.EnactPolicy(faction, defs[1]);

                double mod0 = defs[0].statModifiers.First(m => m.stat == sharedStat).value;
                double mod1 = defs[1].statModifiers.First(m => m.stat == sharedStat).value;
                double expected = sharedStat.IdentityValue + mod0 + mod1;

                double actual = faction.GetFactionStatValue(sharedStat);
                TestAssert.AreEqual(expected, actual, 0.001,
                    $"Two additive policy modifiers should stack: {mod0} + {mod1}");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void StatAggregation_BehaviorModifyStat_AppliedAfterStatic()
        {
            var faction = GetFaction();
            if (faction == null || faction.settlements.Count == 0)
                TestAssert.Skip("No faction/settlements");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.egalitarian);

                WorldSettlementFC settlement = faction.settlements.First();
                // Egalitarian adds happiness/10 to taxBonusFlat via ModifyStat
                double expected = Math.Floor(settlement.happiness / 10);
                double baseValue = faction.GetFactionStatValue(FCStatDefOf.taxBonusFlat);
                double fullValue = faction.GetStatValue(FCStatDefOf.taxBonusFlat, settlement);
                // The behavior adds on top of the base faction stat + settlement stat
                TestAssert.IsTrue(fullValue >= baseValue + expected - 1,
                    $"Egalitarian ModifyStat should add ~{expected} to taxBonusFlat (got base={baseValue}, full={fullValue})");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        // ============================
        // Behavior Lifecycle Tests
        // ============================

        [EmpireTest("Policy")]
        public static void BehaviorCache_RebuildAfterPolicyAdd()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                TestAssert.AreEqual(0, faction.cachedBehaviors.Count, "After clearing, cachedBehaviors should be empty");

                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.militaristic);
                TestAssert.IsTrue(faction.cachedBehaviors.Count > 0,
                    "After enacting policy with behavior, cachedBehaviors should be non-empty");
                TestAssert.IsTrue(faction.cachedBehaviors[0] is FCPolicyBehavior_Militaristic,
                    "Cached behavior should be Militaristic instance");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void BehaviorCache_RebuildAfterClear()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.militaristic);
                TestAssert.IsTrue(faction.cachedBehaviors.Count > 0, "Should have behaviors after enacting");

                PolicyTestHelper.ClearAll(faction);
                TestAssert.AreEqual(0, faction.cachedBehaviors.Count,
                    "After clearing all, cachedBehaviors should be empty");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void BehaviorCache_OrderPoliciesBeforeTraits()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);

                // Add a trait with behavior first, then a policy with behavior
                PolicyTestHelper.SetTrait(faction, FCPolicyDefOf.mercantile, 0);
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.militaristic);

                TestAssert.IsTrue(faction.cachedBehaviors.Count >= 2,
                    "Should have at least 2 behaviors");

                // Policies should come before traits in the cache
                TestAssert.IsTrue(faction.cachedBehaviors[0] is FCPolicyBehavior_Militaristic,
                    "First cached behavior should be from policy (Militaristic), not trait");
                TestAssert.IsTrue(faction.cachedBehaviors[1] is FCPolicyBehavior_Mercantile,
                    "Second cached behavior should be from trait (Mercantile)");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        // ============================
        // CooldownAbility Tests
        // ============================

        [EmpireTest("Policy")]
        public static void Cooldown_InitiallyReady()
        {
            var cd = new CooldownAbility { cooldownTicks = GenDate.TicksPerDay * 5 };
            TestAssert.IsTrue(cd.IsReady, "New CooldownAbility should be ready (tickLastUsed=-1)");
            TestAssert.AreEqual(0f, cd.DaysRemaining, 0.001, "DaysRemaining should be 0 when ready");
        }

        [EmpireTest("Policy")]
        public static void Cooldown_AfterUse_NotReady()
        {
            var cd = new CooldownAbility { cooldownTicks = GenDate.TicksPerDay * 5 };
            cd.Use();
            TestAssert.IsFalse(cd.IsReady, "After Use(), CooldownAbility should not be ready");
            TestAssert.GreaterThan(cd.DaysRemaining, 0, "DaysRemaining should be > 0 after Use()");
        }

        [EmpireTest("Policy")]
        public static void Cooldown_DaysRemaining_Correct()
        {
            var cd = new CooldownAbility { cooldownTicks = GenDate.TicksPerDay * 5 };
            cd.Use();
            // DaysRemaining should be approximately 5 days (may be slightly less due to tick timing)
            TestAssert.LessThanOrEqual(cd.DaysRemaining, 5.01,
                $"DaysRemaining should be <= 5.01, got {cd.DaysRemaining}");
            TestAssert.GreaterThan(cd.DaysRemaining, 4.9,
                $"DaysRemaining should be > 4.9, got {cd.DaysRemaining}");
        }

        // ============================
        // Incompatibility Tests
        // ============================

        [EmpireTest("Policy")]
        public static void Incompatible_MutualExclusionDefined()
        {
            // Verify that incompatible policies are defined mutually (A blocks B implies B blocks A)
            // Only enforced within the same category — edict→core blocks are one-directional
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                foreach (FCPolicyDef blocked in def.incompatiblePolicies)
                {
                    if (def.category != blocked.category) continue;
                    TestAssert.IsTrue(blocked.incompatiblePolicies.Contains(def),
                        $"{def.defName} blocks {blocked.defName} but not vice versa — incompatibility should be mutual");
                }
            }
        }

        // ============================
        // Specific Behavior Tests
        // ============================

        [EmpireTest("Policy")]
        public static void Egalitarian_HappinessTaxBonus_ScalesWithHappiness()
        {
            var faction = GetFaction();
            if (faction == null || faction.settlements.Count == 0)
                TestAssert.Skip("No faction/settlements");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.egalitarian);

                WorldSettlementFC settlement = faction.settlements.First();
                double happiness = settlement.happiness;
                double expectedBonus = Math.Floor(happiness / 10);

                // Get the stat value — it includes behavior ModifyStat
                double fullValue = faction.GetStatValue(FCStatDefOf.taxBonusFlat, settlement);

                // The settlement's own base stat value
                double settlementBase = settlement.GetSettlementStatValue(FCStatDefOf.taxBonusFlat);
                double factionBase = faction.GetFactionStatValue(FCStatDefOf.taxBonusFlat);

                // fullValue should be base + expectedBonus
                double baseNoPolicy = settlementBase + factionBase;
                double diff = fullValue - baseNoPolicy;
                TestAssert.AreEqual(expectedBonus, diff, 1.0,
                    $"Egalitarian tax bonus should be ~floor(happiness={happiness}/10)={expectedBonus}, got diff={diff}");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void Expansionist_FirstSettlement_Free()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            // This test only works when there are no settlements and no caravans
            if (faction.settlements.Any() || faction.settlementCaravansList.Any())
                TestAssert.Skip("Cannot test first-settlement-free with existing settlements");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.expansionist);

                double costMult = faction.GetStatValue(FCStatDefOf.settlementCostMultiplier);
                TestAssert.AreEqual(0.0, costMult, 0.001,
                    "Expansionist should make first settlement free (cost multiplier = 0)");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void Expansionist_FeeReduction_50Pct()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");
            if (!faction.settlements.Any())
                TestAssert.Skip("Need existing settlements for this test");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.expansionist);

                // With existing settlements and cooldown ready, should be 50% of default (1.0)
                double costMult = faction.GetStatValue(FCStatDefOf.settlementCostMultiplier);
                // Default is 1.0 (multiplicative), expansionist halves it
                TestAssert.AreEqual(0.5, costMult, 0.01,
                    $"Expansionist should give 50% discount (got {costMult})");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void Expansionist_FeeReduction_UsesOnPay()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");
            if (!faction.settlements.Any())
                TestAssert.Skip("Need existing settlements for this test");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                var policy = PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.expansionist);

                // Cooldown should start ready
                var behavior = policy.behavior as FCPolicyBehavior_Expansionist;
                TestAssert.IsNotNull(behavior, "Expansionist should have a behavior");

                // Trigger the cost paid hook
                behavior.OnSettlementCostPaid(faction);

                // After payment, cost multiplier should no longer give 50% discount
                double costMult = faction.GetStatValue(FCStatDefOf.settlementCostMultiplier);
                TestAssert.AreEqual(1.0, costMult, 0.01,
                    $"After fee reduction used, cost multiplier should be 1.0 (got {costMult})");
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        [EmpireTest("Policy")]
        public static void Militaristic_BuildingUpkeep_Discount()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");
            if (!faction.settlements.Any()) TestAssert.Skip("Need settlements");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                PolicyTestHelper.ClearAll(faction);
                var policy = PolicyTestHelper.EnactPolicy(faction, FCPolicyDefOf.militaristic);

                var behavior = policy.behavior as FCPolicyBehavior_Militaristic;
                TestAssert.IsNotNull(behavior, "Militaristic should have a behavior");

                // Find a military building (one with militaryBaseLevel or militaryCombatEfficiency stat)
                BuildingFCDef milBuilding = DefDatabase<BuildingFCDef>.AllDefsListForReading
                    .FirstOrDefault(b => b.statModifiers.Any(m =>
                        m.stat == FCStatDefOf.militaryBaseLevel || m.stat == FCStatDefOf.militaryCombatEfficiency));

                if (milBuilding == null)
                    TestAssert.Skip("No military buildings found");

                double baseUpkeep = 200;
                double modified = behavior.ModifyBuildingUpkeep(milBuilding, baseUpkeep, faction.settlements.First());
                TestAssert.AreEqual(Math.Max(baseUpkeep - 100, 0), modified, 0.001,
                    $"Military building upkeep should be discounted by 100 (from {baseUpkeep} to {modified})");

                // Non-military building should not be discounted
                BuildingFCDef civBuilding = DefDatabase<BuildingFCDef>.AllDefsListForReading
                    .FirstOrDefault(b => !b.statModifiers.Any(m =>
                        m.stat == FCStatDefOf.militaryBaseLevel || m.stat == FCStatDefOf.militaryCombatEfficiency)
                        && b != BuildingFCDefOf.Empty && b != BuildingFCDefOf.Construction);

                if (civBuilding != null)
                {
                    double civModified = behavior.ModifyBuildingUpkeep(civBuilding, baseUpkeep, faction.settlements.First());
                    TestAssert.AreEqual(baseUpkeep, civModified, 0.001,
                        "Non-military building upkeep should not be discounted");
                }
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }

        // ============================
        // ConfigError Validation
        // ============================

        [EmpireTest("Policy")]
        public static void AllPolicies_NoBehaviorClassErrors()
        {
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                FCPolicyBehaviorExtension ext = def.BehaviorExtension;
                if (ext != null)
                {
                    TestAssert.IsNotNull(ext.behaviorClass,
                        $"{def.defName}: behavior extension has null behaviorClass");
                    TestAssert.IsTrue(typeof(FCPolicyBehavior).IsAssignableFrom(ext.behaviorClass),
                        $"{def.defName}: behaviorClass {ext.behaviorClass.Name} must inherit FCPolicyBehavior");
                }
            }
        }

        [EmpireTest("Policy")]
        public static void AllPolicies_NoNullStatModifiers()
        {
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                for (int i = 0; i < def.statModifiers.Count; i++)
                {
                    TestAssert.IsNotNull(def.statModifiers[i].stat,
                        $"{def.defName}: statModifiers[{i}] has null stat (bad defName in XML?)");
                }
            }
        }

        [EmpireTest("Policy")]
        public static void AllPolicies_StatValues_AreFinite()
        {
            var faction = GetFaction();
            if (faction == null) TestAssert.Skip("No faction");

            var snapshot = PolicyTestHelper.SnapshotPolicies(faction);
            try
            {
                foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
                {
                    if (def == FCPolicyDefOf.empty) continue;

                    PolicyTestHelper.ClearAll(faction);
                    PolicyTestHelper.EnactPolicy(faction, def);

                    foreach (FCStatDef stat in DefDatabase<FCStatDef>.AllDefsListForReading)
                    {
                        double val = faction.GetFactionStatValue(stat);
                        TestAssert.IsFalse(double.IsNaN(val),
                            $"Policy {def.defName} makes stat {stat.defName} NaN");
                        TestAssert.IsFalse(double.IsInfinity(val),
                            $"Policy {def.defName} makes stat {stat.defName} infinite");
                    }
                }
            }
            finally
            {
                PolicyTestHelper.RestorePolicies(faction, snapshot);
            }
        }
    }
}
