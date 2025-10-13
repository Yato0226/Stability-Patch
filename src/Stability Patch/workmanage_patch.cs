using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using LordKuper.WorkManager;
using System.Linq;

namespace Stability_Patch
{
    [HarmonyPatch(typeof(WorkPriorityUpdater), "AssignDedicatedWorkers")]
    public static class WorkPriorityUpdater_AssignDedicatedWorkers_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(WorkPriorityUpdater __instance)
        {
            HashSet<object> capablePawns = AccessTools.Field(typeof(WorkPriorityUpdater), "_capablePawns").GetValue(__instance) as HashSet<object>;
            if (capablePawns == null || capablePawns.Count == 0)
            {
                return false; // Skip original method if no capable pawns
            }

            Dictionary<WorkTypeDef, object> managedWorkTypeRules = AccessTools.Field(typeof(WorkPriorityUpdater), "_managedWorkTypeRules").GetValue(__instance) as Dictionary<WorkTypeDef, object>;
            if (managedWorkTypeRules == null)
            {
                Log.Error("Stability Patch: Could not get _managedWorkTypeRules from WorkPriorityUpdater.");
                return false;
            }

            object workManagerGameComponentInstance = AccessTools.PropertyGetter(typeof(LordKuper.WorkManager.WorkManagerGameComponent), "Instance").Invoke(null, null);
            if (workManagerGameComponentInstance == null)
            {
                Log.Error("Stability Patch: Could not get WorkManagerGameComponent.Instance.");
                return false;
            }

            object workManagerModSettingsInstance = AccessTools.PropertyGetter(typeof(LordKuper.WorkManager.WorkManagerMod), "Settings").Invoke(null, null);
            if (workManagerModSettingsInstance == null)
            {
                Log.Error("Stability Patch: Could not get WorkManagerMod.Settings.");
                return false;
            }

            IEnumerable<WorkTypeDef> dedicatedWorkTypesEnumerable = AccessTools.PropertyGetter(workManagerGameComponentInstance.GetType(), "DedicatedWorkTypes").Invoke(workManagerGameComponentInstance, null) as IEnumerable<WorkTypeDef>;
            if (dedicatedWorkTypesEnumerable == null)
            {
                Log.Error("Stability Patch: Could not get DedicatedWorkTypes from WorkManagerGameComponent.Instance.");
                return false;
            }
            List<WorkTypeDef> dedicatedWorkTypes = dedicatedWorkTypesEnumerable.ToList();
            List<object> list = new List<object>();

            foreach (WorkTypeDef dedicatedWorkType in dedicatedWorkTypes)
            {
                if (managedWorkTypeRules.ContainsKey(dedicatedWorkType))
                {
                    list.Add(managedWorkTypeRules[dedicatedWorkType]);
                }
                else
                {
                    Log.Warning($"Stability Patch: WorkTypeDef '{dedicatedWorkType.defName}' found in DedicatedWorkTypes but not in _managedWorkTypeRules. Skipping this WorkTypeDef to prevent KeyNotFoundException.");
                }
            }

            if (list.Count == 0)
            {
                return false; // Skip original method if no valid dedicated work types
            }



            // Re-implement the logic of AssignDedicatedWorkers
            object workTypeAssignmentRuleComparer = AccessTools.PropertyGetter(typeof(LordKuper.WorkManager.WorkManagerGameComponent), "WorkTypeAssignmentRuleComparer").Invoke(null, null);
            if (workTypeAssignmentRuleComparer == null)
            {
                Log.Error("Stability Patch: Could not get WorkTypeAssignmentRuleComparer from WorkManagerGameComponent.");
                return false;
            }

            // Sort the list using the obtained comparer
            list.Sort((System.Collections.Generic.IComparer<object>)workTypeAssignmentRuleComparer);

            // Access WorkManagerMod.Settings.DedicatedWorkerPriority and WorkManagerMod.Settings.DedicatedWorkerSkillScoreFactor etc.
            // These are public, so direct access is fine.

            // Access WorkManagerMod.Settings.UseDedicatedWorkers
            bool useDedicatedWorkers = (bool)AccessTools.PropertyGetter(workManagerModSettingsInstance.GetType(), "UseDedicatedWorkers").Invoke(workManagerModSettingsInstance, null);
            if (!useDedicatedWorkers)
            {
                return false; // If dedicated workers are not used, skip the rest of the logic
            }

            foreach (object item in list)
            {
                // Access properties of WorkTypeAssignmentRule using reflection
                int num = (int)AccessTools.Method(item.GetType(), "GetTargetWorkersCount").Invoke(item, new object[] { ((Verse.MapComponent)__instance).map, capablePawns.Count, list.Count });
                bool ensureWorkerAssigned = (bool)AccessTools.PropertyGetter(item.GetType(), "EnsureWorkerAssigned").Invoke(item, null);
                if (ensureWorkerAssigned)
                {
                    int minWorkerNumber = (int)AccessTools.PropertyGetter(item.GetType(), "MinWorkerNumber").Invoke(item, null);
                    num = Math.Max(num, minWorkerNumber);
                }

                List<object> list2 = new List<object>(capablePawns.Count);
                foreach (object capablePawn in capablePawns)
                {
                    // Access properties and methods of PawnCache and WorkTypeAssignmentRule using reflection
                    WorkTypeDef itemDef = (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null);
                    bool isAllowedWorker = (bool)AccessTools.Method(capablePawn.GetType(), "IsAllowedWorker").Invoke(capablePawn, new object[] { itemDef });
                    if (isAllowedWorker)
                    {
                        list2.Add(capablePawn);
                    }
                }

                if (list2.Count == 0)
                {
                    continue;
                }

                List<object> list3 = new List<object>(list2.Count);
                foreach (object item2 in list2)
                {
                    WorkTypeDef itemDef = (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null);
                    bool isBadWork = (bool)AccessTools.Method(item2.GetType(), "IsBadWork").Invoke(item2, new object[] { itemDef });
                    bool isDangerousWork = (bool)AccessTools.Method(item2.GetType(), "IsDangerousWork").Invoke(item2, new object[] { itemDef });
                    if (!isBadWork && !isDangerousWork)
                    {
                        list3.Add(item2);
                    }
                }

                int num2 = 0;
                foreach (object capablePawn2 in capablePawns)
                {
                    WorkTypeDef itemDef = (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null);
                    bool isActiveWork = (bool)AccessTools.Method(capablePawn2.GetType(), "IsActiveWork").Invoke(capablePawn2, new object[] { itemDef });
                    int dedicatedWorkerPriority = (int)AccessTools.PropertyGetter(workManagerModSettingsInstance.GetType(), "DedicatedWorkerPriority").Invoke(workManagerModSettingsInstance, null);

                    if (isActiveWork && (int)AccessTools.Method(capablePawn2.GetType(), "GetWorkPriority").Invoke(capablePawn2, new object[] { itemDef }) <= dedicatedWorkerPriority)
                    {
                        num2++;
                    }
                }

                if (num2 >= num)
                {
                    continue;
                }

                // GetDedicatedWorkersScores is a private static method, so we need to use AccessTools.Method
                // The method signature is:
                // private static Dictionary<PawnCache, float> GetDedicatedWorkersScores([NotNull] IReadOnlyCollection<PawnCache> pawns, [NotNull] WorkTypeDef workType, [NotNull] IReadOnlyCollection<WorkTypeAssignmentRule> rules)
                // We need to pass IReadOnlyCollection<object> for pawns and rules.
                System.Reflection.MethodInfo getDedicatedWorkersScoresMethod = AccessTools.Method(typeof(WorkPriorityUpdater), "GetDedicatedWorkersScores");

                // Create IReadOnlyCollection<object> from List<object>
                System.Type iReadOnlyCollectionObjectType = typeof(System.Collections.Generic.IReadOnlyCollection<>).MakeGenericType(typeof(object));
                System.Reflection.MethodInfo asReadOnlyMethod = typeof(System.Collections.Generic.List<object>).GetMethod("AsReadOnly");

                object list3AsReadOnlyCollection = asReadOnlyMethod.Invoke(list3, null);
                object listAsReadOnlyCollection = asReadOnlyMethod.Invoke(list, null);

                System.Collections.Generic.Dictionary<object, float> dedicatedWorkersScores = getDedicatedWorkersScoresMethod.Invoke(null, new object[] { list3AsReadOnlyCollection, (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null), listAsReadOnlyCollection }) as System.Collections.Generic.Dictionary<object, float>;

                if (dedicatedWorkersScores == null)
                {
                    Log.Error("Stability Patch: GetDedicatedWorkersScores returned null.");
                    return false;
                }

                while (num2 < num && dedicatedWorkersScores.Count > 0)
                {
                    object pawnCache = null;
                    float num3 = float.MinValue;
                    foreach (System.Collections.Generic.KeyValuePair<object, float> item3 in dedicatedWorkersScores)
                    {
                        if (item3.Value > num3)
                        {
                            num3 = item3.Value;
                            pawnCache = item3.Key;
                        }
                    }

                    if (pawnCache == null)
                    {
                        break;
                    }

                    WorkTypeDef itemDef = (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null);
                    int dedicatedWorkerPriority = (int)AccessTools.PropertyGetter(workManagerModSettingsInstance.GetType(), "DedicatedWorkerPriority").Invoke(workManagerModSettingsInstance, null);
                    num2++;
                    dedicatedWorkersScores.Remove(pawnCache);
                }

                if (num2 >= num)
                {
                    continue;
                }

                List<object> list4 = new List<object>(capablePawns.Count);
                foreach (object capablePawn3 in capablePawns)
                {
                    bool containsCapablePawn3 = false;
                    foreach (object p in list3)
                    {
                        if (p == capablePawn3)
                        {
                            containsCapablePawn3 = true;
                            break;
                        }
                    }
                    if (!containsCapablePawn3)
                    {
                        list4.Add(capablePawn3);
                    }
                }

                object list4AsReadOnlyCollection = asReadOnlyMethod.Invoke(list4, null);
                dedicatedWorkersScores = getDedicatedWorkersScoresMethod.Invoke(null, new object[] { list4AsReadOnlyCollection, (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null), listAsReadOnlyCollection }) as System.Collections.Generic.Dictionary<object, float>;

                if (dedicatedWorkersScores == null)
                {
                    Log.Error("Stability Patch: GetDedicatedWorkersScores returned null in second call.");
                    return false;
                }

                while (num2 < num && dedicatedWorkersScores.Count > 0)
                {
                    object pawnCache2 = null;
                    float num4 = float.MinValue;
                    foreach (System.Collections.Generic.KeyValuePair<object, float> item4 in dedicatedWorkersScores)
                    {
                        if (item4.Value > num4)
                        {
                            num4 = item4.Value;
                            pawnCache2 = item4.Key;
                        }
                    }

                    if (pawnCache2 == null)
                    {
                        break;
                    }

                    WorkTypeDef itemDef = (WorkTypeDef)AccessTools.PropertyGetter(item.GetType(), "Def").Invoke(item, null);
                    int dedicatedWorkerPriority = (int)AccessTools.PropertyGetter(workManagerModSettingsInstance.GetType(), "DedicatedWorkerPriority").Invoke(workManagerModSettingsInstance, null);
                    AccessTools.Method(pawnCache2.GetType(), "SetWorkPriority").Invoke(pawnCache2, new object[] { itemDef, dedicatedWorkerPriority });
                    num2++;
                    dedicatedWorkersScores.Remove(pawnCache2);
                }
            }

            return false; // Skip original method as we've re-implemented it
        }
    }

    /*[HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
    public static class JobDriver_HaulToContainer_MakeNewToils_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(JobDriver_HaulToContainer __instance)
        {
            if (__instance.job.targetB.Thing == null)
            {
                Log.Warning($"Stability Patch: JobDriver_HaulToContainer for pawn {__instance.pawn.LabelShort} has a null TargetIndex.B (Container). Aborting job to prevent NullReferenceException.");
                return false; // Skip original method, effectively cancelling the job
            }
            if (__instance.job.targetC.Thing == null && __instance.job.targetC.IsValid)
            {
                Log.Warning($"Stability Patch: JobDriver_HaulToContainer for pawn {__instance.pawn.LabelShort} has a null TargetIndex.C (PrimaryDestIndex) but it is valid. Aborting job to prevent NullReferenceException.");
                return false; // Skip original method, effectively cancelling the job
            }
            return true; // Continue to original method
        }
    }*/
}
