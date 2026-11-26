using System.Collections.Generic;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 芯片状态图标：就绪 / 冷却进度的 affliction 显示。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 芯片状态图标
    // ---------------------------------------------------------------------
    private static void UpdateChipStatus(Character character, Dictionary<Identifier, Item> chips, bool reviving, CharacterHealth health)
    {
        if (health == null) return;

        chips.TryGetValue(ChipRespawn, out Item? respawnChip);
        if (respawnChip != null)
        {
            float ratio = CooldownRatio(ReviveCooldown, respawnChip, RespawnCooldownSeconds);

            if (ratio > 0f) SetAfflictionStrength(character, AffRespawnCd, ratio * 100f);
            else ClearAffliction(health, AffRespawnCd);

            if (ratio <= 0f && !reviving) ApplyFull(character, AffRespawnReady);
            else ClearAffliction(health, AffRespawnReady);
        }
        else
        {
            ClearAffliction(health, AffRespawnReady);
            ClearAffliction(health, AffRespawnCd);
        }

        chips.TryGetValue(ChipTeleport, out Item? teleportChip);
        if (teleportChip != null)
        {
            float ratio = CooldownRatio(TeleportCooldown, teleportChip, TeleportCooldownSeconds);
            if (ratio > 0f)
            {
                ClearAffliction(health, AffTeleportReady);
                SetAfflictionStrength(character, AffTeleportCd, ratio * 100f);
            }
            else
            {
                ClearAffliction(health, AffTeleportCd);
                ApplyFull(character, AffTeleportReady);
            }
        }
        else
        {
            ClearAffliction(health, AffTeleportReady);
            ClearAffliction(health, AffTeleportCd);
        }
    }

    private static void ClearChipStatus(Character character)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        for (int i = 0; i < ChipStatusAfflictions.Length; i++)
        {
            ClearAffliction(health, ChipStatusAfflictions[i]);
        }
    }
}
