#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchDirge
{
    // 稀有度
    [HarmonyPatch(typeof(Dirge), MethodType.Constructor)]
    public static class PatchConstructor
    {
        static void Postfix(object __instance)
        {
            var type = __instance.GetType().BaseType!;
            var field = type.GetField("<Rarity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(__instance, CardRarity.Rare);
        }
    }

    // ① Summon BaseValue 固定 4（升前就 4，升后还是 4）
    [HarmonyPatch(typeof(Dirge), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new SummonVar(4m) };
        }
    }

    // ② 跳过 OnUpgrade（Summon.UpgradeValueBy 不再需要）
    [HarmonyPatch(typeof(Dirge), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix() => false;
    }

    // ③ ResolveEnergyXValue 升级后 +1（召次数 + Soul 数量一起影响）
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue))]
    public static class PatchResolveEnergyXValue
    {
        static void Postfix(CardModel __instance, ref int __result)
        {
            if (__instance is Dirge && __instance.IsUpgraded)
                __result++;
        }
    }

    // ④ Transpiler：跳过 Dirge.OnPlay 里的 CardCmd.Upgrade(soul) 调用
    [HarmonyPatch]
    public static class PatchOnPlayStateMachine
    {
        public static void SkipUpgradeSingle(CardModel card) { }
        public static void SkipUpgradeDouble(CardModel card, CardPreviewStyle style) { }

        static MethodBase TargetMethod()
        {
            MethodInfo onPlay = AccessTools.Method(typeof(Dirge), "OnPlay")
                ?? throw new Exception("[PatchDirge] 找不到 Dirge.OnPlay");
            AsyncStateMachineAttribute sm = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()
                ?? throw new Exception("[PatchDirge] OnPlay 不是 async 方法");
            return AccessTools.Method(sm.StateMachineType, "MoveNext")
                ?? throw new Exception("[PatchDirge] 找不到 MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var upgradeSingle = AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Upgrade),
                new[] { typeof(CardModel) });
            var upgradeDouble = AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Upgrade),
                new[] { typeof(CardModel), typeof(CardPreviewStyle) });
            var skipSingle = AccessTools.Method(typeof(PatchOnPlayStateMachine), nameof(SkipUpgradeSingle));
            var skipDouble = AccessTools.Method(typeof(PatchOnPlayStateMachine), nameof(SkipUpgradeDouble));

            int replaced = 0;
            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo mi)
                {
                    if (mi == upgradeSingle) { ins.operand = skipSingle; replaced++; }
                    else if (mi == upgradeDouble) { ins.operand = skipDouble; replaced++; }
                }
                yield return ins;
            }
            if (replaced == 0)
                throw new Exception("[PatchDirge] MoveNext 里没找到 CardCmd.Upgrade 调用");
        }
    }

    // ⑤ ExtraHoverTips 永远显示未升级 Soul
    [HarmonyPatch(typeof(Dirge), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static bool Prefix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new[] { HoverTipFactory.FromCard<Soul>(false) };
            return false;
        }
    }

    // ⑥ 卡面描述：严格保持原版 Dirge 结构，只改两处
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        // ===== 每语言保持原版 Dirge 结构，只做两处最小改动：=====
        // 1. 两行的 X 后面加 {IfUpgraded:show:+1}（X 升级后 +1）
        //    第一行额外加括号：升级后变成 "4(X+1)" 而不是 "4X+1"
        // 2. 去掉 {IfUpgraded:show:Souls+|Souls} 条件，永远显示 Soul

        private const string EngText =
            "[gold]Summon[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} times.\nAdd X{IfUpgraded:show:+1} [gold]Soul[/gold] into your [gold]Draw Pile[/gold].";

        private const string ZhsText =
            "[gold]召唤[/gold]{Summon:diff()}{IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)}次。\n将X{IfUpgraded:show:+1}张[gold]灵魂[/gold]添加到你的[gold]抽牌堆[/gold]中。";

        private const string ZhtText =
            "[gold]召喚[/gold]{Summon:diff()}{IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)}次。\n將X{IfUpgraded:show:+1}張[gold]靈魂[/gold]添加到你的[gold]抽牌堆[/gold]中。";

        private const string DeuText =
            "[gold]Beschwöre[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} Mal.\nLege X{IfUpgraded:show:+1} [gold]Seele[/gold] in deinen [gold]Ziehstapel[/gold].";

        private const string EspText =
            "[gold]Invoca[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} veces.\nAñade X{IfUpgraded:show:+1} [gold]alma[/gold] a tu [gold]pila de robo[/gold].";

        private const string FraText =
            "[gold]Invoquez[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} fois.\nAjoute X{IfUpgraded:show:+1} [gold]âme[/gold] à votre [gold]pioche[/gold].";

        private const string IndText =
            "[gold]Panggil[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} kali.\nTambahkan X{IfUpgraded:show:+1} [gold]Jiwa[/gold] ke dalam [gold]tumpukan undi[/gold]mu.";

        private const string ItaText =
            "[gold]Evoca[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} volte.\nAggiungi X{IfUpgraded:show:+1} [gold]anima[/gold] alla tua [gold]pila di pesca[/gold].";

        private const string JpnText =
            "[gold]召喚[/gold] {Summon:diff()}体{IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)}回。\nX{IfUpgraded:show:+1}枚の[gold]ソウル[/gold]を[gold]山札[/gold]に加える。";

        private const string KorText =
            "[gold]소환[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)}회 합니다.\nX{IfUpgraded:show:+1}장의 [gold]영혼[/gold]을 [gold]드로우 더미[/gold]에 추가합니다.";

        private const string PolText =
            "[gold]Przyzwij[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} razy.\nDodaj X{IfUpgraded:show:+1} [gold]Duszę[/gold] do swojego [gold]stosu dobierania[/gold].";

        private const string PtbText =
            "[gold]Convoque[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} vezes.\nAdicione X{IfUpgraded:show:+1} [gold]Alma[/gold] à sua [gold]pilha de compra[/gold].";

        private const string RusText =
            "[gold]Призовите[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} раз.\nДобавьте X{IfUpgraded:show:+1} [gold]душу[/gold] в вашу [gold]стопку добора[/gold].";

        private const string SpaText =
            "[gold]Invoca[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} veces.\nAñade X{IfUpgraded:show:+1} [gold]alma[/gold] a tu [gold]pila de robo[/gold].";

        private const string ThaText =
            "[gold]อัญเชิญ[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} ครั้ง\nเพิ่ม X{IfUpgraded:show:+1} [gold]วิญญาณ[/gold]ลงใน[gold]กองจั่ว[/gold]ของคุณ";

        private const string TurText =
            "[gold]Çağır[/gold] {Summon:diff()} {IfUpgraded:show:(}X{IfUpgraded:show:+1}{IfUpgraded:show:)} kez.\nX{IfUpgraded:show:+1} [gold]Ruh[/gold]u [gold]Çekme Destene[/gold] ekle.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "DIRGE.description")
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
