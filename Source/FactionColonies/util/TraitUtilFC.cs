using System;
using System.Collections.Generic;
using System.Reflection;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{
    class TraitUtilsFC
    {
        public static double returnVariable(string field, FCTraitEffectDef def)
        {
            if (def == null)
            {
                LogUtil.Warning($"FCTraitEffectDef is null for field '{field}'");
                return 0.0;
            }
            
            Type typ = def.GetType();
            FieldInfo fieldInfo = typ.GetField(field);
            
            if (fieldInfo == null)
            {
                LogUtil.Warning($"Field '{field}' not found on FCTraitEffectDef type '{typ.Name}'");
                return 0.0;
            }
            
            return (double) fieldInfo.GetValue(def);
        }

        public static double cycleTraits(string field, List<FCTraitEffectDef> traits, Operation addOrMultiply)
        {
            string dummy = "";
            return cycleTraits(field, traits, addOrMultiply, false, ref dummy);
        }

        public static double cycleTraits(string field, List<FCTraitEffectDef> traits, Operation addOrMultiply, bool createDesc, ref string desc, bool invert = false, bool hardinvert = false)
        {
            double tempTrait = (int)addOrMultiply;

            if (traits == null || traits.Count == 0)
            {
                return tempTrait;
            }

            foreach (FCTraitEffectDef trait in traits)
            {
                if (addOrMultiply == Operation.Addition)
                {
                    double value = returnVariable(field, trait);
                    if (value != 0)
                    {
                        tempTrait += value;

                        if (createDesc)
                        {
                            desc += TextUtil.colorizeAdditiveBonus(value, invert: invert, hardinvert: hardinvert) + " - " + trait.LabelCap + "\n";
                        }
                    }
                }
                else
                {
                    double value = returnVariable(field, trait);
                    if (value != 1)
                    {
                        tempTrait *= value;

                        if (createDesc)
                        {
                            desc += TextUtil.colorizeMultiplierBonus(value, invert: invert) + " - " + trait.LabelCap + "\n";
                        }
                    }
                }
            }

            return tempTrait;
        }

        public static int returnResearchAmount()
        {
            int research = 0;
            research += Convert.ToInt32(cycleTraits("researchBaseProduction", FactionCache.FactionComp.Traits, Operation.Addition));
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                research += Convert.ToInt32(cycleTraits("researchBaseProduction", settlement.Traits, Operation.Addition));
            }
            return research;
        }
    }
}
