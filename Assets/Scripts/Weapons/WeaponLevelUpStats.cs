using UnityEngine;

public enum WeaponFireType
{
    Projectile,
    Raycast
}

[System.Serializable]
public class WeaponLevelUpStats
{
    [Header("Mejoras activas en este nivel")]
    public bool improveDamage;
    public bool improveBulletSpeed;
    public bool improveRateOfFire;
    public bool improveMagazine;
    public bool improveReserve;
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/New Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identidad")]
    public string weaponName;
    public WeaponFireType fireType;
    public WeaponData nextEvolution;

    [Header("Stats base")]
    public float baseDamage = 10f;
    public float baseBulletSpeed = 25f;
    public bool isAutomatic = false;
    public float baseRateOfFire = 0.25f;
    public int baseMagazineSize = 20;
    public int baseReserveAmmo = 60;

    [Header("Stats Raycast")]
    public float raycastRange = 50f;

    [Header("Mejoras por nivel (hasta 10)")]
    public WeaponLevelUpStats[] levels = new WeaponLevelUpStats[10];

    [Header("Multiplicadores por nivel (1 = sin cambio)")]
    public float[] levelMultipliers;  // Ej: nivel 1→1.0 | nivel 2→1.12 | nivel 3→1.25…

    [Header("Experiencia")]
    public int baseExpToNextLevel = 100;
    public float xpIncreasePercent = 0.20f;  // +20% por nivel


    // ===========================================================
    // GETTERS de stats con multiplicadores personalizados
    // ===========================================================

    private float GetMultiplierForLevel(int level)
    {
        if (levelMultipliers == null) return 1f;
        if (level - 1 < 0 || level - 1 >= levelMultipliers.Length) return 1f;
        return levelMultipliers[level - 1];
    }

    public float GetDamage(int level)
    {
        float value = baseDamage;
        float mult = GetMultiplierForLevel(level);

        for (int i = 0; i < level - 1 && i < levels.Length; i++)
            if (levels[i].improveDamage)
                value *= mult;

        return value;
    }

    public float GetBulletSpeed(int level)
    {
        float value = baseBulletSpeed;
        float mult = GetMultiplierForLevel(level);

        for (int i = 0; i < level - 1 && i < levels.Length; i++)
            if (levels[i].improveBulletSpeed)
                value *= mult;

        return value;
    }

    public float GetRateOfFire(int level)
    {
        float value = baseRateOfFire;
        float mult = GetMultiplierForLevel(level);

        for (int i = 0; i < level - 1 && i < levels.Length; i++)
            if (levels[i].improveRateOfFire)
                value -= value /= mult;

        return value;
    }

    public int GetMagazineSize(int level)
    {
        float value = baseMagazineSize;
        float mult = GetMultiplierForLevel(level);

        for (int i = 0; i < level - 1 && i < levels.Length; i++)
            if (levels[i].improveMagazine)
                value *= mult;

        return Mathf.RoundToInt(value);
    }

    public int GetReserveAmmo(int level)
    {
        float value = baseReserveAmmo;
        float mult = GetMultiplierForLevel(level);

        for (int i = 0; i < level - 1 && i < levels.Length; i++)
            if (levels[i].improveReserve)
                value *= mult;

        return Mathf.RoundToInt(value);
    }


    // ===========================================================
    // Snapshot completo
    // ===========================================================

    public WeaponStatsSnapshot GetStatsSnapshot(int level)
    {
        return new WeaponStatsSnapshot
        {
            damage = GetDamage(level),
            bulletSpeed = GetBulletSpeed(level),
            isAutomatic = isAutomatic,
            rateOfFire = GetRateOfFire(level),
            magazineSize = GetMagazineSize(level),
            reserveAmmo = GetReserveAmmo(level),
            fireType = fireType,
            raycastRange = raycastRange
        };
    }
}

public struct WeaponStatsSnapshot
{
    public float damage;
    public float bulletSpeed;
    public bool isAutomatic;
    public float rateOfFire;
    public int magazineSize;
    public int reserveAmmo;
    public WeaponFireType fireType;
    public float raycastRange;
}
