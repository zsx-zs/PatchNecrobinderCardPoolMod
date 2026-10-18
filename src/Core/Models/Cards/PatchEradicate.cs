using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Eradicate), "get_CanonicalVars")]
public static class PatchEradicate
{
    static void Postfix(ref IEnumerable<DynamicVar> __result)
    {
        __result = new List<DynamicVar> { new DamageVar(10m, ValueProp.Move) };
    }
}