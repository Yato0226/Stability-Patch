using HarmonyLib;
using Verse;
using System.Collections.Generic;

namespace StabilityPatch
{
    public class StabilityPatch : Mod
    {
        public StabilityPatch(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.yourname.StabilityPatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(ListerThings), "ThingsMatching")]
    public static class Patch_ListerThings_ThingsMatching
    {
        public static bool Prefix(ThingRequest req, ref List<Thing> __result)
        {
            // Check if the ThingRequest is valid before proceeding
            if (req.singleDef != null)
            {
                // Use the ThingsMatching method to get the list of things
                // Access ListerThings through the current map
                if (Find.CurrentMap != null)
                {
                    __result = Find.CurrentMap.listerThings.ThingsMatching(req);
                    return false; // Prevent the original method from executing
                }
                else
                {
                    __result = new List<Thing>(); // Return an empty list if no current map
                    return false; // Prevent the original method from executing
                }
            }

            // Suppress the exception for undefined group
            if (req.group == ThingRequestGroup.Undefined)
            {
                __result = new List<Thing>(); // Return an empty list
                return false; // Prevent the original method from executing
            }

            // Allow the original method to execute for valid requests
            return true; // Proceed to the original method
        }
    }
}
