// ============================================================================
// 演示脚本：Waymark 指路助手
// ----------------------------------------------------------------------------
// 功能：
//   在世界中标注所有已放置的 Waymark（A/B/C/D/1-4），
//   并从本地玩家向指定 Waymark 绘制指路箭头与距离文字。
//
// 演示内容：
//   - Waymark 数据读取（ctx.Data.Waymarks）
//   - Init 初始化委托与每帧回调
//   - 箭头（Arrow）绘制、固定坐标绘制、文字标签
//   - 枚举类型用户设置
// ============================================================================

using System;
using BossDrawAssist;
using BossDrawAssist.Draw;
using BossDrawAssist.Scripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

[ScriptType("a1b2c3d4-0003-4000-8000-000000000003", "Waymark指路助手",
    Territories: new uint[0], Version: "1.0.0", Author: "EternityPMoon",
    Description: "标注 Waymark 位置并绘制指路箭头（演示脚本）")]
public class Demo_WaymarkGuide
{
    /// <summary>指路目标 Waymark。</summary>
    public enum GuideTarget
    {
        /// <summary>最近的已放置 Waymark。</summary>
        最近,
        /// <summary>标记 A。</summary>
        A,
        /// <summary>标记 B。</summary>
        B,
        /// <summary>标记 C。</summary>
        C,
        /// <summary>标记 D。</summary>
        D,
        /// <summary>标记 1（数字）。</summary>
        一,
        /// <summary>标记 2（数字）。</summary>
        二,
        /// <summary>标记 3（数字）。</summary>
        三,
        /// <summary>标记 4（数字）。</summary>
        四,
    }

    /// <summary>指路目标。</summary>
    [UserSetting("指路目标", "箭头指向哪个 Waymark")]
    public GuideTarget Target { get; set; } = GuideTarget.最近;

    /// <summary>是否标注所有 Waymark。</summary>
    [UserSetting("标注所有Waymark", "在每个已放置的 Waymark 处绘制小圈与字母")]
    public bool ShowAllMarks { get; set; } = true;

    /// <summary>是否绘制指路箭头。</summary>
    [UserSetting("指路箭头", "从玩家位置绘制指向目标的箭头")]
    public bool ShowArrow { get; set; } = true;

    private readonly Vector4 markColor = new(0.4f, 0.8f, 1f, 0.35f);
    private readonly Vector4 arrowColor = new(1f, 0.9f, 0.3f, 0.5f);

    /// <summary>
    /// 脚本初始化委托（由宿主在设置恢复完成后调用一次）。<br/>
    /// 必须在构造函数中赋值：字段初始值设定项不允许引用实例成员（FrameUpdate）。
    /// </summary>
    public Action<ScriptContext> Init { get; }

    public Demo_WaymarkGuide()
    {
        Init = ctx => ctx.Method.RegistFrameworkUpdateAction(
            () => FrameUpdate(ctx), "Demo_WaymarkGuide_Frame");
    }

    private void FrameUpdate(ScriptContext ctx)
    {
        var me = ctx.Data.MyObject;
        if (me == null)
            return;

        // 每帧先清掉本脚本上帧绘制的图案，避免设置变更（如指路目标从“最近”切换为“A”）后旧图案残留地面
        ctx.Method.RemoveDraw("^demo_wm_");

        var marks = ctx.Data.Waymarks.Where(w => w.Active).ToList();
        if (marks.Count == 0)
            return;

        // 1. 标注所有 Waymark
        if (ShowAllMarks)
        {
            foreach (var wm in marks)
            {
                var dp = ctx.Data.GetDefaultDrawProperties();
                dp.Name = $"demo_wm_mark_{wm.Index}";
                dp.CentreResolvePattern = PositionResolvePattern.Fixed;
                dp.Position = wm.Position;
                dp.Scale = new Vector2(1.5f);
                dp.Color = markColor;
                dp.DestroyAt = 0;
                dp.Label = wm.Label;
                ctx.Method.SendDraw(DrawType.Circle, dp);
            }
        }

        // 2. 指路箭头
        if (ShowArrow)
        {
            var target = PickTarget(marks, me.Position);
            if (target != null)
            {
                var dp = ctx.Data.GetDefaultDrawProperties();
                dp.Name = "demo_wm_arrow";
                dp.CentreResolvePattern = PositionResolvePattern.Fixed;
                dp.Position = me.Position;
                dp.TargetPosition = target.Position;
                dp.Color = arrowColor;
                dp.Scale = new Vector2(2f, 1.2f);
                dp.DestroyAt = 0;
                dp.Label = $"{Vector3.Distance(me.Position, target.Position):F0} y";
                ctx.Method.SendDraw(DrawType.Arrow, dp);
            }
        }
    }

    /// <summary>按设置选择指路目标。</summary>
    private Waymark? PickTarget(List<Waymark> marks, Vector3 playerPos)
    {
        if (marks.Count == 0)
            return null;
        return Target switch
        {
            GuideTarget.A => marks.FirstOrDefault(w => w.Index == 0),
            GuideTarget.B => marks.FirstOrDefault(w => w.Index == 1),
            GuideTarget.C => marks.FirstOrDefault(w => w.Index == 2),
            GuideTarget.D => marks.FirstOrDefault(w => w.Index == 3),
            GuideTarget.一 => marks.FirstOrDefault(w => w.Index == 4),
            GuideTarget.二 => marks.FirstOrDefault(w => w.Index == 5),
            GuideTarget.三 => marks.FirstOrDefault(w => w.Index == 6),
            GuideTarget.四 => marks.FirstOrDefault(w => w.Index == 7),
            _ => marks.OrderBy(w => Vector3.Distance(playerPos, w.Position)).First(),
        };
    }
}
