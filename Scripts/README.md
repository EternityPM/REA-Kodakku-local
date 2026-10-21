# BossDrawAssist 脚本编写指南

本目录下的 `.cs` 文件会被自动编译为脚本并加载。修改保存后约 0.5 秒自动热重载
（也可在插件主窗口点击「重载脚本」或按 F5，游戏内 `/pbreload`）。

每个文件即一个脚本，文件中**必须有一个**标注了 `[ScriptType]` 的公共类。

---

## 1. 脚本骨架

```csharp
using BossDrawAssist.Draw;
using BossDrawAssist.Scripting;
using System.Numerics;

[ScriptType("唯一GUID（生成后不可更改）", "脚本显示名",
    Territories: new uint[] { 1234 },   // 适用地形 TerritoryId，空数组 = 全地形
    Version: "1.0.0", Author: "EternityPMoon",
    Description: "脚本说明")]
public class MyScript
{
    // 用户设置：自动出现在 UI，按 GUID 自动持久化
    [UserSetting("显示名", "说明文字")]
    public float MyRadius { get; set; } = 5f;

    // 事件处理器：方法签名固定为 (GameEvent, ScriptContext)
    [ScriptMethod(EventType.StartCasting, "处理器名（可选）",
        EventCondition: new[] { "ActionId:12345", "SourceDataId:6789" }, // 可选条件过滤
        UserControl: true)]                                          // 是否允许用户在 UI 停用
    public void OnCast(GameEvent evt, ScriptContext ctx)
    {
        // …业务逻辑…
    }

    // 可选的初始化委托：加载完成后（用户设置已恢复）调用一次。
    // 注意：必须在构造函数中赋值。字段初始值设定项不允许引用实例成员，
    // 下面这种「字段初始化 + 引用实例方法」的写法会导致 CS0236 编译失败：
    //     public Action<ScriptContext> Init = ctx => { FrameUpdate(ctx); };  // ✗ 编译错误
    public Action<ScriptContext> Init { get; }

    public MyScript()
    {
        Init = ctx => ctx.Method.RegistFrameworkUpdateAction(
            () => MyFrameUpdate(ctx), "MyScript_Frame");
    }

    private void MyFrameUpdate(ScriptContext ctx) { /* 每帧逻辑 */ }
}
```

---

## 2. 事件类型（EventType）

| 事件 | 触发时机 | 常用字段 |
|---|---|---|
| `StartCasting` | 某单位开始读条 | `SourceId` `SourceDataId` `ActionId` `Duration` `SourcePosition` `SourceRotation` |
| `CancelAction` | 读条结束/取消 | 同上 + `Elapsed` |
| `ActionEffect` | 读条自然完成（近似技能释放） | 同上 |
| `Territory` | 地形变化 | `SourceDataId`（新 TerritoryId） |
| `CombatChanged` | 进入/脱离战斗 | `Info` |
| `AddCombatant` / `RemoveCombatant` | NPC 出现/消失 | `SourceId` `SourceDataId` |
| `Death` | 单位死亡 | `SourceId` |
| `UpdateParty` | 队伍变化 | 扩展属性 `Member0`~`Member7` |
| `StatusAdd` / `StatusRemove` | 获得/失去状态 | `TargetId` `ActionId`（StatusId）+ 扩展 `StackCount` `RemainingTime` `SourceId` |
| `Waymark` | 场地标记变化 | 扩展 `Index` `Label` `Active` `X` `Y` `Z` |
| `Chat` | 聊天消息 | `Info`（文本）+ 扩展 `ChatTypeName` `Sender` |

**条件过滤**：`EventCondition` 中每一项 `"Key:Value"` 都需与事件字段相等
（`SourceId`/`TargetId`/`SourceDataId`/`ActionId` 或扩展属性），例如
`new[] { "ActionId:37986" }` 只处理 ActionId 为 37986 的读条。

---

## 3. 数据接口（ctx.Data）

| 成员 | 说明 |
|---|---|
| `Me` / `MyObject` / `MyPosition` | 本地玩家 |
| `Objects` | IObjectTable，可 LINQ 查询附近对象 |
| `SearchById(entityId)` / `SearchByDataId(dataId)` | 按 EntityId / DataId 查找对象（`SearchById` 用的是 EntityId，不是 GameObjectId） |
| `PartyList` / `PartyCount` | 队伍 EntityId 列表 |
| `Territory` | 当前 TerritoryId |
| `InCombat` / `IsLoggedIn` | 状态 |
| `Waymarks` | Waymark 列表（`Index` 0-6 = A/B/C/1-4，`Active` `Position` `Label`） |
| `DefaultDangerColor` / `DefaultSafeColor` | 默认红/绿色 |
| `GetDefaultDrawProperties()` | 一份默认绘图属性 |

---

## 4. 方法接口（ctx.Method）

| 方法 | 说明 |
|---|---|
| `SendDraw(DrawType, DrawProperties)` | 发送绘制（同名替换） |
| `RemoveDraw("名称" 或 "^前缀" 或 "*")` | 移除绘制，支持正则 |
| `TextInfo(text, ms[, isWarning])` | 屏幕中央文字 |
| `TTS(text[, rate])` | 语音播报 |
| `SendChat(text)` | 游戏内发送消息 |
| `RegistFrameworkUpdateAction(action, key)` | 注册每帧回调，返回 GUID |
| `UnregistFrameworkUpdateAction(guid)` | 注销回调 |
| `ClearDraws(this)` | 清空本脚本的全部绘制 |
| `PostEvent(evt)` | 投递自定义事件 |

---

## 5. 绘图（DrawType 与 DrawProperties）

**DrawType**：`Circle` `Donut` `Fan` `Rect` `Line` `Straight` `Arrow`
`SightAvoid` `Displacement` `HotWing`

**常用属性**：

```csharp
var dp = ctx.Data.GetDefaultDrawProperties();
dp.Name  = "唯一名";                    // 同名会替换旧绘制
dp.Owner = evt.SourceId;                // 跟随该对象移动
dp.CentreResolvePattern = PositionResolvePattern.Fixed;  // 用固定坐标
dp.Position = new Vector3(x, y, z);     // 固定坐标
dp.Offset  = new Vector3(0, 0, 5);      // 相对偏移
dp.Scale   = new Vector2(radius);       // 圆=半径；矩形=长宽
dp.InnerScale = new Vector2(innerR);    // 圆环内径
dp.Rotation = evt.SourceRotation;       // 方向（弧度）
dp.Radian = MathF.PI / 3;               // 扇形张角（0=整圆）
dp.Color  = ctx.Data.DefaultDangerColor;
dp.Label  = "文字";                     // 形状上方文字
dp.Delay  = 2000;                       // 延迟 2 秒显示
dp.DestroyAt = 5000;                    // 5 秒后自动消失（0=持续）
dp.Wave   = true;                       // 波纹动画
ctx.Method.SendDraw(DrawType.Circle, dp);
```

**位置解析模式**：`Owner`（默认，跟随 Owner）/ `Fixed`（固定 Position）/
`OwnerWithOffset` / `Target` / `TargetWithOffset`。

---

## 6. 注意事项

1. GUID 一旦发布**不可更改**，否则用户设置丢失。
2. 编译报错不会影响其它脚本，错误信息显示在 UI 详情中。
3. 处理器抛异常不会导致插件崩溃，错误记录在 `/xllog`。
4. 只读原则：本工具仅读取游戏状态并绘制提示，不发送任何游戏包。
5. 每帧回调应尽量轻量；重型计算请节流（如每 100ms 执行一次）。
