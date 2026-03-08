using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies.util
{
	class DeliveryEvent
	{
		public static TraverseParms DeliveryTraverseParms => new TraverseParms()
		{
			canBashDoors = false,
			canBashFences = false,
			alwaysUseAvoidGrid = false,
			fenceBlocked = false,
			maxDanger = Danger.Deadly,
			mode = TraverseMode.ByPawn
		};

		public static void CreateDeliveryEvent(List<Thing> things, int source, Letter let = null, Message msg = null)
		{
			CreateDeliveryEvent(new FCEvent()
			{
				source = source,
				goods = things,
				customDescription = "",
				timeTillTrigger = Find.TickManager.TicksGame + 10,
				let = let,
				msg = msg
			}); 
		}

		public static void Action(FCEvent evt)
		{
			Action(evt, FactionCache.FactionComp.settlements.FirstOrFallback(settlement => settlement.Tile == evt.source)?.BuildingsComp?.HasBuilding(BuildingFCDefOf.shuttlePort) ?? false);
		}

		public static void Action(FCEvent evt, Letter let = null, Message msg = null, bool CanUseShuttle = false)
		{
			evt.let = let;
			evt.msg = msg;
			Action(evt, CanUseShuttle || (FactionCache.FactionComp.settlements.FirstOrFallback(settlement => settlement.Tile == evt.source)?.BuildingsComp?.HasBuilding(BuildingFCDefOf.shuttlePort) ?? false));
		}

		private static void MakeDeliveryLetterAndMessage(FCEvent evt)
		{
			try
			{
				var notificationMode = FCSettings.taxNotificationMode;
				bool showLetter = notificationMode == TaxNotificationMode.All || notificationMode == TaxNotificationMode.LetterOnly;
				bool showMessage = notificationMode == TaxNotificationMode.All || notificationMode == TaxNotificationMode.MessageOnly;

				if (showLetter)
				{
					if (evt.let != null)
					{
						evt.let.lookTargets = evt.goods;
						Find.LetterStack.ReceiveLetter(evt.let);
					}
					else
					{
						Find.LetterStack.ReceiveLetter("GoodsReceivedFollowing".Translate(evt.def.label.ToLower()), evt.goods.ToLetterString(), LetterDefOf.PositiveEvent, evt.goods);
					}
				}

				if (showMessage && evt.msg != null)
				{
					evt.msg.lookTargets = evt.goods;
					Messages.Message(evt.msg);
				}
				
				if (evt.isDelayed) Messages.Message("deliveryHeldUpArriving".Translate(), evt.goods, MessageTypeDefOf.PositiveEvent);
			} 
			catch
			{
				LogUtil.ErrorOnce("MakeDeliveryLetterAndMessage failed to attach targets to the message", 908347458);
			}
		}

		private static void SendShuttle(FCEvent evt)
		{
			Map playerHomeMap = FactionCache.FactionComp.TaxMap;
			List<ShipLandingArea> landingZones = ShipLandingBeaconUtility.GetLandingZones(playerHomeMap);

			IntVec3 landingCell = DropCellFinder.GetBestShuttleLandingSpot(playerHomeMap, Faction.OfPlayer);

			if (!landingZones.Any() || landingZones.Any(zone => zone.Clear))
			{
				MakeDeliveryLetterAndMessage(evt);
				Thing shuttle = ThingMaker.MakeThing(ThingDefOf.Shuttle);
				TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, evt.goods, shuttle);

				transportShip.ArriveAt(landingCell, playerHomeMap.Parent);
				transportShip.AddJobs(new ShipJobDef[]
				{
								ShipJobDefOf.Unload,
								ShipJobDefOf.FlyAway
				});
			}
			else
			{
				if (!evt.isDelayed)
				{
					Messages.Message(((string)"shuttleLandingBlockedWithItems".Translate(evt.goods.ToLetterString())).Replace("\n", " "), MessageTypeDefOf.RejectInput);
					evt.isDelayed = true;
				}

				if (evt.source == -1) evt.source = playerHomeMap.Tile;

				evt.timeTillTrigger = Find.TickManager.TicksGame + 1000;
				CreateDeliveryEvent(evt);
			}
		}

		private static void SendDropPod(FCEvent evt)
		{
			Map playerHomeMap = FactionCache.FactionComp.TaxMap;
			MakeDeliveryLetterAndMessage(evt);
			DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(playerHomeMap), playerHomeMap, evt.goods, 110, false, false, false, false);
		}

		private static bool DoDelayCaravanDueToDanger(FCEvent evt)
		{
			Map playerHomeMap = FactionCache.FactionComp.TaxMap;
			if (playerHomeMap.dangerWatcher.DangerRating != StoryDanger.None)
			{

				if (!evt.isDelayed)
				{
					Messages.Message(((string)"caravanDangerTooHighWithItems".Translate(evt.goods.ToLetterString())).Replace("\n", " "), MessageTypeDefOf.RejectInput);
					evt.isDelayed = true;
				}

				if (evt.source == -1) evt.source = playerHomeMap.Tile;

				evt.timeTillTrigger = Find.TickManager.TicksGame + 1000;
				CreateDeliveryEvent(evt);
				return true;
			}

			return false;
		}

		private static void SendCaravan(FCEvent evt)
		{
			Map playerHomeMap = FactionCache.FactionComp.TaxMap;
			if (DoDelayCaravanDueToDanger(evt)) return;

			MakeDeliveryLetterAndMessage(evt);
			List<Pawn> pawns = new List<Pawn>();
			List<Pawn> securityGuards = new List<Pawn>();
			
			var factionFC = FactionCache.FactionComp;
			
			// Generate delivery pawns using allowed xenotypes first
			int maxAttempts = 100; // Prevent infinite loops
			int attempts = 0;
			
			while (evt.goods.Count() > 0 && attempts < maxAttempts)
			{
				attempts++;
				try
				{
					Pawn deliveryPawn = null;
					// We set the xenotype in a prefix patch on GeneratePawn, so don't bother with setting the xenotype here
					try
					{
						// Create request that allows ANY xenotype (including non-violent ones)
						var request = FCPawnGenerator.CivilianRequest();
								
						deliveryPawn = PawnGenerator.GeneratePawn(request);
						if (deliveryPawn != null)
						{
							LogUtil.Message($"Successfully generated delivery pawn with civilian request");
						}
					}
					catch (Exception ex)
					{
						LogUtil.Warning($"Failed to generate pawn with civilian request: {ex.Message}");
						continue;
					}
					
					// If no xenotype worked, try a simple delivery request
					if (deliveryPawn == null)
					{
						LogUtil.Warning("Failed to generate pawn with CivilianRequest, trying simple delivery request");
						deliveryPawn = PawnGenerator.GeneratePawn(FCPawnGenerator.SimpleDeliveryRequest());
					}
					
					// If still no pawn, fall back to animals (like wolves)
					if (deliveryPawn == null)
					{
						LogUtil.Warning("Failed to generate human pawn, falling back to animals");
						var availableAnimals = FactionCache.AllCombatAnimalKindDefs
							.OrderByDescending(def => def.combatPower)
							.Take(5)
							.ToList();

						if (availableAnimals.Any())
						{
							var animalRequest = FCPawnGenerator.AnimalRequest(availableAnimals.RandomElement());
							deliveryPawn = PawnGenerator.GeneratePawn(animalRequest);
						}
					}
					
					if (deliveryPawn == null)
					{
						LogUtil.Error("Could not generate any pawn for delivery, skipping item");
						evt.goods.RemoveAt(0); // Remove the item we can't deliver
						continue;
					}

					Thing next = evt.goods.First();

					if (deliveryPawn.carryTracker.innerContainer.TryAdd(next))
					{
						evt.goods.Remove(next);
					}

					pawns.Add(deliveryPawn);
				}
				catch (Exception ex)
				{
					LogUtil.Error($"Error generating pawn for delivery: {ex.Message}");
					evt.goods.RemoveAt(0); // Remove the problematic item
				}
			}

			LogUtil.Message($"# Delivery pawns generated: {pawns.Count}");
			
			if (attempts >= maxAttempts)
			{
				LogUtil.Warning("Reached maximum attempts for generating delivery pawns, some items may not be delivered");
			}
			
			// Always add at least one guard animal for protection, plus extra if caravan is small
			LogUtil.Message("Adding guard animals for delivery caravan protection");
			
			// Add extra capable pawns using allowed xenotypes if caravan is small
			if (pawns.Count < 3)
			{
				int extraPawnsNeeded = 3 - pawns.Count;
				for (int i = 0; i < extraPawnsNeeded; i++)
				{
					try
					{
						Pawn extraPawn = null;
                        // We set the xenotype in a prefix patch on GeneratePawn, so don't bother with setting the xenotype here
                        // Create request that allows ANY xenotype (including non-violent ones)
                        var request = FCPawnGenerator.CivilianRequest();
                        extraPawn = PawnGenerator.GeneratePawn(request);

                        // Fallback to simple request
                        if (extraPawn == null)
						{
							extraPawn = PawnGenerator.GeneratePawn(FCPawnGenerator.SimpleDeliveryRequest());
						}
						
						if (extraPawn != null)
						{
							pawns.Add(extraPawn);
						}
					}
					catch (Exception ex)
					{
						LogUtil.Warning($"Failed to spawn extra pawn: {ex.Message}");
					}
				}
			}
			
			// Add guard animals (like wolves) for protection - always add at least 2 as it's good protection! Keep your highmate-only faction safe!!
			// This protects deliveries by keeping it immersive, adhering to xenotype preferences. Bears and wargs are problematic. 
			var guardAnimals = FactionCache.AllCombatAnimalKindDefs
				.OrderByDescending(def => def.combatPower)
				.Take(5); // Take more options to ensure we can get 2 guards

			// Log available guard animals for debugging
			var availableGuardAnimals = guardAnimals.ToList();
			if (availableGuardAnimals.Any())
			{
				LogUtil.Message($"Available guard animals: {string.Join(", ", availableGuardAnimals.Select(a => $"{a.label} (Combat: {a.combatPower:F0})"))}");
			}

			int guardsAdded = 0;
			foreach (var guardAnimal in guardAnimals)
			{
				try
				{
					Pawn guard = PawnGenerator.GeneratePawn(FCPawnGenerator.AnimalRequest(guardAnimal));
					if (guard != null)
					{
						securityGuards.Add(guard);
						guardsAdded++;
						LogUtil.Message($"Added guard animal: {guardAnimal.label} (Combat Power: {guardAnimal.combatPower:F0})");
						if (guardsAdded >= 2) break; // Always add at least 2 guards
					}
				}
				catch (Exception ex)
				{
					LogUtil.Warning($"Failed to spawn security guard {guardAnimal.label}: {ex.Message}");
				}
			}

			// Combine all pawns
			pawns.AddRange(securityGuards);

			PawnsArrivalModeWorker_EdgeWalkIn pawnsArrivalModeWorker = new PawnsArrivalModeWorker_EdgeWalkIn();
			IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, playerHomeMap);
			parms.spawnRotation = Rot4.FromAngleFlat((((Map)parms.target).Center - parms.spawnCenter).AngleFlat);

			RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, playerHomeMap, CellFinder.EdgeRoadChance_Friendly);

			pawnsArrivalModeWorker.Arrive(pawns, parms);
			
			// Create the lord and ensure all pawns (including animals) are properly assigned
			var lord = LordMaker.MakeNewLord(FCPawnGenerator.WorkerOrMilitaryRequest().Faction, new LordJob_DeliverSupplies(parms.spawnCenter), playerHomeMap, pawns);
			
			// Ensure all guard animals are properly assigned to the lord and will follow the caravan
			foreach (var guard in securityGuards)
			{
				if (guard != null && guard.Map == playerHomeMap)
				{
					// Make sure the animal is assigned to the lord
					if (guard.GetLord() != lord)
					{
						lord.AddPawn(guard);
					}
					
					// Ensure the animal will leave with the caravan by setting it to follow a human pawn
					if (guard.mindState != null)
					{
						var humanPawn = lord.ownedPawns.FirstOrDefault(p => !p.RaceProps.Animal);
						if (humanPawn != null)
						{
							// let's make sure the guard animal stays close to the caravan 
							guard.mindState.duty = new PawnDuty(DutyDefOf.Follow, humanPawn, 3f); // 3 tile radius
						}
					}
				}
			}

		}

		private static void SpawnOnTaxSpot(FCEvent evt)
		{
			MakeDeliveryLetterAndMessage(evt);
			evt.goods.ForEach(thing => PaymentUtil.PlaceThing(thing));
		}

		public static TaxDeliveryMode TaxDeliveryModeForSettlement(bool canUseShuttle, PlanetTile sourceTile)
		{
			WorldSettlementFC settlement = FactionCache.FactionComp.settlements.FirstOrFallback((WorldSettlementFC s) => s.Tile == sourceTile);
			if (settlement != null)
			{
				return settlement.settlementDef.GetTaxDeliveryMode(canUseShuttle, sourceTile);
			}
			LogUtil.Error($"Trying to deliver taxes for a null settlement!");
			return TaxDeliveryMode.Caravan;
		}

		public static void Action(FCEvent evt, bool canUseShuttle = false)
		{
			try
			{
				TaxDeliveryMode taxDeliveryMode = TaxDeliveryModeForSettlement(canUseShuttle, evt.source);

				switch (taxDeliveryMode)
				{
					case TaxDeliveryMode.Caravan:
						SendCaravan(evt);
						break;
					case TaxDeliveryMode.DropPod:
						SendDropPod(evt);
						break;
					case TaxDeliveryMode.Shuttle:
						SendShuttle(evt);
						break;
					default:
						SpawnOnTaxSpot(evt);
						break;
				}
			} 
			catch(Exception e)
			{
				LogUtil.ErrorOnce("Critical delivery failure, spawning things on tax spot instead! Message: " + e.Message + " StackTrace: " + e.StackTrace + " Source: " + e.Source, 77239232);
				evt.goods.ForEach(thing => PaymentUtil.PlaceThing(thing));
			}
		}

		public static void CreateDeliveryEvent(FCEvent evtParams)
		{
			FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.deliveryArrival);
			evt.source = evtParams.source;
			evt.goods = evtParams.goods;
			evt.customDescription = evtParams.customDescription;
			evt.hasCustomDescription = true;
			evt.timeTillTrigger = evtParams.timeTillTrigger;
			evt.let = evtParams.let;
			evt.msg = evtParams.msg;
			evt.isDelayed = evtParams.isDelayed;

			FactionCache.FactionComp.AddEvent(evt);
		}

		public static string ShuttleEventInjuredString
		{
			get
			{
				if (FactionCache.TechTransportPods.IsFinished)
				{
					if (ModsConfig.RoyaltyActive)
					{
						return "transportingInjuredShuttle".Translate();
					}
					return "transportingInjuredDropPod".Translate();
				}
				return "transportingInjuredCaravan".Translate();
			}
		}
		
		public static string ShuttleEventInjuredLostString
		{
			get
			{
				if (FactionCache.TechTransportPods.IsFinished)
				{
					if (ModsConfig.RoyaltyActive)
					{
						return "transportingInjuredShuttleLost".Translate();
					}
					return "transportingInjuredDropPodLost".Translate();
				}
				return "transportingInjuredCaravanLost".Translate();
			}
		}

		public static IntVec3 GetDeliveryCell(TraverseParms traverseParms, Map map)
		{
			if (!PaymentUtil.CheckForTaxSpot(map, out IntVec3 intVec3))
			{
				intVec3 = ValidLandingCell(new IntVec2(1, 1), map, true);
			}

			// Validate that we have a valid starting position
			if (!intVec3.IsValid)
			{
				// Fallback to map center if we somehow got an invalid position
				intVec3 = map.Center;
			}

			IntVec3 oldVec = intVec3;
			for (int i = 0; i < 10; i++)
			{
				// Additional validation before calling CellFinder
				if (intVec3.IsValid && intVec3.InBounds(map))
				{
					if (CellFinder.TryFindRandomReachableNearbyCell(intVec3, map, i, traverseParms, 
						cell => cell.IsValid && cell.InBounds(map) && map.thingGrid.ThingsAt(cell) != null, 
						null, out IntVec3 foundCell))
					{
						if (foundCell.IsValid)
						{
							intVec3 = foundCell;
							break;
						}
					}
				}

				if (i == 9)
				{
					intVec3 = oldVec.IsValid ? oldVec : map.Center;
				}
			}

			// Final validation
			if (!intVec3.IsValid || !intVec3.InBounds(map))
			{
				intVec3 = map.Center;
			}

			return intVec3;
		}

		private static IntVec3 ValidLandingCell(IntVec2 requiredSpace, Map map, bool canLandRoofed = false)
		{
			IEnumerable<IntVec3> validCells = map.areaManager.Home.ActiveCells.Where(cell => (!map.roofGrid.Roofed(cell) || canLandRoofed) && cell.CellFulfilsSpaceRequirementForSkyFaller(requiredSpace, map));

			if (validCells.Count() == 0)
			{
				validCells = map.areaManager.Home.ActiveCells.Where(cell => cell.CellFulfilsSpaceRequirementForSkyFaller(requiredSpace, map));
			}

			if (validCells.Count() == 0)
			{
				validCells = map.AllCells.Where(cell => !map.areaManager.Home.ActiveCells.Contains(cell) && cell.Standable(map));
			}

			if (validCells.Count() == 0)
			{
				validCells = map.AllCells.Where(cell => cell.Standable(map));
			}

			// Final fallback - if we still have no valid cells, use map center
			if (validCells.Count() == 0)
			{
				return map.Center;
			}

			IntVec3 result = validCells.RandomElement();
			return result.IsValid ? result : map.Center;
		}
	}

	public enum TaxDeliveryMode
	{
		None,
		TaxSpot,
		Caravan,
		DropPod,
		Shuttle
	}

	public enum EmpireDifficultyLevel
	{
		Peaceful = 0,
		CommunityBuilder = 1,
		AdventureStory = 2,
		StriveToSurvive = 3,
		BloodAndDust = 4,
		LosingIsFun = 5,
		Custom = 6
	}

	public enum TaxNotificationMode
	{
		All,        // Show both Letter and Message
		LetterOnly, // Only show Letter (blue notification)
		MessageOnly,// Only show Message (top-screen text)
		None        // Hide all tax delivery notifications
	}
}

