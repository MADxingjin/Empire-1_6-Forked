using Verse;

namespace FactionColonies
{
    /// <summary>
    /// A DefModExtension for FCTraitEffectDef that enables custom trait effects without
    /// modifying the base DLL. Subclass this in C# and attach to an FCTraitEffectDef via XML.
    ///
    /// Callback semantics:
    ///   OnAppliedToSettlement / OnRemovedFromSettlement — fire for EACH settlement that
    ///     receives or loses the trait (including propagation from faction-level add/remove).
    ///   OnAppliedToFaction / OnRemovedFromFaction — fire once for the faction when the
    ///     trait is added/removed at the faction level (separate from per-settlement callbacks).
    ///   Tick — called every game tick for each settlement that currently holds this trait.
    ///
    /// Example XML:
    ///   <FCTraitEffectDef>
    ///     <defName>MyMod_CustomTrait</defName>
    ///     <modExtensions>
    ///       <li Class="MyMod.MyCustomTraitEffect"/>
    ///     </modExtensions>
    ///   </FCTraitEffectDef>
    /// </summary>
    public class FCTraitEffectModExtension : DefModExtension
    {
        /// <summary>
        /// Called when this trait is added to a specific settlement.
        /// Invoked after resource bonuses are applied.
        /// </summary>
        public virtual void OnAppliedToSettlement(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Called when this trait is removed from a specific settlement.
        /// Invoked after resource bonuses are removed.
        /// </summary>
        public virtual void OnRemovedFromSettlement(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Called when this trait is added to the faction as a whole.
        /// Invoked after settlement propagation (if AppliesToSettlements() returns true,
        /// OnAppliedToSettlement will have already fired for each settlement before this).
        /// </summary>
        public virtual void OnAppliedToFaction(FactionFC faction)
        {
        }

        /// <summary>
        /// Called when this trait is removed from the faction as a whole.
        /// Invoked after settlement propagation.
        /// </summary>
        public virtual void OnRemovedFromFaction(FactionFC faction)
        {
        }

        /// <summary>
        /// Called every game tick for each settlement that currently holds this trait.
        /// Keep this implementation lightweight — it is called frequently.
        /// Note: unlike callbacks, this has no try/catch wrapper; exceptions will propagate.
        /// </summary>
        public virtual void Tick(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Returns additional description lines to append to the trait's bonus description.
        /// Return TaggedString.Empty (default) for no extra description.
        /// </summary>
        public virtual TaggedString GetDescription()
        {
            return TaggedString.Empty;
        }

        /// <summary>
        /// If this returns true, the trait will also be propagated to all settlements
        /// when added/removed at the faction level (in addition to the hardcoded stat check).
        /// </summary>
        public virtual bool AppliesToSettlements()
        {
            return false;
        }
    }
}
