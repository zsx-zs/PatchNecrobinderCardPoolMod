#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchCountdown
{
    [HarmonyPatch(typeof(Countdown), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new PowerVar<CountdownPower>(8m) };
        }
    }
    
    [HarmonyPatch(typeof(Countdown), "OnUpgrade")]
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
            
            var doom = dynamicVars!.GetType().GetProperty("Item")!.GetValue(dynamicVars, new object[] { "CountdownPower" });
            doom!.GetType().GetMethod("UpgradeValueBy")!.Invoke(doom, new object[] { 2m });
            return false;
        }
    }

}