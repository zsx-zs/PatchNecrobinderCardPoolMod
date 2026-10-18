using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchDevourLife
{
    [HarmonyPatch(typeof(DevourLife), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new PowerVar<DevourLifePower>(2m) };
        }
    }

    [HarmonyPatch(typeof(DevourLife), MethodType.Constructor)]
    public static class PatchConstructor
    {
        static void Postfix(object __instance)
        {
            var type = __instance.GetType().BaseType!;
            var field = type.GetField("<Rarity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(__instance, CardRarity.Uncommon);
        }
    }
}
