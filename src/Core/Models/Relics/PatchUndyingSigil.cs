#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace PatchNecrobinderCardPoolMod.Relics;

[HarmonyPatch(typeof(UndyingSigil))]
public static class PatchUndyingSigil
{
    // —— 用 Dictionary 存每个 UndyingSigil 实例的 _usedThisTurn 状态
    //    相当于 Regalite 类里的 private bool _usedThisTurn —— 因为 UndyingSigil 没有这个字段
    //    UndyingSigil 实例数量极少（一个 run 里通常 0-1 个），不会泄漏
    private static readonly Dictionary<UndyingSigil, bool> UsedThisTurnTable = new();

    private static bool IsUsedThisTurn(UndyingSigil sigil)
        => UsedThisTurnTable.TryGetValue(sigil, out var used) && used;

    private static void MarkUsed(UndyingSigil sigil) => UsedThisTurnTable[sigil] = true;
    private static void ResetUsed(UndyingSigil sigil) => UsedThisTurnTable.Remove(sigil);

    // ========== ① 换数值：DamageDecrease(0.5) → BlockVar(5) ==========
    [HarmonyPatch(typeof(UndyingSigil), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar>
            {
                new BlockVar(5m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered)
            };
        }
    }

    // ========== ② 去掉原来的 DoomPower ExtraHoverTips ==========
    [HarmonyPatch(typeof(UndyingSigil), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static void Postfix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new List<IHoverTip>();
        }
    }

    // ========== ③ 跳过原版减伤逻辑 ==========
    [HarmonyPatch(typeof(UndyingSigil), nameof(UndyingSigil.ModifyDamageMultiplicative))]
    public static class PatchModifyDamage
    {
        static bool Prefix(ref decimal __result)
        {
            __result = 1m; // 不减伤
            return false;  // 跳过原版
        }
    }

    // ========== ④ 核心新逻辑：AfterPowerAmountChanged（基类 virtual，UndyingSigil 没 override）==========
    //    每回合首次给予 DoomPower → GainBlock(5)
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterPowerAmountChanged))]
    public static class PatchAfterPowerAmountChanged
    {
        static async Task Postfix(Task __result, AbstractModel __instance, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
        {
            await __result;

            if (__instance is not UndyingSigil sigil) return;
            if (power is not DoomPower) return;
            if (applier == null || applier != sigil.Owner.Creature) return;
            if (amount <= 0) return;
            if (IsUsedThisTurn(sigil)) return;

            MarkUsed(sigil);
            sigil.Flash();
            await CreatureCmd.GainBlock(sigil.Owner.Creature, sigil.DynamicVars.Block, null, fast: true);
        }
    }

    // ========== ⑤ 重置标记：BeforeSideTurnStart（每回合开始时）==========
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.BeforeSideTurnStart))]
    public static class PatchBeforeSideTurnStart
    {
        static void Postfix(AbstractModel __instance, IReadOnlyList<Creature> participants)
        {
            if (__instance is not UndyingSigil sigil) return;
            if (!participants.Contains(sigil.Owner.Creature)) return;
            ResetUsed(sigil);
        }
    }

    // ========== ⑥ 重置标记：AfterCombatEnd（战斗结束时）==========
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCombatEnd))]
    public static class PatchAfterCombatEnd
    {
        static void Postfix(AbstractModel __instance)
        {
            if (__instance is not UndyingSigil sigil) return;
            ResetUsed(sigil);
        }
    }

    // ========== ⑦ 本地化描述：relics 表里的 UNDYING_SIGIL.description ==========
    //    参照 Regalite 原版格式：[blue]{Block}[/blue] + [gold]Doom[/gold] + [gold]Block[/gold]
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        // —— 16 种语言，Eng 为默认 ——
        private const string EngText = "The first time you apply [gold]Doom[/gold] each turn, gain [blue]{Block}[/blue] [gold]Block[/gold].";
        private const string ZhsText = "每回合你第一次给予[gold]灾厄[/gold]时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。";
        private const string ZhtText = "每回合你第一次給予[gold]災厄[/gold]時，獲得[blue]{Block}[/blue]點[gold]格擋[/gold]。";
        private const string DeuText = "Wenn du in diesem Zug erstmals [gold]Verhängnis[/gold] zufügst, erhalte [blue]{Block}[/blue] [gold]Block[/gold].";
        private const string EspText = "La primera vez que apliques [gold]Condena[/gold] cada turno, obtén [blue]{Block}[/blue] de [gold]Bloqueo[/gold].";
        private const string FraText = "La première fois que vous appliquez [gold]Péril[/gold] ce tour, gagnez [blue]{Block}[/blue] [gold]Blocage[/gold].";
        private const string IndText = "Pertama kali kamu memberikan [gold]Kekutukan[/gold] setiap giliran, dapatkan [blue]{Block}[/blue] [gold]Blok[/gold].";
        private const string ItaText = "La prima volta che applichi [gold]Fato[/gold] ogni turno, ottieni [blue]{Block}[/blue] [gold]Blocco[/gold].";
        private const string JpnText = "このターン初めて[gold]運命[/gold]を付与したとき、[blue]{Block}[/blue]の[gold]ブロック[/gold]を得る。";
        private const string KorText = "매 턴 당신이 처음으로 [gold]파멸[/gold]을 부여할 때, [blue]{Block}[/blue]의 [gold]방어[/gold]를 얻습니다.";
        private const string PolText = "Gdy po raz pierwszy zadasz [gold]Przeznaczenie[/gold] w tej turze, otrzymasz [blue]{Block}[/blue] [gold]Bloku[/gold].";
        private const string PtbText = "Na primeira vez que você aplicar [gold]Condenação[/gold] neste turno, ganhe [blue]{Block}[/blue] [gold]Bloqueio[/gold].";
        private const string RusText = "В первый раз за этот ход, когда вы накладываете [gold]Рок[/gold], получите [blue]{Block}[/blue] [gold]Блока[/gold].";
        private const string ThaText = "ทุกครั้งแรกที่คุณให้[gold]กรรมถึงวาระ[/gold]ในเทิร์นนี้ รับ[blue]{Block}[/blue][gold]บล็อก[/gold]";
        private const string TurText = "Bu turda ilk kez [gold]Kıyamet[/gold] verdiğinizde, [blue]{Block}[/blue] [gold]Blok[/gold] kazanırsınız.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "relics" || __instance.LocEntryKey != "UNDYING_SIGIL.description") return true;
            // Godot locale 格式: "zh_CN", "en", "ja", "de" 等
            var locale = Godot.TranslationServer.GetLocale();
            __result = locale switch
            {
                var l when l.StartsWith("zh") && l.Contains("T") => ZhtText,     // zh_TW, zh_HK
                var l when l.StartsWith("zh") => ZhsText,                         // zh_CN
                var l when l.StartsWith("de") => DeuText,
                var l when l.StartsWith("es") => EspText,                         // es, es_MX
                var l when l.StartsWith("fr") => FraText,
                var l when l.StartsWith("id") => IndText,
                var l when l.StartsWith("it") => ItaText,
                var l when l.StartsWith("ja") => JpnText,
                var l when l.StartsWith("ko") => KorText,
                var l when l.StartsWith("pl") => PolText,
                var l when l.StartsWith("pt") && l.Contains("B") => PtbText,      // pt_BR
                var l when l.StartsWith("pt") => PtbText,                         // pt (用葡巴版)
                var l when l.StartsWith("ru") => RusText,
                var l when l.StartsWith("th") => ThaText,
                var l when l.StartsWith("tr") => TurText,
                _ => EngText
            };
            return false;
        }
    }
}
