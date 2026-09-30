# 勇士传说（2D Platformer Demo）

一个使用 Unity 制作的 2D 横版动作冒险 Demo。项目包含角色移动、跳跃与战斗，敌人状态机，可交互物品，场景传送，运行期存档/读档，生命值 UI，音频管理，以及基于 Tilemap 搭建的森林、洞穴和蜂巢关卡。

## 项目概览

- 游戏类型：2D 横版动作 / 平台跳跃
- 开发引擎：Unity `2023.1.22f1`
- 开发语言：C#
- 当前主流程：`Persistent → Menu → Cave ↔ Forest 3`
- 其他关卡场景：`Forest 1`、`Forest 2`、`Forest 4`、`Hive`

`Persistent` 是常驻场景，负责保存玩家、UI、音频、数据管理和场景加载器等全局对象。菜单与游戏关卡通过 Addressables 以 Additive 模式加载和卸载。

## 如何运行

1. 使用 Unity Hub 安装并选择 Unity `2023.1.22f1`。
2. 使用 Unity Hub 打开本项目根目录。
3. 等待 Unity 完成 Package 和资源导入。
4. 打开 `Assets/Scenes/Persistent.unity`。
5. 确认只从 `Persistent` 场景进入 Play Mode。
6. 点击 Unity 顶部的 Play 按钮。场景加载器会自动加载主菜单。

如果需要制作独立构建，请先将 `Persistent.unity` 作为启动场景加入 Build Settings 并启用，然后通过 Addressables Groups 构建 Addressable 内容。

## 游玩教程

### 操作方式

| 操作 | 键盘按键 | 说明 |
| --- | --- | --- |
| 左右移动 | `A` / `D` 或 `←` / `→` | 控制角色水平移动 |
| 跳跃 | `K` | 角色在接触地面时跳跃 |
| 攻击 | `J` | 播放攻击动作并启用攻击判定 |
| 交互 | `E` | 打开宝箱或使用传送点 |
| 加载游戏 | `L` | 读取本次运行期间最近保存的场景、位置和生命值 |
| UI 操作 | 鼠标或方向键 | 选择菜单和界面按钮 |

Input System 同时配置了手柄左摇杆移动；跳跃、攻击和交互目前以键盘绑定为主。

### 菜单

- **新的冒险**：初始化角色状态并进入 Cave 场景。
- **继续游戏**：读取本次运行期间最近保存的数据。
- **退出游戏**：关闭已构建的游戏；在 Unity Editor 中只会输出退出日志。

### 基础玩法

1. 在 Cave 中熟悉移动、跳跃和攻击。
2. 靠近可交互物时，角色旁边会显示 `E` 键提示。
3. 按 `E` 可以打开宝箱或使用传送点。
4. 野猪会在场景中巡逻，发现玩家后切换为追逐状态。
5. 使用 `J` 攻击敌人；受到攻击后角色会进入短暂无敌时间并产生击退。
6. 生命值归零或掉入危险水域后会显示 Game Over 界面，可读取已保存状态重新开始。
7. Cave 的传送点可前往 Forest 3，Forest 3 中的传送点可返回 Cave。
8. 设置按钮可打开暂停面板并调整主音量。

> “继续游戏”使用当前运行进程中的内存数据。退出游戏或停止 Play Mode 后，数据不会保留到下一次启动。

## 已实现功能

### 玩家系统

- 基于 Rigidbody2D 的水平移动和跳跃。
- Ground Check 地面、左墙和右墙检测。
- 根据移动方向翻转角色及攻击判定位置。
- 待机、移动、跳跃、下落、攻击、受伤和死亡动画状态。
- 近战攻击触发器与伤害结算。
- 受伤击退、无敌时间和死亡处理。

### 战斗与敌人

- 通用 `Character` 生命值、受伤和死亡事件。
- 通用 `Attack` 伤害组件。
- 野猪敌人及巡逻/追逐状态机。
- 基于 BoxCast 的玩家发现范围。
- 敌人受伤、击退、死亡动画和销毁流程。

### 互动系统

- 基于 `IInteractable` 的统一交互接口。
- 进入交互范围后显示并播放 `E` 键提示动画。
- 宝箱开启后的精灵切换及交互音效。
- 场景传送点与指定出生位置。

### 场景与关卡

- `Persistent` 常驻场景管理全局对象。
- Addressables 异步加载和卸载场景。
- Additive 场景切换及玩家位置迁移。
- DOTween 场景淡入淡出过渡。
- Tile Palette、Tilemap、Rule Tile 和 Tilemap Collider 组成的 2D 地形。
- Forest 1–4、Cave、Hive 和 Menu 等场景。

### UI 与音频

- 主菜单：新的冒险、继续游戏和退出游戏。
- 玩家生命值条及延迟掉血效果。
- Game Over、重新读取、设置和返回菜单界面。
- 暂停面板与主音量滑块。
- 基于 AudioSource、AudioMixer 和事件通道的 BGM/音效播放。
- Cinemachine Impulse 镜头震动支持。

### 数据系统

- `ISaveable` 可保存对象接口。
- `DataDefinition` 唯一 ID 标识。
- 保存当前场景、角色位置和生命值。
- 通过 ScriptableObject 事件触发保存与读取。
- 支持在游戏过程中按 `L` 快速读取最近保存的数据。
- 当前实现为运行期内存存档，尚未写入本地文件。

## 场景说明

| 场景 | 用途 |
| --- | --- |
| `Persistent` | 玩家、UI、音频、数据和场景加载等全局对象 |
| `Menu` | 游戏标题和主菜单 |
| `Cave` | 当前新游戏起始关卡，包含野猪、宝箱、存档点和传送点 |
| `Forest 3` | 与 Cave 双向连接的森林关卡 |
| `Forest 1` | 森林平台关卡布局 |
| `Forest 2` | 瀑布与水域主题森林布局 |
| `Forest 4` | 树冠与蜂巢平台布局 |
| `Hive` | 蜂巢内部主题关卡布局 |

## 技术栈

| 技术 | 项目中的用途 |
| --- | --- |
| Unity 2023.1.22f1 | 游戏引擎与编辑器 |
| C# | 游戏逻辑与编辑器扩展 |
| Unity 2D Physics | Rigidbody2D、Collider2D、Trigger 和碰撞检测 |
| Tilemap / Tile Palette / Rule Tile | 地形绘制、分层和平台碰撞 |
| Input System 1.7.0 | 玩家输入和 UI 输入 |
| Addressables 1.21.21 | 场景异步加载、卸载和引用管理 |
| Cinemachine 2.9.7 | 镜头约束及震动反馈 |
| Animator | 玩家与敌人动画状态控制 |
| uGUI / TextMeshPro 3.0.9 | 菜单、状态栏和游戏界面 |
| ScriptableObject Event Channel | 场景、生命值、音频、淡入淡出和存读档事件解耦 |
| DOTween | 场景切换时的 UI 淡入淡出 |
| AudioMixer | 主音量控制和 BGM/FX 分流 |

## 项目结构

```text
Assets/
├─ Art Assets/          # 像素美术、Tile、动画和其他视觉素材
├─ Data SO/             # 场景和事件等 ScriptableObject 数据
├─ Scenes/              # Persistent、Menu 和各游戏关卡
├─ Scripts/
│  ├─ Audio/            # BGM、音效和音量管理
│  ├─ Combat/           # 攻击与伤害判定
│  ├─ Enemy/            # 敌人及状态机
│  ├─ General/          # 宝箱等场景对象
│  ├─ Player/           # 玩家控制、属性、动画和交互
│  ├─ Save Load/        # 运行期存读档框架
│  ├─ ScriptableObject/ # 事件通道和场景数据
│  ├─ Tansition/        # 场景加载与传送
│  ├─ UI/               # 菜单、状态栏和淡入淡出
│  └─ Utilities/        # 枚举、接口和镜头控制
└─ Setting/input/       # Input Actions 及生成的输入代码
```

## 开发提示

- 游戏应从 `Persistent` 场景启动，不要直接从单个关卡场景进入 Play Mode。
- 新增可加载关卡时，需要创建对应的 `GameSceneSO`，并将场景加入 Addressables。
- 可交互对象需要使用 `Interactable` Tag、Trigger Collider，并实现 `IInteractable`。
- 可碰撞 Tilemap 需要设置为 `Ground` Layer，并配置 `TilemapCollider2D`。
- 角色的 Ground Check 只检测 `Ground` Layer。

## 素材与许可

项目美术资源位于 `Assets/Art Assets`。仓库当前未包含统一的 LICENSE 文件；二次发布或商业使用前，请分别确认代码和第三方美术、字体、音频资源的授权范围。
