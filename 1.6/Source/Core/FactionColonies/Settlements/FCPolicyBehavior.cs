using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Per-policy runtime behavior instance. Only needed for policies that require
    /// procedural logic (cooldowns, periodic spawns, conditional stat mods, custom UI).
    /// Pure-XML policies (e.g., Isolationist, Industrious) need no behavior class.
    ///
    /// Created by FCPolicy constructor when def.behaviorClass is non-null.
    /// Owns its own state directly.
    /// Serialized via Scribe_Deep so all state survives save/load.
    /// </summary>
    public abstract class FCPolicyBehavior : IExposable
    {
        /// <summary>Back-reference to the owning FCPolicy. Set after construction and after load.</summary>
        [Unsaved] public FCPolicy policy;

        // ── Lifecycle ────────────────────────────────────────────────

        /// <summary>Called when this policy is enacted on the faction.</summary>
        public virtual void OnEnacted(FactionFC faction) { }

        /// <summary>Called when this policy is removed from the faction.</summary>
        public virtual void OnRemoved(FactionFC faction) { }

        /// <summary>Called every game tick while this policy is active.</summary>
        public virtual void Tick(FactionFC faction) { }

        // ── Settlement Events ────────────────────────────────────────

        /// <summary>Called when a new settlement is created while this policy is active.</summary>
        public virtual void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement) { }

        /// <summary>Called when a settlement is about to be removed while this policy is active.</summary>
        public virtual void OnSettlementRemoved(FactionFC faction, WorldSettlementFC settlement) { }

        /// <summary>Called after the player pays for a new settlement.</summary>
        public virtual void OnSettlementCostPaid(FactionFC faction) { }

        // ── Conditional Stat Modifier ────────────────────────────────

        /// <summary>
        /// Runtime-dependent stat modifier. Only override this for values that
        /// genuinely depend on game state at query time (e.g., Egalitarian happiness-based
        /// tax bonus, Expansionist first-settlement-free). For static modifiers, use
        /// FCPolicyDef.statModifiers XML instead.
        ///
        /// Aggregation contract:
        /// - For Additive stats (defaultValue=0): add/subtract from currentValue
        /// - For Multiplicative stats (defaultValue=1): multiply currentValue
        /// Check stat.aggregation if uncertain.
        /// </summary>
        public virtual double ModifyStat(FCStatDef stat, double currentValue, WorldSettlementFC settlement)
            => currentValue;

        /// <summary>
        /// Returns a description of this behavior's runtime contribution to the given stat for tooltips.
        /// Return null or empty for stats this behavior doesn't modify.
        /// </summary>
        public virtual string GetStatDescription(FCStatDef stat, WorldSettlementFC settlement) => null;

        // ── Military Events ──────────────────────────────────────────

        /// <summary>Called after a squad is deployed from a settlement.</summary>
        public virtual void OnSquadDeployed(FactionFC faction, WorldSettlementFC settlement, bool isExtraSquad) { }

        /// <summary>Called when a squad is recalled/returned to a settlement.</summary>
        public virtual void OnSquadRecalled(FactionFC faction, WorldSettlementFC settlement) { }

        // ── Tax Events ───────────────────────────────────────────────

        /// <summary>Called when taxes are collected from a settlement.</summary>
        public virtual void OnTaxCollected(FactionFC faction, WorldSettlementFC settlement) { }

        // ── Diplomacy ────────────────────────────────────────────────

        /// <summary>Handle sending a diplomatic envoy to a target faction. Return true if handled.</summary>
        public virtual bool HandleDiplomaticEnvoy(FactionFC faction, Faction targetFaction) => false;

        // ── UI ───────────────────────────────────────────────────────

        /// <summary>Return labeled action buttons to render in the main tab button bar. Null means no buttons.</summary>
        public virtual IEnumerable<(TaggedString label, Action onClick)> GetMainTabActionButtons(FactionFC faction) => null;

        /// <summary>Return extra float menu options for a settlement's context menu. Null means none.</summary>
        public virtual IEnumerable<FloatMenuOption> GetSettlementActions(FactionFC faction, WorldSettlementFC settlement) => null;

        /// <summary>Return extra deployment options when a settlement's main squad is already deployed. Null means none.</summary>
        public virtual IEnumerable<FloatMenuOption> GetExtraDeploymentOptions(FactionFC faction, WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp) => null;

        /// <summary>Return additional description lines to append to the policy's tooltip.</summary>
        public virtual TaggedString GetDescription() => TaggedString.Empty;

        // ── Serialization ────────────────────────────────────────────

        public virtual void ExposeData() { }
    }
}
