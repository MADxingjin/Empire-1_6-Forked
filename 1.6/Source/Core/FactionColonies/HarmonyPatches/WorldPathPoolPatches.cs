using System;
using System.Threading;
using HarmonyLib;
using RimWorld.Planet;

namespace FactionColonies
{
    /// <summary>
    /// Makes WorldPathPool thread-safe so FCRoadQueue can run A* pathfinding
    /// on a background thread. The lock is only held during pool operations;
    /// the actual A* computation runs completely lock-free.
    /// </summary>
    [HarmonyPatch(typeof(WorldPathPool))]
    [HarmonyPatch("GetEmptyWorldPath")]
    class Patch_WorldPathPool_GetEmptyWorldPath
    {
        internal static readonly object PoolLock = new object();

        static void Prefix()
        {
            Monitor.Enter(PoolLock);
        }

        static Exception Finalizer(Exception __exception)
        {
            Monitor.Exit(PoolLock);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(WorldPath))]
    [HarmonyPatch("ReleaseToPool")]
    class Patch_WorldPath_ReleaseToPool
    {
        static void Prefix()
        {
            Monitor.Enter(Patch_WorldPathPool_GetEmptyWorldPath.PoolLock);
        }

        static Exception Finalizer(Exception __exception)
        {
            Monitor.Exit(Patch_WorldPathPool_GetEmptyWorldPath.PoolLock);
            return __exception;
        }
    }
}
