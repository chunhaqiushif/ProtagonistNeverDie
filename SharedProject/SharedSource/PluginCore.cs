using System;
using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Networking;

namespace ProtagonistNeverDie;

/// <summary>
/// 逐帧入口与单角色结算主流程。其余职责按文件拆在 Runtime/ 下（同一 partial 类）。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 每帧入口
    // ---------------------------------------------------------------------
    public static void Tick()
    {
        try
        {
            if (!ShouldRun()) return;

            double now = Timing.TotalTimeUnpaused;

            // 首帧 / 时间回退（读档、重载）时 dt 记 0
            double delta = _lastClock < 0 ? 0 : now - _lastClock;
            _lastClock = now;
            if (delta < 0) delta = 0;
            float deltaTime = (float)delta;

            _clock = now; // 冷却 / 延迟动作统一用这个未暂停时钟

            RunScheduled();

            _elapsed += deltaTime;
            if (_elapsed < TickInterval) return;

            float step = _elapsed;
            _elapsed = 0f;
            if (step > MaxStep) step = MaxStep;

            List<Character> characters = Character.CharacterList;
            if (characters == null) return;

            // 倒序遍历：允许中途移除
            for (int i = characters.Count - 1; i >= 0; i--)
            {
                try { UpdateCharacter(characters[i], step); }
                catch (Exception e) { LogError(e); }
            }
        }
        catch (Exception e)
        {
            Plugin.LogError("Logic error: " + e);
        }
    }

    /// <summary>单机/主机/客机结算方式区分，客机客户端交给服务端同步</summary>
    private static bool ShouldRun()
    {
        NetworkMember net = GameMain.NetworkMember;
        if (net == null) return true;   // 单机
        return net.IsServer;            // 主机 / 专用服务器
    }

    private static void RunScheduled()
    {
        for (int i = Scheduled.Count - 1; i >= 0; i--)
        {
            if (Scheduled[i].Due > _clock) continue;
            Action action = Scheduled[i].Action;
            Scheduled.RemoveAt(i);
            try { action(); }
            catch (Exception e) { LogError(e); }
        }
    }

    private static void Schedule(int delayMs, Action action)
    {
        Scheduled.Add(new ScheduledAction { Due = _clock + delayMs / 1000.0, Action = action });
    }

    private static void LogError(Exception e)
    {
        if (_errorCount >= 5) return;
        _errorCount++;
        Plugin.LogError("Logic error: " + e);
    }

    // ---------------------------------------------------------------------
    // 单角色结算
    // ---------------------------------------------------------------------
    private static void UpdateCharacter(Character character, float dt)
    {
        if (character == null || character.Removed) return;

        // 尸体：只保留「传送芯片把尸体送回船上」
        if (character.IsDead)
        {
            TryTeleportCorpse(character);
            return;
        }

        CharacterInventory inventory = character.Inventory;
        if (inventory == null) return;

        Item headset = inventory.FindEquippedItemByTag(Tag);
        if (headset == null)
        {
            if (BuffChipHolders.ContainsKey(character.ID)) RemovePermanentBuffs(character);
            if (ImmortalChipHolders.ContainsKey(character.ID)) RemoveImmortalBuffs(character);
            if (MaxHealthChipHolders.Remove(character.ID)) RemoveAfflictionByIdentifier(character.CharacterHealth, AffMaxHealthBuff);
            ClearChipStatus(character);
            // 已经生效的复生祝福不因摘耳机而中断
            TickBlessingHeal(character, dt);
            return;
        }

        CharacterHealth health = character.CharacterHealth;
        if (health == null) return;

        Dictionary<Identifier, Item> chips = CollectChips(headset);
        chips.TryGetValue(ChipRespawn, out Item? respawnChip);
        chips.TryGetValue(ChipTeleport, out Item? teleportChip);
        chips.TryGetValue(ChipBuff, out Item? buffChip);
        chips.TryGetValue(ChipImmortal, out Item? immortalChip);
        chips.TryGetValue(ChipMaxHealth, out Item? maxHealthChip);

        // 1) 增益芯片：全速拖拽
        if (buffChip != null)
        {
            ApplyPermanentBuffs(character);
        }
        else if (BuffChipHolders.ContainsKey(character.ID))
        {
            RemovePermanentBuffs(character);
        }

        // 1.5) 免死芯片：奇迹进行中 + 水压奇迹
        if (immortalChip != null)
        {
            ApplyImmortalBuffs(character);

            bool immuneToPressure = character.IsImmuneToPressure;
            if (!immuneToPressure
                && character.InPressure
                && character.PressureProtection < PressureThreshold
                && GetStrength(character, AffSymbiosis) < 1f
                && GetStrength(character, AffHusk) < HuskInfectionLimit
                && !character.IsProtectedFromPressure)
            {
                ApplyFull(character, AffPressure);
            }

            if (immuneToPressure) ReduceStrength(character, AffPressure, PressureReducePerTick);
        }
        else if (ImmortalChipHolders.ContainsKey(character.ID))
        {
            RemoveImmortalBuffs(character);
        }

        // 1.6) 生命芯片：最大生命增加
        if (maxHealthChip != null)
        {
            MaxHealthChipHolders[character.ID] = true;
            if (GetStrength(character, AffMaxHealthBuff) < AffFullThreshold)
            {
                ApplyFull(character, AffMaxHealthBuff);
            }
        }
        else if (MaxHealthChipHolders.Remove(character.ID))
        {
            RemoveAfflictionByIdentifier(health, AffMaxHealthBuff);
        }

        // 2) NPC 自愈（停用）
        // if (character.IsBot && health.HealthPercentage > 80f && health.HealthPercentage < 100f)
        //     HealDamage(character, BotHealPerSecond * dt);

        // 3) 濒死处理
        bool reviving = HasAffliction(character, AffBlessing);

        // 拔掉复生芯片：除颤等待阶段拔芯片不在触发回血，已生效的不管
        if (respawnChip == null) PendingBless.Remove(character.ID);

        if (!reviving && !PendingBless.ContainsKey(character.ID))
        {
            float vitality = VitalityPercent(character);

            // 3.1) 传送芯片
            if (teleportChip != null && vitality <= TeleportThreshold)
            {
                bool teleported = TryTeleport(character, headset, teleportChip);

                // 传送生效时，如果有复生芯片则直接触发复生（两颗芯片各扣一次耐久）
                if (teleported && respawnChip != null) 
                    TryBless(character, headset, respawnChip); 
            }

            // 3.2) 复生芯片
            if (respawnChip != null && vitality <= RespawnThreshold)
            {
                bool blessed = TryBless(character, headset, respawnChip);

                // 复生生效时，如果有传送芯片则直接触发传送（两颗芯片各扣一次耐久）
                if (blessed && teleportChip != null)
                    TryTeleport(character, headset, teleportChip);
            }

            // 3.3) 血量已小于等于阈值：吃代价
            if (vitality <= DeathThreshold)
            {
                ApplyFull(character, AffReaper);
                ApplyFull(character, AffIgnored);
            }
        }

        // 3.5) 复生祝福持续治疗
        reviving = TickBlessingHeal(character, dt);

        // 4) 芯片状态图标
        UpdateChipStatus(character, chips, reviving, health);

        // 5) 耐久归零的芯片：锁定 + 结算完成后的销毁 / 兜底清理
        MaintainDepletedChips(character, chips);
    }
}
