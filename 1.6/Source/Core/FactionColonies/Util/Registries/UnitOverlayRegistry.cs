using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactionColonies
{
    public static class UnitOverlayRegistry
    {
        private static readonly List<IUnitOverlayRenderer> _renderers = new List<IUnitOverlayRenderer>();

        public static void Register(IUnitOverlayRenderer renderer)
        {
            if (!_renderers.Contains(renderer)) _renderers.Add(renderer);
        }

        public static void Unregister(IUnitOverlayRenderer renderer) => _renderers.Remove(renderer);
        public static void ClearAll() => _renderers.Clear();
        public static IReadOnlyList<IUnitOverlayRenderer> Renderers => _renderers;

        public static void InvokeDrawOverlay(Rect unitRect, MilUnitFC unit, Mercenary merc)
        {
            foreach (IUnitOverlayRenderer r in _renderers)
            {
                try { r.DrawOverlay(unitRect, unit, merc); }
                catch (Exception e) { LogUtil.Error($"IUnitOverlayRenderer {r.GetType().Name} threw in DrawOverlay: {e}"); }
            }
        }
    }
}
