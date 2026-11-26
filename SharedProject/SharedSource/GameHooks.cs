using System;
using System.Reflection;
using Barotrauma;
using Barotrauma.Plugins;
using HarmonyLib;

namespace ProtagonistNeverDie;

/// <summary>
/// Harmony 补丁装配层：这里只负责「挂哪个方法、挂到哪个回调、失败怎么记日志」，
/// 回调本体留在 <see cref="PluginCore"/>，保持逻辑与挂钩解耦。
///
///   * Inventory.RemoveItem 前缀：阻止把耐久归零的芯片从耳机里取出
///   * GameSession.StartRound 后缀：回合开始（新回合 / 读档 / 结束回合后重开）时清空上一回合残留状态
/// </summary>
internal static class GameHooks
{
    public static void Install(IHarmonyProvider? provider)
    {
        if (provider == null)
        {
            Plugin.LogError("Harmony provider unavailable; depleted chip lock will only apply at the UI layer.");
            return;
        }

        try
        {
            Harmony harmony = provider.GetHarmony();

            MethodInfo? removeItem = AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(Item) });
            MethodInfo? prefix = typeof(PluginCore).GetMethod(nameof(PluginCore.RemoveItemPrefix), BindingFlags.Static | BindingFlags.NonPublic);

            if (removeItem == null || prefix == null)
            {
                Plugin.LogError("Inventory.RemoveItem not found; chip lock will only apply at the UI layer.");
                return;
            }

            harmony.Patch(removeItem, prefix: new HarmonyMethod(prefix));

            PatchRoundStart(harmony);
        }
        catch (Exception e)
        {
            Plugin.LogError("Harmony patching failed: " + e.Message);
        }
    }

    /// <summary>
    /// 回合开始（新回合 / 读档 / 结束回合后重开）时清掉上一回合遗留的冷却与延迟动作。
    ///
    /// GameSession.StartRound 有两个重载（levelSeed 版与 levelData 版），逐个挂钩；
    /// postfix 不带参数，因此不依赖重载的签名细节，游戏版本微调形参也不受影响。
    /// </summary>
    private static void PatchRoundStart(Harmony harmony)
    {
        MethodInfo? callback = typeof(PluginCore).GetMethod(
            nameof(PluginCore.OnRoundStarted), BindingFlags.Static | BindingFlags.NonPublic);

        if (callback == null)
        {
            Plugin.LogError("Round-start callback not found; cooldowns may persist across rounds.");
            return;
        }

        int patched = 0;
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(GameSession)))
        {
            if (method.Name != nameof(GameSession.StartRound)) continue;

            harmony.Patch(method, postfix: new HarmonyMethod(callback));
            patched++;
        }

        if (patched == 0)   
        {
            Plugin.LogError("GameSession.StartRound not found; cooldowns may persist across rounds.");
        }
        else
        {
            //Plugin.Log($"Hooked round start (StartRound overloads: {patched}).");
        }
    }
}
