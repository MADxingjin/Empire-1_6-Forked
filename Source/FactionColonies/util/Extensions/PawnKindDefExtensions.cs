using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
        private static List<string> BlackListedTradeTags
		{
			get
			{
				return new List<string>() 
				{
					"AnimalDryad",
					"AnimalMonster",
					"AnimalGenetic",
					"AnimalAlpha"
				};
			}
		}

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
	}
	
}
