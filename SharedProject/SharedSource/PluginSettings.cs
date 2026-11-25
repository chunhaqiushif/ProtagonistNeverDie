using System;
using Barotrauma;
using Barotrauma.Networking;
using Barotrauma.Plugins;
using Microsoft.Xna.Framework;

namespace ProtagonistNeverDie;

/// <summary>
/// 模组参数 → 插件设置（ISettingsService）
/// </summary>
internal static class PluginSettings
{
    private const string TagPrefix = "pluginsetting.";

    // 默认值与旧版常量保持一致
    private const int DefaultTeleportThreshold = -50;
    private const int DefaultRespawnThreshold = -65;
    private const int DefaultTeleportCooldown = 60;
    private const int DefaultRespawnCooldown = 120;
    private const int DefaultTeleportChipLoss = 15;
    private const int DefaultTeleportChipLossBot = 5;
    private const int DefaultRespawnChipLoss = 25;
    private const int DefaultRespawnChipLossBot = 10;

    private static IntSetting? _teleportThreshold;
    private static IntSetting? _respawnThreshold;
    private static IntSetting? _teleportCooldown;
    private static IntSetting? _respawnCooldown;
    private static IntSetting? _teleportChipLoss;
    private static IntSetting? _teleportChipLossBot;
    private static IntSetting? _respawnChipLoss;
    private static IntSetting? _respawnChipLossBot;

    /// <summary>在插件 Init 阶段调用；注册顺序即界面显示顺序。</summary>
    public static void Register()
    {
        ISettingsService service = PluginServiceProvider.GetService<ISettingsService>();

        _teleportThreshold   = RegisterPercentThreshold(service, "teleport_threshold", DefaultTeleportThreshold);
        _respawnThreshold    = RegisterPercentThreshold(service, "respawn_threshold", DefaultRespawnThreshold);
        _teleportCooldown    = RegisterInt(service, "teleport_cooldown", DefaultTeleportCooldown, 10, 600, 5);
        _respawnCooldown     = RegisterInt(service, "respawn_cooldown", DefaultRespawnCooldown, 10, 600, 5);
        _teleportChipLoss    = RegisterInt(service, "teleport_chip_loss", DefaultTeleportChipLoss, 5, 100, 1);
        _teleportChipLossBot = RegisterInt(service, "teleport_chip_loss_bot", DefaultTeleportChipLossBot, 5, 100, 1);
        _respawnChipLoss     = RegisterInt(service, "respawn_chip_loss", DefaultRespawnChipLoss, 5, 100, 1);
        _respawnChipLossBot  = RegisterInt(service, "respawn_chip_loss_bot", DefaultRespawnChipLossBot, 5, 100, 1);
    }

    // ---------------------------------------------------------------------
    // 生效值：由 PluginCore 直接读取（using static）
    // ---------------------------------------------------------------------
    /// <summary>传送芯片触发阈值（生命力百分比，-100 ~ 0）。</summary>
    public static float TeleportThreshold => Clamp(_teleportThreshold?.Value ?? DefaultTeleportThreshold, -100f, 0f);

    /// <summary>复生芯片触发阈值（生命力百分比，-100 ~ 0）。</summary>
    public static float RespawnThreshold => Clamp(_respawnThreshold?.Value ?? DefaultRespawnThreshold, -100f, 0f);

    /// <summary>传送芯片冷却（秒）。</summary>
    public static float TeleportCooldownSeconds => Clamp(_teleportCooldown?.Value ?? DefaultTeleportCooldown, 10f, 600f);

    /// <summary>复生芯片冷却（秒）。</summary>
    public static float RespawnCooldownSeconds => Clamp(_respawnCooldown?.Value ?? DefaultRespawnCooldown, 10f, 600f);

    /// <summary>传送芯片单次耐久消耗（0 ~ 1 系数，界面按百分比显示）。</summary>
    public static float TeleportChipLoss => Clamp(_teleportChipLoss?.Value ?? DefaultTeleportChipLoss, 5f, 100f) / 100f;

    /// <summary>传送芯片单次耐久消耗 · NPC（0 ~ 1）。</summary>
    public static float TeleportChipLossBot => Clamp(_teleportChipLossBot?.Value ?? DefaultTeleportChipLossBot, 5f, 100f) / 100f;

    /// <summary>复生芯片单次耐久消耗（0 ~ 1 系数，界面按百分比显示）。</summary>
    public static float RespawnChipLoss => Clamp(_respawnChipLoss?.Value ?? DefaultRespawnChipLoss, 5f, 100f) / 100f;

    /// <summary>复生芯片单次耐久消耗 · NPC（0 ~ 1）。</summary>
    public static float RespawnChipLossBot => Clamp(_respawnChipLossBot?.Value ?? DefaultRespawnChipLossBot, 5f, 100f) / 100f;

    // ---------------------------------------------------------------------
    // 注册辅助
    // ---------------------------------------------------------------------
    private static IntSetting RegisterPercentThreshold(ISettingsService service, string id, int defaultValue)
    {
        // 生命力阈值的区间固定为 [-100, 0]
        return RegisterInt(service, id, defaultValue, -100, 0, 1);
    }

    private static IntSetting RegisterInt(ISettingsService service, string id, int defaultValue, int min, int max, int step)
    {
        IntSetting setting = new IntSetting(new Identifier(id), defaultValue, TextManager.Get(TagPrefix + id + ".name"))
        {
            ShowInUI = true,
            ToolTip = TextManager.Get(TagPrefix + id + ".desc"),
            Range = new Point(min, max),
            StepValue = step,
            SyncMode = SettingSyncMode.ServerAuthority,
            IsAllowedToSet = CanClientEdit,
        };

        service.RegisterSetting(setting);
        return setting;
    }

    /// <summary>
    /// 服务端唯一授权点，同时也是客户端置灰依据。
    /// client == null 表示单机 / 主机本地操作；联机客户端需要 ManageSettings 权限。
    /// </summary>
    private static bool CanClientEdit(ISetting setting, Client? client)
        => client == null || client.Permissions.HasFlag(ClientPermissions.ManageSettings);

    private static float Clamp(int value, float min, float max) => Math.Clamp(value, min, max);
}
