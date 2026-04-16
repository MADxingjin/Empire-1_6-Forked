using System.Collections.Generic;

namespace FactionColonies
{
    public static class SettlementButtonRegistry
    {
        private static readonly List<ISettlementWindowButton> _entries = new List<ISettlementWindowButton>();

        public static void Register(ISettlementWindowButton entry)
        {
            if (!_entries.Contains(entry)) _entries.Add(entry);
        }
        public static void Unregister(ISettlementWindowButton entry) => _entries.Remove(entry);
        public static void ClearAll() => _entries.Clear();
        public static IReadOnlyList<ISettlementWindowButton> Entries => _entries;
    }
}
