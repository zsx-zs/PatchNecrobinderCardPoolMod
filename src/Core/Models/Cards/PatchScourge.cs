#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchScourge
{
    [HarmonyPatch(typeof(Scourge), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            var vars = new List<DynamicVar>(__result);
            for (int i = 0; i < vars.Count; i++)
            {
                if (vars[i] is PowerVar<DoomPower>)
                {
                    vars[i] = new PowerVar<DoomPower>(12m);
                }

                else if (vars[i] is CardsVar)
                {
                    vars[i] = new CardsVar(2);
                }
            }

            __result = vars.AsReadOnly();
        }
    }

    [HarmonyPatch(typeof(Scourge), "OnUpgrade")]
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
            doom!.GetType().GetMethod("UpgradeValueBy")!.Invoke(doom, new object[] { 2m });

            var cards = dynamicVars.GetType().GetProperty("Cards")!.GetValue(dynamicVars);
            cards!.GetType().GetMethod("UpgradeValueBy")!.Invoke(cards, new object[] { 0m });

            return false;
        }
    }
}