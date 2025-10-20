using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Stability_Patch
{
    [HarmonyPatch(typeof(Toils_Ingest), "FinalizeIngest")]
    public static class JobDriverIngestPatch
    {
        public static bool Prefix(Pawn ingester, TargetIndex ingestibleInd)
        {
            if (ingester?.jobs?.curJob == null || !ingester.jobs.curJob.GetTarget(ingestibleInd).HasThing)
            {
                return false; // Skip original method if essential components are null
            }
            return true; // Continue with original method
        }
    }
}