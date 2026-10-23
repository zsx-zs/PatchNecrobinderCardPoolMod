#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchDeathbringer
{
    [HarmonyPatch(typeof(Deathbringer), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            var vars = new List<DynamicVar>(__result);
            for (int i = 0; i < vars.Count; i++)
            {
                if (vars[i] is PowerVar<DoomPower>)
                {
                    vars[i] = new PowerVar<DoomPower>(17m);
                    break;
                }
            }

            __result = vars.AsReadOnly();
        }
    }

    [HarmonyPatch(typeof(Deathbringer), "OnUpgrade")]
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

            var doom = dynamicVars!.GetType().GetProperty("Doom")!.GetValue(dynamicVars);
            doom!.GetType().GetMethod("UpgradeValueBy")!.Invoke(doom, new object[] { 4m });

            var weak = dynamicVars.GetType().GetProperty("Weak")!.GetValue(dynamicVars);
            weak!.GetType().GetMethod("UpgradeValueBy")!.Invoke(weak, new object[] { 1m });

            return false;
        }
    }
}