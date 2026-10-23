#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchBansheesCry
{
    // ---------- 稀有度 Rare → Uncommon ----------

    [HarmonyPatch(typeof(BansheesCry), MethodType.Constructor)]
    public static class PatchConstructor
    {
        static void Postfix(object __instance)
        {
            var field = AccessTools.Field(__instance.GetType(), "<Rarity>k__BackingField");
            field?.SetValue(__instance, CardRarity.Uncommon);
        }
    }

    // ---------- 基础费用固定为 3，升级不变 ----------

    [HarmonyPatch(typeof(CardModel), "get_CanonicalEnergyCost")]
    public static class PatchBansheesCryCost
    {
        static void Postfix(object __instance, ref int __result)
        {
            if (__instance is not BansheesCry) return;
            __result = 3;
        }
    }

    // ---------- 基础伤害 15，升级后 18 ----------

    [HarmonyPatch(typeof(BansheesCry), "get_CanonicalVars")]
    public static class PatchCanonicalVars
    {
        static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            var vars = new List<DynamicVar>(__result);
            for (int i = 0; i < vars.Count; i++)
            {
                if (vars[i] is DamageVar)
                    vars[i] = new DamageVar(15m, ValueProp.Move);
                else if (vars[i] is EnergyVar)
                    vars[i] = new EnergyVar(1);
            }
            __result = vars.AsReadOnly();
        }
    }

    // ---------- OnUpgrade：只给伤害 +3，不再减费 ----------

    [HarmonyPatch(typeof(BansheesCry), "OnUpgrade")]
    public static class PatchOnUpgrade
    {
        static bool Prefix(object __instance)
        {
            var type = __instance.GetType();
            PropertyInfo? prop = null;
            while (type != null)
            {
                prop = type.GetProperty("DynamicVars", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null) break;
                type = type.BaseType;
            }
            var dynamicVars = prop!.GetValue(__instance);

            var damage = dynamicVars!.GetType().GetProperty("Damage")!.GetValue(dynamicVars);
            damage!.GetType().GetMethod("UpgradeValueBy")!.Invoke(damage, new object[] { 3m });

            return false;
        }
    }

    // ---------- 关键：费用减免「本场战斗」→「本回合」 ----------
    // 原版用 AddThisCombat + History.Count（整场）
    // 本回合版照 Pinpoint 模板：AddThisTurn + History.Count + HappenedThisTurn 过滤

    [HarmonyPatch(typeof(BansheesCry), "AfterCardEnteredCombat")]
    public static class PatchAfterCardEnteredCombat
    {
        static bool Prefix(BansheesCry __instance, CardModel card, ref Task __result)
        {
            // 克隆卡和非自己的卡直接跳过（保留原版逻辑）
            if (card != __instance || __instance.IsClone)
            {
                __result = Task.CompletedTask;
                return false;
            }

            // 只统计本回合打出的虚无牌
            int count = CombatManager.Instance.History.CardPlaysFinished
                .Count(e => e.WasEthereal
                        && e.CardPlay.Player == __instance.Owner
                        && e.HappenedThisTurn(__instance.CombatState!));

            __instance.EnergyCost.AddThisTurn(-count * __instance.DynamicVars.Energy.IntValue);
            __result = Task.CompletedTask;
            return false;
        }
    }

    [HarmonyPatch(typeof(BansheesCry), "AfterCardPlayed")]
    public static class PatchAfterCardPlayed
    {
        static bool Prefix(BansheesCry __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            // 不是自己打出的，跳过
            if (cardPlay.Card.Owner != __instance.Owner)
            {
                __result = Task.CompletedTask;
                return false;
            }
            // 不是虚无牌，跳过
            if (!cardPlay.Card.Keywords.Contains(CardKeyword.Ethereal))
            {
                __result = Task.CompletedTask;
                return false;
            }

            // 关键：AddThisCombat → AddThisTurn
            __instance.EnergyCost.AddThisTurn(-__instance.DynamicVars.Energy.IntValue);
            __result = Task.CompletedTask;
            return false;
        }
    }

    // ---------- 本地化：描述文本里「本场战斗」→「本回合」（全语言） ----------

    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        private const string EngText =
            "Deal {Damage:diff()} damage to ALL enemies.\nCosts {Energy:energyIcons()} less for each [gold]Ethereal[/gold] card played this turn.";

        private const string ZhsText =
            "对所有敌人造成{Damage:diff()}点伤害。\n本回合中每打出过一张[gold]虚无[/gold]牌，此牌的耗能就减少{Energy:energyIcons()}。";

        private const string ZhtText =
            "對所有敵人造成{Damage:diff()}點傷害。\n本回合中每打出過一張[gold]虛無[/gold]牌，此牌的耗能就減少{Energy:energyIcons()}。";

        private const string DeuText =
            "Verursache {Damage:diff()} Schaden an ALLE Gegner.\nKostet {Energy:energyIcons()} weniger für jede in diesem Zug gespielte [gold]Ätherisch[/gold]-Karte.";

        private const string EspText =
            "Inflige {Damage:diff()} de daño a TODOS los enemigos.\nCuesta {Energy:energyIcons()} menos por cada carta [gold]Etérea[/gold] jugada este turno.";

        private const string FraText =
            "Inflige {Damage:diff()} dégâts à TOUS les ennemis.\nCoûte {Energy:energyIcons()} de moins pour chaque carte [gold]Éthérée[/gold] jouée ce tour.";

        private const string IndText =
            "Berikan {Damage:diff()} kerusakan kepada SEMUA musuh.\nBiayanya berkurang {Energy:energyIcons()} untuk setiap kartu [gold]Etereal[/gold] yang dimainkan giliran ini.";

        private const string ItaText =
            "Infliggi {Damage:diff()} danni a TUTTI i nemici.\nCosta {Energy:energyIcons()} in meno per ogni carta [gold]Eterea[/gold] giocata in questo turno.";

        private const string JpnText =
            "すべての敵に{Damage:diff()}ダメージを与える。\nこのターンにプレイされた[gold]虚無[/gold]カードごとに、コストが{Energy:energyIcons()}減少する。";

        private const string KorText =
            "모든 적에게 {Damage:diff()} 피해를 줍니다.\n이번 턴에 플레이된 [gold]허무[/gold] 카드마다 비용이 {Energy:energyIcons()} 감소합니다.";

        private const string PolText =
            "Zadaj {Damage:diff()} obrażeń WSZYSTKIM wrogom.\nKosztuje o {Energy:energyIcons()} mniej za każdą zagrzną w tej turze kartę [gold]Eteryczną[/gold].";

        private const string PtbText =
            "Causa {Damage:diff()} de dano a TODOS os inimigos.\nCusta {Energy:energyIcons()} menos por cada carta [gold]Etereal[/gold] jogada neste turno.";

        private const string RusText =
            "Нанесите {Damage:diff()} урона ВСЕМ врагам.\nСтоимость снижается на {Energy:energyIcons()} за каждую карту [gold]Эфирная[/gold}, сыгранную в этот ход.";

        private const string SpaText =
            "Inflige {Damage:diff()} de daño a TODOS los enemigos.\nCuesta {Energy:energyIcons()} menos por cada carta [gold]Etérea[/gold] jugada este turno.";

        private const string ThaText =
            "สร้างความเสียหาย {Damage:diff()} แก่ศัตรูทั้งหมด\nลดค่าใช้จ่ายลง {Energy:energyIcons()} สำหรับการ์ด [gold]สูญญ์[/gold] ทุกใบที่เล่นในเทิร์นนี้";

        private const string TurText =
            "TÜM düşmanlara {Damage:diff()} hasar ver.\nBu turda oynanan her [gold]Eterik[/gold] kartı için maliyet {Energy:energyIcons()} azalır.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "BANSHEES_CRY.description")
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
                _ => EngText
            };
            return false;
        }
    }
}