using System;
using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Plugins;

namespace ProtagonistNeverDie;

/// <summary>
/// 芯片耐久：扣除、归零锁定、延迟销毁，以及阻止取出归零芯片的 Harmony 前缀。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 芯片耐久
    // ---------------------------------------------------------------------
    private static bool ChipUsable(Item chip)
    {
        if (chip == null || chip.Removed) return false;
        return chip.Condition > 0f;
    }

    private static void DamageChip(Item chip, Character character, float playerLoss, float botLoss)
    {
        if (chip == null) return;

        float maxCondition = chip.MaxCondition;
        if (maxCondition <= 0f) maxCondition = 100f;

        float loss = character != null && character.IsBot ? botLoss : playerLoss;
        float newCondition = chip.Condition - maxCondition * loss;
        if (newCondition < 0f) newCondition = 0f;

        chip.Condition = newCondition;

        if (chip.Condition <= 0f)
        {
            SetChipInteractable(chip, false);     // 立即锁定，玩家拿不走（含客户端同步）
            ScheduleChipDestroy(chip);            // 计时后移除物品
        }
    }

    /// <summary>
    /// 设置芯片是否可被玩家交互（锁定 / 解锁）。
    /// </summary>
    private static void SetChipInteractable(Item chip, bool interactable)
    {
        if (chip == null || chip.Removed) return;

        bool wantNonInteractable = !interactable;
        if (chip.NonInteractable == wantNonInteractable) return;   // 状态没变，不重复发包

        chip.NonInteractable = wantNonInteractable;

        try
        {
            if (GameMain.NetworkMember == null || !GameMain.NetworkMember.IsServer) return;   // 单机 / 纯客户端
            if (chip.SerializableProperties == null) return;
            if (!chip.SerializableProperties.TryGetValue(NonInteractableProperty, out SerializableProperty prop)) return;

            IGameNetwork gameNetwork = PluginServiceProvider.GetService<IGameNetwork>();
            gameNetwork?.CreateVanillaEntityEvent(chip, new Item.ChangePropertyEventData(prop, chip));
        }
        catch (Exception e) { LogError(e); }
    }

    /// <summary>NonInteractable 在 SerializableProperties 里的键。</summary>
    private static readonly Identifier NonInteractableProperty = new Identifier("NonInteractable");

    /// <summary>
    /// 触发瞬间锁定芯片：在效果 delay 计时开始之前调用，防止玩家在结算过程中把芯片卸下。
    /// 持续 <see cref="ChipLockDurationMs"/> 毫秒；到期后耐久仍为 0 的继续保持锁定，等销毁计时处理。
    /// </summary>
    private static void LockChip(Item chip)
    {
        if (chip == null || chip.Removed) return;

        SetChipInteractable(chip, false);

        if (!ChipLocked.Add(chip.ID)) return;   // 已在锁定期，不重复排程

        Schedule(ChipLockDurationMs, () =>
        {
            ChipLocked.Remove(chip.ID);
            if (chip == null || chip.Removed) return;

            // 耐久已归零的继续保持锁定（由销毁计时接手）
            SetChipInteractable(chip, chip.Condition > 0f);
        });
    }

    /// <summary>排入「N 秒后删除」的计时，同一颗芯片只排一次。</summary>
    private static void ScheduleChipDestroy(Item chip)
    {
        if (chip == null || chip.Removed) return;
        if (!DestroyScheduled.Add(chip.ID)) return;

        Schedule(ChipDestroyDelayMs, () =>
        {
            DestroyScheduled.Remove(chip.ID);

            // 计时期间被修理 / 耐久被改回正数：取消销毁
            if (chip.Condition > 0f) return;

            DestroyChip(chip);
        });
    }

    /// <summary>把芯片排进移除队列（实际删除发生在下一帧）。</summary>
    private static void DestroyChip(Item chip)
    {
        if (chip == null || chip.Removed || chip.IsInRemoveQueue) return;
        try { Entity.Spawner.AddItemToRemoveQueue(chip, null); }
        catch (Exception e) { LogError(e); }
    }

    /// <summary>
    /// 维护「耐久归零」芯片的状态：
    ///   * 归零 → 锁住（UI 层拿不走），并确保已排入销毁计时
    ///   * 被修理 / 外部改回正耐久 → 解锁
    ///   * 读档等场景下耐久已是 0 但没有计时器 → 这里补排
    /// </summary>
    private static void MaintainDepletedChips(Character character, Dictionary<Identifier, Item> chips)
    {
        foreach (Item chip in chips.Values)
        {
            if (chip == null) continue;

            // 锁定期内保持锁定，不让每 tick 的维护逻辑把它解锁
            if (ChipLocked.Contains(chip.ID))
            {
                SetChipInteractable(chip, false);
            }
            else
            {
                SetChipInteractable(chip, chip.Condition > 0f);
            }

            if (chip.Condition > 0f) continue;

            ScheduleChipDestroy(chip);
        }
    }

    /// <summary>
    /// Harmony Prefix：阻止把芯片从耳机里取出。
    /// 拦截条件：耐久已归零，或处于触发后的锁定期（这两种都不允许卸下）。
    /// 销毁队列中的、耳机本体正在拆解的，一律放行。
    /// </summary>
    internal static bool RemoveItemPrefix(Inventory __instance, Item item)
    {
        try
        {
            if (item == null || item.Removed || item.IsInRemoveQueue) return true;

            // 耐久正常且不在锁定期 → 不是我们要管的物品
            if (item.Condition > 0f && !ChipLocked.Contains(item.ID)) return true;

            ItemContainer container = (__instance as ItemInventory)?.Container;
            Item owner = container?.Item;
            if (owner == null || !owner.HasTag(Tag)) return true;

            // 耳机本体正在被销毁 / 拆解时放行，否则 0 耐久芯片会残留在被清空的容器里
            if (owner.Removed || owner.IsInRemoveQueue) return true;

            return false;   // 拒绝取出
        }
        catch
        {
            return true;
        }
    }
}
