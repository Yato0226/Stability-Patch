using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Stability_Patch
{
    [HarmonyPatch(typeof(JobDriver_Ingest), "FinalizeIngest")]
    public static class JobDriverIngestPatch
    {
        static bool Prefix(JobDriver_Ingest __instance)
        {
            if (__instance.pawn == null || __instance.job == null || __instance.job.targetA.Thing == null)
            {
                return false; // Skip original method if essential components are null
            }
            return true; // Continue with original method
        }
    }
}