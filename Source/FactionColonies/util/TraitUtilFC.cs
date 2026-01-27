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
            double tempTrait = (int) addOrMultiply;

            if (traits == null || traits.Count == 0)
            {
                return tempTrait;
            }

            foreach (FCTraitEffectDef trait in traits)
            {
                if (addOrMultiply == Operation.Addition)
                {
                    tempTrait += returnVariable(field, trait);
                }
                else
                {
                    tempTrait *= returnVariable(field, trait);
                }
            }

            return tempTrait;
        }

        public static int returnResearchAmount()
        {
            int research = 0;
            research += Convert.ToInt32(cycleTraits("researchBaseProduction", Find.World.GetComponent<FactionFC>().Traits, Operation.Addition));
            foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                research += Convert.ToInt32(cycleTraits("researchBaseProduction", settlement.Traits, Operation.Addition));
            }
            return research;
        }
    }
}
