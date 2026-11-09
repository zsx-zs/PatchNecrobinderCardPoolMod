#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchCountdown
{
    // 标记哪些 CountdownPower 实例来自升级后的 Countdown 卡
    private static readonly ConditionalWeakTable<CountdownPower, object> _upgradedCountdownMarkers = new();
    private static readonly object _markerToken = new();

    // —— CanonicalVars: 固定 9 层灾厄 ——
    [HarmonyPatch(typeof(Countdown), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            __result = new List<DynamicVar> { new PowerVar<CountdownPower>(9m) };
        }
    }

    // —— 跳过原版 OnUpgrade ——
    [HarmonyPatch(typeof(Countdown), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix() => false;
    }

    // —— CountdownPower 悬浮图标描述 ——
    [HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
    public static class PatchCountdownPowerHoverTips
    {
        static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance is not CountdownPower cdPower) return;

            bool isUpgraded = _upgradedCountdownMarkers.TryGetValue(cdPower, out _);
            string amount = cdPower.Amount.ToString("0");

            string desc = LocManager.Instance.Language switch
            {
                "eng" => $"At the start of your turn, apply {amount} [gold]Doom[/gold] to {(isUpgraded ? "ALL enemies" : "a random enemy")}.",
                "zhs" => $"在你的回合开始时，给予{(isUpgraded ? "所有敌人" : "随机敌人")}{amount}层[gold]灾厄[/gold]。",
                "zht" => $"在你的回合開始時，給予{(isUpgraded ? "所有敵人" : "隨機敵人")}{amount}層[gold]災厄[/gold]。",
                "deu" => $"Zu Beginn deines Zuges wende {amount} [gold]Verhängnis[/gold] an {(isUpgraded ? "ALLE Feinde" : "einen zufälligen Feind")} an.",
                "esp" => $"Al comienzo de tu turno, aplica {amount} [gold]Condena[/gold] a {(isUpgraded ? "TODOS los enemigos" : "un enemigo aleatorio")}.",
                "fra" => $"Au début de votre tour, appliquez {amount} [gold]Péril[/gold] à {(isUpgraded ? "TOUS les ennemis" : "un ennemi aléatoire")}.",
                "ind" => $"Di awal giliranmu, berikan {amount} [gold]Kekutukan[/gold] ke {(isUpgraded ? "SEMUA musuh" : "musuh acak")}.",
                "ita" => $"All'inizio del tuo turno, applica {amount} [gold]Fato[/gold] a {(isUpgraded ? "TUTTI i nemici" : "un nemico casuale")}.",
                "jpn" => $"あなたのターン開始時、{amount}層の[gold]運命[/gold]を{(isUpgraded ? "全ての敵" : "ランダムな敵1体")}に与える。",
                "kor" => $"당신의 턴 시작 시, {amount}층의 [gold]파멸[/gold]을 {(isUpgraded ? "모든" : "랜덤한")} 적에게 적용합니다.",
                "pol" => $"Na początku twojej tury, zastosuj {amount} [gold]Przeznaczenie[/gold] do {(isUpgraded ? "WSZYSTKICH wrogów" : "losowego wroga")}.",
                "ptb" => $"No início do seu turno, aplique {amount} [gold]Condenação[/gold] a {(isUpgraded ? "TODOS os inimigos" : "um inimigo aleatório")}.",
                "rus" => $"В начале вашего хода наложите {amount} [gold]Рок[/gold] на {(isUpgraded ? "ВСЕХ врагов" : "случайного врага")}.",
                "tha" => $"เมื่อเริ่มต้นเทิร์นของคุณ ให้ {amount} ชั้น [gold]กรรมถึงวาระ[/gold] แก่ศัตรู{(isUpgraded ? "ทั้งหมด" : "สุ่ม 1 ตัว")}。",
                "tur" => $"Turunun başlangıcında, {amount} [gold]Kıyamet[/gold] {(isUpgraded ? "TÜM düşmanlara" : "rastgele bir düşmana")} uygula.",
                _ => $"At the start of your turn, apply {amount} [gold]Doom[/gold] to {(isUpgraded ? "ALL enemies" : "a random enemy")}."
            };

            __result = new List<IHoverTip> { new HoverTip(__instance, desc, isSmart: false) };
        }
    }

    // —— 标记升级后的 CountdownPower 实例 ——
    [HarmonyPatch(typeof(Countdown), "OnPlay")]
    public static class PatchCountdownOnPlay
    {
        static async Task Postfix(Task __result, Countdown __instance)
        {
            await __result;

            if (!__instance.IsUpgraded) return;

            var summoner = __instance.Owner.Creature;
            var cdPower = summoner.Powers.OfType<CountdownPower>().FirstOrDefault();
            if (cdPower == null) return;

            _upgradedCountdownMarkers.Remove(cdPower);
            _upgradedCountdownMarkers.Add(cdPower, _markerToken);
        }
    }

    // —— 升级版 CountdownPower: 给所有敌人加灾厄 ——
    // Harmony 不支持 async Prefix，所以：
    //   Prefix 同步 bool 返回 → true=让原版跑 / false=跳过原版
    //   Postfix 用 ref Task __result 替换成我们自己的实现
    [HarmonyPatch(typeof(CountdownPower), "AfterSideTurnStart")]
    public static class PatchCountdownPowerAfterSideTurnStart
    {
        static bool Prefix(CountdownPower __instance, IReadOnlyList<Creature> participants)
        {
            // 没升级 / 不是自己回合 → 让原版正常跑
            if (!participants.Contains(__instance.Owner)) return true;
            if (!_upgradedCountdownMarkers.TryGetValue(__instance, out _)) return true;
            // 升级版 → 跳过原版，Postfix 会替换 Task
            return false;
        }

        static void Postfix(ref Task __result, CountdownPower __instance, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            // Prefix 已经处理了，这里只关心升级版且自己回合的情况
            if (!participants.Contains(__instance.Owner)) return;
            if (!_upgradedCountdownMarkers.TryGetValue(__instance, out _)) return;

            __result = Task.Run(async () =>
            {
                // 给所有敌人加 Doom（原版只加随机 1 个）
                foreach (var enemy in combatState.HittableEnemies)
                {
                    await PowerCmd.Apply<DoomPower>(
                        new ThrowingPlayerChoiceContext(),
                        enemy,
                        __instance.Amount,
                        __instance.Owner,
                        null);
                }
            });
        }
    }

    // —— 本地化描述 ——
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "COUNTDOWN.description") return true;

            __result = LocManager.Instance.Language switch
            {
                "eng" => "At the start of your turn, apply {CountdownPower:diff()} [gold]Doom[/gold] to {IfUpgraded:show:ALL enemies|a random enemy}.",
                "zhs" => "在你的回合开始时，给予{IfUpgraded:show:所有敌人|随机敌人}{CountdownPower:diff()}层[gold]灾厄[/gold]。",
                "zht" => "在你的回合開始時，給予{IfUpgraded:show:所有敵人|隨機敵人}{CountdownPower:diff()}層[gold]災厄[/gold]。",
                "deu" => "Zu Beginn deines Zuges wende {CountdownPower:diff()} [gold]Verhängnis[/gold] an {IfUpgraded:show:ALLE Feinde|einen zufälligen Feind} an.",
                "esp" => "Al comienzo de tu turno, aplica {CountdownPower:diff()} [gold]Condena[/gold] a {IfUpgraded:show:TODOS los enemigos|un enemigo aleatorio}.",
                "fra" => "Au début de votre tour, appliquez {CountdownPower:diff()} [gold]Péril[/gold] à {IfUpgraded:show:TOUS les ennemis|un ennemi aléatoire}.",
                "ind" => "Di awal giliranmu, berikan {CountdownPower:diff()} [gold]Kekutukan[/gold] ke {IfUpgraded:show:SEMUA musuh|musuh acak}.",
                "ita" => "All'inizio del tuo turno, applica {CountdownPower:diff()} [gold]Fato[/gold] a {IfUpgraded:show:TUTTI i nemici|un nemico casuale}.",
                "jpn" => "あなたのターン開始時、{CountdownPower:diff()}層の[gold]運命[/gold]を{IfUpgraded:show:全ての敵|ランダムな敵1体}に与える。",
                "kor" => "당신의 턴 시작 시, {CountdownPower:diff()}층의 [gold]파멸[/gold]을 {IfUpgraded:show:모든|랜덤한} 적에게 적용합니다.",
                "pol" => "Na początku twojej tury, zastosuj {CountdownPower:diff()} [gold]Przeznaczenie[/gold] do {IfUpgraded:show:WSZYSTKICH wrogów|losowego wroga}.",
                "ptb" => "No início do seu turno, aplique {CountdownPower:diff()} [gold]Condenação[/gold] a {IfUpgraded:show:TODOS os inimigos|um inimigo aleatório}.",
                "rus" => "В начале вашего хода наложите {CountdownPower:diff()} [gold]Рок[/gold] на {IfUpgraded:show:ВСЕХ врагов|случайного врага}.",
                "tha" => "เมื่อเริ่มต้นเทิร์นของคุณ ให้ {CountdownPower:diff()} ชั้น [gold]กรรมถึงวาระ[/gold] แก่ศัตรู{IfUpgraded:show:ทั้งหมด|สุ่ม 1 ตัว}。",
                "tur" => "Turunun başlangıcında, {CountdownPower:diff()} [gold]Kıyamet[/gold] {IfUpgraded:show:TÜM düşmanlara|rastgele bir düşmana} uygula.",
                _ => "At the start of your turn, apply {CountdownPower:diff()} [gold]Doom[/gold] to {IfUpgraded:show:ALL enemies|a random enemy}."
            };
            return false;
        }
    }
}
