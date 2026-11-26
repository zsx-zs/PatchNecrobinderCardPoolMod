#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchCleanse
{

    [HarmonyPatch(typeof(CardModel), "get_CanonicalKeywords")]
    public static class PatchCleanseKeywords
    {
        static void Postfix(object __instance, ref IEnumerable<CardKeyword> __result)
        {
            if (__instance is not Cleanse) return;
            __result = new List<CardKeyword> { CardKeyword.Ethereal };
        }
    }

    [HarmonyPatch(typeof(Cleanse), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new SummonVar(2m) };
        }

    }

    [HarmonyPatch(typeof(CardModel), "get_CanonicalEnergyCost")]
    public static class PatchCleanseCost
    {
        static void Postfix(object __instance, ref int __result)
        {
            if (__instance is not Cleanse) return;
            __result = 0;
        }
    }
}