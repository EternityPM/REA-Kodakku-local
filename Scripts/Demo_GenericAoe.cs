// ============================================================================
// 演示脚本：通用 AOE 预警
// ----------------------------------------------------------------------------
// 功能：
//   监听所有读条（StartCasting），在读条来源脚下绘制危险圈，
//   同时进行 TTS 播报与屏幕文字提示；读条结束后自动淡出。
//
// 演示内容：
//   - [ScriptType] / [ScriptMethod] / [UserSetting] 特性的使用
//   - 方法内过滤（读条时长、对象类型）
//   - 圆形绘制、Owner 跟随、DestroyAt 生命周期
//   - TTS 与屏幕文字提示
// ============================================================================

using BossDrawAssist.Draw;
using BossDrawAssist.Scripting;
using System.Numerics;

[ScriptType("a1b2c3d4-0001-4000-8000-000000000001", "通用AOE预警",
    Territories: new uint[0], Version: "1.0.0", Author: "EternityPMoon",
    Description: "对任意 Boss 读条绘制脚下危险圈并语音提示（演示脚本）")]
public class Demo_GenericAoe
{
    /// <summary>危险圈半径（游戏单位 yalms）。</summary>
    [UserSetting("危险圈半径", "绘制圈形的半径，单位 yalms")]
    public float CircleRadius { get; set; } = 6f;

    /// <summary>是否语音播报。</summary>
    [UserSetting("语音播报", "读条开始时播放 TTS 提示")]
    public bool EnableTts { get; set; } = true;

    /// <summary>是否仅在副本中生效。</summary>
    [UserSetting("仅副本生效", "开启后只在普通副本/高难副本中生效")]
    public bool OnlyInDuty { get; set; } = false;

    /// <summary>读条最短时长（秒），过短的读条不提示，避免刷屏。</summary>
    [UserSetting("最短读条(秒)", "短于该时长的读条不提示")]
    public float MinCastTime { get; set; } = 1.0f;

    [ScriptMethod(EventType.StartCasting, "读条开始预警", UserControl: true)]
    public void OnCastStart(GameEvent evt, ScriptContext ctx)
    {
        // 只处理带 ActionId 的读条（0 为无效值）
        if (evt.ActionId == 0)
            return;

        if (OnlyInDuty && ctx.Data.Territory == 0)
            return;

        if (evt.Duration > 0 && evt.Duration < MinCastTime)
            return;

        // 只处理 Boss/NPC 的读条
        var source = ctx.Data.SearchById((uint)evt.SourceId);
        if (source == null)
            return;

        // 1. 绘制跟随来源的危险圈（Owner 跟随对象移动）
        var dp = ctx.Data.GetDefaultDrawProperties();
        dp.Name = "demo_aoe_circle";
        dp.Owner = evt.SourceId;
        dp.Scale = new Vector2(CircleRadius);
        dp.Color = ctx.Data.DefaultDangerColor;
        dp.DestroyAt = (long)(evt.Duration * 1000f) + 800; // 读条结束后 0.8s 消失
        dp.Label = $"{evt.Duration:F1}s";
        ctx.Method.SendDraw(DrawType.Circle, dp);

        // 2. 语音 + 屏幕文字
        if (EnableTts)
            ctx.Method.TTS("读条");
        ctx.Method.TextInfo($"危险读条 {evt.Duration:F1} 秒", (int)(evt.Duration * 1000f));
    }

    [ScriptMethod(EventType.CancelAction, "读条结束提示", UserControl: true)]
    public void OnCastEnd(GameEvent evt, ScriptContext ctx)
    {
        // 读条自然结束时（接近总时长）播报“释放”
        if (evt.Duration > 0 && evt.Elapsed >= evt.Duration - 0.3f && evt.Elapsed > 0.1f)
        {
            if (EnableTts)
                ctx.Method.TTS("释放");
        }
    }
}
