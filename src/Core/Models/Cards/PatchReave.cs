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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchReave
{
    // ---------- ① 升级后不再升级生成的 Soul（OnPlay 是 async，改状态机的 MoveNext） ----------

    [HarmonyPatch]
    public static class PatchOnPlayStateMachine
    {
        // 替换目标：签名必须和 CardCmd.Upgrade(CardModel, CardPreviewStyle) 完全一致
        // 参数只是占位符（Rider 的未使用参数警告可无视），不能删，否则 IL 栈对不上
        public static void SkipUpgrade(CardModel card, CardPreviewStyle style)
        {
        }

        static MethodBase TargetMethod()
        {
            MethodInfo onPlay = AccessTools.Method(typeof(Reave), "OnPlay")
                ?? throw new Exception("[PatchReave] 找不到 Reave.OnPlay");

            AsyncStateMachineAttribute sm = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()
                ?? throw new Exception("[PatchReave] OnPlay 不是 async 方法");

            return AccessTools.Method(sm.StateMachineType, "MoveNext")
                ?? throw new Exception("[PatchReave] 找不到 MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo upgrade = AccessTools.Method(
                typeof(CardCmd),
                nameof(CardCmd.Upgrade),
                new[] { typeof(CardModel), typeof(CardPreviewStyle) })
                ?? throw new Exception("[PatchReave] 找不到 CardCmd.Upgrade(CardModel, CardPreviewStyle)");

            MethodInfo replacement = AccessTools.Method(typeof(PatchOnPlayStateMachine), nameof(SkipUpgrade));

            int replaced = 0;
            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo mi && mi == upgrade)
                {
                    ins.operand = replacement; // 只换调用目标，栈效果不变
                    replaced++;
                }
                yield return ins;
            }

            if (replaced == 0)
                throw new Exception("[PatchReave] MoveNext 里没找到 CardCmd.Upgrade 调用，patch 未生效");
        }
    }

    // ---------- ② 升级后 Cards +1：灵魂 → 2张灵魂 ----------

    [HarmonyPatch(typeof(Reave), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static void Postfix(Reave __instance)
        {
            object dynamicVars = AccessTools.Property(__instance.GetType(), "DynamicVars")!
                .GetValue(__instance)!;
            object cards = AccessTools.Property(dynamicVars.GetType(), "Cards")!
                .GetValue(dynamicVars)!;
            AccessTools.Method(cards.GetType(), "UpgradeValueBy", new[] { typeof(decimal) })!
                .Invoke(cards, new object[] { 1m });
        }
    }

    // ---------- ③ 悬停提示显示未升级版 Soul ----------

    [HarmonyPatch(typeof(Reave), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static bool Prefix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new[] { HoverTipFactory.FromCard<Soul>(false) };
            return false;
        }
    }

    // ---------- ④ 卡面描述：去掉"灵魂+"字样，显示实际数量（全语言） ----------

    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        private const string EngText =
            "Deal {Damage:diff()} damage.\nAdd {Cards:diff()} [gold]{Cards:plural:Soul|Souls}[/gold] into your [gold]Draw Pile[/gold].";

        private const string ZhsText =
            "造成{Damage:diff()}点伤害。\n将{Cards:diff()}张[gold]灵魂[/gold]加入你的[gold]抽牌堆[/gold]。";

        private const string ZhtText =
            "造成{Damage:diff()}點傷害。\n將{Cards:diff()}張[gold]靈魂[/gold]加入你的[gold]抽牌堆[/gold]。";

        private const string DeuText =
            "Verursache {Damage:diff()} Schaden.\nMische {Cards:diff()} [gold]{Cards:plural:Seele|Seelen}[/gold] in deinen [gold]Ziehstapel[/gold].";

        private const string EspText =
            "Inflige {Damage:diff()} puntos de daño.\nAñade {Cards:diff()} [gold]{Cards:plural:alma|almas}[/gold] a tu [gold]pila de robo[/gold].";

        private const string FraText =
            "Inflige {Damage:diff()} dégâts.\nAjoute {Cards:diff()} [gold]{Cards:plural:âme|âmes}[/gold] à votre [gold]pioche[/gold].";

        private const string IndText =
            "Berikan {Damage:diff()} kerusakan.\nTambahkan {Cards:diff()} [gold]Jiwa[/gold] ke dalam [gold]tumpukan undi[/gold]mu.";

        private const string ItaText =
            "Infliggi {Damage:diff()} danni.\nAggiungi {Cards:diff()} [gold]{Cards:plural:anima|anime}[/gold] alla tua [gold]pila di pesca[/gold].";

        private const string JpnText =
            "{Damage:diff()}ダメージを与える。\n[gold]ソウル[/gold]を{Cards:diff()}枚[gold]山札[/gold]に加える。";

        private const string KorText =
            "{Damage:diff()} 피해를 줍니다.\n[gold]영혼[/gold] {Cards:diff()}장을 [gold]드로우 더미[/gold]에 추가합니다.";

        private const string PolText =
            "Zadaj {Damage:diff()} obrażeń.\nDodaj {Cards:diff()} [gold]{Cards:plural:duszę|dusze|dusz}[/gold] do swojego [gold]stosu dobierania[/gold].";

        private const string PtbText =
            "Cause {Damage:diff()} de dano.\nAdicione {Cards:diff()} [gold]{Cards:plural:Alma|Almas}[/gold] à sua [gold]pilha de compra[/gold].";

        private const string RusText =
            "Нанесите {Damage:diff()} урона.\nДобавьте {Cards:diff()} [gold]{Cards:plural:душу|души|душ}[/gold] в вашу [gold]стопку добора[/gold].";

        private const string SpaText =
            "Inflige {Damage:diff()} puntos de daño.\nAñade {Cards:diff()} [gold]{Cards:plural:alma|almas}[/gold] a tu [gold]pila de robo[/gold].";

        private const string ThaText =
            "สร้างความเสียหาย {Damage:diff()} หน่วย\nเพิ่ม[gold]วิญญาณ[/gold] {Cards:diff()} ใบลงใน[gold]กองจั่ว[/gold]ของคุณ";

        private const string TurText =
            "{Damage:diff()} hasar ver.\n[gold]Çekme Destene[/gold] {Cards:diff()} [gold]Ruh[/gold] ekle.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            // 只拦截这一条词条，其他全部放行
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "REAVE.description")
                return true;

            __result = LocManager.Instance.Language switch
            {
                "eng" => EngText,
                "zhs" => ZhsText,
                "zht" => ZhtText,
                "deu" => DeuText,
                "esp" => EspText,
                "fra" => FraText,
                "ind" => IndText,
                "ita" => ItaText,
                "jpn" => JpnText,
                "kor" => KorText,
                "pol" => PolText,
                "ptb" => PtbText,
                "rus" => RusText,
                "spa" => SpaText,
                "tha" => ThaText,
                "tur" => TurText,
                _ => EngText   // 未来新增语言的兜底
            };
            return false;
        }
    }
}



