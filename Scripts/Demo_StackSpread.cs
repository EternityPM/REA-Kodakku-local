// ============================================================================
// 演示脚本：分摊 / 分散助手
// ----------------------------------------------------------------------------
// 功能：
//   监听指定 StatusId 的获得事件。当队伍成员获得该状态（分摊 Debuff）时，
//   在其脚下绘制绿色集合圈；未获得的成员绘制红色分散圈。
//
// 演示内容：
//   - Init 初始化委托与每帧回调（RegistFrameworkUpdateAction）
//   - StatusAdd / StatusRemove 事件
//   - 队伍遍历、对象位置读取
//   - 安全色 / 危险色绘制、按序号命名绘制
//
// 使用方法：
//   在 UI 的“用户设置”中填入目标机制的 StatusId（例如某个分摊 Debuff），
//   设为 0 表示停用。
// ============================================================================

using System;
using BossDrawAssist.Draw;
using BossDrawAssist.Scripting;
using System.Collections.Generic;
using System.Numerics;

[ScriptType("a1b2c3d4-0002-4000-8000-000000000002", "分摊分散助手",
    Territories: new uint[0], Version: "1.0.0", Author: "EternityPMoon",
    Description: "按配置的 StatusId 高亮分摊/分散目标（演示脚本）")]
public class Demo_StackSpread
{
    /// <summary>分摊 Debuff 的 StatusId（0 = 停用）。</summary>
    [UserSetting("分摊StatusId", "需要监测的分摊 Debuff 状态 ID，0 表示停用")]
    public uint StackStatusId { get; set; } = 0;

    /// <summary>圈形半径（ylams）。</summary>
    [UserSetting("圈形半径", "高亮圈的半径")]
    public float CircleRadius { get; set; } = 3f;

    /// <summary>是否显示未获得分摊的成员（红色分散圈）。</summary>
    [UserSetting("显示分散圈", "为未获得分摊状态的成员绘制红色圈")]
    public bool ShowSpread { get; set; } = true;

    // 当前持有分摊 Debuff 的成员 EntityId 集合
    private readonly HashSet<uint> stackTargets = new();

    /// <summary>
    /// 初始化委托：脚本加载完成后调用一次（用户设置已恢复）。
    /// 这里注册每帧回调用于持续刷新圈形位置。
    /// </summary>
    public Action<ScriptContext> Init { get; }

    public Demo_StackSpread()
    {
        Init = ctx => ctx.Method.RegistFrameworkUpdateAction(
            () => FrameUpdate(ctx), "Demo_StackSpread_Frame");
    }

    [ScriptMethod(EventType.StatusAdd, "获得分摊", UserControl: true)]
    public void OnStatusAdd(GameEvent evt, ScriptContext ctx)
    {
        if (StackStatusId == 0 || evt.ActionId != StackStatusId)
            return;
        stackTargets.Add((uint)evt.TargetId);
        ctx.Log.Debug($"[分摊分散] {evt.TargetId:X} 获得分摊状态 {evt.ActionId}");
    }

    [ScriptMethod(EventType.StatusRemove, "分摊消失", UserControl: true)]
    public void OnStatusRemove(GameEvent evt, ScriptContext ctx)
    {
        if (StackStatusId == 0 || evt.ActionId != StackStatusId)
            return;
        stackTargets.Remove((uint)evt.TargetId);
    }

    [ScriptMethod(EventType.Territory, "切地形清理", UserControl: false)]
    public void OnTerritory(GameEvent evt, ScriptContext ctx)
    {
        stackTargets.Clear();
    }

    /// <summary>每帧刷新圈形。</summary>
    private void FrameUpdate(ScriptContext ctx)
    {
        if (StackStatusId == 0 || !ctx.Data.InCombat)
        {
            // 未启用或未战斗：清理残留绘制
            ctx.Method.RemoveDraw("^demo_ss_");
            return;
        }

        var party = ctx.Data.PartyList;
        var index = 0;
        foreach (var memberId in party)
        {
            if (memberId == 0)
            {
                index++;
                continue;
            }

            var hasStack = stackTargets.Contains(memberId);
            if (hasStack || ShowSpread)
            {
                var dp = ctx.Data.GetDefaultDrawProperties();
                dp.Name = $"demo_ss_{index}";
                dp.Owner = memberId;
                dp.Scale = new Vector2(CircleRadius);
                dp.Color = hasStack ? ctx.Data.DefaultSafeColor : ctx.Data.DefaultDangerColor;
                dp.DestroyAt = 0;   // 持续显示，由下一帧替换
                dp.Label = hasStack ? "分摊" : "分散";
                ctx.Method.SendDraw(DrawType.Circle, dp);
            }
            index++;
        }

        // 清理超出队伍长度的残留绘制（例如人数变化）
        ctx.Method.RemoveDraw($"^demo_ss_[{party.Count}-9]$");
    }
}
