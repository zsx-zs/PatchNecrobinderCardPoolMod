using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Sow), "get_CanonicalVars")]
public static class PatchSow
{
    static void Postfix(ref IEnumerable<DynamicVar> __result)
    {
        __result = new List<DynamicVar> { new DamageVar(9m, ValueProp.Move) };
    }

}