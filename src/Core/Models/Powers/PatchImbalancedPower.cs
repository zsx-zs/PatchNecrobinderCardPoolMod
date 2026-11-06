#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Powers;

// —— ImbalancedPower（BowlbugRock 失衡 Debuff）patch ——
// 修改对象：盛碗虫（石）安父
[HarmonyPatch(typeof(ImbalancedPower), nameof(ImbalancedPower.AfterDamageGiven))]
public static class PatchImbalancedOsty
{
    // 跳过原版 async Task 方法体，自己重写（保留原版逻辑 + 加 Pet 挡条件）
    static bool Prefix() => false;

    static void Postfix(ImbalancedPower __instance, PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource, ref Task __result)
    {
        if (dealer != __instance.Owner) { __result = Task.CompletedTask; return; }

        // Pet result 进来直接跳过（它的 WasFullyBlocked 没被赋值，判断无意义）
        // Summoner result 才是我们需要的
        if (result.Receiver.IsPet) { __result = Task.CompletedTask; return; }

        // 核心：Summoner result.UnblockedDamage == 0 涵盖所有被挡情况
        //   无 Pet 场景：Block 全挡 → UnblockedDamage = 0
        //   有 Pet 场景：Pet 全吃（活/刚好死）→ Overkill = 0 → UnblockedDamage = 0
        //   有 Pet 但吃不下：Overkill 回 Summoner → UnblockedDamage > 0 → 不该触发
        bool shouldTrigger = result.UnblockedDamage == 0;

        if (!shouldTrigger) { __result = Task.CompletedTask; return; }

        // 触发失衡
        var flashMethod = AccessTools.Method(typeof(AbstractModel), "Flash");
        flashMethod?.Invoke(__instance, null);

        if (__instance.Owner.Monster is BowlbugRock bowlbugRock)
        {
            bowlbugRock.IsOffBalance = true;
            __result = Task.CompletedTask;
        }
        else
        {
            __result = CreatureCmd.Stun(__instance.Owner);
        }
    }
}
