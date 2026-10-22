#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace PatchNecrobinderCardPoolMod.Cards;

public static class PatchSeance
{
    // ---------- ① 基础效果：变形目标从 Soul 改为 Soul+ ----------

    [HarmonyPatch(typeof(Seance), "OnPlay")]
    public static class PatchOnPlay
    {
        static bool Prefix(Seance __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            __result = Run(__instance, choiceContext);
            return false;
        }

        static async Task Run(Seance self, PlayerChoiceContext choiceContext)
        {
            // 和原版一样：从抽牌堆选 Cards 张卡
            List<CardModel> selection = (await CardSelectCmd.FromCombatPile(
                    choiceContext,
                    PileType.Draw.GetPile(self.Owner),
                    self.Owner,
                    new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, self.DynamicVars.Cards.IntValue)))
                .ToList();

            await CreatureCmd.TriggerAnim(self.Owner.Creature, "Cast", self.Owner.Character.CastAnimDelay);

            foreach (CardModel item in selection)
            {
                // TransformTo<Soul> 内部就是这两步，展开后在中间插入升级
                CardModel soul = item.CardScope!.CreateCard<Soul>(item.Owner);
                CardCmd.Upgrade(soul);              // 关键改动：先入升级，此时卡不在牌堆，Upgrade 内的分支都是 null 安全
                await CardCmd.Transform(item, soul); // 默认 style 与原版 TransformTo 一致
            }
        }
    }

    // ---------- ② 悬停提示显示升级版 Soul ----------

    [HarmonyPatch(typeof(Seance), "get_ExtraHoverTips")]
    public static class PatchExtraHoverTips
    {
        static bool Prefix(ref IEnumerable<IHoverTip> __result)
        {
            __result = new[] { HoverTipFactory.FromCard<Soul>(true) };
            return false;
        }
    }

    // ---------- ③ 卡面描述：Soul → Soul+（全语言） ----------

    [HarmonyPatch(typeof(LocString), nameof(LocString.GetRawText))]
    public static class PatchDescription
    {
        private const string EngText =
            "Transform a card in your [gold]Draw Pile[/gold] into [gold]Soul+[/gold].";

        private const string ZhsText =
            "将你[gold]抽牌堆[/gold]中的一张牌变化为[gold]灵魂+[/gold]。";

        private const string ZhtText =
            "將你[gold]抽牌堆[/gold]中的一張牌變化為[gold]靈魂+[/gold]。";

        private const string DeuText =
            "Verwandle eine Karte in deinem [gold]Ziehstapel[/gold] in [gold]Seele+[/gold].";

        private const string EspText =
            "Transforma una carta de tu [gold]pila de robo[/gold] en [gold]Alma+[/gold].";

        private const string FraText =
            "Transformez une carte de votre [gold]pioche[/gold] en [gold]Âme+[/gold].";

        private const string IndText =
            "Ubah satu kartu di [gold]tumpukan undi[/gold]mu menjadi [gold]Jiwa+[/gold].";

        private const string ItaText =
            "Trasforma una carta nella tua [gold]pila di pesca[/gold] in [gold]Anima+[/gold].";

        private const string JpnText =
            "あなたの[gold]山札[/gold]のカード1枚を[gold]ソウル+[/gold]に変換する。";

        private const string KorText =
            "[gold]드로우 더미[/gold]의 카드 한 장을 [gold]영혼+[/gold](으)로 변환합니다.";

        private const string PolText =
            "Przekształć kartę ze swojego [gold]stosu dobierania[/gold] w [gold]Duszę+[/gold].";

        private const string PtbText =
            "Transforme uma carta da sua [gold]pilha de compra[/gold] em [gold]Alma+[/gold].";

        private const string RusText =
            "Превратите карту из вашей [gold]стопки добора[/gold] в [gold]душу+[/gold].";

        private const string SpaText =
            "Transforma una carta de tu [gold]pila de robo[/gold] en [gold]Alma+[/gold].";

        private const string ThaText =
            "แปลงการ์ดหนึ่งใบใน[gold]กองจั่ว[/gold]ของคุณให้เป็น[gold]วิญญาณ+[/gold]";

        private const string TurText =
            "[gold]Çekme Destendeki[/gold] bir kartı [gold]Ruh+[/gold]'a dönüştür.";

        static bool Prefix(LocString __instance, ref string __result)
        {
            // 只拦截这一条词条，其他全部放行
            if (__instance.LocTable != "cards" || __instance.LocEntryKey != "SEANCE.description")
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