#nullable enable
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Lethality), "get_CanonicalKeywords")]
public static class PatchLethalityKeywords
{
    static void Postfix(object __instance, ref IEnumerable<CardKeyword> __result)
    {
        __result = new List<CardKeyword>();   // 别放 None，直接空
    }
}