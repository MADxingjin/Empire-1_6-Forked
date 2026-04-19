using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Shared buy-side filtering for Empire traders. Determines which ThingDefs
    /// Empire settlements and caravans are willing to purchase from the player.
    /// </summary>
    public static class EmpireTradeFilterUtil
    {
        /// <summary>
        /// Returns true if Empire traders should be willing to buy this ThingDef.
        /// Rejects dangerous, worthless, or non-tradeable items.
        /// </summary>
        public static bool ShouldHandleThingDef(ThingDef thingDef)
        {
            if (thingDef is null)
                return false;

            if (thingDef.destroyOnDrop)
                return false;

            if (!thingDef.genericMarketSellable)
                return false;

            if (thingDef.category != ThingCategory.Item
                && thingDef.category != ThingCategory.Pawn)
                return false;

            if (thingDef.BaseMarketValue <= 0f)
                return false;

            if (thingDef.thingCategories is object
                && (thingDef.thingCategories.Contains(ThingCategoryDefOf.Chunks)
                    || thingDef.thingCategories.Contains(ThingCategoryDefOf.StoneChunks)))
                return false;

            return true;
        }
    }
}
