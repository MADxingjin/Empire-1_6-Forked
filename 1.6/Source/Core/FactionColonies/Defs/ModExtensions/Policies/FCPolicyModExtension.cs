using System.Collections.Generic;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// A DefModExtension for FCPolicyDef that enables custom policy/faction-trait
    /// effects without modifying the base DLL. Subclass this in C# and attach to
    /// an FCPolicyDef via XML modExtensions.
    ///
    /// Hooks are called via FactionFC.ForEachPolicyExtension, which iterates a
    /// cached flat list of active extensions — no per-call GetModExtension overhead.
    ///
    /// Example XML:
    ///   <FactionColonies.FCPolicyDef>
    ///     <defName>myPolicy</defName>
    ///     <category>Core</category>
    ///     <modExtensions>
    ///       <li Class="MyMod.MyPolicyExtension" />
    ///     </modExtensions>
    ///   </FactionColonies.FCPolicyDef>
    /// </summary>
    public abstract class FCPolicyModExtension : DefModExtension
    {
        // ── Lifecycle ──────────────────────────────────────────────

        /// <summary>Called when this policy/trait is enacted on a faction.</summary>
        public virtual void OnEnacted(FactionFC faction, FCPolicy policy) { }

        /// <summary>Called when this policy/trait is removed from a faction.</summary>
        public virtual void OnRemoved(FactionFC faction, FCPolicy policy) { }

        /// <summary>Called every game tick while this policy/trait is active.</summary>
        public virtual void Tick(FactionFC faction, FCPolicy policy) { }

        // ── Settlement ─────────────────────────────────────────────

        /// <summary>Called when a new settlement is created while this policy is active.</summary>
        public virtual void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement) { }

        /// <summary>Modify the silver cost to create a new settlement.</summary>
        public virtual double ModifySettlementCost(double cost) => cost;

        /// <summary>Called after the player pays for a new settlement and the cost was modified by a policy.</summary>
        public virtual void OnSettlementCostPaid(FactionFC faction, FCPolicy policy) { }

        // ── Buildings ──────────────────────────────────────────────

        /// <summary>Modify the time in ticks for a building to be constructed or upgraded.</summary>
        public virtual int ModifyBuildTime(int ticks) => ticks;

        /// <summary>Modify the upkeep cost for a building.</summary>
        public virtual double ModifyBuildingUpkeep(double upkeep, BuildingFCDef building) => upkeep;

        // ── Military ───────────────────────────────────────────────

        /// <summary>Modify a settlement's military force level and efficiency before battle.</summary>
        public virtual void ModifyMilitaryForce(ref double level, ref double efficiency, bool isAttacking) { }

        /// <summary>Modify prosperity/happiness/loyalty penalties when a settlement loses a battle.</summary>
        public virtual void ModifyBattlePenalties(ref double prosperityLoss, ref double happinessLoss, ref double loyaltyLoss) { }

        /// <summary>Modify the military cooldown in ticks after a military action.</summary>
        public virtual int ModifyMilitaryCooldown(int ticks, MilitaryJob job) => ticks;

        /// <summary>Modify the loot value multiplier from raids/battles.</summary>
        public virtual double ModifyLootMultiplier(double mult) => mult;

        /// <summary>Modify the per-dead-pawn cooldown multiplier when returning from deployment.
        /// Default is 10000 ticks per dead pawn. Return a modified value.</summary>
        public virtual int ModifyDeadPawnCooldownMultiplier(int multiplier) => multiplier;

        /// <summary>Return true to prevent building and level destruction when a settlement loses a battle.</summary>
        public virtual bool PreventBuildingDestruction() => false;

        /// <summary>Called after a squad is deployed from a settlement.</summary>
        public virtual void OnSquadDeployed(FactionFC faction, FCPolicy policy, WorldSettlementFC settlement, bool isExtraSquad) { }

        /// <summary>Return extra deployment options when a settlement's main squad is already deployed. Null means none.</summary>
        public virtual IEnumerable<FloatMenuOption> GetExtraDeploymentOptions(FactionFC faction, FCPolicy policy, WorldSettlementFC settlement, WorldObjectComp_SettlementMilitary milComp) => null;

        // ── Economy ────────────────────────────────────────────────

        /// <summary>Modify the total tithe value multiplier.</summary>
        public virtual double ModifyTitheMultiplier(double mult) => mult;

        /// <summary>Modify the tax bonus for a settlement.</summary>
        public virtual double ModifyTaxBonus(double bonus, WorldSettlementFC settlement) => bonus;

        /// <summary>Modify the research points contributed by a settlement.</summary>
        public virtual double ModifyResearchContribution(double contribution, WorldSettlementFC settlement) => contribution;

        /// <summary>Modify the additional workers above the base softcap.</summary>
        public virtual int ModifyExtraWorkersSoftcap(int extra) => extra;

        /// <summary>Modify the flat adjustment to overmax workers (workers allowed above the softcap). Default 0.</summary>
        public virtual int ModifyOverMaxWorkers(int adjustment) => adjustment;

        /// <summary>Modify the per-settlement tax-time multiplier (called once per settlement per tax collection).</summary>
        public virtual double ModifyTaxTimeMultiplier(double mult, WorldSettlementFC settlement) => mult;

        /// <summary>Return additional base happiness gain for a settlement. Default 0.</summary>
        public virtual double GetSettlementHappinessBonus(WorldSettlementFC settlement) => 0;

        /// <summary>Return additional base prosperity recovery for a settlement. Default 0.</summary>
        public virtual double GetSettlementProsperityBonus(WorldSettlementFC settlement) => 0;

        // ── Permissions ────────────────────────────────────────────

        /// <summary>Return true to block the given action type (e.g., isolationist blocks capture).</summary>
        public virtual bool BlocksAction(FCActionType action) => false;

        /// <summary>Return true to enable the given action type (e.g., authoritarian enables enslave).</summary>
        public virtual bool EnablesAction(FCActionType action) => false;

        /// <summary>Return true to suppress happiness/unrest penalties from faction member deaths.</summary>
        public virtual bool SuppressMemberDeathPenalty() => false;

        // ── Diplomacy ─────────────────────────────────────────────

        /// <summary>Handle sending a diplomatic envoy to a target faction. Return true if handled.</summary>
        public virtual bool HandleDiplomaticEnvoy(FactionFC faction, FCPolicy policy, Faction targetFaction) => false;

        // ── UI ─────────────────────────────────────────────────────

        /// <summary>Return extra gizmos/buttons for the main faction tab. Null means none.</summary>
        public virtual IEnumerable<Gizmo> GetMainTabGizmos(FactionFC faction) => null;

        /// <summary>Return labeled action buttons to render in the main tab button bar. Null means no buttons.</summary>
        public virtual IEnumerable<(TaggedString label, System.Action onClick)> GetMainTabActionButtons(FactionFC faction) => null;

        /// <summary>Return extra float menu options for a settlement's context menu. Null means none.</summary>
        public virtual IEnumerable<FloatMenuOption> GetSettlementActions(FactionFC faction, WorldSettlementFC settlement) => null;

        /// <summary>Return extra gizmos for the world map when targeting a world object. Null means none.</summary>
        public virtual IEnumerable<Gizmo> GetWorldMapGizmos(FactionFC faction) => null;

        // ── State ──────────────────────────────────────────────────

        /// <summary>
        /// Override to return a custom FCPolicyState subclass for per-policy runtime data
        /// (cooldowns, timers, etc.). Called once when the policy is enacted.
        /// Return null (default) if no state is needed.
        /// </summary>
        public virtual FCPolicyState CreateState() => null;

        // ── Description ────────────────────────────────────────────

        /// <summary>
        /// Return additional description lines to append to the policy's tooltip.
        /// Return TaggedString.Empty (default) for no extra description.
        /// </summary>
        public virtual TaggedString GetDescription() => TaggedString.Empty;
    }
}
