using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StabilityPatch
{
    [HarmonyPatch]
    public static class WorkManager_AssignDedicatedWorkers_Patch
    {
        private static Type _workPriorityUpdaterType;
        private static Type _workTypeAssignmentRuleType;
        private static Type _pawnCacheType;
        private static Type _workManagerGameComponentType;
        private static Type _defCacheType;
        private static Type _workManagerModType;
        private static Type _settingsType;

        public static bool Prepare()
        {
            _workPriorityUpdaterType = AccessTools.TypeByName("LordKuper.WorkManager.WorkPriorityUpdater");
            _workTypeAssignmentRuleType = AccessTools.TypeByName("LordKuper.WorkManager.WorkTypeAssignmentRule");
            _pawnCacheType = AccessTools.TypeByName("LordKuper.WorkManager.Cache.PawnCache");
            _workManagerGameComponentType = AccessTools.TypeByName("LordKuper.WorkManager.WorkManagerGameComponent");
            _defCacheType = AccessTools.TypeByName("LordKuper.Common.Cache.DefCache`1").MakeGenericType(typeof(WorkTypeDef));
            _workManagerModType = AccessTools.TypeByName("LordKuper.WorkManager.WorkManagerMod");
            _settingsType = AccessTools.TypeByName("LordKuper.WorkManager.Settings");

            return _workPriorityUpdaterType != null && _workTypeAssignmentRuleType != null && _pawnCacheType != null && _workManagerGameComponentType != null && _defCacheType != null && _workManagerModType != null && _settingsType != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(_workPriorityUpdaterType, "AssignDedicatedWorkers");
        }

        [HarmonyPrefix]
        public static bool AssignDedicatedWorkers_Prefix(object __instance)
        {
            // Get private fields from the WorkPriorityUpdater instance
            var capablePawns = (IEnumerable)AccessTools.Field(_workPriorityUpdaterType, "_capablePawns").GetValue(__instance);
            var managedWorkTypeRules = (IDictionary)AccessTools.Field(_workPriorityUpdaterType, "_managedWorkTypeRules").GetValue(__instance);
            var map = (Map)AccessTools.Property(_workPriorityUpdaterType, "map").GetValue(__instance);

            if (!capablePawns.GetEnumerator().MoveNext())
            {
                return false; // Skip original method
            }

            var gameComponent = AccessTools.Property(_workManagerGameComponentType, "Instance").GetValue(null);
            var dedicatedWorkTypes = (IEnumerable)AccessTools.Property(_workManagerGameComponentType, "DedicatedWorkTypes").GetValue(gameComponent);

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_workTypeAssignmentRuleType));
            foreach (WorkTypeDef dedicatedWorkType in dedicatedWorkTypes)
            {
                if (managedWorkTypeRules.Contains(dedicatedWorkType))
                {
                    list.Add(managedWorkTypeRules[dedicatedWorkType]);
                }
            }

            if (list.Count == 0)
            {
                return false; // Skip original method
            }

            var comparer = AccessTools.Property(_workManagerGameComponentType, "WorkTypeAssignmentRuleComparer").GetValue(gameComponent);
            var sortMethod = list.GetType().GetMethod("Sort", new Type[] { comparer.GetType() });
            sortMethod.Invoke(list, new object[] { comparer });

            var settings = AccessTools.Property(_workManagerModType, "Settings").GetValue(null);
            var dedicatedWorkerPriority = (int)AccessTools.Property(_settingsType, "DedicatedWorkerPriority").GetValue(settings);

            foreach (var item in list)
            {
                var getTargetWorkersCountMethod = AccessTools.Method(_workTypeAssignmentRuleType, "GetTargetWorkersCount");
                var ensureWorkerAssignedProperty = AccessTools.Property(_workTypeAssignmentRuleType, "EnsureWorkerAssigned");
                var minWorkerNumberProperty = AccessTools.Property(_workTypeAssignmentRuleType, "MinWorkerNumber");
                var defProperty = AccessTools.Property(_defCacheType, "Def");

                int num = (int)getTargetWorkersCountMethod.Invoke(item, new object[] { map, Count(capablePawns), list.Count });
                if ((bool)ensureWorkerAssignedProperty.GetValue(item))
                {
                    num = Math.Max(num, (int)minWorkerNumberProperty.GetValue(item));
                }

                var list2 = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_pawnCacheType));
                var isAllowedWorkerMethod = AccessTools.Method(_pawnCacheType, "IsAllowedWorker");
                var def = defProperty.GetValue(item);

                foreach (var capablePawn in capablePawns)
                {
                    if ((bool)isAllowedWorkerMethod.Invoke(capablePawn, new object[] { def }))
                    {
                        list2.Add(capablePawn);
                    }
                }

                if (list2.Count == 0)
                {
                    continue;
                }

                int num2 = 0;
                var isActiveWorkMethod = AccessTools.Method(_pawnCacheType, "IsActiveWork");
                var getWorkPriorityMethod = AccessTools.Method(_pawnCacheType, "GetWorkPriority");

                foreach (var capablePawn2 in capablePawns)
                {
                    if ((bool)isActiveWorkMethod.Invoke(capablePawn2, new object[] { def }) && (int)getWorkPriorityMethod.Invoke(capablePawn2, new object[] { def }) <= dedicatedWorkerPriority)
                    {
                        num2++;
                    }
                }

                if (num2 >= num)
                {
                    continue;
                }

                var setWorkPriorityMethod = AccessTools.Method(_pawnCacheType, "SetWorkPriority");
                foreach (var pawn in list2)
                {
                    if (num2 >= num) break;

                    if (!(bool)isActiveWorkMethod.Invoke(pawn, new object[] { def }))
                    {
                        setWorkPriorityMethod.Invoke(pawn, new object[] { def, dedicatedWorkerPriority });
                        num2++;
                    }
                }
            }

            return false; // Prevent the original (buggy) method from running
        }

        private static int Count(IEnumerable source)
        {
            if (source is ICollection collection)
            {
                return collection.Count;
            }
            int count = 0;
            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }
            return count;
        }
    }
}