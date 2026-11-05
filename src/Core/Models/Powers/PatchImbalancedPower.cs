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
    // 跳过原版 async Task 方法体，自己重写（保留原版逻辑 + 加 Pet 条件）
    static bool Prefix() => false;

    static void Postfix(ImbalancedPower __instance, PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource, ref Task __result)
    {
        bool shouldTrigger = dealer == __instance.Owner && (
            result.WasFullyBlocked ||
            (result.Receiver != null && result.Receiver.IsPet && result.Receiver.IsAlive && !result.WasTargetKilled)
        );

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
