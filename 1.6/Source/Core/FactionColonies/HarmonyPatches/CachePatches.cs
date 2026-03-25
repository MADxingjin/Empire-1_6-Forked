using HarmonyLib;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Centralized invalidation for all Empire static caches and registries.
    /// Called from Harmony postfixes on both <see cref="Game.Dispose"/> and <see cref="Game.ClearCaches"/>.
    /// </summary>
    internal static class EmpireCacheUtil
    {
        public static void InvalidateAll()
        {
            FactionCache.InvalidateCache();

            TaxTickRegistry.ClearAll();
            MainTableRegistry.ClearAll();
            LifecycleRegistry.ClearAll();
            BattleModifierRegistry.ClearAll();
            BuildingFilterRegistry.ClearAll();

            AutoDefenderRegistry.ClearAll();
            MilitaryTabRegistry.ClearAll();
            RaidTargetRegistry.ClearAll();
            DefenseValidatorRegistry.ClearAll();
            SquadAssignmentRegistry.ClearAll();
            ThreatScalingRegistry.ClearAll();
            SilverPaymentRegistry.ClearAll();

            SettlementTypeExtension_Orbital.InvalidateCache();
            FactionDefDescriptionPatch.Invalidate();
        }
    }

    [HarmonyPatch(typeof(Game), "Dispose")]
    class CachePatches
    {
        public static void Postfix()
        {
            EmpireCacheUtil.InvalidateAll();
        }
    }

    /// <summary>
    /// Game.ClearCaches is called at the start of Game.LoadGame() and
    /// Page_SelectScenario.BeginScenarioConfiguration(). This ensures Empire's
    /// caches are invalidated when starting a new game or loading a save, not
    /// just on Game.Dispose(), which doesn't fire when backing out of the new
    /// game flow through pages.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
    class ClearCachesPatch
    {
        public static void Postfix()
        {
            EmpireCacheUtil.InvalidateAll();
        }
    }
}
