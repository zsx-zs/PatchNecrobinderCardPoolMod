#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Powers;

// —— 把 UndyingSigil 原版的减伤效果内置到 DoomPower 机制里 ——
// 原版 UndyingSigil: 当敌人（dealer）拥有 DoomPower 且 Amount >= CurrentHp 时，
//                   敌人对玩家造成的 PoweredAttack 伤害降低 50%
// 现在变成: DoomPower 本身自带这个效果，不需要遗物触发
public static class PatchDoomPower
{
    // ==================================================================
    // ① 核心逻辑：减伤（不动）
    // ==================================================================
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageMultiplicative))]
    public static class PatchDoomDamageReduction
    {
        static void Postfix(AbstractModel __instance, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, ref decimal __result)
        {
            if (__instance is not DoomPower doomPower) return;
            var owner = doomPower.Owner;
            if (dealer == null || dealer != owner) return;
            if (!props.IsPoweredAttack()) return;
            if (owner.CurrentHp > doomPower.Amount) return;
            __result *= 0.5m;
        }
    }

    // ==================================================================
    // ② 战斗中 DoomPower 图标悬停 Tooltip
    //    参照 PatchNecroMastery.PatchPowerModelHoverTips
    // ==================================================================
    [HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
    public static class PatchPowerModelHoverTips
    {
        static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance is not DoomPower doomPower) return;

            var amount = doomPower.Amount;
            string desc = LocManager.Instance.Language switch
            {
                // 对齐原版 smartDescription 措辞，加减伤行
                "eng" => $"At the end of this creature's turn, if it has at least as much [gold]Doom[/gold] as HP, it dies.\nCreatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage.",
                "zhs" => $"在该生物的回合结束时，如果其灾厄层数等于或大于其生命，则其直接死亡。\n灾厄层数大于等于生命值的生物造成的伤害降低[blue]50%[/blue]。",
                "zht" => $"在該生物的回合結束時，如果其災厄層數等於或大於其生命，則其直接死亡。\n災厄層數大於等於生命值的生物造成的傷害降低[blue]50%[/blue]。",
                _ => $"At the end of this creature's turn, if it has at least as much [gold]Doom[/gold] as HP, it dies.\nCreatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage."
            };

            var tip = new HoverTip(__instance, desc, isSmart: false);
            __result = new List<IHoverTip> { tip };
        }
    }

    // ==================================================================
    // ③ DoomPower 本地化 — 同时 patch description 和 smartDescription
    //    必须在原版基础上加减伤行，不能重写原版措辞！
    //    smartDescription 里的 {OnPlayer}/{OwnerName}/{Amount} 格式器必须保留！
    // ==================================================================
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "powers") return true;

            // —— description（静态，无变量）——
            if (__instance.LocEntryKey == "DOOM_POWER.description")
            {
                __result = LocManager.Instance.Language switch
                {
                    // 原版: "At the end of the enemy turn, if it has at least as much Doom as HP, it dies."
                    "eng" => "At the end of the enemy turn, if it has at least as much Doom as HP, it dies. Creatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage.",
                    // 原版: "在敌人的回合结束时，如果其灾厄值等于或大于其生命，则其直接死亡。"
                    "zhs" => "在敌人的回合结束时，如果其灾厄值等于或大于其生命，则其直接死亡。灾厄层数大于等于生命值的生物造成的伤害降低[blue]50%[/blue]。",
                    "zht" => "在敵人的回合結束時，如果其災厄值等於或大於其生命，則其直接死亡。災厄層數大於等於生命值的生物造成的傷害降低[blue]50%[/blue]。",
                    // 原版: "Am Ende der Gegnerrunde, wenn sie mindestens so viel Verhängnis wie HP hat, stirbt sie."
                    "deu" => "Am Ende der Gegnerrunde, wenn sie mindestens so viel Verhängnis wie HP hat, stirbt sie. Kreaturen mit mindestens so viel [gold]Verhängnis[/gold] wie HP erleiden [blue]50%[/blue] weniger Schaden.",
                    // 原版: "Al final del turno del enemigo, si tiene al menos tanta Condena como PV, muere."
                    "esp" => "Al final del turno del enemigo, si tiene al menos tanta Condena como PV, muere. Las criaturas con al menos tanta [gold]Condena[/gold] como PV reciben [blue]50%[/blue] menos daño.",
                    // 原版: "À la fin du tour de l'ennemi, s'il a au moins autant de Péril que de PV, il meurt."
                    "fra" => "À la fin du tour de l'ennemi, s'il a au moins autant de Péril que de PV, il meurt. Les créatures avec au moins autant de [gold]Péril[/gold] que de PV infligent [blue]50%[/blue] de dégâts en moins.",
                    // 原版: "Di akhir giliran musuh, jika ia memiliki setidaknya Kekutukan sebanyak HP, ia mati."
                    "ind" => "Di akhir giliran musuh, jika ia memiliki setidaknya Kekutukan sebanyak HP, ia mati. Makhluk dengan setidaknya [gold]Kekutukan[/gold] sebanyak HP memberikan [blue]50%[/blue] lebih sedikit kerusakan.",
                    // 原版: "Alla fine del turno del nemico, se ha almeno tanto Fato quanto HP, muore."
                    "ita" => "Alla fine del turno del nemico, se ha almeno tanto Fato quanto HP, muore. Le creature con almeno tanto [gold]Fato[/gold] quanto HP infliggono [blue]50%[/blue] di danni in meno.",
                    // 原版: "敵のターン終了時、HPと同じ量以上の運命を持っていれば、死亡する。"
                    "jpn" => "敵のターン終了時、HPと同じ量以上の運命を持っていれば、死亡する。HP以上の[gold]運命[/gold]を持つ生物は、受けるダメージが[blue]50%[/blue]減少する。",
                    // 原版: "적의 턴 종료 시, HP 이상의 파멸을 가지고 있으면 사망합니다."
                    "kor" => "적의 턴 종료 시, HP 이상의 파멸을 가지고 있으면 사망합니다. HP 이상의 [gold]파멸[/gold]을 가진 생명체는 받는 피해가 [blue]50%[/blue] 감소합니다.",
                    // 原版: "Na koniec tury wroga, jeśli ma co najmniej tyle Przeznaczenie co HP, ginie."
                    "pol" => "Na koniec tury wroga, jeśli ma co najmniej tyle Przeznaczenie co HP, ginie. Stworzenia z co najmniej takim samym [gold]Przeznaczenie[/gold] co HP zadają o [blue]50%[/blue] mniej obrażeń.",
                    // 原版: "No final do turno do inimigo, se tiver pelo menos tanta Condenação quanto PV, morre."
                    "ptb" => "No final do turno do inimigo, se tiver pelo menos tanta Condenação quanto PV, morre. Criaturas com pelo menos tanta [gold]Condenação[/gold] quanto PV causam [blue]50%[/blue] menos dano.",
                    // 原版: "В конце хода врага, если у него не меньше Рок чем HP, он умирает."
                    "rus" => "В конце хода врага, если у него не меньше Рок чем HP, он умирает. Существа с количеством [gold]Рок[/gold] не меньшим HP получают [blue]50%[/blue] меньше урона.",
                    // 原版: "เมื่อสิ้นสุดเทิร์นศัตรู หากมีกรรมถึงวาระมากกว่าหรือเท่ากับ HP มันจะตาย"
                    "tha" => "เมื่อสิ้นสุดเทิร์นศัตรู หากมีกรรมถึงวาระมากกว่าหรือเท่ากับ HP มันจะตาย สิ่งมีชีวิตที่มี[gold]กรรมถึงวาระ[/gold]มากกว่าหรือเท่ากับ HP จะได้รับความเสียหายน้อยลง[blue]50%[/blue]",
                    // 原版: "Düşmanın turunun sonunda, HP kadar veya daha fazla Kıyamet varsa ölür."
                    "tur" => "Düşmanın turunun sonunda, HP kadar veya daha fazla Kıyamet varsa ölür. HP kadar veya daha fazla [gold]Kıyamet[/gold] olan yaratıklar [blue]50%[/blue] daha az hasar verirler.",
                    _ => "At the end of the enemy turn, if it has at least as much Doom as HP, it dies. Creatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage."
                };
                return false;
            }

            // —— smartDescription（动态，有 {OnPlayer}/{OwnerName}/{Amount}）——
            if (__instance.LocEntryKey == "DOOM_POWER.smartDescription")
            {
                __result = LocManager.Instance.Language switch
                {
                    // 原版: "At the end of the {OnPlayer:player|enemy} turn, if [gold]{OwnerName}[/gold] has [blue]{Amount}[/blue] or less HP, it dies."
                    "eng" => "At the end of the {OnPlayer:player|enemy} turn, if [gold]{OwnerName}[/gold] has [blue]{Amount}[/blue] or less HP, it dies. Creatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage.",
                    // 原版: "在{OnPlayer:玩家|敌人}的回合结束时，如果[gold]{OwnerName}[/gold]的生命值不高于[blue]{Amount}[/blue]，则[gold]{OwnerName}[/gold]立即死亡。"
                    "zhs" => "在{OnPlayer:玩家|敌人}的回合结束时，如果[gold]{OwnerName}[/gold]的生命值不高于[blue]{Amount}[/blue]，则[gold]{OwnerName}[/gold]立即死亡。灾厄层数大于等于生命值的生物造成的伤害降低[blue]50%[/blue]。",
                    "zht" => "在{OnPlayer:玩家|敵人}的回合結束時，如果[gold]{OwnerName}[/gold]的生命值不高於[blue]{Amount}[/blue]，則[gold]{OwnerName}[/gold]立即死亡。災厄層數大於等於生命值的生物造成的傷害降低[blue]50%[/blue]。",
                    // 原版: "Am Ende des {OnPlayer:Spieler|Gegner}zugs, wenn [gold]{OwnerName}[/gold] [blue]{Amount}[/blue] oder weniger HP hat, stirbt sie."
                    "deu" => "Am Ende des {OnPlayer:Spieler|Gegner}zugs, wenn [gold]{OwnerName}[/gold] [blue]{Amount}[/blue] oder weniger HP hat, stirbt sie. Kreaturen mit mindestens so viel [gold]Verhängnis[/gold] wie HP erleiden [blue]50%[/blue] weniger Schaden.",
                    // 原版: "Al final del turno del {OnPlayer:jugador|enemigo}, si [gold]{OwnerName}[/gold] tiene [blue]{Amount}[/blue] PV o menos, muere."
                    "esp" => "Al final del turno del {OnPlayer:jugador|enemigo}, si [gold]{OwnerName}[/gold] tiene [blue]{Amount}[/blue] PV o menos, muere. Las criaturas con al menos tanta [gold]Condena[/gold] como PV reciben [blue]50%[/blue] menos daño.",
                    // 原版: "À la fin du tour du {OnPlayer:joueur|ennemi}, si [gold]{OwnerName}[/gold] a [blue]{Amount}[/blue] PV ou moins, elle meurt."
                    "fra" => "À la fin du tour du {OnPlayer:joueur|ennemi}, si [gold]{OwnerName}[/gold] a [blue]{Amount}[/blue] PV ou moins, elle meurt. Les créatures avec au moins autant de [gold]Péril[/gold] que de PV infligent [blue]50%[/blue] de dégâts en moins.",
                    // 原版: "Di akhir giliran {OnPlayer:pemain|musuh}, jika [gold]{OwnerName}[/gold] memiliki [blue]{Amount}[/blue] HP atau kurang, ia mati."
                    "ind" => "Di akhir giliran {OnPlayer:pemain|musuh}, jika [gold]{OwnerName}[/gold] memiliki [blue]{Amount}[/blue] HP atau kurang, ia mati. Makhluk dengan setidaknya [gold]Kekutukan[/gold] sebanyak HP memberikan [blue]50%[/blue] lebih sedikit kerusakan.",
                    // 原版: "Alla fine del turno del {OnPlayer:giocatore|nemico}, se [gold]{OwnerName}[/gold] ha [blue]{Amount}[/blue] HP o meno, muore."
                    "ita" => "Alla fine del turno del {OnPlayer:giocatore|nemico}, se [gold]{OwnerName}[/gold] ha [blue]{Amount}[/blue] HP o meno, muore. Le creature con almeno tanto [gold]Fato[/gold] quanto HP infliggono [blue]50%[/blue] di danni in meno.",
                    // 原版: "{OnPlayer:プレイヤー|敵}のターン終了時、[gold]{OwnerName}[/gold]のHPが[blue]{Amount}[/blue]以下なら、死亡する。"
                    "jpn" => "{OnPlayer:プレイヤー|敵}のターン終了時、[gold]{OwnerName}[/gold]のHPが[blue]{Amount}[/blue]以下なら、死亡する。HP以上の[gold]運命[/gold]を持つ生物は、受けるダメージが[blue]50%[/blue]減少する。",
                    // 原版: "{OnPlayer:플레이어|적}의 턴 종료 시, [gold]{OwnerName}[/gold]의 HP가 [blue]{Amount}[/blue] 이하이면 사망합니다."
                    "kor" => "{OnPlayer:플레이어|적}의 턴 종료 시, [gold]{OwnerName}[/gold]의 HP가 [blue]{Amount}[/blue] 이하이면 사망합니다. HP 이상의 [gold]파멸[/gold]을 가진 생명체는 받는 피해가 [blue]50%[/blue] 감소합니다.",
                    // 原版: "Na koniec tury {OnPlayer:gracz|wroga}, jeśli [gold]{OwnerName}[/gold] ma [blue]{Amount}[/blue] HP lub mniej, ginie."
                    "pol" => "Na koniec tury {OnPlayer:gracz|wroga}, jeśli [gold]{OwnerName}[/gold] ma [blue]{Amount}[/blue] HP lub mniej, ginie. Stworzenia z co najmniej takim samym [gold]Przeznaczenie[/gold] co HP zadają o [blue]50%[/blue] mniej obrażeń.",
                    // 原版: "No final do turno do {OnPlayer:jogador|inimigo}, se [gold]{OwnerName}[/gold] tiver [blue]{Amount}[/blue] PV ou menos, morre."
                    "ptb" => "No final do turno do {OnPlayer:jogador|inimigo}, se [gold]{OwnerName}[/gold] tiver [blue]{Amount}[/blue] PV ou menos, morre. Criaturas com pelo menos tanta [gold]Condenação[/gold] quanto PV causam [blue]50%[/blue] menos dano.",
                    // 原版: "В конце хода {OnPlayer:игрока|врага}, если у [gold]{OwnerName}[/gold] HP [blue]{Amount}[/blue] или меньше, он умирает."
                    "rus" => "В конце хода {OnPlayer:игрока|врага}, если у [gold]{OwnerName}[/gold] HP [blue]{Amount}[/blue] или меньше, он умирает. Существа с количеством [gold]Рок[/gold] не меньшим HP получают [blue]50%[/blue] меньше урона.",
                    // 原版: "เมื่อสิ้นสุดเทิร์น{OnPlayer:ผู้เล่น|ศัตรู} หาก[gold]{OwnerName}[/gold]มีHPไม่เกิน[blue]{Amount}[/blue] มันจะตาย"
                    "tha" => "เมื่อสิ้นสุดเทิร์น{OnPlayer:ผู้เล่น|ศัตรู} หาก[gold]{OwnerName}[/gold]มีHPไม่เกิน[blue]{Amount}[/blue] มันจะตาย สิ่งมีชีวิตที่มี[gold]กรรมถึงวาระ[/gold]มากกว่าหรือเท่ากับ HP จะได้รับความเสียหายน้อยลง[blue]50%[/blue]",
                    // 原版: "{OnPlayer:oyuncu|düşman} turunun sonunda, [gold]{OwnerName}[/gold]'nin HP'si [blue]{Amount}[/blue] veya daha azsa ölür."
                    "tur" => "{OnPlayer:oyuncu|düşman} turunun sonunda, [gold]{OwnerName}[/gold]'nin HP'si [blue]{Amount}[/blue] veya daha azsa ölür. HP kadar veya daha fazla [gold]Kıyamet[/gold] olan yaratıklar [blue]50%[/blue] daha az hasar verirler.",
                    _ => "At the end of the {OnPlayer:player|enemy} turn, if [gold]{OwnerName}[/gold] has [blue]{Amount}[/blue] or less HP, it dies. Creatures with at least as much [gold]Doom[/gold] as HP deal [blue]50%[/blue] less damage."
                };
                return false;
            }

            return true;
        }
    }
}
