using System;
using System.Collections.Generic;
using FactionColonies.util;

namespace FactionColonies
{
    public static class MainTableRegistry
    {
        private static readonly List<IMainTabWindowOverview> _tabs = new List<IMainTabWindowOverview>();

        public static void Register(IMainTabWindowOverview tab)
        {
            if (!_tabs.Contains(tab)) _tabs.Add(tab);
        }
        public static void Unregister(IMainTabWindowOverview tab) => _tabs.Remove(tab);
        public static void ClearAll() => _tabs.Clear();
        public static IReadOnlyList<IMainTabWindowOverview> Tabs => _tabs;

        public static void InvokePostCloseWindow()
        {
            foreach (IMainTabWindowOverview tab in _tabs)
            {
                try { tab.PostCloseWindow(); }
                catch (Exception e) { LogUtil.Error($"IMainTabWindowOverview {tab.GetType().Name} threw in PostCloseWindow: {e}"); }
            }
        }
    }
}
