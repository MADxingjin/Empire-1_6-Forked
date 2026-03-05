using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public class ResourcePoolExt_Research : ResourcePoolExtension
    {
        public override double createPool(double production, WorldSettlementFC settlement = null)
        {
            FactionFC faction = FactionCache.FactionComp;

            double result = Math.Max(Math.Round(production * FCSettings.productionResearchBase), 0);
            result *= faction.GetStatValue(FCStatDefOf.researchContributionMultiplier, settlement);
            return (float)result;
        }
        public override bool resetAtTaxTime()
        {
            return false;
        }
        public override void addedToGlobalPool(double value)
        {
            Messages.Message("PointsAddedToResearchPool".Translate(value), MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<FloatMenuOption> GetFactionMenuFloatMenuOptions(ResourcePool pool)
        {
            IEnumerable<FloatMenuOption> boptions = base.GetFactionMenuFloatMenuOptions(pool);
            if (boptions != null)
            {
                foreach (FloatMenuOption option in boptions)
                {
                    yield return option;
                }
            }
            FactionFC faction = FactionCache.FactionComp;

            yield return new FloatMenuOption("ActivateResearch".Translate(), delegate
            {
                dailyUpdate(pool);
            });

            yield return new FloatMenuOption("ResearchLevel".Translate(), delegate
            {
                Messages.Message("CurrentResearchLevel".Translate(faction.techLevel.ToString(), faction.returnNextTechToLevel()), MessageTypeDefOf.NeutralEvent);
            });
        }
        public override void dailyUpdate(ResourcePool pool)
        {
            double researchPointPool = pool.pool;
            //Research adding
            if ((Find.ResearchManager.GetProject() == null) && researchPointPool != 0)
            {
                Messages.Message("NoResearchExpended".Translate(Math.Round(researchPointPool)), MessageTypeDefOf.NeutralEvent);
            }
            else if (researchPointPool != 0 && Find.ResearchManager.GetProject() != null)
            {
                //LogUtil.Message(researchTotal.ToString());
                float neededPoints;
                neededPoints = (float)Math.Ceiling(Find.ResearchManager.GetProject().CostApparent - Find.ResearchManager.GetProject().ProgressApparent);
                LogUtil.Message("Needed points: " + neededPoints);

                double expendedPoints;
                if (researchPointPool >= neededPoints)
                {
                    researchPointPool -= neededPoints;
                    expendedPoints = neededPoints;
                }
                else
                {
                    expendedPoints = researchPointPool;
                    researchPointPool = 0;
                    LogUtil.Message("Used all research points in the pool.");
                }

                LogUtil.Message("Expended points: " + expendedPoints);

                Find.LetterStack.ReceiveLetter(
                    "ResearchPointsExpended".Translate(),
                    "ResearchExpended".Translate(Math.Round(expendedPoints),
                    Find.ResearchManager.GetProject().LabelCap,
                    Math.Round(researchPointPool)),
                    LetterDefOf.PositiveEvent);
                if (Find.ColonistBar.GetColonistsInOrder().Count > 0)
                {
                    Pawn pawn = Find.ColonistBar.GetColonistsInOrder()[0];
                    Find.ResearchManager.ResearchPerformed(
                        (float)Math.Ceiling(((1 * Find.ResearchManager.GetProject().CostFactor(pawn.Faction.def.techLevel)) /
                            (0.00825 * Find.Storyteller.difficulty.researchSpeedFactor)) * expendedPoints),
                        pawn);
                }
                else
                {
                    LogUtil.Message("Could not find colonist to research with");
                    Find.ResearchManager.ResearchPerformed((float)Math.Ceiling((1 /
                        (0.00825 * Find.Storyteller.difficulty.researchSpeedFactor)) * expendedPoints), null);
                }
                pool.pool = researchPointPool;
            }
        }
    }
}
