using System.Collections.Generic;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 冷却查询与进度换算（按物品 ID + 全局未暂停时钟记录）。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 冷却
    // ---------------------------------------------------------------------
    private static bool InCooldown(Dictionary<ushort, double> records, Item chip, float seconds)
    {
        if (chip == null) return false;
        return records.TryGetValue(chip.ID, out double last) && _clock - last < seconds;
    }

    private static float CooldownRatio(Dictionary<ushort, double> records, Item chip, float seconds)
    {
        if (chip == null || seconds <= 0f) return 0f;
        if (!records.TryGetValue(chip.ID, out double last)) return 0f;

        double remaining = seconds - (_clock - last);
        if (remaining <= 0.0)
        {
            records.Remove(chip.ID);
            return 0f;
        }
        return (float)(remaining / seconds);
    }
}
