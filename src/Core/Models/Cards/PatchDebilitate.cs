#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchDebilitate
{
    // ① 耗能：3 → 升级后 2
    [HarmonyPatch(typeof(CardModel), "get_CanonicalEnergyCost")]
    public static class PatchCost
    {
        static void Postfix(CardModel __instance, ref int __result)
        {
            if (__instance is Debilitate) __result = __instance.IsUpgraded ? 2 : 3;
        }
    }

    // ② CardType.Skill：get_Type Postfix
    [HarmonyPatch(typeof(CardModel), "get_Type")]
    public static class PatchCardType
    {
        static void Postfix(CardModel __instance, ref CardType __result)
        {
            if (__instance is Debilitate) __result = CardType.Skill;
        }
    }

    // ③ TargetType.Self：get_TargetType Postfix
    [HarmonyPatch(typeof(CardModel), "get_TargetType")]
    public static class PatchTargetType
    {
        static void Postfix(CardModel __instance, ref TargetType __result)
        {
            if (__instance is Debilitate) __result = TargetType.Self;
        }
    }

    // ④ Keywords：Ethereal（虚无）
    [HarmonyPatch(typeof(CardModel), "get_CanonicalKeywords")]
    public static class PatchKeywords
    {
        static void Postfix(object __instance, ref IEnumerable<CardKeyword> __result)
        {
            if (__instance is not Debilitate) return;
            __result = new List<CardKeyword> { CardKeyword.Ethereal };
        }
    }

    // ⑤ CanonicalVars：EnergyVar(4)
    //    Debilitate 有 override CanonicalVars，必须 patch typeof(Debilitate)，拦不住基类 patch
    [HarmonyPatch(typeof(Debilitate), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new EnergyVar(4) };
        }
    }

    // ⑥ ExtraHoverTips：空（Debilitate 只是给能量，不需要额外提示）
    //    Debilitate 有 override ExtraHoverTips，同样必须 patch typeof(Debilitate)
    [HarmonyPatch(typeof(Debilitate), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static void Postfix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new List<IHoverTip>();
        }
    }

    // ⑦ OnUpgrade：跳过原版（原版碰 Damage/PowerVar，我们没这些了）
    //    Postfix 里反射把 _energyCost 置 null，强制下次访问时 CardEnergyCost 重建
    //    重建时会调我们 patch 过的 get_CanonicalEnergyCost（返回 2），Canonical 和 _base 自动正确
    [HarmonyPatch(typeof(Debilitate), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        private static readonly FieldInfo EnergyCostField =
            typeof(CardModel).GetField("_energyCost", BindingFlags.NonPublic | BindingFlags.Instance)!;

        static bool Prefix() => false;

        static void Postfix(Debilitate __instance)
        {
            EnergyCostField.SetValue(__instance, null);
        }
    }

    // ⑧ OnPlay：跳过原版（原版会 Attack + ApplyDebilitatePower）
    //    自己 GainEnergy(4, Owner)
    [HarmonyPatch(typeof(Debilitate), "OnPlay")]
    public static class PatchOnPlay
    {
        static bool Prefix() => false;

        static void Postfix(Debilitate __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            __result = PlayerCmd.GainEnergy(4, __instance.Owner);
        }
    }

    // ⑨ LocString：DEBILITATE.description
    //    {Energy:diff()} 自动渲染 4（EnergyVar.BaseValue）
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        // ===== 完全对齐原版 Wisp 的能量描述格式：{Energy:energyIcons()} =====
        private const string EngText = "Gain {Energy:energyIcons()}.";
        private const string ZhsText = "获得{Energy:energyIcons()}。";
        private const string ZhtText = "獲得{Energy:energyIcons()}。";
        private const string DeuText = "[gold]Erhalte[/gold] {Energy:energyIcons()}.";
        private const string EspText = "[gold]Gana[/gold] {Energy:energyIcons()}.";
        private const string FraText = "[gold]Gagnez[/gold] {Energy:energyIcons()}.";
        private const string IndText = "[gold]Dapatkan[/gold] {Energy:energyIcons()}.";
        private const string ItaText = "[gold]Ottieni[/gold] {Energy:energyIcons()}.";
        private const string JpnText = "[gold]エネルギー[/gold]を{Energy:energyIcons()}点[gold]獲得[/gold]する。";
        private const string KorText = "{Energy:energyIcons()} [gold]에너지[/gold]를 얻습니다.";
        private const string PolText = "[gold]Zdobądź[/gold] {Energy:energyIcons()}.";
        private const string PtbText = "[gold]Ganhe[/gold] {Energy:energyIcons()}.";
        private const string RusText = "[gold]Получите[/gold] {Energy:energyIcons()}.";
        private const string SpaText = "[gold]Gana[/gold] {Energy:energyIcons()}.";
        private const string ThaText = "[gold]ได้รับ[/gold] {Energy:energyIcons()}.";
        private const string TurText = "[gold]{Energy:energyIcons()}[/gold] kazan.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "DEBILITATE.description")
                return true;

            __result = LocManager.Instance.Language switch
            {
                "eng" => EngText, "zhs" => ZhsText, "zht" => ZhtText, "deu" => DeuText,
                "esp" => EspText, "fra" => FraText, "ind" => IndText, "ita" => ItaText,
                "jpn" => JpnText, "kor" => KorText, "pol" => PolText, "ptb" => PtbText,
                "rus" => RusText, "spa" => SpaText, "tha" => ThaText, "tur" => TurText,
                _ => EngText
            };
            return false;
        }
    }

    // ⑩ LocString：DEBILITATE.title（卡名：浮灵 / Souls Adrift）
    //    与 ⑨ 同出口，key 互斥，不会互相覆盖
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchTitle
    {
        private const string EngTitle = "Souls Adrift";
        private const string ZhsTitle = "浮灵";
        private const string ZhtTitle = "浮靈";
        private const string DeuTitle = "Getriebene Seelen";
        private const string EspTitle = "Almas a la deriva";
        private const string FraTitle = "Âmes à la dérive";
        private const string IndTitle = "Jiwa Terapung";
        private const string ItaTitle = "Anime alla deriva";
        private const string JpnTitle = "漂う魂";
        private const string KorTitle = "떠도는 영혼";
        private const string PolTitle = "Dryfujące dusze";
        private const string PtbTitle = "Almas à deriva";
        private const string RusTitle = "Блуждающие души";
        private const string SpaTitle = "Almas a la deriva";
        private const string ThaTitle = "วิญญาณล่องลอย";
        private const string TurTitle = "Sürüklenen Ruhlar";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "DEBILITATE.title")
                return true;

            __result = LocManager.Instance.Language switch
            {
                "eng" => EngTitle, "zhs" => ZhsTitle, "zht" => ZhtTitle, "deu" => DeuTitle,
                "esp" => EspTitle, "fra" => FraTitle, "ind" => IndTitle, "ita" => ItaTitle,
                "jpn" => JpnTitle, "kor" => KorTitle, "pol" => PolTitle, "ptb" => PtbTitle,
                "rus" => RusTitle, "spa" => SpaTitle, "tha" => ThaTitle, "tur" => TurTitle,
                _ => EngTitle
            };
            return false;
        }
    }
}
