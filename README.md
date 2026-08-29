# SteelRoar · 钢铁咆哮

![Unity](https://img.shields.io/badge/Unity-2022.3.62f2c1-blue) ![C#](https://img.shields.io/badge/C%23-9.0-green) ![License](https://img.shields.io/badge/License-MIT-yellow)

> **肉鸽坦克生存战** —— 波次生存 + Buff构筑三选一 + 元素弹种 + 三阶段Boss 的 3D 坦克射击游戏。
> 从教程项目逐步改造为个人独立开发项目：自建轻量级框架、有限状态机AI、双配置驱动、异或加密存档。

<!-- TODO(演示GIF)：此处插入战斗演示GIF（录屏工具：Win+Alt+R 或 OBS），内容含：波次刷怪、Buff三选一、燃烧弹DoT、Boss弹幕 -->

## 游戏玩法

驾驶坦克在无尽波次中生存：击毁敌方坦克获取分数，每波结束从随机三张**强化卡**中三选一构筑流派（攻击/移速/护盾/吸血/冰冻弹/燃烧弹……可叠加成层），每5波遭遇**三阶段Boss**。场上有可破坏掩体，打破箱子掉落补给。

| 按键 | 功能 |
|------|------|
| W / S | 前进 / 后退 |
| A / D | 车体转向 |
| 鼠标移动 | 炮台**绝对瞄准**（射线与地面求交，指哪打哪） |
| 鼠标左键 | 开火 |
| ESC | 暂停 / 继续 |

## 架构设计

```
Assets/scripts/
├── Framework/   框架层：泛型单例基类 / 事件中心(观察者) / 对象池 / 场景与音频管理 / Json+XOR存档 / UIFactory
├── Data/        配置层：BuffInfo(ScriptableObject) / MonsterInfo / WaveInfo (Json序列化)
├── Gameplay/    逻辑层：WaveManager波次 / MonsterFactory工厂 / IState四态FSM / BossObj三阶段 / GameMgr流程
└── tank/        存量层：教程迁移的实体类（坦克/子弹/箱子），已接入新框架
```

**数据流**：波次配置(Json) → WaveManager → MonsterFactory → 对象池生成怪物；战斗事件（受伤/击杀/死亡）→ 事件中心 → UI层订阅刷新，**战斗逻辑与UI零直接引用**。

## 技术亮点

- **对象池全链路**：子弹/特效/怪物/掉落物全部池化，战斗全程零 Instantiate/Destroy；支持总开关（`PoolManager.PoolsEnabled`）用于 Profiler A/B 对照
- **事件中心解耦**：观察者模式广播战斗事件，UI与战斗逻辑零直接引用——删除整个UI层战斗代码依然可编译
- **FSM四态敌人AI**：巡逻→追击→攻击→撤退，转移条件表驱动；异常状态机支持减速/燃烧DoT（浮点伤害积累器按秒取整）
- **Boss三阶段阶段机**：血量驱动的扇形弹幕→召唤+冲撞→狂暴，弹幕全走对象池
- **双配置驱动**：怪物/波次数值 Json 配置（工厂模式读取+容错降级），Buff强化 ScriptableObject 配置（编辑器可视化调参）——**新增内容零代码改动**
- **Json+异或加密存档**：防玩家手改，读档容错（损坏不崩、沿用默认）
- **射线绝对瞄准**：修复教程相对旋转的帧率相关缺陷（鼠标位移×deltaTime 导致高帧率下转速衰减）
- **UI运行时构建**：原子化构造（组件随物体同生）+ 自动补建 EventSystem/GraphicRaycaster + 文字射线过滤

## 性能实测

> TODO(量化数据)：用下方"A/B对照方法"测出后填入

| 场景（同一段30秒战斗） | GC Alloc | 备注 |
|---|---|---|
| 对象池开启 | 待填 | Profiler → GC Alloc 列 |
| 对象池关闭（直创建/销毁） | 待填 | `PoolManager.PoolsEnabled = false` |

## 排查案例：全工程预制体死链治理

接手工程时发现波次刷出的怪物/子弹脚本全部静默失效。**自写GUID扫描脚本**比对全工程预制体 `m_Script` 引用与脚本 `.meta` 的GUID，揪出**28处**教程遗留死链（引用指向已不存在的脚本），批量重连并建立"改预制体必查组件完整性"的规约。详见 `Docs/错题集.md` 的排查四步法。

## 构建

1. Unity Hub 安装 **2022.3.62f2c1**，打开本工程（首次导入需等待资源重建）
2. 打开 `Assets/Scenes/BeginScene`，Play 即可
3. 打包：File → Build Settings → Windows x86_64 → Build

## 开发说明

本项目由教程项目渐进改造而来，改造过程（约12个开发日、40+次提交）的模块决策、踩坑记录与面试复盘见仓库 `Docs/` 目录。Roadmap：联机对战（TCP）/ xLua数值热更 / A*寻路 已在规划中。
