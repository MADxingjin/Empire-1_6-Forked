using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;

namespace FactionColonies
{
    /// <summary>
    /// Utility class for specialized value-to-string functions.
    /// </summary>
    public static class TextUtil
    {
        public static string FloorStat(double stat)
        {
            return Convert.ToString(Math.Floor((stat * 100)) / 100);
        }

        /// <summary>
        /// Takes an additive bonus and colorizes it: red for a negative bonus, green for a positive bonus.
        /// <para>By default, a bonus that is less than 0 is considered negative, while a bonus that is greater than 0 is considered positive. This can be reversed by passing in 'true' for the 'invert' parameter.</para>
        /// </summary>
        /// <param name="bonus">The numeric bonus to colorize</param>
        /// <param name="invert">If true, negative values are colorized as positive, and vice versa. Defaults to false</param>
        /// <param name="hardinvert">If true, the bonus is multiplied by -1 before being processed.</param>
        /// <param name="addPlusSign">If true, adds a "+" before positive values. Defaults to true</param>
        /// <returns></returns>
        public static TaggedString colorizeAdditiveBonus(double bonus, bool invert = false, bool addPlusSign = true, bool hardinvert = false)
        {
            if (hardinvert)
            {
                bonus *= -1;
            }
            string baseBonus = bonus.ToString();
            if (bonus > 0 && addPlusSign)
            {
                baseBonus = "+" + baseBonus;
            }

            if ((!invert && bonus < 0) || (invert && bonus > 0))
            {
                return baseBonus.Colorize(Color.red);
            }
            else
            {
                return baseBonus.Colorize(Color.green);
            }
        }
        public static string CleaveAtNewline(string input)
        {
            int newline = input.IndexOf('\n');
            if (newline == 0)
            {
                // If the first character in the string is a newline, then skip over it and return the next line of text.
                // If the newline is the only character in the string, though, then just return an empty string.
                if (input.Length > 1)
                {
                    return CleaveAtNewline(input.Substring(1, input.Length - 1));
                }
                else
                {
                    return string.Empty;
                }
            }
            if (newline > 0)
            {
                return input.Substring(0, newline);
            }
            return input;
        }
        /// <summary>
        /// Takes a multiplier bonus and colorizes it: red for a negative bonus, green for a positive bonus.
        /// <para>By default, a bonus that is less than 1 is considered negative, while a bonus that is greater than 1 is considered positive. This can be reversed by passing in 'true' for the 'invert' parameter.</para>
        /// </summary>
        /// <param name="bonus">The numeric bonus to colorize</param>
        /// <param name="invert">If true, values less than 1 are colorized as positive, and vice versa. Defaults to false</param>
        /// <param name="addXsign">If true, adds a "x" before the bonus. Defaults to true</param>
        /// <returns></returns>
        public static TaggedString colorizeMultiplierBonus(double bonus, bool invert = false, bool addXsign = true)
        {
            string baseBonus = bonus.ToString();
            if (addXsign)
            {
                baseBonus = "x" + baseBonus;
            }

            if ((!invert && bonus < 1) || (invert && bonus > 1))
            {
                return baseBonus.Colorize(Color.red);
            }
            else
            {
                return baseBonus.Colorize(Color.green);
            }
        }

        public static string GetTownTitle(WorldSettlementFC settlement)
        {
            int level = settlement.settlementLevel <= 3 ? 1
                      : settlement.settlementLevel <= 6 ? 2
                      : 3;

            string resourceKey = "";
            double highest = -1;
            foreach (ResourceFC resource in settlement.Resources)
            {
                if (resource.rawTotalProduction > highest)
                {
                    highest = resource.rawTotalProduction;
                    resourceKey = resource.def.defName;
                }
            }

            string titleKey = (settlement.def as WorldSettlementDef)?.titleKey;
            if (titleKey != null)
            {
                string typeSpecificKey = "FCTitle_" + titleKey + "_" + resourceKey + "_" + level;
                if (typeSpecificKey.CanTranslate())
                    return typeSpecificKey.Translate();
            }

            return ("FCTitle_" + resourceKey + "_" + level).Translate();
        }

        public static string GetQualityLabelCap(QualityCategory? cat)
        {
            if (cat is null)
            {
                return $"({"Select".Translate()})";
            }
            return QualityUtility.GetLabel(cat ?? QualityCategory.Normal).CapitalizeFirst();
        }
    }
}
