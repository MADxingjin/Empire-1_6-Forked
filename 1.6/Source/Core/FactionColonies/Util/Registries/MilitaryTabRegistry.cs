using System.Collections.Generic;

namespace FactionColonies
{
    public static class MilitaryTabRegistry
    {
        private static readonly List<IMilitaryTabEntry> _entries = new List<IMilitaryTabEntry>();

        public static void Register(IMilitaryTabEntry entry)
        {
            if (!_entries.Contains(entry)) _entries.Add(entry);
        }
        public static void Unregister(IMilitaryTabEntry entry) => _entries.Remove(entry);
        public static void ClearAll() => _entries.Clear();
        public static IReadOnlyList<IMilitaryTabEntry> Entries => _entries;
    }
}
