using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 增益芯片与免死芯片提供的常驻祝福（幂等施加 / 摘下移除）。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 芯片提供的常驻增益
    // ---------------------------------------------------------------------
    private static void ApplyPermanentBuffs(Character character)
    {
        if (character == null) return;
        BuffChipHolders[character.ID] = true;
        if (GetStrength(character, AffDrag) < AffFullThreshold) ApplyFull(character, AffDrag);
    }

    private static void RemovePermanentBuffs(Character character)
    {
        if (character == null) return;
        BuffChipHolders.Remove(character.ID);
        RemoveAfflictionByIdentifier(character.CharacterHealth, AffDrag);
    }

    private static void ApplyImmortalBuffs(Character character)
    {
        if (character == null) return;
        ImmortalChipHolders[character.ID] = true;

        // 奇迹进行中只给友方；强度不足时补满
        if (character.TeamID == CharacterTeamType.Team1
            && GetStrength(character, AffMiracle) < AffFullThreshold)
        {
            ApplyFull(character, AffMiracle);
        }
    }

    private static void RemoveImmortalBuffs(Character character)
    {
        if (character == null) return;
        ImmortalChipHolders.Remove(character.ID);

        CharacterHealth health = character.CharacterHealth;
        RemoveAfflictionByIdentifier(health, AffMiracle);
        RemoveAfflictionByIdentifier(health, AffPressure);
    }
}
