using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

[HarmonyPatch(typeof(Veilpiercer), MethodType.Constructor)]
public class PatchVeilpiercer
{
    static void Postfix(object __instance)
    {
        var type = __instance.GetType().BaseType!;
        var field = type.GetField("<Rarity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? type.GetField("_rarity", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? type.GetField("rarity", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(__instance, CardRarity.Common);
    }
}