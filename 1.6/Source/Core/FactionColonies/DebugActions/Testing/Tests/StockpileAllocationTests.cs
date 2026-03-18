namespace FactionColonies
{
    public static class StockpileAllocationTests
    {
        // Creates a ResourceFC with known production, without requiring game state.
        // productionBase = productionPerWorker (one additive, no multipliers → mult=1)
        // rawTotalProduction = productionPerWorker * workers
        private static ResourceFC MakeResource(double productionPerWorker, int workers)
        {
            var res = new ResourceFC();
            // A non-empty desc is required to avoid a null-settlement warning branch in AddProductionAdditive
            res.AddProductionAdditive("test.production", productionPerWorker, "test bonus");
            res.assignedWorkers = workers;
            return res;
        }

        // --- Zero-allocation regression ---
        // Verify the property chain is identical to the pre-stockpile state when no
        // allocations are registered (empty dictionary must behave like the old literal 0).

        [EmpireTest("StockpileAllocation")]
        public static void NoAllocations_TotalStockpileAllocation_IsZero()
        {
            var res = MakeResource(10.0, 3);
            TestAssert.AreEqual(0.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void NoAllocations_EffectiveRawEquals_RawTotalProduction()
        {
            var res = MakeResource(10.0, 3);
            TestAssert.AreEqual(res.rawTotalProduction, res.effectiveRawTotalProduction);
        }

        [EmpireTest("StockpileAllocation")]
        public static void NoAllocations_TaxableMarketValue_EqualsEffectiveTimesRate()
        {
            var res = MakeResource(10.0, 3);
            double expected = res.effectiveRawTotalProduction * FCSettings.silverPerResource;
            TestAssert.AreEqual(expected, res.taxableProductionMarketValue);
        }

        // --- SetStockpileAllocation: acceptance/rejection ---

        [EmpireTest("StockpileAllocation")]
        public static void SetAllocation_WithinCapacity_ReturnsTrue()
        {
            var res = MakeResource(10.0, 3); // rawTotalProduction = 30
            TestAssert.IsTrue(res.SetStockpileAllocation("mod.a", 20.0));
        }

        [EmpireTest("StockpileAllocation")]
        public static void SetAllocation_ExactlyAtCapacity_ReturnsTrue()
        {
            var res = MakeResource(10.0, 3); // rawTotalProduction = 30
            TestAssert.IsTrue(res.SetStockpileAllocation("mod.a", 30.0));
        }

        [EmpireTest("StockpileAllocation")]
        public static void SetAllocation_ExceedsCapacity_ReturnsFalse()
        {
            var res = MakeResource(10.0, 3); // rawTotalProduction = 30
            TestAssert.IsFalse(res.SetStockpileAllocation("mod.a", 31.0));
        }

        [EmpireTest("StockpileAllocation")]
        public static void SetAllocation_ExceedsCapacity_NotRegistered()
        {
            var res = MakeResource(10.0, 3);
            res.SetStockpileAllocation("mod.a", 31.0); // rejected
            TestAssert.AreEqual(0.0, res.totalStockpileAllocation);
        }

        // --- Multi-mod composition ---

        [EmpireTest("StockpileAllocation")]
        public static void MultipleAllocations_SumCorrectly()
        {
            var res = MakeResource(10.0, 5); // rawTotalProduction = 50
            res.SetStockpileAllocation("mod.a", 10.0);
            res.SetStockpileAllocation("mod.b", 15.0);
            TestAssert.AreEqual(25.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void SecondAllocation_ExceedsCombinedCapacity_Rejected()
        {
            var res = MakeResource(10.0, 5); // rawTotalProduction = 50
            res.SetStockpileAllocation("mod.a", 40.0);
            TestAssert.IsFalse(res.SetStockpileAllocation("mod.b", 15.0));
        }

        [EmpireTest("StockpileAllocation")]
        public static void UpdateExistingKey_TreatsOldAmountAsReplaced()
        {
            var res = MakeResource(10.0, 3); // rawTotalProduction = 30
            res.SetStockpileAllocation("mod.a", 20.0);
            // Re-register the same key with a smaller amount: the old amount should not
            // count twice in the capacity check, so this should succeed.
            TestAssert.IsTrue(res.SetStockpileAllocation("mod.a", 10.0));
            TestAssert.AreEqual(10.0, res.totalStockpileAllocation);
        }

        // --- Property chain with an active allocation ---

        [EmpireTest("StockpileAllocation")]
        public static void WithAllocation_EffectiveRaw_ReducedByAllocation()
        {
            var res = MakeResource(10.0, 3); // rawTotalProduction = 30
            res.SetStockpileAllocation("mod.a", 12.0);
            TestAssert.AreEqual(18.0, res.effectiveRawTotalProduction);
        }

        // --- ClearStockpileAllocation ---

        [EmpireTest("StockpileAllocation")]
        public static void Clear_RemovesAllocation()
        {
            var res = MakeResource(10.0, 3);
            res.SetStockpileAllocation("mod.a", 10.0);
            res.ClearStockpileAllocation("mod.a");
            TestAssert.AreEqual(0.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void Clear_DoesNotInvokeCallback()
        {
            var res = MakeResource(10.0, 3);
            bool callbackFired = false;
            res.SetStockpileAllocation("mod.a", 10.0, () => callbackFired = true);
            res.ClearStockpileAllocation("mod.a");
            TestAssert.IsFalse(callbackFired, "Callback should not fire on voluntary clear");
        }

        // --- PruneStockpileAllocations ---

        [EmpireTest("StockpileAllocation")]
        public static void Prune_WithinCapacity_NothingEvicted()
        {
            var res = MakeResource(10.0, 5); // rawTotalProduction = 50
            res.SetStockpileAllocation("mod.a", 20.0);
            res.PruneStockpileAllocations();
            TestAssert.AreEqual(20.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void Prune_NoAllocations_NoOp()
        {
            var res = MakeResource(10.0, 3);
            res.PruneStockpileAllocations(); // must not throw
            TestAssert.AreEqual(0.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void Prune_EvictsLargestFirst()
        {
            // rawTotalProduction = 30 at 3 workers; both allocations fit exactly.
            var res = MakeResource(10.0, 3);
            res.SetStockpileAllocation("mod.small", 5.0);
            res.SetStockpileAllocation("mod.large", 25.0);
            // Drop to 2 workers: rawTotalProduction = 20, total alloc = 30 → over capacity.
            res.assignedWorkers = 2;
            res.PruneStockpileAllocations();
            // mod.large (25) should be evicted; mod.small (5) fits and survives.
            TestAssert.AreEqual(5.0, res.totalStockpileAllocation);
        }

        [EmpireTest("StockpileAllocation")]
        public static void Prune_CallbackFiredOnEviction()
        {
            var res = MakeResource(10.0, 3);
            res.SetStockpileAllocation("mod.small", 5.0);
            bool callbackFired = false;
            res.SetStockpileAllocation("mod.large", 25.0, () => callbackFired = true);
            res.assignedWorkers = 2; // rawTotalProduction drops to 20
            res.PruneStockpileAllocations();
            TestAssert.IsTrue(callbackFired, "Eviction callback should have fired");
        }

        [EmpireTest("StockpileAllocation")]
        public static void Prune_CallbackNotFiredForSurvivingEntry()
        {
            var res = MakeResource(10.0, 3);
            bool smallCallbackFired = false;
            res.SetStockpileAllocation("mod.small", 5.0, () => smallCallbackFired = true);
            res.SetStockpileAllocation("mod.large", 25.0);
            res.assignedWorkers = 2; // rawTotalProduction drops to 20; large is evicted
            res.PruneStockpileAllocations();
            TestAssert.IsFalse(smallCallbackFired, "Surviving entry's callback should not fire");
        }
    }
}
