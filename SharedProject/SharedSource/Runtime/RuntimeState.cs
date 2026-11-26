using System;
using System.Collections.Generic;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 运行时状态容器与生命周期（插件卸载复位 / 回合开始清场 / 内容加载后丢弃 prefab 缓存）。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 运行时状态
    // ---------------------------------------------------------------------
    private static readonly Dictionary<ushort, bool> BuffChipHolders = new Dictionary<ushort, bool>();
    private static readonly Dictionary<ushort, bool> ImmortalChipHolders = new Dictionary<ushort, bool>();
    private static readonly Dictionary<ushort, bool> MaxHealthChipHolders = new Dictionary<ushort, bool>();
    private static readonly Dictionary<ushort, bool> PendingBless = new Dictionary<ushort, bool>();
    private static readonly Dictionary<ushort, double> TeleportCooldown = new Dictionary<ushort, double>();
    private static readonly Dictionary<ushort, double> ReviveCooldown = new Dictionary<ushort, double>();
    private static readonly HashSet<ushort> DestroyScheduled = new HashSet<ushort>();
    private static readonly HashSet<ushort> ChipLocked = new HashSet<ushort>();   // 触发后的锁定期，期间禁止卸下
    private static readonly Dictionary<Identifier, AfflictionPrefab> PrefabCache = new Dictionary<Identifier, AfflictionPrefab>();

    private struct ScheduledAction
    {
        public double Due;
        public Action Action;
    }

    private static readonly List<ScheduledAction> Scheduled = new List<ScheduledAction>();
    private static readonly Random Rng = new Random();

    private static double _clock;         // 未暂停时钟（Timing.TotalTimeUnpaused，单位秒）
    private static double _lastClock = -1; // 上一帧的时间戳；< 0 表示还没跑过第一帧
    private static float _elapsed;        // 距离下次结算的累计
    private static int _errorCount;

    /// <summary>插件卸载时调用：清空整个插件的静态状态。</summary>
    public static void Reset()
    {
        ClearRuntimeState();
        PrefabCache.Clear();
        _clock = 0;
        _errorCount = 0;
    }

    /// <summary> /// 回合开始时调用（新回合 / 读档 / 结束回合后重开）/// </summary>
    internal static void OnRoundStarted() => ClearRuntimeState();

    /// <summary>清理所有「按实体 ID 索引」的运行时状态。</summary>
    private static void ClearRuntimeState()
    {
        BuffChipHolders.Clear();
        ImmortalChipHolders.Clear();
        MaxHealthChipHolders.Clear();
        PendingBless.Clear();
        TeleportCooldown.Clear();
        ReviveCooldown.Clear();
        DestroyScheduled.Clear();
        ChipLocked.Clear();
        Scheduled.Clear();

        // 读图耗时不应计入下一帧的 dt
        _lastClock = -1;
        _elapsed = 0f;
    }

    /// <summary>
    /// 内容包加载完成后调用。重载 mod 时注册表会整批重建，旧的 prefab 缓存必须丢弃，
    /// 否则 Instantiate 出来的病症与当前注册表不匹配。
    /// </summary>
    public static void OnContentLoaded()
    {
        PrefabCache.Clear();
        _lastClock = -1;
    }
}
