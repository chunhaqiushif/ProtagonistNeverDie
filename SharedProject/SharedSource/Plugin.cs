using System;
using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Plugins;
using Microsoft.Xna.Framework;

namespace ProtagonistNeverDie;

/// <summary>
/// 异星遗物插槽耳机 · 插件版入口（官方插件系统 IBarotraumaPlugin）。
///
/// 本文件只负责生命周期编排：取服务 → 注册设置 → 装配钩子 → 启动每帧驱动 → 交给平台分部。
/// 具体职责分别在：
///   * <see cref="GameHooks"/>      —— Harmony 补丁装配
///   * <see cref="PluginCore"/>     —— 游戏逻辑（Runtime/ 下按职责分文件）
/// </summary>
public partial class Plugin : IBarotraumaPlugin
{
    internal const string CoroutineName = "ProtagonistNeverDieTick";

    public static readonly IDebugConsole DebugConsole = PluginServiceProvider.GetService<IDebugConsole>();

    internal static IHarmonyProvider? HarmonyProvider { get; private set; }

    public void Init()
    {
        HarmonyProvider = PluginServiceProvider.GetService<IHarmonyProvider>();

        Log("Protagonist Never Die (C#) loaded.");

        GameHooks.Install(HarmonyProvider);

        // 每帧驱动：客户端 + 服务端都有 CoroutineManager.Update，暂停时不推进
        try
        {
            CoroutineManager.StartCoroutine(TickLoop(), CoroutineName);
        }
        catch (Exception e)
        {
            LogError("Failed to start tick coroutine: " + e.Message);
        }

        InitProjectSpecific();
    }

    /// <summary>内容包（物品、病症 prefab）全部加载完成后再做一次复位，避免首帧读到半成品注册表。</summary>
    public void OnContentLoaded()
    {
        PluginCore.OnContentLoaded();
    }

    public void Dispose()
    {
        try { CoroutineManager.StopCoroutines(CoroutineName); } catch { /* 卸载阶段允许失败 */ }

        PluginCore.Reset();

        HarmonyProvider = null;

        Log("Plugin unloaded.");
    }

    public static void Log(string message) => DebugConsole.NewMessage(message, Color.Lime);

    internal static void LogError(string message) => DebugConsole.NewMessage(message, Color.Red);

    private static IEnumerable<CoroutineStatus> TickLoop()
    {
        while (true)
        {
            PluginCore.Tick();
            yield return CoroutineStatus.Running;
        }
    }

    /// <summary>由 ClientProject / ServerProject 分别实现，放平台专属内容。</summary>
    public partial void InitProjectSpecific();
}
