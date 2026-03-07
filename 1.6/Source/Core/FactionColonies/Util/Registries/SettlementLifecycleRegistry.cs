using System;
using System.Collections.Generic;
using FactionColonies.util;

namespace FactionColonies
{
    public static class SettlementLifecycleRegistry
    {
        private static readonly List<ISettlementLifecycleParticipant> _participants = new List<ISettlementLifecycleParticipant>();

        public static void Register(ISettlementLifecycleParticipant participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }
        public static void Unregister(ISettlementLifecycleParticipant participant) => _participants.Remove(participant);
        public static void ClearAll() => _participants.Clear();
        public static IReadOnlyList<ISettlementLifecycleParticipant> Participants => _participants;

        public static void InvokeOnSettlementCreated(WorldSettlementFC settlement)
        {
            foreach (ISettlementLifecycleParticipant participant in _participants)
            {
                try { participant.OnSettlementCreated(settlement); }
                catch (Exception e) { LogUtil.Error($"ISettlementLifecycleParticipant {participant.GetType().Name} threw in OnSettlementCreated: {e}"); }
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSettlementRemoved(WorldSettlementFC settlement)
        {
            foreach (ISettlementLifecycleParticipant participant in _participants)
            {
                try { participant.OnSettlementRemoved(settlement); }
                catch (Exception e) { LogUtil.Error($"ISettlementLifecycleParticipant {participant.GetType().Name} threw in OnSettlementRemoved: {e}"); }
            }
        }

        public static void InvokeOnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel)
        {
            foreach (ISettlementLifecycleParticipant participant in _participants)
            {
                try { participant.OnSettlementUpgraded(settlement, oldLevel, newLevel); }
                catch (Exception e) { LogUtil.Error($"ISettlementLifecycleParticipant {participant.GetType().Name} threw in OnSettlementUpgraded: {e}"); }
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSettlementTypeChanged(WorldSettlementFC settlement, WorldSettlementDef oldDef, WorldSettlementDef newDef)
        {
            foreach (ISettlementLifecycleParticipant participant in _participants)
            {
                try { participant.OnSettlementTypeChanged(settlement, oldDef, newDef); }
                catch (Exception e) { LogUtil.Error($"ISettlementLifecycleParticipant {participant.GetType().Name} threw in OnSettlementTypeChanged: {e}"); }
            }
            settlement.InvalidateStatCache();
        }
    }
}
