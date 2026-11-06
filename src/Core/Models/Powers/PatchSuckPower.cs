using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Powers;

// —— SuckPower（FossilStalker 破挡加力量 Buff）patch ——
// 修改对象：化石追踪者
//
// 核心逻辑：Summoner 的 DamageResult.UnblockedDamage 就是它实际掉的 HP
//   Pet.HP ≥ 转给 Pet 的溢出伤害 → Pet.OverkillDamage = 0 → Summoner LoseHpInternal(0) → UnblockedDamage = 0
//   Pet.HP < 转给 Pet 的溢出伤害 → Pet.OverkillDamage > 0 → Summoner LoseHpInternal(Overkill) → UnblockedDamage > 0
// 所以 Summoner.UnblockedDamage == 0 涵盖 Pet 全挡的所有情况（Pet 活 / Pet 刚好死临界态）
[HarmonyPatch(typeof(SuckPower), nameof(SuckPower.AfterAttack))]
public static class PatchSuckPowerOsty
{
    static bool Prefix() => false; // 跳过原版 async Task 方法体

    static void Postfix(SuckPower __instance, PlayerChoiceContext choiceContext, AttackCommand command, ref Task __result)
    {
        if (command.Attacker != __instance.Owner || command.TargetSide == __instance.Owner.Side || !command.DamageProps.IsPoweredAttack())
        {
            __result = Task.CompletedTask;
            return;
        }

        var allResults = command.Results.ToList();
        int triggerCount = 0;

        foreach (var hitGroup in allResults)
        {
            var petHits = hitGroup.Where(r => r.Receiver.IsPet).ToList();

            // ===== 在移除 Summoner result 之前，先检查 Summoner 的 UnblockedDamage =====
            // Summoner result = PetOwner 的那条 result（CreatureCmd 第 304-310 行那条）
            var summonerResults = petHits
                .Select(petHit => hitGroup.FirstOrDefault(r => r.Receiver == petHit.Receiver.PetOwner?.Creature))
                .Where(r => r != null).Cast<DamageResult>().ToList();

            // 从 Summoner 视角：有 Pet 挡 && 自己没掉血 → 不算破挡
            // 涵盖场景：Pet 活（全挡）/ Pet 刚好死（无 Overkill 回 Summoner）/ Block 全挡
            // 不算挡的场景：Pet 吃不下 Overkill 回 Summoner → Summoner.UnblockedDamage > 0
            bool petFullyBlocked = summonerResults.Any() && summonerResults.All(s => s.UnblockedDamage == 0);

            // ===== 原版逻辑：移除 Summoner result，避免 Summoner 的 BlockedDamage 干扰 =====
            foreach (var petHit in petHits)
                hitGroup.RemoveAll(r => r.Receiver == petHit.Receiver.PetOwner?.Creature);

            if (!petFullyBlocked && hitGroup.Any(r => r.UnblockedDamage > 0))
                triggerCount++;
        }

        if (triggerCount == 0) { __result = Task.CompletedTask; return; }

        // 触发加力量
        var flashMethod = AccessTools.Method(typeof(PowerModel), "Flash");
        flashMethod?.Invoke(__instance, null);

        __result = PowerCmd.Apply<StrengthPower>(choiceContext, __instance.Owner, __instance.Amount * triggerCount, __instance.Owner, null);
    }
}
