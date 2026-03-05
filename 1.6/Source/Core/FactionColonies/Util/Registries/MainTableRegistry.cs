using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies
{
    public static class MainTableRegistry
    {
        private static readonly List<IMainTabWindowOverview> _tabs = new List<IMainTabWindowOverview>();

        public static void Register(IMainTabWindowOverview tab) => _tabs.Add(tab);
        public static void Unregister(IMainTabWindowOverview tab) => _tabs.Remove(tab);
        public static IReadOnlyList<IMainTabWindowOverview> Tabs => _tabs;
    }
}
