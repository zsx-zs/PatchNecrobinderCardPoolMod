# 杀戮尖塔2 Mod：亡灵契约师卡牌调整

使用 Harmony 补丁修改亡灵契约师（Necrobinder）角色的部分卡牌。

## 调整卡牌

### 白卡/基础卡

| 卡牌                       | 修改内容                                                     |
|----------------------------|--------------------------------------------------------------| 
| 雕琢打击 (SculptingStrike) | 稀有度 白卡 → 蓝卡                                           |
| 吸引仇恨 (PullAggro)       | 召唤 4-5 → 召唤 5-6                                          |
| 来生 (Afterlife)           | 召唤 6-9 → 召唤 7-10                                         |
| 播种 (Sow)                 | 伤害 8-11 → 伤害 9-12                                        |
| 鞭打 (Scourge)             | 灾厄 13-16 → 灾厄 12-14，抽牌 1-2 → 抽牌2-2                  |
| 剥夺 (Reave)               | 灵魂-灵魂+ → 灵魂-2张灵魂                                    |
| 恐惧 (Fear)                | 重做卡牌：0费，技能，消耗，给予所有敌人2层易伤，升级：去消耗 |

### 蓝卡

| 卡牌                     | 修改内容                                             |
|--------------------------|------------------------------------------------------|
| 刺破帷幕 (Veilpiercer)   | 稀有度 蓝卡 → 白卡                                   |
| 倒数计时 (Countdown)     | 灾厄 6-9 → 灾厄 9-9，升级：单体→给予所有敌人         |
| 增生 (Spur)              | 召唤 3-5 → 召唤 4-6                                  |
| 死亡使者 (Deathbringer)  | 灾厄 21-26 → 灾厄 19-21，虚弱1-1 → 虚弱1-2           |
| 洁净 (Cleanse)           | 费用 1 → 费用 0，新增虚无关键词，召唤 3-5 → 召唤 3-4 |
| 亡魂牵引 (PullFromBelow) | 伤害 5-7 → 伤害 6-8                                  |
| 致死性 (Lethality)       | 移除特性：“虚无”                                     |
| 摧残 (Debilitate)        | 重做卡牌：3-2费，技能，虚无，获得4点能量             |
| 挽歌 (Dirge)             | 3X-4X → 4X-4(X+1)，X→（X+1），灵魂-灵魂+ → 灵魂-灵魂 |

### 金卡

| 卡牌                    | 修改内容                                                                            |
|-------------------------|-------------------------------------------------------------------------------------|
| 吞噬生命 (DevourLife)   | 召唤 1-2 → 召唤 2-3                                                                 |
| 死者苏生 (Reanimate)    | 召唤 20-25 → 召唤 22-28                                                             | 
| 降灵 (Seance)           | 灵魂-灵魂 → 灵魂+-灵魂+                                                             |
| 亡灵精通 (Necromastery) | 新增效果：奥斯提每回合回复2                                                         |

## 技术说明

- 使用 [Harmony](https://github.com/pardeike/Harmony) 进行运行时补丁
- 基础数值通过 patch `get_CanonicalVars` 修改
- 升级数值通过 patch `OnUpgrade` 修改
- 稀有度通过构造函数 Postfix + 反射修改后备字段
- 费用和关键词通过 patch `CardModel` 基类属性 getter + 实例类型判断

## 行为（OnPlay）的两种改法
OnPlay 是`async` ，编译器会把真正的方法体搬进状态机`<OnPlay>d__N.MoveNext()`,
`OnPlay` 本体只剩一个存根。所以：

手法 A：删/换某一次调用 → Transpiler 打状态机 （Reave 用的）

- 用`AsyncStateMachineAttribute.StateMachineType` 定位状态机（别猜名字）
- 找到那条`call CardCmd.Upgrade` ，把调用目标换成 同签名的空方法 ——栈效果不变，零风险
- 适用：只想去掉/替换某一步，其余逻辑保留

- 手法 B：加逻辑/大改 → Prefix 整体替换 （Seance 用的）

`static bool Prefix(XxxCard __instance, ..., ref Task __result)
{
    __result = MyOwnOnPlay(...);  // 自己写一份
    return false;                  // 原方法不执行
}`

- 前提：用到的成员（`Owner` /`DynamicVars` /`CardScope` ）在你的程序集里可访问——StS2 里它们都是 public，所以成立
- 如果哪天遇到 protected 成员，用`AccessTools.Property(...)` 反射取值（你的 PatchOnUpgrade 就是这么写的）
  怎么选 ：改动越小越适合 transpiler；要插入新步骤、重写流程就用 Prefix 替换。

- `get_ExtraHoverTips` Prefix ：悬停提示的卡面预览，参数传`true/false` 控制显示升级版与否
- 文本拦截 Prefix ：按「表名 + key」精准拦截，按`LocManager.Instance.Language` 分语言返回。
- 占位符（`{Cards:diff()}` 、`plural` ）照抄原版同类型词条的写法

- 属性修改：子类 override 的属性/方法 → patch 子类；没 override → patch CardModel + 类型筛选
- 悬停图标修改：[HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
  public static class PatchPowerModelHoverTips
  {
  static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
  {
  if (__instance is not NecroMasteryPower) return;

        string desc = LocManager.Instance.Language switch
        {
            "eng" => "...", "zhs" => "...", /* 16 语言 */
            _ => "..."
        };

        var tip = new HoverTip(__instance, desc, isSmart: false);
        __result = new List<IHoverTip> { tip };
  }
  }

## 调整遗物

| 遗物 | 修改内容 |
|---|---|
| 不灭印记 (UndyingSigil) | **完全重做**：原版"灾厄层数≥生命时减伤50%"→"每回合首次给予灾厄时获得5点格挡" |

技术实现：
- 原版减伤逻辑已内置到 DoomPower 机制本身（见下），UndyingSigil 不再自己减伤
- 核心逻辑 patch `AbstractModel.AfterPowerAmountChanged` + `is UndyingSigil` filter：
  - 过滤条件 `power is DoomPower && applier == Owner && amount > 0 && 回合未触发过`
  - 用 `ConditionalWeakTable<UndyingSigil, bool>` 存每个实例的 `_usedThisTurn` 状态
  - `BeforeSideTurnStart` / `AfterCombatEnd` 重置标记
- 本地化 patch `"relics"` 表 + `"UNDYING_SIGIL.description"`，16 语言

## 调整机制（Power 层 patch）

### DoomPower 内置减伤
原版 UndyingSigil 的"灾厄层数≥生命时减伤50%"效果变成 DoomPower 机制自带——任何持有 DoomPower 且层数≥生命的生物，其 PoweredAttack 伤害减半。
- patch `AbstractModel.ModifyDamageMultiplicative` + `is DoomPower` filter
- 判定条件 `DoomPower.Amount >= dealer.CurrentHp`

### ImbalancedPower（BowlbugRock 失衡）
原版触发条件：BowlbugRock 攻击完全被玩家格挡（`WasFullyBlocked`）。
**新增条件**：伤害被 DieForYouPower 转移给 Pet 且 Pet 活下来——从玩家视角也算"被挡了"。
- patch `ImbalancedPower.AfterDamageGiven`，`Prefix return false` + 手写 void Postfix + `ref Task __result`
- 判定条件 OR：`WasFullyBlocked || (Receiver.IsPet && Receiver.IsAlive && !WasTargetKilled)`

### SuckPower（FossilStalker 破挡加力量）
原版：跳过 Pet 伤害后只看主人身上 `UnblockedDamage > 0` → 加 Strength。
**盲区**：DieForYouPower 转移后 Owner 和 Pet 结果在不同内层组，Pet 那组没被正确跳过。
**修正**：Pet 活下来时不加力量（从主人视角 = 被挡了）。
- patch `SuckPower.AfterAttack`，同上 `Prefix return false` + void Postfix + `ref Task __result`
- 判定 `petSurvived = petHits.Any(r => IsAlive && !WasTargetKilled)`

## StS2 反编译关键签名速查

### Property / Method 分类表

| 想 patch 什么 | 子类 override？ | patch 目标 |
|---|---|---|
| CardModel 属性（Type、TargetType 等） | 不一定 | **有 override patch 子类，没有 patch CardModel + is filter** |
| RelicModel 属性 | 不一定 | 同上 |
| PowerModel 属性/Combat Hook | 不一定 | 同上 |
| `CanonicalEnergyCost` | Debilitate 没有 | patch CardModel 基类 + is filter |
| `get_ExtraHoverTips` | 可能有 | 子类/基类均可 |
| `DynamicVars` 缓存 | —— | Lazy 初始化 `_dynamicVars`，patch CanonicalVars 后要反射置 null 强制重建 |

### 本地化

| 表名 | 适用 |
|---|---|
| `"cards"` | 卡牌 |
| `"relics"` | 遗物 |
| `"powers"` | Power |
| `"monsters"` | 野怪 |

**语言检测**：用 `Godot.TranslationServer.GetLocale()`（Godot 游戏内语言），**不要用** `CultureInfo.CurrentUICulture`（.NET 运行时值可能与游戏不一致）。

### Format 常用格式器

| 格式器 | 输出 |
|---|---|
| `{Energy:energyIcons()}` | N 个能量图标 |
| `{energyPrefix:energyIcons(1)}` | 1 个能量图标（带单位） |
| `{Amount}` | PowerModel 自动填层数 |
| `{OwnerName}` | Power 持有者名字 |
| `{OnPlayer:player\|enemy}` | 玩家/敌人侧动态显示 |
| `{IfUpgraded:show:+1\|}` | 升级态显示内容 |

### Harmony Task Postfix 坑

原版是 `async Task` 方法 + `Prefix() => false` 跳过原版时：
- ❌ **不能**写 `static Task Postfix(...) { return ...; }` → Harmony 认为是 pass-through 模式但签名不匹配 → HarmonyException
- ✅ **正确写法**：`static void Postfix(..., ref Task __result) { __result = ...; }`

### DevConsole 常用命令

| 命令 | 用途 |
|---|---|
| `fight <EncounterID>` | 跳到指定战斗，如 `FOSSIL_STALKER_NORMAL`、`BOWLBUGS_NORMAL` |
| `power <PowerID> <amount> <target-index>` | 给指定 Creature 加 Power，目标索引 0=玩家，1=第一个敌人 |
| `help` / Tab 补全 | 查命令列表和参数 |