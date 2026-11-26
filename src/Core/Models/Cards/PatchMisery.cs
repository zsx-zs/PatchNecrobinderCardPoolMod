using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchMisery
{
    //耗能：0-1
    [HarmonyPatch(typeof(CardModel), "get_CanonicalEnergyCost")]
    public static class PatchMiseryCost
    {
        static void Postfix(object __instance, ref int __result)
        {
            if (__instance is not Misery) return;
            __result = 1;
        }
    }
    
    //卡牌类型
    [HarmonyPatch(typeof(CardModel), "get_CanonicalKeywords")]
    public static class PatchMiseryKeywords
    {
        static void Postfix(object __instance, ref IEnumerable<CardKeyword> __result)
        {
            if (__instance is not Misery) return;
            __result = new List<CardKeyword> { CardKeyword.Exhaust };
        }
    }
    
    //效果修改：跳过原版 debuff 传递，改为对目标打伤害 + 翻倍其身上的易伤/虚弱/紧追不放层数
    [HarmonyPatch(typeof(Misery), "OnPlay")]
    public static class PatchOnPlay
    {
        static bool Prefix(Misery __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            // 让 OnPlayWrapper 等待我们的自定义逻辑，而不是跳过原版后拿到 null Task
            __result = DoCustomPlay(__instance, choiceContext, cardPlay);
            return false;
        }

        static async Task DoCustomPlay(Misery card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null) return;

            // ① 保留原版的 7 点攻击（含 upgraded +2）
            var damage = card.DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage).FromCard(card, cardPlay).Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // ② 对目标身上的三种 debuff 各自翻倍
            await DoublePower<VulnerablePower>(target, choiceContext, card);
            await DoublePower<WeakPower>(target, choiceContext, card);
            await DoublePower<SicEmPower>(target, choiceContext, card);
        }

        /// <summary>
        /// 找到目标身上类型为 T 的所有 power 实例并翻倍 Amount。
        /// 对普通 Debuff（易伤/虚弱）只会有 1 个实例；对 InstancedPerApplier 的（紧追不放）会把每个 applier 都翻倍。
        /// </summary>
        static async Task DoublePower<T>(Creature target, PlayerChoiceContext choiceContext, CardModel cardSource) where T : PowerModel
        {
            var instances = target.GetPowerInstances<T>().ToList();
            foreach (var power in instances)
            {
                int current = power.Amount;
                if (current <= 0) continue;
                // ModifyAmount 是 add offset — 加一个 current 等于翻倍
                await PowerCmd.ModifyAmount(choiceContext, power, current, power.Applier, cardSource);
            }
        }
    }

    // —— 本地化描述：覆盖 MISERY.description ——
    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        // 参照 MOLTEN_FIST 原版格式：[gold]关键字[/gold]
        private const string EngText = "Deal {Damage:diff()} damage.\nDouble the enemy's [gold]Vulnerable[/gold], [gold]Weak[/gold], and [gold]Sic 'Em[/gold].";
        private const string ZhsText = "造成{Damage:diff()}点伤害。\n将该敌人身上的[gold]易伤[/gold]、[gold]虚弱[/gold]和[gold]紧追不放[/gold]层数翻倍。";
        private const string ZhtText = "造成{Damage:diff()}點傷害。\n將該敵人身上的[gold]易傷[/gold]、[gold]虛弱[/gold]和[gold]緊追不放[/gold]層數翻倍。";
        private const string JpnText = "{Damage:diff()}ダメージを与える。\nその敵の[gold]脆弱[/gold]、[gold]虚弱[/gold]、[gold]シック・エム[/gold]を2倍にする。";
        private const string KorText = "{Damage:diff()} 피해를 가한다.\n그 적의 [gold]취약[/gold], [gold]허약[/gold], [gold]식이테이블[/gold]을 2배로 한다.";
        private const string SpaText = "Inflige {Damage:diff()} de daño.\nDuplica el [gold]Vulnerable[/gold], [gold]Débil[/gold] y [gold]Atacar[/gold] del enemigo.";
        private const string FraText = "Inflige {Damage:diff()} dégâts.\nDouble le [gold]Vulnérable[/gold], [gold]Faible[/gold] et [gold]Attaque[/gold] de l'ennemi.";
        private const string DeuText = "Fügt {Damage:diff()} Schaden zu.\nVerdoppelt [gold]Verwundbar[/gold], [gold]Schwach[/gold] und [gold]Sic Em[/gold] des Gegners.";
        private const string RusText = "Наносит {Damage:diff()} урона.\nУдваивает [gold]Уязвимость[/gold], [gold]Слабость[/gold] и [gold]Крепыш[/gold] врага.";
        private const string ItaText = "Infliggi {Damage:diff()} danni.\nRaddoppia [gold]Vulnerabile[/gold], [gold]Debole[/gold] e [gold]Attacco[/gold] del nemico.";
        private const string PolText = "Zadaje {Damage:diff()} obrażeń.\nPodwaja [gold]Wrażliwość[/gold], [gold]Słabość[/gold] i [gold]Atak[/gold] wroga.";
        private const string PorText = "Inflige {Damage:diff()} de dano.\nDobra o [gold]Vulnerável[/gold], [gold]Fraco[/gold] e [gold]Atacar[/gold] do inimigo.";
        private const string ThaText = "สร้างความเสียหาย {Damage:diff()}.\nทำให้[gold]เปราะ[/gold] [gold]อ่อนแอ[/gold] และ [gold]ล่อให้โจมตี[/gold] ของศัตรูเพิ่มเป็นสองเท่า.";
        private const string TurText = "{Damage:diff()} hasar verir.\nDüşmanın [gold]Hassasiyet[/gold], [gold]Zayıflık[/gold] ve [gold]Sic Em[/gold] değerlerini iki katına çıkarır.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "MISERY.description") return true;
            __result = LocManager.Instance.Language switch
            {
                "eng" => EngText,
                "zhs" => ZhsText,
                "zht" => ZhtText,
                "jpn" => JpnText,
                "kor" => KorText,
                "spa" => SpaText,
                "fra" => FraText,
                "deu" => DeuText,
                "rus" => RusText,
                "ita" => ItaText,
                "pol" => PolText,
                "por" => PorText,
                "tha" => ThaText,
                "tur" => TurText,
                _ => EngText
            };
            return false;
        }
    }
}