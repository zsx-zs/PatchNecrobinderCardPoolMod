# 杀戮尖塔2 Mod：亡灵契约师卡牌调整

使用 Harmony 补丁修改亡灵契约师（Necrobinder）角色的部分卡牌。

## 调整卡牌

### 白卡/基础卡

| 卡牌 | 修改内容 |
|------|----------|
| 护卫 (Bodyguard) | 召唤 5-7 → 召唤 6-8 |
| 雕琢打击 (SculptingStrike) | 稀有度 白卡 → 蓝卡 |
| 吸引仇恨 (PullAggro) | 召唤 4-5 → 召唤 5-6 |
| 来生 (Afterlife) | 召唤 6-9 → 召唤 7-10 |
| 播种 (Sow) | 伤害 8-11 → 伤害 9-12 |
| 鞭打 (Scourge) | 灾厄 13-16 → 灾厄 12-14，抽牌 1-2 → 抽牌2-2|

### 蓝卡

| 卡牌 | 修改内容 |
|------|----------|
| 刺破帷幕 (Veilpiercer) | 稀有度 蓝卡 → 白卡 |
| 倒数计时 (Countdown) | 灾厄 6-9 → 灾厄 8-10 |
| 增生 (Spur) |  召唤 3-5 → 召唤 4-6 |
| 死亡使者 (Deathbringer) | 灾厄 21-26 → 灾厄 17-20，虚弱1-1 → 虚弱1-2 |
| 净化 (Cleanse) | 费用 1 → 费用 0，新增虚无关键词，召唤 3-5 → 召唤 3-4 |
| 亡魂牵引 (PullFromBelow) | 伤害 5-7 → 伤害 7-8，稀有度 蓝卡 → 金卡 |

### 金卡

| 卡牌 | 修改内容 |
|------|----------|
| 吞噬生命 (DevourLife) | 召唤 1-2 → 召唤 2-3，稀有度 金卡 → 蓝卡 |
| 复苏 (Reanimate) | 召唤 20-25 → 召唤 22-28 |
| 根除 (Eradicate) | 伤害 11-14 → 伤害 10-13 |

## 技术说明

- 使用 [Harmony](https://github.com/pardeike/Harmony) 进行运行时补丁
- 基础数值通过 patch `get_CanonicalVars` 修改
- 升级数值通过 patch `OnUpgrade` 修改
- 稀有度通过构造函数 Postfix + 反射修改后备字段
- 费用和关键词通过 patch `CardModel` 基类属性 getter + 实例类型判断
