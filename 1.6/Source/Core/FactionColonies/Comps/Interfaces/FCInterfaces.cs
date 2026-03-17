using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Defines an interface to let classes specify additional tabs to add to the main tab window.
    /// </summary>
    public interface IMainTabWindowOverview
    {
        void PreOpenWindow(FactionFC faction);
        void OnTabSwitch();
        void DrawOverviewTab(Rect boundingBox);
        void PostCloseWindow();
        string TabName();
    }
    /// <summary>
    /// Defines an interface that WorldObjectComps can implement in order to add a new overview tab to the settlement window.
    /// <para>This must be implemented by a WorldObjectComp. It will not be invoked otherwise.</para>
    /// </summary>
    public interface ISettlementWindowOverview
    {
        void PreOpenWindow(WorldSettlementFC settlement);
        void OnTabSwitch();
        void DrawOverviewTab(Rect boundingBox);
        void PostCloseWindow();
        string OverviewTabName();
    }
    /// <summary>
    /// A simple interface that a WorldObjectComp -- attached to a WorldSettlementFC -- can implement to affect non-resource stats.
    /// <para>Results are cached alongside stat modifiers. Caches are automatically invalidated after all lifecycle
    /// events (building, settlement, military, research, tax hooks). Only call
    /// <c>((WorldSettlementFC)parent).InvalidateStatCache()</c> manually if changing values outside a lifecycle callback.</para>
    /// </summary>
    public interface IStatModifierProvider
    {
        double GetStatModifier(FCStatDef stat);
        string GetStatModifierDesc(FCStatDef stat);
    }
    /// <summary>
    /// A WorldObjectComp interface for contributing dynamic, per-resource production bonuses.
    /// Unlike <see cref="IStatModifierProvider"/> (which operates at the stat level), this operates
    /// directly on <see cref="ResourceFC"/> instances, letting comps target specific resources.
    /// <para>Results are queried during production calculation (lazy-cached by ResourceFC's dirty flags).
    /// Caches are automatically invalidated after all lifecycle events (building, settlement, military,
    /// research, tax hooks). Only call <c>((WorldSettlementFC)parent).InvalidateResourceCaches()</c>
    /// manually if changing values outside a lifecycle callback.</para>
    /// </summary>
    public interface IResourceProductionModifier
    {
        /// <summary>
        /// Returns an additive production bonus for the given resource. Return 0 for no effect.
        /// </summary>
        double GetResourceAdditiveModifier(ResourceFC resource);
        /// <summary>
        /// Returns a multiplicative production modifier for the given resource. Return 1 for no effect.
        /// </summary>
        double GetResourceMultiplierModifier(ResourceFC resource);
        /// <summary>
        /// Returns a description of this comp's contribution for tooltip display.
        /// Return null or empty if not contributing to this resource.
        /// </summary>
        string GetResourceModifierDesc(ResourceFC resource);
    }
    /// <summary>
    /// A WorldObjectComp interface for injecting additional tithe budget into a resource.
    /// The injected budget raises the tithe income cap (<see cref="ResourceFC.GetTitheIncome"/>).
    /// In <see cref="ResourceFC.actualIncome"/>, only the portion of tithe actually covered by the
    /// injection is offset, so the settlement is not penalised for externally-sourced goods.
    /// <para>Queried during tithe budget calculation via <see cref="ResourceFC.externalTitheBudget"/>.
    /// Caches are automatically invalidated after all lifecycle events. Call
    /// <c>((WorldSettlementFC)parent).InvalidateStatCache()</c> manually if changing values outside
    /// a lifecycle callback.</para>
    /// </summary>
    public interface ITitheBudgetModifier
    {
        /// <summary>
        /// Returns additional tithe budget (in silver value) for the given resource.
        /// Return 0 for no effect.
        /// </summary>
        double GetExternalTitheBudget(ResourceFC resource);

        /// <summary>
        /// Description text for the tithe budget breakdown tooltip. Return null or empty for no entry.
        /// </summary>
        string GetExternalTitheBudgetDesc(ResourceFC resource);
    }
    /// <summary>
    /// Defines an interface to let classes hook into the tax system.
    /// </summary>
    public interface ITaxTickParticipant
    {
        void PreTaxResolution(FactionFC faction);
        void PostTaxResolution(FactionFC faction);
        /// <summary>
        /// Called at the start of tax collection, after pre-tax preparation (cache invalidation, resource pruning) and SettlementTypeExtension.PreTax.
        /// </summary>
        void PreSettlementCreateTax(WorldSettlementFC settlement);
        /// <summary>
        /// Called at the end of tax collection, after all calculations are complete and SettlementTypeExtension.PostTax.
        /// </summary>
        void PostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings);
    }
    /// <summary>
    /// Unified lifecycle hook for settlement, building, military, and research events.
    /// Register implementations via <see cref="LifecycleRegistry"/>.
    /// Use <see cref="LifecycleParticipantBase"/> to avoid stubbing unused methods.
    /// </summary>
    public interface ILifecycleParticipant
    {
        void OnSettlementCreated(WorldSettlementFC settlement);
        void OnSettlementRemoved(WorldSettlementFC settlement);
        void OnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel);
        void OnSettlementTypeChanged(WorldSettlementFC settlement, WorldSettlementDef oldDef, WorldSettlementDef newDef);
        void OnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
        void OnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
        void OnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad);
        void OnSquadRecalled(WorldSettlementFC settlement);
        void OnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result);
        void OnResearchCompleted(ResearchProjectDef project);
    }
    /// <summary>
    /// Defines an interface to let classes modify military forces before a battle is resolved.
    /// </summary>
    public interface IBattleModifier
    {
        /// <summary>
        /// Called before the battle loop begins. Modify the force's militaryLevel, militaryEfficiency,
        /// or forceRemaining to affect the outcome.
        /// </summary>
        void ModifyForce(militaryForce force, bool isAttacker);
    }
    /// <summary>
    /// Allows submods to veto or filter defense assignments. Called when a settlement
    /// is considered as a defender for another settlement (both manual selection and auto-defend).
    /// Register implementations via <see cref="DefenseValidatorRegistry"/>.
    /// </summary>
    public interface IDefenseValidator
    {
        /// <summary>
        /// Returns true if <paramref name="defender"/> is allowed to defend <paramref name="target"/>.
        /// Return false to exclude it from the defender list or auto-defend selection.
        /// </summary>
        bool CanDefend(WorldSettlementFC defender, WorldSettlementFC target);
    }
    /// <summary>
    /// Allows submods to veto squad assignments. Called before a squad loadout is
    /// assigned to a settlement. Register implementations via <see cref="SquadAssignmentRegistry"/>.
    /// </summary>
    public interface ISquadAssignmentValidator
    {
        /// <summary>
        /// Returns true if <paramref name="squad"/> can be assigned to <paramref name="settlement"/>.
        /// If false, <paramref name="reason"/> is shown to the player as a rejection message.
        /// </summary>
        bool CanAssign(WorldSettlementFC settlement, MilSquadFC squad, out string reason);
    }
    /// <summary>
    /// Allows submods to contribute additive or multiplicative modifiers to the Empire Threat Level (ETL).
    /// Register implementations via <see cref="ThreatScalingRegistry"/>.
    /// </summary>
    public interface IThreatScalingContributor
    {
        /// <summary>
        /// Returns an additive contribution to the ETL (added to the raw score before multiplication).
        /// Return 0 for no effect.
        /// </summary>
        double GetAdditiveContribution(FactionFC faction);

        /// <summary>
        /// Returns a multiplicative contribution to the ETL (multiplied into the final result).
        /// Return 1.0 for no effect.
        /// </summary>
        double GetMultiplicativeContribution(FactionFC faction);
    }

    /// <summary>
    /// Defines an interface to let classes intercept and modify silver payments before they are processed.
    /// </summary>
    public interface ISilverPaymentModifier
    {
        /// <summary>
        /// Called before silver is consumed. Modify context.Amount to change how much is charged.
        /// Use the context's Reason and Settlement fields to determine what the payment is for.
        /// </summary>
        void ModifyPayment(SilverPaymentContext context);
    }
    /// <summary>
    /// Allows external mods to register world objects as raid targets for Empire's military system.
    /// Registered targets are included in the attack target pool alongside Empire settlements,
    /// receive the same 24-hour warning, and auto-resolve via <see cref="SimulateBattleFc.FightBattle"/>.
    /// Register implementations via <see cref="RaidTargetRegistry"/>.
    /// </summary>
    public interface IRaidTarget
    {
        /// <summary>The world object this target wraps (for serialization and <see cref="LookTargets"/>).</summary>
        WorldObject WorldObject { get; }
        string Name { get; }
        int Tile { get; }
        /// <summary>Virtual military level used for targeting weight and auto-defend comparison.</summary>
        int MilitaryLevel { get; }
        /// <summary>Set by the attack system to prevent duplicate attacks. Cleared on resolution.</summary>
        bool IsUnderAttack { get; set; }
        void OnRaidWon(BattleResult result);
        void OnRaidLost(BattleResult result);
    }
    /// <summary>
    /// Allows external mods to register world objects as auto-defenders for Empire settlements
    /// (and other <see cref="IRaidTarget"/>s). Defenders create a <see cref="militaryForce"/> and
    /// are placed on cooldown after battle resolution.
    /// Register implementations via <see cref="AutoDefenderRegistry"/>.
    /// </summary>
    public interface IAutoDefender
    {
        WorldObject WorldObject { get; }
        int MilitaryLevel { get; }
        /// <summary>Maximum tile distance for auto-defense eligibility.</summary>
        int Range { get; }
        /// <summary>True if the defender is available (enabled, not busy, not packing, etc.).</summary>
        bool CanAutoDefend { get; }
        militaryForce CreateDefendingForce();
        void OnDefenseStarted(WorldObject target);
        void OnDefenseComplete(bool won, BattleResult result);
        /// <summary>Called when this defender is replaced by another force (not defeated).</summary>
        void OnDefenseReplaced();
        /// <summary>
        /// Returns pawns to fight in a manual battle, or null to generate pawns from force points.
        /// Implementations should remove pawns from their source before returning them.
        /// </summary>
        List<Pawn> GetDefendingPawns();
        /// <summary>
        /// Called after a manual battle ends to return surviving pawns.
        /// Pawns will already be despawned from the battle map.
        /// </summary>
        void ReturnDefendingPawns(List<Pawn> pawns);
    }
    /// <summary>
    /// Allows external mods to display entries in Empire's military tab alongside settlements.
    /// Entries appear as simplified cards with name, military level, status, and an auto-defend toggle.
    /// Register implementations via <see cref="MilitaryTabRegistry"/>.
    /// </summary>
    public interface IMilitaryTabEntry
    {
        WorldObject WorldObject { get; }
        string Name { get; }
        int MilitaryLevel { get; }
        bool AutoDefend { get; set; }
        bool IsUnderAttack { get; }
        bool IsBusy { get; }
        string StatusLabel { get; }
        Color AccentColor { get; }
    }
}
