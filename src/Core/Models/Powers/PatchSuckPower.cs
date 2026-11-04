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
//修改对象：化石追踪者
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
            foreach (var petHit in petHits)
                hitGroup.RemoveAll(r => r.Receiver == petHit.Receiver.PetOwner?.Creature);

            // Pet 活下来 = 从主人视角也是"被挡了"，不算破挡
            bool petSurvived = petHits.Any(r => r.Receiver.IsAlive && !r.WasTargetKilled);

            if (!petSurvived && hitGroup.Any(r => r.UnblockedDamage > 0))
                triggerCount++;
        }

        if (triggerCount == 0) { __result = Task.CompletedTask; return; }

        // 触发加力量
        var flashMethod = AccessTools.Method(typeof(PowerModel), "Flash");
        flashMethod?.Invoke(__instance, null);

        __result = PowerCmd.Apply<StrengthPower>(choiceContext, __instance.Owner, __instance.Amount * triggerCount, __instance.Owner, null);
    }
}
