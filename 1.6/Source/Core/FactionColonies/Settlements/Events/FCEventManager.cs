using System;
using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    // Owns the faction's event queue and auxiliary cooldown / fire-count bookkeeping.
    // The only code that mutates the queue lives here; everywhere else reads via
    // the IReadOnlyList facade (exposed through FactionFC.Events) or calls one of
    // the methods below.
    //
    // Side effects that cascade to settlements (stat modifiers, cache invalidation)
    // are NOT handled here. They live on FactionFC.AddEvent, which calls
    // Enqueue(evt) as its one queue-touching step.
    public class FCEventManager : IExposable
    {
        private List<FCEvent> events = new List<FCEvent>();
        private Dictionary<string, int> eventCooldowns = new Dictionary<string, int>();
        private Dictionary<string, int> eventFireCounts = new Dictionary<string, int>();
        private int version;

        public IReadOnlyList<FCEvent> Events => events;
        public int Version => version;
        public int Count => events.Count;

        // Raw append. Does NOT apply stat modifiers or invalidate caches;
        // FactionFC.AddEvent is responsible for cascading side effects.
        public void Enqueue(FCEvent evt)
        {
            if (evt is null) return;
            events.Add(evt);
            version++;
        }

        public bool Remove(FCEvent evt)
        {
            if (evt is null) return false;
            if (!events.Remove(evt)) return false;
            evt.fired = true;
            version++;
            return true;
        }

        public int RemoveWhere(Predicate<FCEvent> match)
        {
            if (match is null) return 0;
            int removed = 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (!match(events[i])) continue;
                events[i].fired = true;
                events.RemoveAt(i);
                removed++;
            }
            if (removed > 0) version++;
            return removed;
        }

        public void Clear()
        {
            if (events.Count == 0) return;
            events.Clear();
            version++;
        }

        // Atomically collects every event whose timeTillTrigger has passed,
        // removes them from the queue, and returns them as a new list.
        // Does NOT set 'fired'; FCEventMaker.ProcessEvents still wants its
        // per-event re-entrancy guard to gate processing.
        public List<FCEvent> CollectDueEvents(int currentTick)
        {
            List<FCEvent> due = null;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i].timeTillTrigger > currentTick) continue;
                if (due is null) due = new List<FCEvent>();
                due.Add(events[i]);
                events.RemoveAt(i);
            }
            if (due != null) version++;
            return due;
        }

        // Bulk seed from a legacy list. Used only by FactionFC's save-migration
        // path to move pre-manager events into the manager. Bumps version once.
        public void SeedFromLegacy(IEnumerable<FCEvent> legacyEvents)
        {
            if (legacyEvents is null) return;
            bool any = false;
            foreach (FCEvent evt in legacyEvents)
            {
                if (evt is null) continue;
                events.Add(evt);
                any = true;
            }
            if (any) version++;
        }

        public void RecordCooldown(FCEventDef def)
        {
            if (def is null) return;
            eventCooldowns[def.defName] = Find.TickManager.TicksGame;
        }

        public bool IsOnCooldown(FCEventDef def)
        {
            if (def is null || def.cooldownTicks <= 0) return false;
            if (!eventCooldowns.TryGetValue(def.defName, out int lastTick)) return false;
            return Find.TickManager.TicksGame - lastTick < def.cooldownTicks;
        }

        public void RecordFired(FCEventDef def)
        {
            if (def is null) return;
            eventFireCounts.TryGetValue(def.defName, out int count);
            eventFireCounts[def.defName] = count + 1;
        }

        public bool HasReachedMaxFireCount(FCEventDef def)
        {
            if (def is null || def.maxFireCount <= 0) return false;
            if (!eventFireCounts.TryGetValue(def.defName, out int count)) return false;
            return count >= def.maxFireCount;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref events, "events", LookMode.Deep);
            if (events is null) events = new List<FCEvent>();
            Scribe_Collections.Look(ref eventCooldowns, "eventCooldowns", LookMode.Value, LookMode.Value);
            if (eventCooldowns is null) eventCooldowns = new Dictionary<string, int>();
            Scribe_Collections.Look(ref eventFireCounts, "eventFireCounts", LookMode.Value, LookMode.Value);
            if (eventFireCounts is null) eventFireCounts = new Dictionary<string, int>();
        }
    }
}
