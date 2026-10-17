using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Bodyguard), "get_CanonicalVars")]
public static class PatchBodyguard
{
    static void Postfix(ref IEnumerable<DynamicVar> __result)
    {
        __result = new List<DynamicVar> { new SummonVar(6m) };
    }

}