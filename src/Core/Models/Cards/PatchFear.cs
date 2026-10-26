#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchFear
{
    // ---------- ① 耗能 0：get_CanonicalEnergyCost Postfix（和 BansheesCry 同款） ----------
    //    不能改 Constructor backing field——费用是动态算的，会被覆盖
    [HarmonyPatch(typeof(CardModel), "get_CanonicalEnergyCost")]
    public static class PatchCost
    {
        static void Postfix(object __instance, ref int __result)
        {
            if (__instance is not Fear) return;
            __result = 0;
        }
    }

    // ---------- ② CardType.Skill：get_Type Postfix（反编译确认属性叫 Type，不是 CardType） ----------
    [HarmonyPatch(typeof(CardModel), "get_Type")]
    public static class PatchCardType
    {
        static void Postfix(CardModel __instance, ref CardType __result)
        {
            if (__instance is Fear) __result = CardType.Skill;
        }
    }

    // ---------- ③ TargetType.AllEnemies：get_TargetType Postfix ----------
    [HarmonyPatch(typeof(CardModel), "get_TargetType")]
    public static class PatchTargetType
    {
        static void Postfix(CardModel __instance, ref TargetType __result)
        {
            if (__instance is Fear) __result = TargetType.AllEnemies;
        }
    }

    // ---------- ④ Keywords：Exhaust（消耗） ----------
    [HarmonyPatch(typeof(Fear), "get_CanonicalKeywords")]
    public static class PatchCanonicalKeywords
    {
        static void Postfix(ref IEnumerable<CardKeyword> __result)
        {
            __result = new List<CardKeyword> { CardKeyword.Exhaust };
        }
    }

    // ---------- ⑤ CanonicalVars：只剩 PowerVar<VulnerablePower>(1m) ----------
    [HarmonyPatch(typeof(Fear), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new PowerVar<VulnerablePower>(1m) };
        }
    }

    // ---------- ⑥ ExtraHoverTips：易伤 Power ----------
    [HarmonyPatch(typeof(Fear), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static void Postfix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new[] { HoverTipFactory.FromPower<VulnerablePower>() };
        }
    }

    // ---------- ⑦ OnUpgrade：跳过原版（原版会给不存在的 DamageVar 升级，会崩） ----------
    //    Postfix 里自己调 VulnerablePower.UpgradeValueBy(1m) → 让 BaseValue 真从 1→2
    [HarmonyPatch(typeof(Fear), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix() => false;  // 跳过原版

        static void Postfix(Fear __instance)
        {
            // VulnerablePower 的 BaseValue 从 1 升到 2
            __instance.DynamicVars.Vulnerable.UpgradeValueBy(1m);
        }
    }

    // ---------- ⑧ OnPlay：Postfix pass-through，全体敌人 VulnerablePower.BaseValue ----------
    //    直接用 VulnerablePower.BaseValue（升级后自动是 2，不用硬编码）
    [HarmonyPatch(typeof(Fear), "OnPlay")]
    public static class PatchOnPlay
    {
        static void Postfix(Fear __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            int amount = (int)__instance.DynamicVars.Vulnerable.BaseValue;
            var combatState = __instance.CombatState!;
            var enemies = combatState.Enemies.Where(e => e != null && e.IsAlive).ToList();

            __result = Task.CompletedTask.ContinueWith(async _ =>
            {
                foreach (var enemy in enemies)
                {
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, amount, __instance.Owner.Creature, __instance);
                }
            }).Unwrap();
        }
    }

    // ---------- ⑨ LocString：FEAR.description ----------
    //    VulnerablePower.BaseValue 升级后真变成 2，所以直接 {VulnerablePower:diff()} 就行
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        private const string EngText =
            "[gold]Apply[/gold] {VulnerablePower:diff()} [gold]Vulnerable[/gold] to ALL enemies.";

        private const string ZhsText =
            "对所有敌人施加{VulnerablePower:diff()}层[gold]易伤[/gold]。";

        private const string ZhtText =
            "對所有敵人施加{VulnerablePower:diff()}層[gold]易傷[/gold]。";

        private const string DeuText =
            "Verleihe allen Gegnern {VulnerablePower:diff()} [gold]Verwundbar[/gold].";

        private const string EspText =
            "Aplica {VulnerablePower:diff()} [gold]Vulnerable[/gold] a todos los enemigos.";

        private const string FraText =
            "Applique {VulnerablePower:diff()} [gold]Vulnérable[/gold] à TOUS les ennemis.";

        private const string IndText =
            "Berikan {VulnerablePower:diff()} [gold]Rentan[/gold] kepada SEMUA musuh.";

        private const string ItaText =
            "Applica {VulnerablePower:diff()} [gold]Vulnerabile[/gold] a TUTTI i nemici.";

        private const string JpnText =
            "すべての敵に{VulnerablePower:diff()}層の[gold]脆弱[/gold]を付与する。";

        private const string KorText =
            "모든 적에게 {VulnerablePower:diff()} [gold]취약[/gold]을 부여합니다.";

        private const string PolText =
            "Nałóż {VulnerablePower:diff()} [gold]Wrażliwy[/gold] na WSZYSTKICH wrogów.";

        private const string PtbText =
            "Aplique {VulnerablePower:diff()} [gold]Vulnerável[/gold] a TODOS os inimigos.";

        private const string RusText =
            "Нанесите {VulnerablePower:diff()} [gold]Уязвимый[/gold] ВСЕМ врагам.";

        private const string SpaText =
            "Aplica {VulnerablePower:diff()} [gold]Vulnerable[/gold] a todos los enemigos.";

        private const string ThaText =
            "ให้[gold]อ่อนแอ[/gold] {VulnerablePower:diff()} แก่ศัตรูทั้งหมด";

        private const string TurText =
            "TÜM düşmanlara {VulnerablePower:diff()} [gold]Hassas[/gold] uygula.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "FEAR.description")
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
}
