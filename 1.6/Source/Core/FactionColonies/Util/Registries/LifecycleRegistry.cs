using System;
using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    public static class LifecycleRegistry
    {
        private static readonly List<ILifecycleParticipant> _participants = new List<ILifecycleParticipant>();

        public static void Register(ILifecycleParticipant participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }
        public static void Unregister(ILifecycleParticipant participant) => _participants.Remove(participant);
        public static void ClearAll() => _participants.Clear();
        public static IReadOnlyList<ILifecycleParticipant> Participants => _participants;

        // ── Settlement ──

        public static void InvokeOnSettlementCreated(WorldSettlementFC settlement)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSettlementCreated(settlement); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSettlementCreated: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSettlementRemoved(WorldSettlementFC settlement)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSettlementRemoved(settlement); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSettlementRemoved: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSettlementUpgraded(settlement, oldLevel, newLevel); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSettlementUpgraded: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSettlementTypeChanged(WorldSettlementFC settlement, WorldSettlementDef oldDef, WorldSettlementDef newDef)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSettlementTypeChanged(settlement, oldDef, newDef); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSettlementTypeChanged: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        // ── Building ──

        public static void InvokeOnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnBuildingConstructed(settlement, building, slot); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnBuildingConstructed: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnBuildingDeconstructed(settlement, building, slot); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnBuildingDeconstructed: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        // ── Military ──

        public static void InvokeOnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad = false)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSquadDeployed(settlement, job, isExtraSquad); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSquadDeployed: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSquadRecalled(WorldSettlementFC settlement)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnSquadRecalled(settlement); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnSquadRecalled: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnBattleResolved(settlement, job, victory, result); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnBattleResolved: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                settlement.InvalidateStatCache();
            }
            settlement.InvalidateStatCache();
        }

        // ── Mercenary ──

        public static void InvokeOnMercenaryDeath(MercenaryDeathEvent evt)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnMercenaryDeath(evt); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnMercenaryDeath: {e}"); }
            }
        }

        // ── Research ──

        public static void InvokeOnResearchCompleted(ResearchProjectDef project)
        {
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnResearchCompleted(project); }
                catch (Exception e) { LogUtil.Error($"ILifecycleParticipant {p.GetType().Name} threw in OnResearchCompleted: {e}"); }
                // Intentional: invalidate per-participant so the next participant sees fresh cache
                FactionCache.FactionComp?.InvalidateAllSettlementStatCaches();
            }
            FactionCache.FactionComp?.InvalidateAllSettlementStatCaches();
        }
    }
}
