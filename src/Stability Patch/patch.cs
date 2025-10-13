using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SafeFacilityDrawPatchMod
{
    [StaticConstructorOnStartup]
    public static class SafeFacilityDrawPatchInitializer
    {
        static SafeFacilityDrawPatchInitializer()
        {
            var harmony = new Harmony("com.louize.stabilitypatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CompFacility), nameof(CompFacility.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo))]
    public static class SafeFacilityDrawPatch
    {
        static bool Prefix(ThingDef myDef, Map map)
        {
            if (myDef == null || map == null)
            {
                return false;
            }
            var compProperties = myDef.GetCompProperties<CompProperties_Facility>();
            if (compProperties == null || compProperties.linkableBuildings == null)
            {
                return false;
            }
            return true;
        }
    }
}