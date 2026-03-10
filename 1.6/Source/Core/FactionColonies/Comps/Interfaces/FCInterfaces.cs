using FactionColonies;
using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
