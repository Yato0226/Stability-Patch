using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
 
namespace MyFixes
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.HasJobOnThing))]
    public static class Patch_AM_WorkGiver_AddGPU
    {
        public static bool Prepare()
        {
            return AccessTools.TypeByName("AM_WorkGiver_AddGPU") != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("AM_WorkGiver_AddGPU"), nameof(WorkGiver_Scanner.HasJobOnThing));
        }

        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}