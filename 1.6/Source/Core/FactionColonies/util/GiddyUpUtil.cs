using Verse;
using Verse.AI;

namespace FactionColonies.util
{
    [StaticConstructorOnStartup]
    public static class GiddyUpUtil
    {
        private static readonly JobDef Mounting;

        static GiddyUpUtil()
        {
            Mounting = DefDatabase<JobDef>.GetNamedSilentFail("Mount");
        }

        public static void Mount(Pawn rider, Pawn animal)
        {
            if (Mounting == null) return;
            Job mountJob = new Job(Mounting,
                new LocalTargetInfo(animal))
            { count = 1 };
            rider.jobs.StartJob(mountJob);
        }
    }
}