﻿#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(CardModel), "get_CanonicalKeywords")]
public static class PatchCleanseKeywords
{
    static void Postfix(object __instance, ref IEnumerable<CardKeyword> __result)
    {
        if (__instance is not Cleanse) return;
        __result = new List<CardKeyword> { CardKeyword.Ethereal };
    }
}

[HarmonyPatch(typeof(Cleanse), "OnUpgrade")]
public static class PatchCleanseOnUpgrade
{
    static bool Prefix(object __instance)
    {
        var type = __instance.GetType();
        PropertyInfo? prop = null;
        while (type != null)
        {
            prop = type.GetProperty("DynamicVars", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (prop != null) break;
            type = type.BaseType;
        }
        var dynamicVars = prop!.GetValue(__instance);
        var summon = dynamicVars!.GetType().GetProperty("Summon")!.GetValue(dynamicVars);
        summon!.GetType().GetMethod("UpgradeValueBy")!.Invoke(summon, new object[] { 1m });
        return false;
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