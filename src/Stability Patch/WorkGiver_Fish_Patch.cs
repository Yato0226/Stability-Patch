//using HarmonyLib;
//using RimWorld;
//using Verse;
//
//namespace Stability_Patch
//{
//    [HarmonyPatch(typeof(WorkGiver_Fish), nameof(WorkGiver_Fish.PotentialWorkThingRequest), MethodType.Getter)]
//    public static class WorkGiver_Fish_PotentialWorkThingRequest_Patch
//    {
//        [HarmonyPrefix]
//        public static bool Prefix(ref ThingRequest __result)
//        {
//            __result = ThingRequest.ForGroup(ThingRequestGroup.Nothing);
//            return false; // Skip original method
//        }
//    }
//}