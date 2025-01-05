using HarmonyLib;
using Verse;
using System;
using System.Collections.Generic;

namespace StabilityPatch
{
    [StaticConstructorOnStartup]
    public static class StabilityPatch
    {
        static StabilityPatch()
        {
            if (StabilityPatchMod.settings.enableStabilityPatch)
            {
                var harmony = new Harmony("com.louize.StabilityPatch");
                harmony.PatchAll();
            }
        }
    }

    [HarmonyPatch(typeof(ListerThings), "ThingsMatching")]
    public static class Patch_ListerThings_ThingsMatching
    {
        public static void Postfix(ThingRequest req, ref List<Thing> __result)
        {
            try
            {
                if (__result == null)
                {
                    __result = new List<Thing>();
                }
            }
            catch (InvalidOperationException)
            {
                __result = new List<Thing>();
            }
        }
    }
}