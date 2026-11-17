#nullable enable
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace PatchNecrobinderCardPoolMod.Powers;

// —— SuckPower（FossilStalker 破挡加力量 Buff）patch ——
//
// 原版 bug：
//   1. 只移除 PetOwner(Summoner) 的 result，不移除 Pet 自己的 → Osty.uBD>0 留在 hitGroup 里照样触发。
//   2. Osty 和 Wuppy 都是 IsPet=true + IsMonster=true，原版根本区分不了。
//
// 区分方式：Monster.GetType().Name — "Wuppy" = Wuppy, "Osty" = Osty, null + !IsPet = Summoner。
//
// Summoner 最终没受伤 → 不加；Wuppy 全挡是特殊情况强制视为"没挡住"→加。
[HarmonyPatch(typeof(SuckPower), nameof(SuckPower.AfterAttack))]
public static class PatchSuckPowerOsty
{
    static bool Prefix() => false;

    static void Postfix(SuckPower __instance, PlayerChoiceContext choiceContext, AttackCommand command, ref Task __result)
    {
        if (command.Attacker != __instance.Owner || command.TargetSide == __instance.Owner.Side || !command.DamageProps.IsPoweredAttack())
        {
            __result = Task.CompletedTask;
            return;
        }

        int triggerCount = 0;

        foreach (var hitGroup in command.Results)
        {
            bool hasWuppy    = hitGroup.Any(r => r.Receiver.Monster?.GetType().Name == "Wuppy");
            bool hasSummoner = hitGroup.Any(r => r.Receiver.Monster == null && !r.Receiver.IsPet);

            if (hasWuppy)
            {
                if (!hasSummoner)
                {
                    // Wuppy 单体挡：Summoner 被从 targetData 换掉 → 强制视为没挡住 → 触发
                    triggerCount++;
                    continue;
                }
                // Wuppy AOE 挡：看 Summoner Block 有没有被碰
                var s = hitGroup.First(r => r.Receiver.Monster == null && !r.Receiver.IsPet);
                if (s.BlockedDamage == 0 && s.UnblockedDamage == 0)
                    triggerCount++;
                // 否则：Summoner Block 挡了溢出 或 Summoner 真受伤 → 不触发
            }
            else if (hasSummoner)
            {
                var s = hitGroup.First(r => r.Receiver.Monster == null && !r.Receiver.IsPet);
                if (s.UnblockedDamage > 0)
                    triggerCount++;
                // 否则：Summoner 没受伤（Osty 挡 / Summoner Block 挡）→ 不触发
            }
        }

        if (triggerCount == 0) { __result = Task.CompletedTask; return; }

        var flashMethod = AccessTools.Method(typeof(PowerModel), "Flash");
        flashMethod?.Invoke(__instance, null);

        __result = PowerCmd.Apply<StrengthPower>(choiceContext, __instance.Owner, __instance.Amount * triggerCount, __instance.Owner, null);
    }
}
