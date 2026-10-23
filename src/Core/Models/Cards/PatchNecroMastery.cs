#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchNecroMastery
{
    // ① 回合开始回复 2 HP
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterPlayerTurnStart))]
    public static class PatchAbstractModelAfterPlayerTurnStart
    {
        static Task Postfix(Task __result, AbstractModel __instance, PlayerChoiceContext choiceContext, Player player)
        {
            if (__instance is not NecroMasteryPower) return __result;
            return __result.ContinueWith(async _ =>
            {
                if (player.Osty != null && player.Osty.IsAlive)
                    await CreatureCmd.Heal(player.Osty, 2m);
            });
        }
    }

    // ② 战斗中 Power Tooltip — patch HoverTips 属性 getter（真实入口！）
    [HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
    public static class PatchPowerModelHoverTips
    {
        static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance is not NecroMasteryPower) return;

            string desc = LocManager.Instance.Language switch
            {
                "eng" => "Whenever [gold]Osty[/gold] loses HP,\nALL enemies lose that much HP as well.\nAt the start of each turn, [gold]Osty[/gold] heals 2 HP.",
                "zhs" => "每当[gold]奥斯提[/gold]失去生命值时，所有敌人失去等量生命值。\n每回合开始时，[gold]奥斯提[/gold]回复2点生命。",
                "zht" => "每當[gold]奧斯提[/gold]失去生命時，所有敵人失去等量生命。\n每回合開始時，[gold]奧斯提[/gold]回復2生命。",
                "deu" => "Immer wenn [gold]Osty[/gold] HP verliert, verlieren alle Feinde genauso viel HP.\nZu Beginn jeder Runde stellt [gold]Osty[/gold] 2 HP wieder her.",
                "esp" => "Cada vez que [gold]Osty[/gold] pierde HP, todos los enemigos pierden esa misma cantidad de HP.\nAl comienzo de cada turno, [gold]Osty[/gold] se cura 2 HP.",
                "fra" => "Chaque fois que [gold]Osty[/gold] perd des PV, tous les ennemis perdent autant de PV.\nAu début de chaque tour, [gold]Osty[/gold] se soigne de 2 PV.",
                "ind" => "Setiap kali [gold]Osty[/gold] kehilangan HP, semua musuh kehilangan HP sebanyak itu.\nDi awal setiap giliran, [gold]Osty[/gold] memulihkan 2 HP.",
                "ita" => "Ogni volta che [gold]Osty[/gold] perde HP, tutti i nemici perdono la stessa quantità di HP.\nAll'inizio di ogni turno, [gold]Osty[/gold] cura 2 HP.",
                "jpn" => "[gold]オスティ[/gold]がHPを失うたび、すべての敵も同じだけHPを失う。\n毎ターン開始時、[gold]オスティ[/gold]が2 HP回復する。",
                "kor" => "[gold]오스티[/gold]가 HP를 잃을 때마다 모든 적도 그만큼 HP를 잃습니다.\n매 턴 시작 시 [gold]오스티[/gold]가 2 HP 회복합니다.",
                "pol" => "Za każdym razem gdy [gold]Osty[/gold] traci HP, wszyscy wrogowie tracą tyle samo HP.\nNa początku każdej tury [gold]Osty[/gold] leczy się o 2 HP.",
                "ptb" => "Sempre que [gold]Osty[/gold] perde HP, todos os inimigos perdem a mesma quantidade de HP.\nNo início de cada turno, [gold]Osty[/gold] recupera 2 HP.",
                "rus" => "Каждый раз когда [gold]Ости[/gold] теряет HP, все враги теряют столько же HP.\nВ начале каждого хода [gold]Ости[/gold] восстанавливает 2 HP.",
                "spa" => "Cada vez que [gold]Osty[/gold] pierde HP, todos los enemigos pierden esa misma cantidad de HP.\nAl comienzo de cada turno, [gold]Osty[/gold] se cura 2 HP.",
                "tha" => "เมื่อใดก็ตามที่[gold]ออสตี้[/gold]เสีย HP ศัตรูทั้งหมดจะเสีย HP เท่านั้น\nเมื่อเริ่มเทิร์น [gold]ออสตี้[/gold]รักษา 2 HP",
                "tur" => "[gold]Osty[/gold] her HP kaybettiğinde, tüm düşmanlar o kadar HP kaybeder.\nHer turun başında [gold]Osty[/gold] 2 HP iyileşir.",
                _ => "Whenever [gold]Osty[/gold] loses HP,\nALL enemies lose that much HP as well.\nAt the start of each turn, [gold]Osty[/gold] heals 2 HP."
            };

            var tip = new HoverTip(__instance, desc, isSmart: false);
            __result = new List<IHoverTip> { tip };
        }
    }

    // ③ 卡牌描述 patch
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        private const string EngText =
            "[gold]Summon[/gold] {Summon:diff()}.\nWhenever [gold]Osty[/gold] loses HP,\nALL enemies lose that much HP as well.\nAt the start of each turn, [gold]Osty[/gold] heals 2 HP.";
        private const string ZhsText =
            "[gold]召唤[/gold] {Summon:diff()}。\n每当[gold]奥斯提[/gold]失去生命，\n所有敌人也会失去等量生命。\n每回合开始时，[gold]奥斯提[/gold]回复2点生命。";
        private const string ZhtText =
            "[gold]召喚[/gold] {Summon:diff()}。\n每當[gold]奧斯提[/gold]失去生命，\n所有敵人也會失去等量生命。\n每回合開始時，[gold]奧斯提[/gold]回復2生命。";
        private const string DeuText =
            "[gold]Beschwöre[/gold] {Summon:diff()}.\nImmer wenn [gold]Osty[/gold] HP verliert,\nverlieren alle Feinde genauso viel HP.\nZu Beginn jeder Runde stellt [gold]Osty[/gold] 2 HP wieder her.";
        private const string EspText =
            "[gold]Invoca[/gold] {Summon:diff()}.\nCada vez que [gold]Osty[/gold] pierde HP,\ntodos los enemigos pierden esa misma cantidad de HP.\nAl comienzo de cada turno, [gold]Osty[/gold] se cura 2 HP.";
        private const string FraText =
            "[gold]Invoquez[/gold] {Summon:diff()}.\nChaque fois que [gold]Osty[/gold] perd des PV,\ntous les ennemis perdent autant de PV.\nAu début de chaque tour, [gold]Osty[/gold] se soigne de 2 PV.";
        private const string IndText =
            "[gold]Panggil[/gold] {Summon:diff()}.\nSetiap kali [gold]Osty[/gold] kehilangan HP,\nsemua musuh kehilangan HP sebanyak itu.\nDi awal setiap giliran, [gold]Osty[/gold] memulihkan 2 HP.";
        private const string ItaText =
            "[gold]Evoca[/gold] {Summon:diff()}.\nOgni volta che [gold]Osty[/gold] perde HP,\ntutti i nemici perdono la stessa quantità di HP.\nAll'inizio di ogni turno, [gold]Osty[/gold] cura 2 HP.";
        private const string JpnText =
            "[gold]召喚[/gold] {Summon:diff()}。\n[gold]オスティ[/gold]がHPを失うたび、\nすべての敵も同じだけHPを失う。\n毎ターン開始時、[gold]オスティ[/gold]が2 HP回復する。";
        private const string KorText =
            "[gold]소환[/gold] {Summon:diff()}。\n[gold]오스티[/gold]가 HP를 잃을 때마다\n모든 적도 그만큼 HP를 잃습니다.\n매 턴 시작 시 [gold]오스티[/gold]가 2 HP 회복합니다.";
        private const string PolText =
            "[gold]Przyzwij[/gold] {Summon:diff()}.\nZa każdym razem gdy [gold]Osty[/gold] traci HP,\nwszyscy wrogowie tracą tyle samo HP.\nNa początku każdej tury [gold]Osty[/gold] leczy się o 2 HP.";
        private const string PtbText =
            "[gold]Convoque[/gold] {Summon:diff()}.\nSempre que [gold]Osty[/gold] perde HP,\ntodos os inimigos perdem a mesma quantidade de HP.\nNo início de cada turno, [gold]Osty[/gold] recupera 2 HP.";
        private const string RusText =
            "[gold]Призовите[/gold] {Summon:diff()}.\nКаждый раз когда [gold]Ости[/gold] теряет HP,\nвсе враги теряют столько же HP.\nВ начале каждого хода [gold]Ости[/gold] восстанавливает 2 HP.";
        private const string SpaText =
            "[gold]Invoca[/gold] {Summon:diff()}.\nCada vez que [gold]Osty[/gold] pierde HP,\ntodos los enemigos pierden esa misma cantidad de HP.\nAl comienzo de cada turno, [gold]Osty[/gold] se cura 2 HP.";
        private const string ThaText =
            "[gold]อัญเชิญ[/gold] {Summon:diff()}\nเมื่อใดก็ตามที่[gold]ออสตี้[/gold]เสีย HP\nศัตรูทั้งหมดจะเสีย HP เท่านั้น\nเมื่อเริ่มเทิร์น [gold]ออสตี้[/gold]รักษา 2 HP";
        private const string TurText =
            "[gold]Çağır[/gold] {Summon:diff()}.\n[gold]Osty[/gold] her HP kaybettiğinde,\ntüm düşmanlar o kadar HP kaybeder.\nHer turun başında [gold]Osty[/gold] 2 HP iyileşir.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable == "cards" && __instance.LocEntryKey == "NECRO_MASTERY.description")
            {
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
            return true;
        }
    }
}

