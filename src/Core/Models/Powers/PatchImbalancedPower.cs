#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Powers;

// —— ImbalancedPower（BowlbugRock 失衡 Debuff）patch ——
//
// 原版 bug（CreatureCmd.cs line 338）：
//   wasFullyBlocked = !Unblockable && (blockedDamage>0 || originalTarget.Block>0) && uBD==0
//   Wuppy 全挡时 Summoner Block 没被碰（blockedDamage=0）但 Summoner 上回合 Block 有存量
//   → originalTarget.Block>0 导致 wasFullyBlocked=true → 原版误触发。
//   Osty 全挡时 blockedDamage=0 + Summoner.Block=0 → wasFullyBlocked=false → 原版不触发（反了）。
//
// 关键：Osty 和 Wuppy 都是 MonsterModel（IsMonster=true, IsPet=true, IsPlayer=false），
//   只能用 Monster.GetType().Name 精确区分；用 CombatState.Creatures（全集，含 Pets）
//   + Side==Player 过滤玩家侧，查活 Wuppy 在场。
//
// 只处理 Summoner 的 DamageResult（!IsMonster && !IsPet）。
//
// Summoner result 特征 → 场景 → 判断
//   uBD > 0                    → 真受伤        → 不触发
//   B > 0, uBD == 0            → Block 真被碰  → 触发
//   B == 0, uBD == 0 + 活 Wuppy → Wuppy 挡     → 不触发
//   B == 0, uBD == 0 + 无活 Wuppy → Osty 挡   → 触发
[HarmonyPatch(typeof(ImbalancedPower), nameof(ImbalancedPower.AfterDamageGiven))]
public static class PatchImbalancedOsty
{
    static bool Prefix() => false;

    static void Postfix(ImbalancedPower __instance, PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource, ref Task __result)
    {
        if (dealer != __instance.Owner) { __result = Task.CompletedTask; return; }

        // 只处理 Summoner 的 result
        if (result.Receiver.IsPet || result.Receiver.IsMonster) { __result = Task.CompletedTask; return; }

        bool wuppyAlive = result.Receiver.CombatState!.Creatures.Any(c =>
            c.Side == CombatSide.Player && c.Monster?.GetType().Name == "Wuppy" && !c.IsDead);

        // ① Summoner 真受伤 → 不触发
        if (result.UnblockedDamage > 0)
        {
            __result = Task.CompletedTask;
            return;
        }

        // ② Wuppy 活在场 + Summoner Block 没被碰 → Wuppy 挡 → 不触发
        if (wuppyAlive && result.BlockedDamage == 0)
        {
            __result = Task.CompletedTask;
            return;
        }

        // ③ Summoner 没受伤（Block 挡 或 Osty 挡）→ 触发
        Trigger(__instance);
        __result = Task.CompletedTask;
    }

    static void Trigger(ImbalancedPower power)
    {
        var flashMethod = AccessTools.Method(typeof(AbstractModel), "Flash");
        flashMethod?.Invoke(power, null);

        if (power.Owner.Monster is BowlbugRock bowlbugRock)
            bowlbugRock.IsOffBalance = true;
        else
            CreatureCmd.Stun(power.Owner);
    }
}
