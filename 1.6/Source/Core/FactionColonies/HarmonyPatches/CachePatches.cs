using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch(typeof(Game), "Dispose")]
    class CachePatches
    {
        /* Simple postfix to invalidate the static FactionCache when loading a save or returning to the main menu. */
        public static void Postfix()
        {
            FactionCache.InvalidateCache();
        }
    }
}
