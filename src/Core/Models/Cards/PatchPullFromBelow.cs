#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchPullFromBelow
{
    [HarmonyPatch(typeof(PullFromBelow), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            var vars = new List<DynamicVar>(__result);
            for (int i = 0; i < vars.Count; i++)
            {
                if (vars[i] is DamageVar)
                {
                    vars[i] = new DamageVar(7m, ValueProp.Move);
                    break;
                }
            }
            __result = vars.AsReadOnly();
        }
    }

    [HarmonyPatch(typeof(PullFromBelow), "OnUpgrade")]
    public static class PatchOnUpgrade
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
            var damage = dynamicVars!.GetType().GetProperty("Damage")!.GetValue(dynamicVars);
            damage!.GetType().GetMethod("UpgradeValueBy")!.Invoke(damage, new object[] { 1m });
            return false;
        }
    }

    [HarmonyPatch(typeof(PullFromBelow), MethodType.Constructor)]
    public static class PatchConstructor
    {
        static void Postfix(object __instance)
        {
            var type = __instance.GetType().BaseType!;
            var field = type.GetField("<Rarity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? type.GetField("_rarity", BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? type.GetField("rarity", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(__instance, CardRarity.Rare);
        }
    }
}
