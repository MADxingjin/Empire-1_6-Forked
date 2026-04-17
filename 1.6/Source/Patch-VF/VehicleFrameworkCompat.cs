using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace FactionColonies.VF
{
    [StaticConstructorOnStartup]
    public static class VehicleFrameworkCompatInit
    {
        static VehicleFrameworkCompatInit()
        {
            new Harmony("com.Matathias.Empire.VF").PatchAll(Assembly.GetExecutingAssembly());
            LogUtil.MessageForce("Vehicle Framework patched");
        }
    }

    /// <summary>
    /// Prefix on CaravanDefend to handle VehicleCaravan pawns correctly.
    /// VehicleCaravan stores passengers inside VehicleRoleHandlers, not in the
    /// base caravan pawn list. Without this patch, vehicles and their passengers
    /// are never spawned onto the defense map.
    /// </summary>
    [HarmonyPatch(typeof(WorldObjectComp_SettlementMilitary))]
    [HarmonyPatch("CaravanDefend")]
    public static class Patch_CaravanDefend_Vehicle
    {
        public static bool Prefix(WorldObjectComp_SettlementMilitary __instance, Caravan caravan)
        {
            if (!(caravan is VehicleCaravan vehicleCaravan))
                return true;

            VehicleCaravanDefend(__instance, vehicleCaravan);
            return false;
        }

        private static void VehicleCaravanDefend(
            WorldObjectComp_SettlementMilitary comp,
            VehicleCaravan vehicleCaravan)
        {
            // Snapshot vehicles and dismounted pawns before destroying the caravan.
            List<VehiclePawn> vehicles = vehicleCaravan.VehiclesListForReading.ListFullCopy();
            List<Pawn> dismounted = vehicleCaravan.DismountedPawnsListForReading.ListFullCopy();

            // Build the combined pawn list: vehicles + dismounted + all passengers aboard.
            // Passengers are included so they end up in CaravanSupporting for post-battle
            // caravan reformation even if the player disembarks them during the fight.
            var allPawns = new List<Pawn>();
            foreach (VehiclePawn vehicle in vehicles)
            {
                allPawns.Add(vehicle);
                allPawns.AddRange(vehicle.AllPawnsAboard);
            }
            allPawns.AddRange(dismounted);

            // Register with the defense system (lord, CaravanSupporting, defenders list).
            // The lord setup callback runs in a deferred LongEventHandler queue, so by
            // the time it fires the pawns are already spawned on the map.
            comp.AddToDefenceFromList(allPawns, vehicleCaravan.Tile);

            if (!vehicleCaravan.Destroyed)
                vehicleCaravan.Destroy();

            Map map = comp.Map;
            IntVec3 enterCell = WorldObjectComp_SettlementMilitary.FindNearEdgeCell(map);

            // Spawn vehicles with passengers still aboard.
            foreach (VehiclePawn vehicle in vehicles)
            {
                IntVec3 loc = CellFinder.RandomSpawnCellForPawnNear(enterCell, map);
                GenSpawn.Spawn(vehicle, loc, map, Rot4.Random);
            }

            // Spawn dismounted pawns normally.
            foreach (Pawn pawn in dismounted)
            {
                IntVec3 loc = CellFinder.RandomSpawnCellForPawnNear(enterCell, map);
                GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
            }
        }
    }
}
