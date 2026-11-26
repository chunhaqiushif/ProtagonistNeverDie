using System.Collections.Generic;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 物品 / 病症标识符集中定义，便于与 items.xml、afflictions.xml 对照。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 常量：物品 / 病症标识符
    // ---------------------------------------------------------------------
    private static readonly Identifier Tag = new Identifier("artifact_slot_headset");

    private static readonly Identifier ChipImmortal = new Identifier("immortal_chip");
    private static readonly Identifier ChipRespawn = new Identifier("respawn_chip");
    private static readonly Identifier ChipTeleport = new Identifier("teleport_chip");
    private static readonly Identifier ChipBuff = new Identifier("buff_chip");
    private static readonly Identifier ChipMaxHealth = new Identifier("maxhealth_chip");

    private static readonly HashSet<Identifier> ChipIdentifiers = new HashSet<Identifier>
    {
        ChipImmortal, ChipRespawn, ChipTeleport, ChipBuff, ChipMaxHealth,
    };

    private static readonly Identifier RespawnPointId = new Identifier("respawn_point");

    private static readonly Identifier AffMiracle = new Identifier("custom_miracleworker");
    private static readonly Identifier AffPressure = new Identifier("custom_pressurestabilized");
    private static readonly Identifier AffReaper = new Identifier("custom_reaperstax");
    private static readonly Identifier AffIgnored = new Identifier("custom_IgnoredByEnemyAI");
    private static readonly Identifier AffDrag = new Identifier("custom_emergencyresponse");
    private static readonly Identifier AffHusk = new Identifier("huskinfection");
    private static readonly Identifier AffSymbiosis = new Identifier("husksymbiosis");
    private static readonly Identifier AffBlessing = new Identifier("custom_respawnblessing");
    private static readonly Identifier AffMaxHealthBuff = new Identifier("custom_maxheathbuff");
    private static readonly Identifier AffRespawnReady = new Identifier("custom_respawnready");
    private static readonly Identifier AffRespawnCd = new Identifier("custom_respawncooldown");
    private static readonly Identifier AffTeleportReady = new Identifier("custom_teleportready");
    private static readonly Identifier AffTeleportCd = new Identifier("custom_teleportcooldown");

    private static readonly Identifier[] ChipStatusAfflictions =
    {
        AffRespawnReady, AffRespawnCd, AffTeleportReady, AffTeleportCd,
    };

    /// <summary>复生祝福期间会被削掉的持续掉血类病症。</summary>
    private static readonly HashSet<Identifier> CleanseTypes = new HashSet<Identifier>
    {
        new Identifier("damage"), new Identifier("bleeding"), new Identifier("burn"),
        new Identifier("oxygenlow"), new Identifier("poison"), new Identifier("bloodloss"),
    };
}
