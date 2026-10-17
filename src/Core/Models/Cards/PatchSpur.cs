using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Spur), "get_CanonicalVars")]
public static class PatchSpur
{
    static void Postfix(ref IEnumerable<DynamicVar> __result)
    {
        var vars = new List<DynamicVar>(__result);
        for (int i = 0; i < vars.Count; i++)
        {
            if (vars[i] is SummonVar)
            {
                vars[i] = new SummonVar(4m);
                break;
            }
        }
        __result = vars.AsReadOnly();
    }
}