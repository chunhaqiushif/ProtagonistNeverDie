using System;
using System.Collections.Generic;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace ProtagonistNeverDie;

/// <summary>
/// 传送芯片：濒死传送、尸体回收、落点选择与特效。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 传送
    // ---------------------------------------------------------------------
    private static bool TryTeleport(Character character, Item headset, Item chip)
    {
        if (!ChipUsable(chip)) return false;
        if (IsOnMainSub(character)) return false;
        if (InCooldown(TeleportCooldown, chip, TeleportCooldownSeconds)) return false;

        Item? point = FindNearestRespawnPoint(character);
        Vector2? destination = point != null ? point.WorldPosition : FindRandomSpawnPoint(character);
        if (destination == null) return false;

        TeleportCooldown[chip.ID] = _clock;

        // 判定触发后立刻锁定：delay 期间玩家无法把芯片卸下
        LockChip(chip);

        DamageChip(chip, character, TeleportChipLoss, TeleportChipLossBot);

        PlayTeleportFx(character, headset);
        Vector2 target = destination.Value;
        Schedule(TeleportDelayMs, () =>
        {
            try { if (character != null && !character.Removed) character.TeleportTo(target); }
            catch (Exception e) { LogError(e); }
            PlayTeleportFx(character!, headset);
            // 芯片销毁统一由「耐久归零 → 计时」处理，这里不再介入
        });

        return true;
    }

    private static void TryTeleportCorpse(Character character)
    {
        if (IsOnMainSub(character)) return;

        CharacterInventory inventory = character.Inventory;
        if (inventory == null) return;

        Item headset = inventory.FindEquippedItemByTag(Tag);
        if (headset == null) return;

        if (!CollectChips(headset).TryGetValue(ChipTeleport, out Item? chip)) return;

        TryTeleport(character, headset, chip);
    }

    private static void PlayTeleportFx(Character character, Item headset)
    {
        if (headset == null || character == null) return;
        try
        {
            headset.ApplyStatusEffects(ActionType.OnUse, 0f, character, null, null, true, character.WorldPosition);
        }
        catch (Exception e) { LogError(e); }
    }

    private static Item? FindNearestRespawnPoint(Character character)
    {
        List<Item> list = Item.ItemList;
        if (list == null) return null;

        Vector2 position = character.WorldPosition;
        Item? best = null;
        float bestDistance = float.MaxValue;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            Item item = list[i];
            if (item == null || item.Removed) continue;
            if (item.Prefab == null || item.Prefab.Identifier != RespawnPointId) continue;

            float distance = Vector2.DistanceSquared(position, item.WorldPosition);
            if (distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Vector2? FindRandomSpawnPoint(Character character)
    {
        List<WayPoint> list = WayPoint.WayPointList;
        if (list == null) return null;

        Submarine? submarine = Submarine.MainSub ?? character.Submarine;
        bool filterSub = submarine != null;
        ushort subId = submarine?.ID ?? 0;

        List<Vector2> candidates = new List<Vector2>();
        for (int i = 0; i < list.Count; i++)
        {
            WayPoint waypoint = list[i];
            if (waypoint == null) continue;
            if (waypoint.SpawnType != SpawnType.Human) continue;

            if (filterSub)
            {
                Submarine wpSub = waypoint.Submarine;
                if (wpSub != null && wpSub.ID != subId) continue;
            }

            candidates.Add(waypoint.WorldPosition);
        }

        if (candidates.Count == 0) return null;
        return candidates[Rng.Next(candidates.Count)];
    }

    /// <summary>是否待在自己的主潜艇里（只认 Submarine.MainSub）。</summary>
    private static bool IsOnMainSub(Character character)
    {
        Submarine sub = character.Submarine;
        Submarine main = Submarine.MainSub;
        if (sub == null || main == null) return false;
        return sub.ID == main.ID;
    }
}
