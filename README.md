# 杀戮尖塔2 Mod：亡灵契约师卡牌调整

使用 Harmony 补丁修改亡灵契约师（Necrobinder）角色的部分卡牌。

## 调整卡牌

### 白卡/基础卡

| 卡牌                       | 修改内容                                    |
|----------------------------|---------------------------------------------|
| 护卫 (Bodyguard)           | 召唤 5-7 → 召唤 6-8                         |
| 雕琢打击 (SculptingStrike) | 稀有度 白卡 → 蓝卡                          |
| 吸引仇恨 (PullAggro)       | 召唤 4-5 → 召唤 5-6                         |
| 来生 (Afterlife)           | 召唤 6-9 → 召唤 7-10                        |
| 播种 (Sow)                 | 伤害 8-11 → 伤害 9-12                       |
| 鞭打 (Scourge)             | 灾厄 13-16 → 灾厄 12-14，抽牌 1-2 → 抽牌2-2 |
| 剥夺 (Reave)               | 灵魂-灵魂+ → 灵魂-2张灵魂                   |

### 蓝卡

| 卡牌                     | 修改内容                                             |
|--------------------------|------------------------------------------------------|
| 刺破帷幕 (Veilpiercer)   | 稀有度 蓝卡 → 白卡                                   |
| 倒数计时 (Countdown)     | 灾厄 6-9 → 灾厄 8-10                                 |
| 增生 (Spur)              | 召唤 3-5 → 召唤 4-6                                  |
| 死亡使者 (Deathbringer)  | 灾厄 21-26 → 灾厄 17-21，虚弱1-1 → 虚弱1-2           |
| 洁净 (Cleanse)           | 费用 1 → 费用 0，新增虚无关键词，召唤 3-5 → 召唤 3-4 |
| 亡魂牵引 (PullFromBelow) | 伤害 5-7 → 伤害 7-8，稀有度 蓝卡 → 金卡              |
| 致死性 (Lethality)       | 移除特性：“虚无”                                     |

### 金卡

| 卡牌                   | 修改内容                                                                  |
|------------------------|---------------------------------------------------------------------------|
| 吞噬生命 (DevourLife)  | 召唤 1-2 → 召唤 2-3，稀有度 金卡 → 蓝卡                                   |
| 复苏 (Reanimate)       | 召唤 20-25 → 召唤 22-28                                                   |
| 根除 (Eradicate)       | 伤害 11-14 → 伤害 10-13                                                   |
| 降灵 (Seance)          | 灵魂-灵魂 → 灵魂+-灵魂+                                                   |
| 女妖之嚎 (BansheesCry) | 费用9-7 → 3-3，稀有度 金卡 → 蓝卡，伤害 33-33→ 伤害15-18，本场战斗→本回合 |

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