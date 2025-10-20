using HarmonyLib;
using RimWorldRealFoW;
using System;
using System.Reflection;
using Verse;
using RimWorld;

namespace Stability_Patch
{
    [HarmonyPatch(typeof(MapComponentSeenFog), "setMapMeshDirtyFlag")]
    public static class MapComponentSeenFog_setMapMeshDirtyFlag_Patch
    {
        private static readonly FieldInfo sectionsSizeXField = AccessTools.Field(typeof(MapComponentSeenFog), "sectionsSizeX");
        private static readonly FieldInfo sectionsSizeYField = AccessTools.Field(typeof(MapComponentSeenFog), "sectionsSizeY");
        private static readonly FieldInfo sectionsField = AccessTools.Field(typeof(MapComponentSeenFog), "sections");
        private static readonly FieldInfo mapSizeZField = AccessTools.Field(typeof(MapComponentSeenFog), "mapSizeZ");
        private static readonly FieldInfo playerVisibilityChangeTickField = AccessTools.Field(typeof(MapComponentSeenFog), "playerVisibilityChangeTick");
        private static readonly FieldInfo currentGameTickField = AccessTools.Field(typeof(MapComponentSeenFog), "currentGameTick");

        public static bool Prefix(MapComponentSeenFog __instance, int idx)
        {
            int mapSizeX = __instance.mapSizeX;
            int mapSizeZ = (int)mapSizeZField.GetValue(__instance);
            int sectionsSizeX = (int)sectionsSizeXField.GetValue(__instance);
            int sectionsSizeY = (int)sectionsSizeYField.GetValue(__instance);
            Array sections = (Array)sectionsField.GetValue(__instance);
            int[] playerVisibilityChangeTick = (int[])playerVisibilityChangeTickField.GetValue(__instance);
            int currentGameTick = (int)currentGameTickField.GetValue(__instance);

            if (sections == null) return false;

            int num = idx % mapSizeX;
            int num2 = idx / mapSizeX;

            int num5 = Math.Max(0, num - 1);
            int num6 = Math.Min(num2 + 2, mapSizeZ);
            int num7 = Math.Min(num + 2, mapSizeX) - num5;
            for (int i = Math.Max(0, num2 - 1); i < num6; i++)
            {
                int num8 = i * mapSizeX + num5;
                for (int j = 0; j < num7; j++)
                {
                    if (num8 + j < playerVisibilityChangeTick.Length)
                    {
                        playerVisibilityChangeTick[num8 + j] = currentGameTick;
                    }
                }
            }

            int num3 = num / 17;
            int num4 = num2 / 17;

            Action<int, int> setDirty = (x, y) =>
            {
                if (x >= 0 && x < sectionsSizeX && y >= 0 && y < sectionsSizeY)
                {
                    int index = y * sectionsSizeX + x;
                    if (index >= 0 && index < sections.Length)
                    {
                        Section section = (Section)sections.GetValue(index);
                        section.dirtyFlags |= 8192UL;
                    }
                }
            };
             
            setDirty(num3, num4);

            int num9 = num % 17;
            int num10 = num2 % 17;

            if (num9 == 0)
            {
                setDirty(num3 - 1, num4);
                if (num10 == 0) setDirty(num3 - 1, num4 - 1);
                else if (num10 == 16) setDirty(num3 - 1, num4 + 1);
            }
            else if (num9 == 16)
            {
                setDirty(num3 + 1, num4);
                if (num10 == 0) setDirty(num3 + 1, num4 - 1);
                else if (num10 == 16) setDirty(num3 + 1, num4 + 1);
            }

            if (num10 == 0)
            {
                setDirty(num3, num4 - 1);
            }
            else if (num10 == 16)
            {
                setDirty(num3, num4 + 1);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MapComponentSeenFog), "DecrementSeen")]
    public static class MapComponentSeenFog_DecrementSeen_Patch
    {
        public static bool Prefix(MapComponentSeenFog __instance, Faction faction, short[] factionShownCells, int idx)
        {
            if (factionShownCells == null || idx < 0 || idx >= factionShownCells.Length)
            {
                return false;
            }
            return true;
        }
    }
}