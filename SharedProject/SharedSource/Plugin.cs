using System;
using System.Collections.Generic;
using System.Reflection;
using Barotrauma;
using Barotrauma.Plugins;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace ProtagonistNeverDie;

/// <summary>
/// 异星遗物插槽耳机 · 插件版入口（官方插件系统 IBarotraumaPlugin）。
/// </summary>
public partial class Plugin : IBarotraumaPlugin
{
    internal const string LogPrefix = "[Protagonist Never Die] ";
    internal const string CoroutineName = "ProtagonistNeverDieTick";

    public static readonly IDebugConsole DebugConsole = PluginServiceProvider.GetService<IDebugConsole>();

    internal static IHarmonyProvider? HarmonyProvider { get; private set; }

    public void Init()
    {
        HarmonyProvider = PluginServiceProvider.GetService<IHarmonyProvider>();

        Log("异星遗物插槽耳机 插件版已加载");

        // 模组参数注册到「设置 → 模组设置」，服务端权威 + 全服同步
        try
        {
            PluginSettings.Register();
        }
        catch (Exception e)
        {
            LogError("设置注册失败：" + e.Message);
        }

        ApplyHarmonyPatches();

        // 每帧驱动：客户端 + 服务端都有 CoroutineManager.Update，暂停时不推进
        try
        {
            CoroutineManager.StartCoroutine(TickLoop(), CoroutineName);
        }
        catch (Exception e)
        {
            LogError("协程启动失败：" + e.Message);
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

        Log("插件已卸载");
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

    /// <summary>
    /// 拦截「把耐久已归零的芯片从耳机里取出」：
    /// UI 层已用 NonInteractable 挡住拖拽，这里兜住联机时客户端绕过界面直接发请求的情况。
    /// ItemInventory.RemoveItem 会先调用 base.RemoveItem，所以只补基类即可覆盖容器场景。
    /// </summary>
    private static void ApplyHarmonyPatches()
    {
        if (HarmonyProvider == null)
        {
            LogError("Harmony 服务不可用，耐久归零芯片的锁定只在 UI 层生效");
            return;
        }

        try
        {
            Harmony harmony = HarmonyProvider.GetHarmony();

            MethodInfo? removeItem = AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(Item) });
            MethodInfo? prefix = typeof(PluginCore).GetMethod(nameof(PluginCore.RemoveItemPrefix), BindingFlags.Static | BindingFlags.NonPublic);

            if (removeItem == null || prefix == null)
            {
                LogError("未找到 Inventory.RemoveItem，芯片锁定只在 UI 层生效");
                return;
            }

            harmony.Patch(removeItem, prefix: new HarmonyMethod(prefix));
        }
        catch (Exception e)
        {
            LogError("Harmony 补丁失败：" + e.Message);
        }
    }

    /// <summary>由 ClientProject / ServerProject 分别实现，放平台专属内容。</summary>
    public partial void InitProjectSpecific();
}
