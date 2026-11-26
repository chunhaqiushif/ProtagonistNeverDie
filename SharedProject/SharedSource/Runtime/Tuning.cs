namespace ProtagonistNeverDie;

/// <summary>
/// 结算用固定参数（阈值、冷却、耐久消耗）。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 可调参数
    // ---------------------------------------------------------------------
    private const float TickInterval = 0.1f;          // 结算间隔（秒）
    private const float MaxStep = 0.5f;               // 单次补偿上限，防卡顿后跳帧

    private const float PressureThreshold = 80f;      // 压力保护低于多少米触发水压奇迹
    private const float HuskInfectionLimit = 75f;     // 画皮感染强度达到该值后不再保护
    private const float PressureReducePerTick = 100f; // 已有抗压能力时每 tick 削减的水压奇迹强度
    private const float BotHealPerSecond = 2f;        // NPC 自愈速度（当前停用）

    private const float DeathThreshold = -95f;        // 没被任何芯片救回来时吃「主角的代价」
    private const float TeleportThreshold = -50f;     // 传送芯片触发阈值（生命力百分比）
    private const float RespawnThreshold = -65f;      // 复生芯片触发阈值（生命力百分比）

    // 冷却与耐久消耗
    private const float TeleportCooldownSeconds = 60f;   // 传送芯片冷却（秒）
    private const float RespawnCooldownSeconds = 120f;   // 复生芯片冷却（秒）
    private const float TeleportChipLoss = 0.15f;        // 传送芯片耐久消耗（玩家）
    private const float TeleportChipLossBot = 0.05f;     // 传送芯片耐久消耗（NPC）
    private const float RespawnChipLoss = 0.25f;         // 复生芯片耐久消耗（玩家）
    private const float RespawnChipLossBot = 0.10f;      // 复生芯片耐久消耗（NPC）

    private const float HealPerSecond = 15f;          // 复生祝福每秒治疗的病症强度
    private const float PenaltyReducePerSecond = 10f; // 复生祝福每秒削掉的「主角的代价」
    private const int TeleportDelayMs = 200;          // 传送前播特效的提前量
    private const int BlessingDelayMs = 2660;         // 除颤音效到回血生效的间隔

    private const float AffFullThreshold = 0.999f;    // 常驻 buff 低于该强度视为「未满」，下个 tick 补满
    private const int ChipDestroyDelayMs = 5000;      // 耐久归零 → 锁定，N 秒后销毁
    private const int ChipLockDurationMs = 5000;      // 芯片触发后的锁定时间（期间无法从耳机卸下）
}
