using System;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 复生芯片：除颤调度、复生祝福的持续治疗。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 复生祝福治疗
    // ---------------------------------------------------------------------
    private static bool TickBlessingHeal(Character character, float dt)
    {
        if (character == null || character.Removed || character.IsDead) return false;

        CharacterHealth health = character.CharacterHealth;
        if (health == null) return false;

        Affliction blessing = health.GetAffliction(AffBlessing, true);
        if (blessing == null || blessing.Strength <= 0f) return false;

        ReduceStrength(character, AffReaper, PenaltyReducePerSecond * dt);
        CleanseAfflictions(character, HealPerSecond * dt);
        return true;
    }

    // ---------------------------------------------------------------------
    // 复生
    // ---------------------------------------------------------------------
    private static bool TryBless(Character character, Item headset, Item chip)
    {
        if (!ChipUsable(chip)) return false;

        CharacterHealth health = character.CharacterHealth;
        if (health == null) return false;

        if (HasAffliction(character, AffBlessing)) return false;
        if (PendingBless.ContainsKey(character.ID)) return false;
        if (InCooldown(ReviveCooldown, chip, RespawnCooldownSeconds)) return false;

        ReviveCooldown[chip.ID] = _clock;
        PendingBless[character.ID] = true;

        // 判定触发后立刻锁定：除颤期间玩家无法把芯片卸下
        LockChip(chip);

        DamageChip(chip, character, RespawnChipLoss, RespawnChipLossBot);

        PlayDefibSound(character, headset);
        Schedule(BlessingDelayMs, () =>
        {
            // 期间被拔芯片 / 取消：放弃
            if (!PendingBless.Remove(character.ID)) return;
            if (character == null || character.Removed || character.IsDead) return;
            ApplyFull(character, AffBlessing);
        });

        return true;
    }

    private static void PlayDefibSound(Character character, Item headset)
    {
        if (headset == null || character == null) return;
        try
        {
            headset.ApplyStatusEffects(ActionType.OnSecondaryUse, 0f, character, null, null, true, character.WorldPosition);
        }
        catch (Exception e) { LogError(e); }
    }
}
