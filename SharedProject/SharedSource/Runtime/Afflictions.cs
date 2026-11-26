using System;
using Barotrauma;

namespace ProtagonistNeverDie;

/// <summary>
/// 病症工具：prefab 取用、强度读写、整体清除、持续掉血类净化、生命力换算。
/// </summary>
internal static partial class PluginCore
{
    // ---------------------------------------------------------------------
    // 病症工具
    // ---------------------------------------------------------------------
    private static AfflictionPrefab? GetPrefab(Identifier identifier)
    {
        // 每次都从注册表取当前实例，再与缓存比对：
        //   * 早期调用可能取不到（加载顺序 / 重载），不能把 null 写进缓存，否则之后永远拿不到
        //   * 游戏内重载 mod 后注册表会重建，旧 prefab 实例 Instantiate 出的 affliction 与新注册表不匹配
        AfflictionPrefab? registered = null;
        try { registered = AfflictionPrefab.Prefabs[identifier]; }
        catch (Exception e) { LogError(e); }

        if (registered == null) return null;

        if (PrefabCache.TryGetValue(identifier, out AfflictionPrefab? cached)
            && ReferenceEquals(cached, registered))
        {
            return cached;
        }

        PrefabCache[identifier] = registered;
        return registered;
    }

    private static float GetStrength(Character character, Identifier identifier)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return 0f;
        return health.GetAfflictionStrengthByIdentifier(identifier, true);
    }

    private static bool HasAffliction(Character character, Identifier identifier)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return false;
        Affliction? affliction = health.GetAffliction(identifier, true);
        return affliction != null && affliction.Strength > 0f;
    }

    /// <summary>把病症拉满（强度 = prefab.MaxStrength）。</summary>
    private static void ApplyFull(Character character, Identifier identifier)
    {
        AfflictionPrefab? prefab = GetPrefab(identifier);
        if (prefab == null) return;

        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        Affliction? current = health.GetAffliction(identifier, true);
        if (current == null)
        {
            health.ApplyAffliction(null, prefab.Instantiate(prefab.MaxStrength, character));
        }
        else if (current.Strength < prefab.MaxStrength)
        {
            current.SetStrength(prefab.MaxStrength);
        }
    }

    private static void ReduceStrength(Character character, Identifier identifier, float amount)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        Affliction? current = health.GetAffliction(identifier, true);
        if (current == null) return;

        current.SetStrength(Math.Max(0f, current.Strength - amount));
    }

    private static void RemoveAfflictionByIdentifier(CharacterHealth health, Identifier identifier)
    {
        if (health == null) return;

        try { health.RemoveAfflictions(a => a != null && a.Identifier == identifier); }
        catch (Exception e) { LogError(e); }

        Affliction? current = health.GetAffliction(identifier, true);
        if (current != null && current.Strength > 0f) current.SetStrength(0f);
    }

    private static void ClearAffliction(CharacterHealth health, Identifier identifier)
    {
        if (health == null) return;
        if (health.GetAffliction(identifier, true) == null) return;
        RemoveAfflictionByIdentifier(health, identifier);
    }

    private static void SetAfflictionStrength(Character character, Identifier identifier, float value)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        if (value <= 0f)
        {
            ClearAffliction(health, identifier);
            return;
        }

        AfflictionPrefab? prefab = GetPrefab(identifier);
        if (prefab == null) return;

        Affliction? current = health.GetAffliction(identifier, true);
        if (current == null) health.ApplyAffliction(null, prefab.Instantiate(value, character));
        else current.SetStrength(value);
    }

    /// <summary>按类型削掉持续掉血的病症（复生祝福）。</summary>
    private static void CleanseAfflictions(Character character, float amount)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        foreach (Affliction? affliction in health.GetAllAfflictions())
        {
            if (affliction == null || affliction.Strength <= 0f) continue;

            AfflictionPrefab prefab = affliction.Prefab;
            if (prefab == null) continue;

            if (CleanseTypes.Contains(prefab.AfflictionType))
            {
                affliction.SetStrength(Math.Max(0f, affliction.Strength - amount));
            }
        }
    }

    /// <summary>削减所有 damage 类病症（NPC 自愈用，当前停用）。</summary>
    private static void HealDamage(Character character, float amount)
    {
        CharacterHealth? health = character?.CharacterHealth;
        if (health == null) return;

        Identifier damageType = new Identifier("damage");
        foreach (Affliction? affliction in health.GetAllAfflictions())
        {
            if (affliction?.Prefab == null) continue;
            if (affliction.Prefab.AfflictionType == damageType)
            {
                affliction.SetStrength(Math.Max(0f, affliction.Strength - amount));
            }
        }
    }

    private static float VitalityPercent(Character character)
    {
        float maxVitality = character.MaxVitality;
        if (maxVitality <= 0f) return 100f;
        return character.Vitality / maxVitality * 100f;
    }
}
