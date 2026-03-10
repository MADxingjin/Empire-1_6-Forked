using System.Collections.Generic;

namespace FactionColonies
{
    public static class BuildingFilterRegistry
    {
        private static readonly List<BuildingFilter> _filters = new List<BuildingFilter>();

        public static void Register(BuildingFilter filter)
        {
            if (!_filters.Contains(filter)) _filters.Add(filter);
        }

        public static void Unregister(BuildingFilter filter) => _filters.Remove(filter);
        public static void ClearAll() => _filters.Clear();

        public static IReadOnlyList<BuildingFilter> Filters => _filters;
    }
}
