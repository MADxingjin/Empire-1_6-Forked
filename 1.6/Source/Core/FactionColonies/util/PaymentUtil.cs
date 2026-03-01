using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using Verse;
using Verse.Noise;

namespace FactionColonies
{
    public class PaymentUtil
    {
        public static (List<BillFC>, List<BillFC>) returnBillTypes(List<BillFC> bills)
        {
            List<BillFC> positiveBills = new List<BillFC>();
            List<BillFC> negativeBills = new List<BillFC>();

            foreach (BillFC bill in bills)
            {
                if (bill.taxes.silverAmount >= 0)
                {
                    positiveBills.Add(bill);
                }
                else
                {
                    negativeBills.Add(bill);
                }
            }

            return (negativeBills, positiveBills);
        }

        public static void autoresolveBills(List<BillFC> bills)
        {
            int resolvedBills = 0;

            (List<BillFC> negativeBills, List<BillFC> positiveBills) = returnBillTypes(bills);


            //Go through each negative bill
            //cycle through each positive bill
            //subtract silver from positive bill until negative bill = 0
            //if negative bill equals zero, move to next.

            //if make it to the end of the positive bills, goto function to check if there's enough silver. If so, pay, if not, return the function
            Reset:
            foreach (BillFC negativeBill in negativeBills)
            {
                ResetInner:
                foreach (BillFC positiveBill in positiveBills)
                {
                    float result = positiveBill.taxes.silverAmount + negativeBill.taxes.silverAmount;
                    if (result == 0)
                    {
                        //LogUtil.Message("Equal");
                        //if bills cancel eachother out
                        //resolve positive bill and negative bill
                        positiveBill.taxes.silverAmount = 0;
                        negativeBill.taxes.silverAmount = 0;
                        positiveBill.resolve();
                        negativeBill.resolve();
                        resolvedBills += 2;
                        (negativeBills, positiveBills) = returnBillTypes(bills);
                        goto Reset;
                    }
                    else if (result > 0)
                    {
                        //LogUtil.Message("More");
                        //if positive bill greater than negative bill
                        positiveBill.taxes.silverAmount = result;
                        negativeBill.taxes.silverAmount = 0;
                        negativeBill.resolve();
                        resolvedBills++;
                        (negativeBills, positiveBills) = returnBillTypes(bills);
                        goto Reset;
                    }
                    else if (result < 0)
                    {
                        //LogUtil.Message("Less");
                        //if negative bill is greater (technically lesser) than positive bill
                        positiveBill.taxes.silverAmount = 0;
                        negativeBill.taxes.silverAmount = result;
                        positiveBill.resolve();
                        resolvedBills++;
                        (negativeBills, positiveBills) = returnBillTypes(bills);
                        goto ResetInner;
                    }
                }

                //if looped through all positive bills, attempt to resolve

                if (negativeBill.attemptResolve())
                {
                    (negativeBills, positiveBills) = returnBillTypes(bills);
                    resolvedBills++;
                }
            }

            ResetOuter:
            foreach (BillFC positiveBill in positiveBills)
            {
                positiveBill.resolve();
                resolvedBills++;
                (negativeBills, positiveBills) = returnBillTypes(bills);
                goto ResetOuter;
            }

            Messages.Message(TranslatorFormattedStringExtensions.Translate("NumberTaxesHasBeenSolved", resolvedBills),
                MessageTypeDefOf.NeutralEvent);
        }

        public static void placeThing(Thing thing)
        {
            Map taxMap = GetActiveTaxDeliveryMap();
            
            IntVec3 intvec;
            if (checkForActiveTaxDeliverySpot(out intvec, out taxMap))
            {
                // Found an active tax delivery spot, use it
                GenPlace.TryPlaceThing(thing, intvec, taxMap, ThingPlaceMode.Near);
            }
            else if (checkForTaxSpot(taxMap, out intvec))
            {
                // Found regular tax spot on the tax map
                GenPlace.TryPlaceThing(thing, intvec, taxMap, ThingPlaceMode.Near);
            }
            else
            {
                // Fallback to drop spot on tax map
                intvec = DropCellFinder.TradeDropSpot(taxMap);
                GenPlace.TryPlaceThing(thing, intvec, taxMap, ThingPlaceMode.Near);
            }
        }

        public static void deliverThings(FCEvent evt, Letter let = null, Message msg = null)
        {
            DeliveryEvent.Action(evt, let, msg);
        }


        public static void deliverThings(List<Thing> things, int source, Letter let = null, Message msg = null)
        {
            DeliveryEvent.CreateDeliveryEvent(things, source, let, msg);
        }

        public static bool paySilver(int amount)
        {
            Paid:
            while (amount > 0)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map.IsPlayerHome)
                    {
                    List:
                        foreach (Thing item in map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(s => s.IsInAnyStorage() == true))
                        {
                            //if silver, add to count
                            if (amount - item.stackCount < 0) //if removing silver would pay too much
                            {
                                int overdraw = -1 * (amount - item.stackCount);
                                amount -= (item.stackCount - overdraw);
                                item.SplitOff(item.stackCount - overdraw).Destroy(DestroyMode.Vanish);
                                goto Paid;
                            }
                            else if (amount - item.stackCount > 0) //if removing silver would leave some
                            {
                                amount -= item.stackCount;
                                item.Destroy(DestroyMode.Vanish);
                                goto List;
                            }
                            else if (amount - item.stackCount == 0) //if removing silver will make amount = 0
                            {
                                amount -= item.stackCount;
                                item.Destroy(DestroyMode.Vanish);
                                goto Paid;
                            }
                        }
                    }
                }
            }

            return true;
        }
        public static int getSilver()
        {
            int silver = 0;

            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where(s => s.IsInAnyStorage() == true))
                    {
                        silver += thing.stackCount;
                    }
                }
            }

            //LogUtil.Message("getSilver {silver}");
            return silver;
        }

        public static bool checkForTaxSpot(Map map, out IntVec3 dropSpot)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist.Where(b => b.def.defName == "TaxSpot"))
            {
                if (building is Building_TaxSpot taxSpot && taxSpot.IsActiveTaxDeliverySpot)
                {
                    dropSpot = taxSpot.Position;
                    return true;
                }
            }

            dropSpot = new IntVec3();
            return false;
        }

        public static ThingSetMakerParams returnThingSetMakerParams(int baseValue, int rangeMod)
        {
            ThingSetMakerParams parms = new ThingSetMakerParams();
            parms.techLevel = Find.FactionManager.OfPlayer.def.techLevel;
            parms.totalMarketValueRange = new FloatRange(baseValue - rangeMod, baseValue + rangeMod);
            return parms;
        }

        public static List<Thing> generateRaidLoot(int lootLevel, TechLevel techLevel)
        {
            FactionFC faction = FactionCache.FactionComp;

            float trait_LootMulitplier = 1f;
            if (faction.hasTrait(FCPolicyDefOf.raiders))
                trait_LootMulitplier = 1.2f;

            List<Thing> things = new List<Thing>();
            ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
            ThingSetMakerParams param = new ThingSetMakerParams();
            param.totalMarketValueRange = new FloatRange((500 + (lootLevel * 200)) * trait_LootMulitplier,
                (1000 + (lootLevel * 500)) * trait_LootMulitplier);
            param.filter = new ThingFilter();
            param.techLevel = techLevel;
            param.countRange = new IntRange(3, 20);

            //set allow
            param.filter.SetAllow(ThingCategoryDefOf.Weapons, true);
            param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
            param.filter.SetAllow(ThingCategoryDefOf.BuildingsArt, true);
            param.filter.SetAllow(ThingCategoryDefOf.Drugs, true);
            param.filter.SetAllow(ThingCategoryDefOf.Items, true);
            param.filter.SetAllow(ThingCategoryDefOf.Medicine, true);
            param.filter.SetAllow(ThingCategoryDefOf.Techprints, true);
            param.filter.SetAllow(ThingCategoryDefOf.Buildings, true);

            //set disallow
            param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);

            things = thingSetMaker.Generate(param);
            return things;
        }

        public static Pawn generatePrisoner(Faction faction)
        {
            Pawn pawn;

            PawnKindDef raceChoice;
            raceChoice = faction.RandomPawnKind();

            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: raceChoice,
                faction: FactionCache.PlayerColonyFaction, context: PawnGenerationContext.NonPlayer, tile: -1, 
                forceGenerateNewPawn: false, allowDead: false, allowDowned: false, 
                canGeneratePawnRelations: false, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0, 
                forceAddFreeWarmLayerIfNeeded: false, allowGay: false, allowFood: false, allowAddictions: false, 
                inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, 
                worldPawnFactionDoesntMatter: false, biocodeWeaponChance: 0, extraPawnForExtraRelationChance: null, 
                relationWithExtraPawnChanceFactor: 0));
            pawn.equipment.DestroyAllEquipment();
            pawn.apparel.DestroyAll();
            pawn.SetFaction(faction);
            pawn.guest.guestStatusInt = GuestStatus.Prisoner;

            return pawn;
        }

        //TODO: this function isn't *really* based on resource types, but look into making this more modular anyways, based
        //      on resourcetypes
        public static List<Thing> generateThing(double valueBase, string resourceOfThing)
        {
            regen:
            List<Thing> things = new List<Thing>();
            ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
            ThingSetMakerParams param = new ThingSetMakerParams();
            param.totalMarketValueRange = new FloatRange((float) (valueBase - 300), (float) (valueBase + 300));
            param.filter = new ThingFilter();
            param.techLevel = FactionCache.PlayerColonyFaction.def.techLevel;

            switch (resourceOfThing)
            {
                case "food": //food
                    param.filter.SetAllow(ThingCategoryDefOf.Foods, true);
                    param.countRange = new IntRange(1, 1);
                    break;
                case "weapons": //weapons
                    param.filter.SetAllow(ThingCategoryDefOf.Weapons, true);
                    param.qualityGenerator = QualityGenerator.Gift;
                    param.totalMarketValueRange =
                        new FloatRange((float) (valueBase - valueBase * .5), (float) (valueBase * 2));
                    param.countRange = new IntRange(1, 1);
                    break;
                case "apparel": //apparel
                    param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
                    param.qualityGenerator = QualityGenerator.Gift;
                    param.totalMarketValueRange =
                        new FloatRange((float) (valueBase - valueBase * .5), (float) (valueBase * 2));
                    param.countRange = new IntRange(1, 1);
                    break;
                case "armor": //armor
                    param.qualityGenerator = QualityGenerator.Gift;
                    param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamed("ApparelArmor"), true);
                    param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
                    param.countRange = new IntRange(1, 1);
                    param.totalMarketValueRange =
                        new FloatRange((float) (valueBase - valueBase * .5), (float) (valueBase * 2));
                    break;
                case "animals": //animals
                    thingSetMaker = new ThingSetMaker_Animals();
                    param.techLevel = TechLevel.Undefined;
                    param.totalMarketValueRange =
                        new FloatRange((float) (valueBase - valueBase * .5), (float) (valueBase * 1.5));
                    //param.countRange = new IntRange(1,4);
                    break;
                case "logging": //Logging
                    param.filter.SetAllow(ThingDefOf.WoodLog, true);
                    param.countRange = new IntRange(1, 10);
                    break;
                case "mining": //Mining
                    param.filter.SetAllow(StuffCategoryDefOf.Metallic, true);
                    param.filter.SetAllow(ThingDefOf.Silver, false);
                    //Android shit?
                    param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);
                    //Remove RimBees Beeswax
                    param.filter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("RB_Waxy"), false);
                    //Remove Alpha Animals skysteel
                    param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("AA_SkySteel"), false);
                    param.countRange = new IntRange(1, 10);
                    break;
                case "drugs": //drugs
                    param.filter.SetAllow(ThingCategoryDefOf.Drugs, true);
                    param.countRange = new IntRange(1, 2);
                    break;
                default: //log error
                    LogUtil.Error("This is an error. Report this to the dev. generateThing - nonexistent case ({resourceOfThing})");
                    break;
            }

            //LogUtil.Message(resourceID.ToString());


            //thingSetMaker.root
            things = thingSetMaker.Generate(param);
            if (PaymentUtil.returnValueOfTithe(things) < param.totalMarketValueRange.Value.min)
            {
                goto regen;
            }

            return things;
        }

        public static double returnValueOfTithe(List<Thing> things)
        {
            double totalValue = 0;
            foreach (Thing thing in things)
            {
                //LogUtil.Message(thing.def + " #" + thing.stackCount + " $" + thing.stackCount * thing.MarketValue);
                totalValue += thing.stackCount * thing.MarketValue;
            }

            //LogUtil.Message("Total Value: $" + totalValue);
            return totalValue;
        }

        private static Map GetActiveTaxDeliveryMap()
        {
            // First try to find a map with an active tax delivery spot
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_TaxSpot taxSpot && taxSpot.IsActiveTaxDeliverySpot)
                    {
                        return map;
                    }
                }
            }
            
            // Fallback to existing tax map logic
            return FactionCache.FactionComp.TaxMap;
        }

        public static bool checkForActiveTaxDeliverySpot(out IntVec3 dropSpot, out Map taxMap)
        {
            // Search all player home maps for an active tax delivery spot
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_TaxSpot taxSpot && taxSpot.IsActiveTaxDeliverySpot)
                    {
                        dropSpot = building.Position;
                        taxMap = map;
                        return true;
                    }
                }
            }
            
            dropSpot = IntVec3.Invalid;
            taxMap = null;
            return false;
        }
    }
}