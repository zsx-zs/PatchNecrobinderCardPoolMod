#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchReanimate
{
    [HarmonyPatch(typeof(Reanimate), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            var vars = new List<DynamicVar>(__result);
            for (int i = 0; i < vars.Count; i++)
            {
                if (vars[i] is SummonVar)
                {
                    vars[i] = new SummonVar(22m);
                    break;
                }
            }

            __result = vars.AsReadOnly();
        }
    }

    [HarmonyPatch(typeof(Reanimate), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix(object __instance)
        {
            var type = __instance.GetType();
            PropertyInfo? prop = null;
            while (type != null)
            {
                prop = type.GetProperty("DynamicVars",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null) break;
                type = type.BaseType;
            }

            var dynamicVars = prop!.GetValue(__instance);
            var summon = dynamicVars!.GetType().GetProperty("Summon")!.GetValue(dynamicVars);
            summon!.GetType().GetMethod("UpgradeValueBy")!.Invoke(summon, new object[] { 6m });
            return false;
        }
    }
}