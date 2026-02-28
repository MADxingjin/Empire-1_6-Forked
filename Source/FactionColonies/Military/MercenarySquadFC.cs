using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{

    public class MercenarySquadFC : IExposable, ILoadReferenceable
    {
        public int loadID = -1;
        public string name;
        public List<Mercenary> mercenaries = new List<Mercenary>();
        public List<Mercenary> animals = new List<Mercenary>();
        public WorldSettlementFC settlement;
        public bool isTraderCaravan;
        public bool isDeployed;
        public bool isExtraSquad;
        public int timeDeployed;
        public IntVec3 orderLocation;
        public bool hitMap;
        public int dead;
        public MilSquadFC outfit;
        public List<ThingWithComps> UsedWeaponList;
        public List<Apparel> UsedApparelList;
        public int tickChanged;
        public bool hasLord;
        public Map map;
        public Lord lord;
        public XenotypeDef xenotype1;
        public List<Gene> GeneList;

        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref mercenaries, "mercenaries", LookMode.Deep);
            Scribe_Collections.Look(ref animals, "animals", LookMode.Deep);
            Scribe_Values.Look(ref isTraderCaravan, "isTraderCaravan");
            Scribe_Values.Look(ref isDeployed, "isDeployed");
            Scribe_Values.Look(ref isExtraSquad, "isExtraSquad");
            Scribe_Values.Look(ref hitMap, "hitMap");
            Scribe_References.Look(ref outfit, "outfit");
            Scribe_Values.Look(ref dead, "dead");
            Scribe_Collections.Look(ref UsedWeaponList, "UsedWeaponList", LookMode.Reference);
            Scribe_Collections.Look(ref UsedApparelList, "UsedApparelList", LookMode.Reference);
            Scribe_References.Look(ref settlement, "Settlement");
            Scribe_Values.Look(ref tickChanged, "tickChanged");
            Scribe_Values.Look(ref timeDeployed, "timeDeployed", -1);
            Scribe_Values.Look(ref orderLocation, "orderLocation");
            Scribe_Values.Look(ref hasLord, "hasLord");
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref lord, "lord");
        }

        public string GetUniqueLoadID()
        {
            return $"MercenarySquadFC_{loadID}";
        }

        public List<Mercenary> EquippedMercenaries
        {
            get
            {
                return mercenaries.Where(merc => (merc.pawn.apparel.WornApparel.Any()
                                                  || merc.pawn.equipment.AllEquipmentListForReading.Any()
                                                  || merc.animal != null) && merc.deployable).ToList();
            }
        }

        public List<Pawn> EquippedMercenaryPawns
        {
            get
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Mercenary merc in EquippedMercenaries)
                {
                    list.Add(merc.pawn);
                }

                return list;
            }
        }

        public List<Pawn> EquippedAnimalMercenaries
        {
            get
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Mercenary animal in animals)
                {
                    list.Add(animal.pawn);
                }

                return list;
            }
        }

        public List<Pawn> AllEquippedMercenaryPawns
        {
            get
            {
                List<Pawn> list = EquippedMercenaries.Select(merc => merc.pawn).ToList();

                list.AddRange(EquippedAnimalMercenaries);
                return list;
            }
        }

        public List<Pawn> AllDeployedMercenaryPawns
        {
            get
            {
                List<Pawn> list = new List<Pawn>();
                foreach (Mercenary merc in DeployedMercenaries)
                {
                    list.Add(merc.pawn);
                }

                foreach (Mercenary animal in DeployedMercenaryAnimals)
                {
                    list.Add(animal.pawn);
                }

                return list;
            }
        }

        public List<Mercenary> DeployedMercenaries
        {
            get
            {
                List<Mercenary> pawns = new List<Mercenary>();
                foreach (Mercenary merc in mercenaries)
                {
                    if (merc.pawn.Map != null)
                    {
                        pawns.Add(merc);
                    }
                }

                return pawns;
            }
        }

        public List<Mercenary> DeployedMercenaryAnimals
        {
            get
            {
                List<Mercenary> pawns = new List<Mercenary>();
                foreach (Mercenary merc in animals)
                {
                    if (merc.pawn.Map != null)
                    {
                        pawns.Add(merc);
                    }
                }
                return pawns;
            }
        }
        public WorldSettlementFC getSettlement
        {
            get
            {
                if (settlement != null)
                {
                    return settlement;
                }

                foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
                {
                    if (settlement.MilitaryComp?.militarySquad != null && settlement.MilitaryComp?.militarySquad == this)
                    {
                        this.settlement = settlement;
                        return settlement;
                    }
                }

                return null;
            }
        }

        public void changeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
        }

        public void initiateSquad()
        {
            mercenaries = new List<Mercenary>();
            UsedApparelList = new List<Apparel>();
            UsedWeaponList = new List<ThingWithComps>();

            if (outfit == null)
            {
                for (int k = 0; k < 30; k++)
                {
                    Mercenary pawn = new Mercenary(true);
                    createNewPawn(ref pawn, null, null);
                    // Only add if pawn was successfully created
                    if (pawn?.pawn != null)
                    {
                        mercenaries.Add(pawn);
                    }
                    else
                    {
                        LogUtil.Warning($"Failed to create mercenary {k + 1}/30 during squad initiation.");
                    }
                }
            }
            else
            {
                for (int k = 0; k < 30; k++)
                {
                    Mercenary pawn = new Mercenary(true);
                    createNewPawn(ref pawn, outfit.units[k].pawnKind, outfit.units[k].xenotype);
                    // Only add if pawn was successfully created
                    if (pawn?.pawn != null)
                    {
                        mercenaries.Add(pawn);
                    }
                    else
                    {
                        LogUtil.Warning($"Failed to create mercenary {k + 1}/30 for unit {outfit.units[k]?.name ?? "unknown"}.");
                    }
                }
            }

            LogUtil.Message($"initiateSquad mercenary count : {mercenaries.Count()}");
            //this.debugMercenarySquad();
            if (loadID == -1)
            {
                loadID = FactionCache.FactionComp.GetNextMercenarySquadID();
            }

            if (outfit != null)
            {
                OutfitSquad(outfit);
            }
        }

        public void resetNeeds()
        {
            foreach (Pawn merc in AllEquippedMercenaryPawns)
            {
                if (merc.health == null)
                    merc.health = new Pawn_HealthTracker(merc);
                HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(merc);
                if (merc.needs == null)
                    merc.needs = new Pawn_NeedsTracker(merc);
                if (merc.needs.food == null)
                    merc.needs.food = new Need_Food(merc);
                if (merc.needs.rest == null)
                    merc.needs.rest = new Need_Rest(merc);
                if (!merc.AnimalOrWildMan() && merc.needs.joy == null)
                    merc.needs.joy = new Need_Joy(merc);
                merc.needs.food.CurLevel = merc.needs.food.MaxLevel;
                merc.needs.rest.CurLevel = merc.needs.rest.MaxLevel;
                if (!merc.AnimalOrWildMan())
                {
                    merc.needs.joy.CurLevel = merc.needs.joy.MaxLevel;
                    merc.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("FC_Mercenary"));
                }
            }
        }

        public void removeDroppedEquipment()
        {
            while (DroppedApparel.Any())
            {
                Apparel apparel = DroppedApparel[0];
                UsedApparelList.Remove(DroppedApparel[0]);
                if (apparel != null && apparel.Destroyed == false)
                {
                    apparel.Destroy();
                }
            }

            while (DroppedWeapons.Any())
            {
                ThingWithComps weapon = DroppedWeapons[0];
                UsedWeaponList.Remove(DroppedWeapons[0]);
                if (weapon != null && weapon.Destroyed == false)
                {
                    weapon.Destroy();
                }
            }
        }

        public void createNewAnimal(ref Mercenary merc, PawnKindDef race)
        {
            Pawn newPawn = PawnGenerator.GeneratePawn(FCPawnGenerator.AnimalRequest(race));
            //merc = (Mercenary)newPawn;

            merc.squad = this;
            merc.settlement = settlement;
            //LogUtil.Message($"createNewAnimal | {newPawn.Name}   State: Dead - {newPawn.health.Dead}    Apparel Count: {newPawn.apparel.WornApparel.Count()}");
            merc.pawn = newPawn;
        }

        public void createNewPawn(ref Mercenary merc, PawnKindDef race, XenotypeDef _xenotype)
        {
            XenotypeDef xenotypeChoice = _xenotype;
            PawnKindDef raceChoice = race;
            FactionFC factionFc = FactionCache.FactionComp;

            if (race == null || !factionFc.raceFilter.Allows(raceChoice.race))
            {
                raceChoice = FactionCache.PlayerColonyFaction.RandomPawnKind();
            }

            // Try to generate pawn with the requested kind
            Pawn newPawn = null;
            try
            {
                newPawn = PawnGenerator.GeneratePawn(FCPawnGenerator.WorkerOrMilitaryRequest(raceChoice, xenotypeChoice));
                
                // Set faction after generation (since we generate without faction to avoid xenotype forcing)
                if (newPawn != null && newPawn.Faction == null)
                {
                    var empireFaction = FactionCache.PlayerColonyFaction;
                    if (empireFaction != null)
                    {
                        newPawn.SetFaction(empireFaction);
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.Warning($"Failed to generate pawn with kind {raceChoice?.defName}: {ex.Message}");
            }
            
            // Fallback 1: Try with Baseliner xenotype and NO faction (avoids faction xenotype forcing)
            if (newPawn == null)
            {
                LogUtil.Warning($"Pawn generation failed for {raceChoice?.defName}. Trying Baseliner fallback without faction.");
                try
                {
                    var simpleRequest = new PawnGenerationRequest(
                        kind: PawnKindDefOf.Colonist,
                        faction: null, // NO faction - this prevents faction xenotype forcing
                        context: PawnGenerationContext.NonPlayer,
                        tile: -1,
                        forceGenerateNewPawn: false,
                        allowDead: false,
                        allowDowned: false,
                        canGeneratePawnRelations: false, // No relations for factionless pawns
                        mustBeCapableOfViolence: true,
                        colonistRelationChanceFactor: 0,
                        forceAddFreeWarmLayerIfNeeded: false,
                        allowGay: true,
                        allowFood: true,
                        allowAddictions: false,
                        forcedXenotype: XenotypeDefOf.Baseliner // Force Baseliner - guaranteed violence capable
                    );
                    newPawn = PawnGenerator.GeneratePawn(simpleRequest);
                    
                    // Set the faction after generation
                    if (newPawn != null)
                    {
                        var empireFaction = FactionCache.PlayerColonyFaction;
                        if (empireFaction != null)
                        {
                            newPawn.SetFaction(empireFaction);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Warning($"Baseliner fallback also failed: {ex.Message}");
                }
            }
            
            // Fallback 2: Absolute minimal request - no faction, no xenotype, no violence requirement
            if (newPawn == null)
            {
                LogUtil.Warning("All standard generation failed. Trying minimal fallback.");
                try
                {
                    var fallbackRequest = new PawnGenerationRequest(
                        kind: PawnKindDefOf.Colonist,
                        faction: null, // NO faction
                        context: PawnGenerationContext.NonPlayer,
                        tile: -1,
                        forceGenerateNewPawn: false,
                        allowDead: false,
                        allowDowned: false,
                        canGeneratePawnRelations: false,
                        mustBeCapableOfViolence: false, // Allow non-violent as absolute last resort
                        colonistRelationChanceFactor: 0,
                        forceAddFreeWarmLayerIfNeeded: false,
                        allowGay: true,
                        allowFood: true,
                        allowAddictions: false
                    );
                    newPawn = PawnGenerator.GeneratePawn(fallbackRequest);
                    
                    // Set the faction after generation
                    if (newPawn != null)
                    {
                        var empireFaction = FactionCache.PlayerColonyFaction;
                        if (empireFaction != null)
                        {
                            newPawn.SetFaction(empireFaction);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Critical - all pawn generation attempts failed: {ex.Message}");
                }
            }
            
            // Final check - if still null, we cannot proceed
            if (newPawn == null)
            {
                LogUtil.Error("Critical error - could not generate any pawn for mercenary squad. Skipping this mercenary.");
                return;
            }
            
            newPawn.apparel?.DestroyAll();
            newPawn.equipment?.DestroyAllEquipment();
            merc.squad = this;
            merc.settlement = settlement;
            merc.pawn = newPawn;

        }
        public void updateSquadStats(int level)
        {
            foreach (Mercenary merc in mercenaries)
            {
                if (merc?.pawn?.skills == null) continue;
                
                var shooting = merc.pawn.skills.GetSkill(SkillDefOf.Shooting);
                var melee = merc.pawn.skills.GetSkill(SkillDefOf.Melee);
                var medicine = merc.pawn.skills.GetSkill(SkillDefOf.Medicine);
                
                if (shooting != null) shooting.Level = Math.Min(level * 2, 20);
                if (melee != null) melee.Level = Math.Min(level * 2, 20);
                if (medicine != null) medicine.Level = Math.Min(level * 1, 20);
            }
        }

        public void PassPawnToDeadMercenaries(Mercenary merc)
        {
            //If ever add past dead pawns, use this code
            /*MilitaryCustomizationUtil util = FactionCache.FactionComp.militaryCustomizationUtil;
            Mercenary pwn = new Mercenary(true);
            if (merc.animal != null)
            {
                Mercenary animal = new Mercenary(true);
                animal = merc.animal;
                util.deadPawns.Add(animal);
            }
            pwn = merc;*/

            //util.deadPawns.Add(pwn);
            Mercenary pawn2 = new Mercenary(true);
            PawnKindDef kindDef = merc?.pawn?.kindDef ?? PawnKindDefOf.Colonist;
            XenotypeDef xenotype = merc?.pawn?.genes?.Xenotype ?? XenotypeDefOf.Baseliner;
            createNewPawn(ref pawn2, kindDef, xenotype);
            
            // Only replace if new pawn was successfully created
            if (pawn2?.pawn != null)
            {
                mercenaries.Replace(merc, pawn2);
            }
            else
            {
                LogUtil.Warning("Failed to replace dead mercenary with new pawn.");
            }
        }

        public void HealPawn(Mercenary merc)
        {
            if (merc?.pawn?.health != null)
            {
                merc.pawn.health.Reset();
            }
        }

        public void StripSquad()
        {
            for (int count = 0; count < mercenaries.Count && count < 30; count++)
            {
                if (mercenaries[count]?.pawn != null)
                {
                    StripPawn(mercenaries[count]);
                }
            }
        }

        public void OutfitSquad(MilSquadFC outfit)
        {
            FactionFC faction = FactionCache.FactionComp;
            int count = 0;
            this.outfit = outfit;
            UsedWeaponList = new List<ThingWithComps>();
            UsedApparelList = new List<Apparel>();
            animals = new List<Mercenary>();
            GeneList = new List<Gene>();
            foreach (MilUnitFC loadout in outfit.units)
            {
                try
                {
                    // Ensure we have enough mercenaries in the list
                    while (mercenaries.Count <= count)
                    {
                        Mercenary newMerc = new Mercenary(true);
                        createNewPawn(ref newMerc, loadout?.pawnKind, loadout?.xenotype);
                        if (newMerc?.pawn != null)
                        {
                            mercenaries.Add(newMerc);
                        }
                        else
                        {
                            LogUtil.Warning($"Could not create mercenary for slot {count}.");
                            break;
                        }
                    }
                    
                    // Skip if we still don't have enough mercenaries
                    if (count >= mercenaries.Count || mercenaries[count]?.pawn == null)
                    {
                        LogUtil.Warning($"Skipping outfit slot {count} - no valid mercenary available.");
                        count++;
                        continue;
                    }

                    if (mercenaries[count]?.pawn?.kindDef != loadout.pawnKind || mercenaries[count].pawn.Dead)
                    {
                        Mercenary pawn = new Mercenary(true);
                        createNewPawn(ref pawn, loadout.pawnKind, loadout.xenotype);
                        // Only replace if new pawn was successfully created
                        if (pawn?.pawn != null)
                        {
                            mercenaries.Replace(mercenaries[count], pawn);
                        }
                        else
                        {
                            LogUtil.Warning($"Failed to create replacement pawn for slot {count}.");
                        }
                    }
                    
                    // Skip operations if pawn is null
                    if (mercenaries[count]?.pawn == null)
                    {
                        count++;
                        continue;
                    }

                    StripPawn(mercenaries[count]);
                    HealPawn(mercenaries[count]);
                    if (loadout != null)
                    {
                        //mercenaries[count];
                        //StripPawn(mercenaries[count]);
                        EquipPawn(mercenaries[count], loadout);
                        if (loadout.animal != null)
                        {
                            Mercenary animal = new Mercenary(true);
                            createNewAnimal(ref animal, loadout.animal);
                            animal.handler = mercenaries[count];
                            mercenaries[count].animal = animal;
                            animals.Add(animal);
                        }

                        mercenaries[count].loadout = loadout;
                        mercenaries[count].deployable = mercenaries[count].loadout != faction.militaryCustomizationUtil.blankUnit;
                    }

                    if (mercenaries[count]?.pawn?.equipment?.AllEquipmentListForReading != null)
                    {
                        UsedWeaponList.AddRange(mercenaries[count].pawn.equipment.AllEquipmentListForReading);

                        //add single check at start of load and mark variable
                    }

                    if (mercenaries[count]?.pawn?.apparel?.WornApparel != null)
                    {
                        UsedApparelList.AddRange(mercenaries[count].pawn.apparel.WornApparel);
                    }

                }
                catch (Exception e)
                {
                    LogUtil.Error("Something went wrong when outfitting a squad: " + e.Message);
                    bool isNullOrEmpty = mercenaries.NullOrEmpty();
                    LogUtil.Error("Mercanaries NullOrEmpty: " + isNullOrEmpty);

                    if (isNullOrEmpty)
                    {
                        LogUtil.Error("Number of Mercs: " + mercenaries.Count);
                        LogUtil.Error("Any mercenary or pawn is null: " + mercenaries.Any(mercenary => mercenary?.pawn == null));
                    }
                }
                count++;
            }

            //debugMercenarySquad();
        }


        public void StripPawn(Mercenary merc)
        {
            if (merc?.pawn == null) return;

            merc.pawn.apparel?.DestroyAll();
            merc.pawn.equipment?.DestroyAllEquipment();
            CombatExtendedUtil.UpdateInventory(merc.pawn);
        }

        public void EquipPawn(Mercenary merc, MilUnitFC loadout)
        {
            foreach (SavedThing apparelDef in loadout.apparel)
            {
                Thing thing = apparelDef.CreateThing();
                if (thing is Apparel ap)
                {
                    thing.SetColor(Color.white);
                    merc.pawn.apparel.Wear(ap);
                }
            }

            foreach (SavedThing weaponDef in loadout.weapons)
            {
                Thing weaponThing = weaponDef.CreateThing();
                if (weaponThing is ThingWithComps twc)
                {
                    merc.pawn.equipment.AddEquipment(twc);

                    if (CombatExtendedUtil.IsCELoaded)
                    {
                        CombatExtendedUtil.EquipWeaponWithAmmo(merc.pawn, merc.pawn.equipment.Primary);
                    }
                }
            }
        }

        public List<ThingWithComps> DroppedWeapons
        {
            get
            {
                List<ThingWithComps> tmpList = new List<ThingWithComps>();

                foreach (ThingWithComps weapon in UsedWeaponList)
                {
                    if (weapon.ParentHolder is Pawn_EquipmentTracker)
                    {
                        if ((((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Faction ==
                             FactionCache.PlayerColonyFaction ||
                             ((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Faction ==
                             Find.FactionManager.OfPlayer) &&
                            ((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Dead == false)
                        {
                        }
                        else
                        {
                            tmpList.Add(weapon);
                        }
                    }
                    else
                    {
                        tmpList.Add(weapon);
                    }
                }

                return tmpList;
            }
        }

        public List<Apparel> DroppedApparel
        {
            get
            {
                List<Apparel> tmpList = new List<Apparel>();

                foreach (Apparel apparel in UsedApparelList)
                {
                    //LogUtil.Message(apparel.ParentHolder.ToString());
                    //LogUtil.Message(apparel.ParentHolder.ParentHolder.ToString());
                    if (apparel.ParentHolder is Pawn_ApparelTracker)
                    {
                        if ((((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Faction ==
                             FactionCache.PlayerColonyFaction ||
                             ((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Faction ==
                             Find.FactionManager.OfPlayer) &&
                            ((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Dead == false)
                        {
                        }
                        else
                        {
                            tmpList.Add(apparel);
                        }
                    }
                    else
                    {
                        tmpList.Add(apparel);
                    }
                }

                return tmpList;
            }
        }


        public void debugMercenarySquad()
        {
            LogUtil.MessageForce("Debug Mercenary Squad");
            foreach (Mercenary merc in mercenaries)
            {
                LogUtil.MessageForce($"\t{merc.pawn.ToString()} \t{merc.pawn.health.Dead.ToString()} \t{merc.pawn.apparel.WornApparelCount} \t{merc.pawn.equipment.AllEquipmentListForReading.Count()}");
            }
        }

        public Mercenary returnPawn(Pawn pawn)
        {
            foreach (Mercenary merc in mercenaries)
            {
                if (merc.pawn == pawn)
                {
                    return merc;
                }
            }

            return null;
        }

        /// <summary>
        /// Makes the squad go into cooldown. Only works on a settlements main squad
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool InitiateCooldownEvent()
        {
            if (!isExtraSquad)
            {
                settlement.MilitaryComp?.cooldownMilitary();
                return true;
            }

            return false;
        }
    }
}