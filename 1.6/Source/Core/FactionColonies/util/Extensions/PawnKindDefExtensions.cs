using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
	static class PawnKindDefExtensions
	{
		public static bool IsHumanLikeRace(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef.race.race?.intelligence == Intelligence.Humanlike && pawnKindDef.race.BaseMarketValue != 0;
		}

		public static bool IsHumanlikeWithLabelRace(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef?.race?.label != null && pawnKindDef.IsHumanLikeRace();
		}

		public static bool IsXenotypeWithLabel(this XenotypeDef xenotypeDef)
		{
			return xenotypeDef?.label != null;
		}
        private static readonly List<string> BlackListedTradeTags =  new List<string>() 
				                                                     {
					                                                    "AnimalDryad",
					                                                    "AnimalMonster",
					                                                    "AnimalGenetic",
					                                                    "AnimalAlpha"
				                                                     };

		/// <summary>
		///		Checks if a given <c>PawnKindDef</c> <paramref name="pawnKindDef"/> is an Animal and if it is not blacklisted by tradeTag 
		/// </summary>
		/// <param name="pawnKindDef"></param>
		/// <returns></returns>
		public static bool IsAnimalAndAllowed(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef.race.race.Animal && pawnKindDef.RaceProps.IsFlesh &&
									pawnKindDef.race.race.animalType != AnimalType.Dryad &&
									pawnKindDef.race.tradeTags != null &&
									!pawnKindDef.race.tradeTags.Any(tag => BlackListedTradeTags.Contains(tag));
		}
        /// <summary>
        /// Checks if a given <c>PawnKindDef</c> <paramref name="pawnKindDef"/> is a valid combat animal.
		/// <para>Bears and wargs are problematic, so we exclude them.</para>
        /// </summary>
        /// <param name="pawnKindDef"></param>
        /// <returns></returns>
        public static bool IsCombatAnimal(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef.IsAnimalAndAllowed() && pawnKindDef.RaceProps.trainability != null &&
				   pawnKindDef.RaceProps.trainability.intelligenceOrder >= TrainabilityDefOf.Intermediate.intelligenceOrder &&
				   pawnKindDef.race.race.predator &&
				   pawnKindDef.combatPower > 50f && // Strong combat animals
				   !pawnKindDef.label.ToLower().Contains("bear") && // Exclude bears
				   !pawnKindDef.label.ToLower().Contains("warg");

        }


        public static int GetReasonableMercenaryAge(this PawnKindDef pawnKindDef) => Rand.Range((int)Math.Ceiling(pawnKindDef.race.race.lifeExpectancy * 0.2625d), (int)Math.Floor(pawnKindDef.race.race.lifeExpectancy * 0.625d));

        /// <summary>
		/// Creates a shallow clone of the given PawnKindDef. The new PawnKindDef will be its own object, but will reference the fields of the original for non-primitive types.
        /// Since Defs shouldn't be modified during runtime, this kind of "shallow deep copy" should be fine. Maybe. Hopefully.
		/// <para>You really aren't supposed to create new defs during runtime, so this is a pretty hacky solution. Only use this function as a last resort.</para>
		/// <para>The things we do for HAR compatibility...</para>
		/// </summary>
		/// <param name="pawnKindDef">PawnKindDef to copy.</param>
		/// <returns>A new object with the same fields as the provided PawnKindDef.</returns>
        // What an ugly function. Honestly, probably shouldn't be doing this. Only exists for compatibility with Humanoid Alien Races.
        // Note for future on-lookers: I've attempted to use UnityEngine.JsonUtility to deserialize the PawnKindDef, and then create a new PawnKindDef object by serializing the de-serialized string.
        //   But this doesn't work. Presumably because many of object fields in PawnKindDef aren't serializable.
        //   So if you're wondering why this function is so long and ugly... that's why.
		public static PawnKindDef ShallowClone(this PawnKindDef pawnKindDef)
        {
            if (pawnKindDef is null)
                return null;

            PawnKindDef copy = new PawnKindDef();

            // Copy simple fields directly
            copy.defName = pawnKindDef.defName;
            copy.label = pawnKindDef.label;
            copy.description = pawnKindDef.description;
            copy.race = pawnKindDef.race;
            copy.defaultFactionDef = pawnKindDef.defaultFactionDef;
            copy.labelPlural = pawnKindDef.labelPlural;
            copy.alternateGraphicChance = pawnKindDef.alternateGraphicChance;
            copy.mutant = pawnKindDef.mutant;
            copy.xenotypeSet = pawnKindDef.xenotypeSet;
            copy.useFactionXenotypes = pawnKindDef.useFactionXenotypes;
            copy.forcedHair = pawnKindDef.forcedHair;
            copy.forcedHairColor = pawnKindDef.forcedHairColor;
            copy.nameMaker = pawnKindDef.nameMaker;
            copy.nameMakerFemale = pawnKindDef.nameMakerFemale;
            copy.preventIdeo = pawnKindDef.preventIdeo;
            copy.studiableAsPrisoner = pawnKindDef.studiableAsPrisoner;
            copy.isBoss = pawnKindDef.isBoss;
            copy.backstoryCryptosleepCommonality = pawnKindDef.backstoryCryptosleepCommonality;
            copy.minGenerationAge = pawnKindDef.minGenerationAge;
            copy.maxGenerationAge = pawnKindDef.maxGenerationAge;
            copy.factionLeader = pawnKindDef.factionLeader;
            copy.fixedGender = pawnKindDef.fixedGender;
            copy.allowOldAgeInjuries = pawnKindDef.allowOldAgeInjuries;
            copy.generateInitialNonFamilyRelations = pawnKindDef.generateInitialNonFamilyRelations;
            copy.pawnGroupDevelopmentStage = pawnKindDef.pawnGroupDevelopmentStage;
            copy.destroyGearOnDrop = pawnKindDef.destroyGearOnDrop;
            copy.canStrip = pawnKindDef.canStrip;
            copy.defendPointRadius = pawnKindDef.defendPointRadius;
            copy.factionHostileOnKill = pawnKindDef.factionHostileOnKill;
            copy.factionHostileOnDeath = pawnKindDef.factionHostileOnDeath;
            copy.hostileToAll = pawnKindDef.hostileToAll;
            copy.forceNoDeathNotification = pawnKindDef.forceNoDeathNotification;
            copy.skipResistant = pawnKindDef.skipResistant;
            copy.controlGroupPortraitZoom = pawnKindDef.controlGroupPortraitZoom;
            copy.overrideDeathOnDownedChance = pawnKindDef.overrideDeathOnDownedChance;
            copy.forceDeathOnDowned = pawnKindDef.forceDeathOnDowned;
            copy.immuneToGameConditionEffects = pawnKindDef.immuneToGameConditionEffects;
            copy.immuneToTraps = pawnKindDef.immuneToTraps;
            copy.collidesWithPawns = pawnKindDef.collidesWithPawns;
            copy.ignoresPainShock = pawnKindDef.ignoresPainShock;
            copy.canMeleeAttack = pawnKindDef.canMeleeAttack;
            copy.basePrisonBreakMtbDays = pawnKindDef.basePrisonBreakMtbDays;
            copy.useFixedRotation = pawnKindDef.useFixedRotation;
            copy.fixedRotation = pawnKindDef.fixedRotation;
            copy.showInDebugSpawner = pawnKindDef.showInDebugSpawner;
            copy.canOpenAnyDoor = pawnKindDef.canOpenAnyDoor;
            copy.canOpenDoors = pawnKindDef.canOpenDoors;
            copy.overrideDebugActionCategory = pawnKindDef.overrideDebugActionCategory;
            copy.royalTitleChance = pawnKindDef.royalTitleChance;
            copy.titleRequired = pawnKindDef.titleRequired;
            copy.minTitleRequired = pawnKindDef.minTitleRequired;
            copy.allowRoyalRoomRequirements = pawnKindDef.allowRoyalRoomRequirements;
            copy.allowRoyalApparelRequirements = pawnKindDef.allowRoyalApparelRequirements;
            copy.isFighter = pawnKindDef.isFighter;
            copy.combatPower = pawnKindDef.combatPower;
            copy.canArriveManhunter = pawnKindDef.canArriveManhunter;
            copy.canBeSapper = pawnKindDef.canBeSapper;
            copy.isGoodBreacher = pawnKindDef.isGoodBreacher;
            copy.allowInMechClusters = pawnKindDef.allowInMechClusters;
            copy.maxPerGroup = pawnKindDef.maxPerGroup;
            copy.isGoodPsychicRitualInvoker = pawnKindDef.isGoodPsychicRitualInvoker;
            copy.canBeScattered = pawnKindDef.canBeScattered;
            copy.appearsRandomlyInCombatGroups = pawnKindDef.appearsRandomlyInCombatGroups;
            copy.aiAvoidCover = pawnKindDef.aiAvoidCover;
            copy.acceptArrestChanceFactor = pawnKindDef.acceptArrestChanceFactor;
            copy.canUseAvoidGrid = pawnKindDef.canUseAvoidGrid;
            copy.itemQuality = pawnKindDef.itemQuality;
            copy.forceWeaponQuality = pawnKindDef.forceWeaponQuality;
            copy.forceNormalGearQuality = pawnKindDef.forceNormalGearQuality;
            copy.weaponMoney = pawnKindDef.weaponMoney;
            copy.weaponStuffOverride = pawnKindDef.weaponStuffOverride;
            copy.weaponStyleDef = pawnKindDef.weaponStyleDef;
            copy.apparelMoney = pawnKindDef.apparelMoney;
            copy.apparelAllowHeadgearChance = pawnKindDef.apparelAllowHeadgearChance;
            copy.ignoreApparelAllowChance = pawnKindDef.ignoreApparelAllowChance;
            copy.apparelIgnoreSeasons = pawnKindDef.apparelIgnoreSeasons;
            copy.apparelIgnorePollution = pawnKindDef.apparelIgnorePollution;
            copy.ignoreFactionApparelStuffRequirements = pawnKindDef.ignoreFactionApparelStuffRequirements;
            copy.apparelColor = pawnKindDef.apparelColor;
            copy.skinColorOverride = pawnKindDef.skinColorOverride;
            copy.favoriteColor = pawnKindDef.favoriteColor;
            copy.ignoreIdeoApparelColors = pawnKindDef.ignoreIdeoApparelColors;
            copy.techHediffsChance = pawnKindDef.techHediffsChance;
            copy.techHediffsMaxAmount = pawnKindDef.techHediffsMaxAmount;
            copy.biocodeWeaponChance = pawnKindDef.biocodeWeaponChance;
            copy.humanPregnancyChance = pawnKindDef.humanPregnancyChance;
            copy.nakedChance = pawnKindDef.nakedChance;
            copy.minApparelQuality = pawnKindDef.minApparelQuality;
            copy.maxApparelQuality = pawnKindDef.maxApparelQuality;
            copy.invNutrition = pawnKindDef.invNutrition;
            copy.invFoodDef = pawnKindDef.invFoodDef;
            copy.chemicalAddictionChance = pawnKindDef.chemicalAddictionChance;
            copy.combatEnhancingDrugsChance = pawnKindDef.combatEnhancingDrugsChance;
            copy.combatEnhancingDrugsCount = pawnKindDef.combatEnhancingDrugsCount;
            copy.trader = pawnKindDef.trader;
            copy.requiredWorkTags = pawnKindDef.requiredWorkTags;
            copy.disabledWorkTags = pawnKindDef.disabledWorkTags;
            copy.extraSkillLevels = pawnKindDef.extraSkillLevels;
            copy.minTotalSkillLevels = pawnKindDef.minTotalSkillLevels;
            copy.minBestSkillLevel = pawnKindDef.minBestSkillLevel;
            copy.labelMale = pawnKindDef.labelMale;
            copy.labelMalePlural = pawnKindDef.labelMalePlural;
            copy.labelFemale = pawnKindDef.labelFemale;
            copy.labelFemalePlural = pawnKindDef.labelFemalePlural;
            copy.wildGroupSize = pawnKindDef.wildGroupSize;
            copy.ecoSystemWeight = pawnKindDef.ecoSystemWeight;
            copy.flyingAnimationFramePathPrefix = pawnKindDef.flyingAnimationFramePathPrefix;
            copy.flyingAnimationFramePathPrefixFemale = pawnKindDef.flyingAnimationFramePathPrefixFemale;
            copy.flyingAnimationFrameCount = pawnKindDef.flyingAnimationFrameCount;
            copy.flyingAnimationTicksPerFrame = pawnKindDef.flyingAnimationTicksPerFrame;
            copy.flyingAnimationDrawSize = pawnKindDef.flyingAnimationDrawSize;
            copy.flyingAnimationDrawSizeIsMultiplier = pawnKindDef.flyingAnimationDrawSizeIsMultiplier;
            copy.flyingAnimationInheritColors = pawnKindDef.flyingAnimationInheritColors;

            // Copy collections by reference (semi-deep copy)
            if (pawnKindDef.backstoryFilters != null)
                copy.backstoryFilters = pawnKindDef.backstoryFilters;

            if (pawnKindDef.backstoryFiltersOverride != null)
                copy.backstoryFiltersOverride = pawnKindDef.backstoryFiltersOverride;

            if (pawnKindDef.backstoryCategories != null)
                copy.backstoryCategories = pawnKindDef.backstoryCategories;

            if (pawnKindDef.lifeStages != null)
                copy.lifeStages = pawnKindDef.lifeStages;

            if (pawnKindDef.alternateGraphics != null)
                copy.alternateGraphics = pawnKindDef.alternateGraphics;

            if (pawnKindDef.forcedTraits != null)
                copy.forcedTraits = pawnKindDef.forcedTraits;

            if (pawnKindDef.disallowedTraitsWithDegree != null)
                copy.disallowedTraitsWithDegree = pawnKindDef.disallowedTraitsWithDegree;

            if (pawnKindDef.disallowedTraits != null)
                copy.disallowedTraits = pawnKindDef.disallowedTraits;

            if (pawnKindDef.missingParts != null)
                copy.missingParts = pawnKindDef.missingParts;

            if (pawnKindDef.abilities != null)
                copy.abilities = pawnKindDef.abilities;

            if (pawnKindDef.styleItemTags != null)
                copy.styleItemTags = pawnKindDef.styleItemTags;

            if (pawnKindDef.meleeAttackInfectionPathways != null)
                copy.meleeAttackInfectionPathways = pawnKindDef.meleeAttackInfectionPathways;

            if (pawnKindDef.rangedAttackInfectionPathways != null)
                copy.rangedAttackInfectionPathways = pawnKindDef.rangedAttackInfectionPathways;

            if (pawnKindDef.titleSelectOne != null)
                copy.titleSelectOne = pawnKindDef.titleSelectOne;

            if (pawnKindDef.weaponTags != null)
                copy.weaponTags = pawnKindDef.weaponTags;

            if (pawnKindDef.apparelRequired != null)
                copy.apparelRequired = pawnKindDef.apparelRequired;

            if (pawnKindDef.apparelTags != null)
                copy.apparelTags = pawnKindDef.apparelTags;

            if (pawnKindDef.apparelDisallowTags != null)
                copy.apparelDisallowTags = pawnKindDef.apparelDisallowTags;

            if (pawnKindDef.specificApparelRequirements != null)
                copy.specificApparelRequirements = pawnKindDef.specificApparelRequirements;

            if (pawnKindDef.techHediffsRequired != null)
                copy.techHediffsRequired = pawnKindDef.techHediffsRequired;

            if (pawnKindDef.techHediffsTags != null)
                copy.techHediffsTags = pawnKindDef.techHediffsTags;

            if (pawnKindDef.techHediffsDisallowTags != null)
                copy.techHediffsDisallowTags = pawnKindDef.techHediffsDisallowTags;

            if (pawnKindDef.forcedAddictions != null)
                copy.forcedAddictions = pawnKindDef.forcedAddictions;

            if (pawnKindDef.skills != null)
                copy.skills = pawnKindDef.skills;

            if (pawnKindDef.startingHediffs != null)
                copy.startingHediffs = pawnKindDef.startingHediffs;

            if (pawnKindDef.existingDamage != null)
                copy.existingDamage = pawnKindDef.existingDamage;

            if (pawnKindDef.fixedInventory != null)
                copy.fixedInventory = pawnKindDef.fixedInventory;

            if (pawnKindDef.fixedChildBackstories != null)
                copy.fixedChildBackstories = pawnKindDef.fixedChildBackstories;

            if (pawnKindDef.fixedAdultBackstories != null)
                copy.fixedAdultBackstories = pawnKindDef.fixedAdultBackstories;

            if (pawnKindDef.chronologicalAgeRange.HasValue)
                copy.chronologicalAgeRange = pawnKindDef.chronologicalAgeRange;

            if (pawnKindDef.initialResistanceRange.HasValue)
                copy.initialResistanceRange = pawnKindDef.initialResistanceRange;

            if (pawnKindDef.initialWillRange.HasValue)
                copy.initialWillRange = pawnKindDef.initialWillRange;

            // Copy dictionary by reference
            if (pawnKindDef.moveSpeedFactorByTerrainTag != null)
                copy.moveSpeedFactorByTerrainTag = pawnKindDef.moveSpeedFactorByTerrainTag;

            // Copy FloatRange values directly (they are structs)
            copy.fleeHealthThresholdRange = pawnKindDef.fleeHealthThresholdRange;

            // Copy inventory options
            if (pawnKindDef.inventoryOptions != null)
                copy.inventoryOptions = pawnKindDef.inventoryOptions;

            return copy;
        }
    }
	
}
