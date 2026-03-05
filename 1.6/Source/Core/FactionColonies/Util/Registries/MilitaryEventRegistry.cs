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
        public static IReadOnlyList<IMilitaryEventParticipant> Participants => _participants;

        public static void InvokeOnSquadDeployed(WorldSettlementFC settlement, MilitaryJob job)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnSquadDeployed(settlement, job); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnSquadDeployed: {e}"); }
            }
        }

        public static void InvokeOnSquadRecalled(WorldSettlementFC settlement)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnSquadRecalled(settlement); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnSquadRecalled: {e}"); }
            }
        }

        public static void InvokeOnBattleResolved(WorldSettlementFC settlement, MilitaryJob job, bool victory)
        {
            foreach (IMilitaryEventParticipant participant in _participants)
            {
                try { participant.OnBattleResolved(settlement, job, victory); }
                catch (Exception e) { LogUtil.Error($"IMilitaryEventParticipant {participant.GetType().Name} threw in OnBattleResolved: {e}"); }
            }
        }
    }
}
