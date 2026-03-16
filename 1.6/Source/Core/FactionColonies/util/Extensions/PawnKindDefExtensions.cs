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
        /// Creates a shallow clone of the given PawnKindDef using <see cref="Gen.MemberwiseClone{T}"/>.
        /// The new PawnKindDef will be its own object, but will share references to the original's collection/object fields.
        /// Since Defs are read-only at runtime, this is safe — RimWorld uses the same approach in DebugAutotests.
        /// <para>Only exists for HAR compatibility when we need runtime PawnKindDefs for alien races.</para>
        /// </summary>
        /// <param name="pawnKindDef">PawnKindDef to copy.</param>
        /// <returns>A new object with the same fields as the provided PawnKindDef.</returns>
		public static PawnKindDef ShallowClone(this PawnKindDef pawnKindDef)
        {
            if (pawnKindDef is null)
                return null;

            return Gen.MemberwiseClone(pawnKindDef);
        }
    }
	
}
