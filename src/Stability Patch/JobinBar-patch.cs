using HarmonyLib;
using Verse;
using System.Reflection;
using System.Collections.Generic;
using JobInBar;

namespace StabilityPatch
{
    [StaticConstructorOnStartup]
    public static class JobInBarPatch
    {
        static JobInBarPatch()
        {
            if (StabilityPatchMod.settings.enableJobInBarPatch)
            {
                var harmony = new Harmony("com.louize.JobInBarPatch");
                harmony.PatchAll();
            }
        }
    }

    [HarmonyPatch(typeof(JobInBar.PawnLabelCustomColors_WorldComponent), "GetDrawJobLabelFor")]
    public static class Patch_JobInBar_GetDrawJobLabelFor
    {
        public static bool Prefix(JobInBar.PawnLabelCustomColors_WorldComponent __instance, Pawn pawn, ref bool __result)
        {
            if (pawn == null)
            {
                __result = false;
                return false;
            }

            var pawnShowJobLabelsField = typeof(JobInBar.PawnLabelCustomColors_WorldComponent).GetField("PawnShowJobLabels", BindingFlags.NonPublic | BindingFlags.Instance);
            var pawnShowJobLabels = pawnShowJobLabelsField.GetValue(__instance) as Dictionary<Pawn, bool>;

            if (pawnShowJobLabels == null || !pawnShowJobLabels.ContainsKey(pawn))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}