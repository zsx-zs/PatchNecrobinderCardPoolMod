#nullable enable
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
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

    // 升级逻辑：固定8层灾厄，升级后目标变成所有敌人
    [HarmonyPatch(typeof(Countdown), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix() => false;  // 跳过原版 UpgradeValueBy(3m)，保持固定 8 层
    }

    // Countdown 没 override TargetType property，必须 patch 基类 CardModel + is filter
    [HarmonyPatch(typeof(CardModel), "get_TargetType")]
    public static class PatchTargetType
    {
        static void Postfix(CardModel __instance, ref TargetType __result)
        {
            if (__instance is Countdown && __instance.IsUpgraded)
                __result = TargetType.AllEnemies;
        }
    }

    // —— 本地化描述 ——
    // ShowIfUpgradedFormatter（StS2 内置，Name="show"）源码逻辑：
    //   {IfUpgraded:show:A|B}
    //     A = list[0] → 升级后正式显示
    //     B = list[1] → 未升级正式显示
    //     UpgradePreview (hover +号) → 自动 [green]A[/green]
    //   关键：StS2 **确实支持 A|B 分支**！第一版漏写了 `show:` 前缀才挂
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "COUNTDOWN.description") return true;

            // 公共词移到 IfUpgraded 外面，分支里只放真正变化的部分
            // 这样 UpgradePreview ([green]) 只高亮变化的词，不高亮公共词
            __result = LocManager.Instance.Language switch
            {
                // "to " 两边都有 → 提到外面
                "eng" => "At the start of your turn, apply {CountdownPower:diff()} [gold]Doom[/gold] to {IfUpgraded:show:ALL enemies|a random enemy}.",
                // "给予" 两边都有 → 提到外面
                "zhs" => "在你的回合开始时，给予{IfUpgraded:show:所有敌人|随机敌人}{CountdownPower:diff()}层[gold]灾厄[/gold]。",
                "zht" => "在你的回合開始時，給予{IfUpgraded:show:所有敵人|隨機敵人}{CountdownPower:diff()}層[gold]災厄[/gold]。",
                // "an " 两边都有 + 尾部 " an." → 都提到外面
                "deu" => "Zu Beginn deines Zuges wende {CountdownPower:diff()} [gold]Verhängnis[/gold] an {IfUpgraded:show:ALLE Feinde|einen zufälligen Feind} an.",
                // "a " 两边都有
                "esp" => "Al comienzo de tu turno, aplica {CountdownPower:diff()} [gold]Condena[/gold] a {IfUpgraded:show:TODOS los enemigos|un enemigo aleatorio}.",
                // "à " 两边都有
                "fra" => "Au début de votre tour, appliquez {CountdownPower:diff()} [gold]Péril[/gold] à {IfUpgraded:show:TOUS les ennemis|un ennemi aléatoire}.",
                // "ke " 两边都有
                "ind" => "Di awal giliranmu, berikan {CountdownPower:diff()} [gold]Kekutukan[/gold] ke {IfUpgraded:show:SEMUA musuh|musuh acak}.",
                // "a " 两边都有
                "ita" => "All'inizio del tuo turno, applica {CountdownPower:diff()} [gold]Fato[/gold] a {IfUpgraded:show:TUTTI i nemici|un nemico casuale}.",
                // 日文：结构本身已经是 {IfUpgraded} 只包住变化部分（全ての敵 vs ランダムな敵1体），を が 両方の外
                "jpn" => "あなたのターン開始時、{CountdownPower:diff()}層の[gold]運命[/gold]を{IfUpgraded:show:全ての敵|ランダムな敵1体}に与える。",
                // " 적에게" 两边都有 → 提到外面
                "kor" => "당신의 턴 시작 시, {CountdownPower:diff()}층의 [gold]파멸[/gold]을 {IfUpgraded:show:모든|랜덤한} 적에게 적용합니다.",
                // "do " 两边都有
                "pol" => "Na początku twojej tury, zastosuj {CountdownPower:diff()} [gold]Przeznaczenie[/gold] do {IfUpgraded:show:WSZYSTKICH wrogów|losowego wroga}.",
                // "a " 两边都有
                "ptb" => "No início do seu turno, aplique {CountdownPower:diff()} [gold]Condenação[/gold] a {IfUpgraded:show:TODOS os inimigos|um inimigo aleatório}.",
                // "на " 两边都有
                "rus" => "В начале вашего хода наложите {CountdownPower:diff()} [gold]Рок[/gold] на {IfUpgraded:show:ВСЕХ врагов|случайного врага}.",
                // "แก่ศัตรู" 两边都有 → 提到外面
                "tha" => "เมื่อเริ่มต้นเทิร์นของคุณ ให้ {CountdownPower:diff()} ชั้น [gold]กรรมถึงวาระ[/gold] แก่ศัตรู{IfUpgraded:show:ทั้งหมด|สุ่ม 1 ตัว}。",
                // 土耳其语结构不同：TÜM düşmanlara vs rastgele bir düşmana，uygula 在两边外面
                "tur" => "Turunun başlangıcında, {CountdownPower:diff()} [gold]Kıyamet[/gold] {IfUpgraded:show:TÜM düşmanlara|rastgele bir düşmana} uygula.",
                _ => "At the start of your turn, apply {CountdownPower:diff()} [gold]Doom[/gold] to {IfUpgraded:show:ALL enemies|a random enemy}."
            };
            return false;
        }
    }
}
