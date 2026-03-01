using System;
using CombatExtended;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace FactionColonies.CE
{
    /// <summary>
    /// Registers the CE bridge on game startup.
    /// This class only exists in Empire.CE.dll, which is only loaded when CE is active
    /// (via LoadFolders.xml conditional loading).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CombatExtendedInit
    {
        static CombatExtendedInit()
        {
            CombatExtendedUtil.Bridge = new CombatExtendedBridge();
            LogUtil.MessageForce("Combat Extended compatibility module loaded.");
        }
    }

    /// <summary>
    /// Direct CE API implementation of ICombatExtendedBridge.
    /// Uses direct type references instead of reflection for all public CE APIs.
    /// Only LoadoutPropertiesExtension's private methods still require Traverse.
    /// </summary>
    public class CombatExtendedBridge : ICombatExtendedBridge
    {
        public void EquipWeaponWithAmmo(Pawn pawn, ThingWithComps weapon)
        {
            if (pawn == null || weapon == null) return;

            try
            {
                var compInventory = pawn.TryGetComp<CompInventory>();
                if (compInventory == null) return;

                var loadoutProps = new LoadoutPropertiesExtension();

                // These are private methods in CE — Traverse is required
                Traverse.Create(loadoutProps).Method("LoadWeaponWithRandAmmo", weapon).GetValue();
                Traverse.Create(loadoutProps).Method("TryGenerateAmmoFor",
                    new object[] { weapon, compInventory, 3 }).GetValue();

                compInventory.UpdateInventory();
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed to equip CE ammo for {pawn.LabelShort}: {e.Message}");
            }
        }

        public void UpdateInventory(Pawn pawn)
        {
            if (pawn == null) return;

            try
            {
                pawn.TryGetComp<CompInventory>()?.UpdateInventory();
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed to update CE inventory for {pawn.LabelShort}: {e.Message}");
            }
        }

        public bool LaunchFireSupportProjectile(ThingDef ammoDef, Map map, IntVec3 source, IntVec3 target)
        {
            try
            {
                // Resolve the AmmoDef to a CE projectile
                ThingDef projectileDef = ResolveAmmoProjectile(ammoDef);
                if (projectileDef == null) return false;

                // Spawn the projectile at the source (map edge)
                var projectile = (ProjectileCE)GenSpawn.Spawn(projectileDef, source, map);
                if (projectile == null) return false;

                // Read gravity from the spawned instance (set during Spawn from ProjectilePropertiesCE)
                float gravity = (float)projectile.gravity;
                if (gravity <= 0f) gravity = 9.8f;

                // Calculate trajectory
                Vector2 sourceVec = new Vector2(source.x, source.z);
                Vector2 destVec = new Vector2(target.x, target.z);
                Vector3 delta = destVec - sourceVec;
                float range = delta.magnitude;

                float shotSpeed = 100f;
                float shotHeight = 10f;
                float shotRotation = (-90f + 57.29578f * Mathf.Atan2(delta.y, delta.x)) % 360;
                float shotAngle = CE_Utility.GetShotAngle(shotSpeed, range, shotHeight, true, gravity);

                projectile.Launch(
                    FactionCache.PlayerColonyFaction?.leader,
                    sourceVec,
                    shotAngle,
                    shotRotation,
                    shotHeight,
                    shotSpeed,
                    null,
                    -1f);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.Error($"CE fire support launch failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resolve an AmmoDef to the ThingDef of its CE projectile via direct API.
        /// </summary>
        private static ThingDef ResolveAmmoProjectile(ThingDef ammoDef)
        {
            if (!(ammoDef is AmmoDef ceAmmo)) return null;

            var ammoSetDefs = ceAmmo.AmmoSetDefs;
            if (ammoSetDefs == null || ammoSetDefs.Count == 0) return null;

            foreach (AmmoLink link in ammoSetDefs[0].ammoTypes)
            {
                if (link.ammo == ceAmmo)
                {
                    return link.projectile;
                }
            }

            return null;
        }
    }
}
