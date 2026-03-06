using System;
using System.Collections.Generic;
using FactionColonies.util;

namespace FactionColonies
{
    public static class MilitaryEventRegistry
    {
        private static readonly List<IMilitaryEventParticipant> _participants = new List<IMilitaryEventParticipant>();

        public static void Register(IMilitaryEventParticipant participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }
        public static void Unregister(IMilitaryEventParticipant participant) => _participants.Remove(participant);
        public static void ClearAll() => _participants.Clear();
        public static IReadOnlyList<IMilitaryEventParticipant> Participants => _participants;

        public static void InvokeOnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad = false)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnSquadDeployed(settlement, job, isExtraSquad); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnSquadDeployed: {e}"); }
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnSquadRecalled(WorldSettlementFC settlement)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnSquadRecalled(settlement); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnSquadRecalled: {e}"); }
            }
            settlement.InvalidateStatCache();
        }

        public static void InvokeOnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnBattleResolved(settlement, job, victory); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnBattleResolved: {e}"); }
            }
            settlement.InvalidateStatCache();
        }
    }
}
