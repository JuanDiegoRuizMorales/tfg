using Nekalypse.Manager;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class BaseWeaponController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private InputManager _inputManager; // ← se inyecta desde fuera
    [SerializeField] private GameObject _bulletPrefab;

    // Múltiples orígenes de disparo
    public Transform bulletOrigin;
    [SerializeField] private Transform[] bulletOrigins;

    [Header("Weapon Data (Stats por nivel)")]
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData; // ← EXPUESTO para identidad

    private WeaponStatsSnapshot stats;

    [Header("Fire settings")]
    [SerializeField] private float _rateOfFire = 0.2f;
    private float _shootCooldown = 0f;
    private bool _canShoot = true;

    [Header("Weapon identity")]
    public string weaponName = "Magic Orb";
    public int weaponLevel = 1;
    public int evolutionStage = 0; // 0 a 3

    [Header("Experience")]
    public int exp;
    public int expToNextLevel = 100;

    [Tooltip("Nivel máximo antes de necesitar evolucionar.")]
    [SerializeField] private int levelCapBeforeEvolution = 10;

    [Header("ammo settings")]
    public int magazineSize = 20;
    public int startingReserve = 60;

    [Header("Debug ammo settings")]
    public int currentAmmo;
    public int reserveAmmo;

    [Header("Auto reload")]
    public float lowAmmoPercent = 0.2f;
    [SerializeField] private float reloadTime = 1.2f;
    [SerializeField] private bool useAnimatorReload = false;
    [SerializeField] private Animator reloadAnimator;
    [SerializeField] private string reloadTrigger = "Reload";

    [Header("Reload fx")]
    [SerializeField] private ParticleSystem reloadParticles;

    [Header("Weapon orb visual")]
    [SerializeField] private Transform orbVisualRoot;
    private Vector3 orbOriginalScale;
    [SerializeField] private float orbScaleLerpSpeed = 12f;
    [SerializeField]
    [Tooltip("Escala mínima proporcional cuando hay 0 munición")]
    private float orbMinScalePercent = 0.25f;

    [Header("Ammo Visual Feedback")]
    [SerializeField] private Light ammoLight;
    [SerializeField] private VisualEffect ammoVFX;
    [SerializeField] private float lightFadeSpeed = 5f;
    private int originalParticleAmount;
    private Coroutine lightFadeRoutine;

    // Eventos UI
    public event Action<string, int, int, int, int> OnAmmoChanged;
    public event Action<int, int, int, int> OnProgressChanged;

    private bool _isReloading = false;

    // Propiedades usadas por la UI
    public string WeaponName => weaponName;
    public int WeaponLevel => weaponLevel;
    public int EvolutionStage => evolutionStage;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public int ReserveAmmo => reserveAmmo;
    public int LowAmmoThreshold => Mathf.CeilToInt(magazineSize * lowAmmoPercent);

    // Estado de nivel / evolución
    public bool IsAtLevelCap => weaponLevel >= levelCapBeforeEvolution;
    public bool CanEvolve => IsAtLevelCap && weaponData != null && weaponData.nextEvolution != null;

    // Setters para inyección de dependencias
    public void SetInput(InputManager mgr) => _inputManager = mgr;

    public void SetBulletOrigins(Transform[] origins)
    {
        bulletOrigins = origins;
        if ((bulletOrigin == null || bulletOrigin == transform) && origins != null && origins.Length > 0)
            bulletOrigin = origins[0];
    }

    private void Start()
    {
        // Munición inicial
        currentAmmo = magazineSize;
        reserveAmmo = startingReserve;

        // Cargar stats desde WeaponData
        if (weaponData != null)
        {
            ApplyWeaponStats();
            RecalculateExpToNextLevel(); // importante: coste inicial según nivel y WeaponData
        }
        else
        {
            // fallback seguro
            expToNextLevel = Mathf.Max(1, expToNextLevel);
        }

        // Visual orbe
        if (orbVisualRoot != null) orbOriginalScale = orbVisualScaleSafe(orbVisualRoot.localScale);
        else orbOriginalScale = Vector3.one;

        if (ammoVFX != null)
            originalParticleAmount = ammoVFX.GetInt("particleAmmount");

        ApplyOrbScaleInstant();

        // Primer pintado de UI
        NotifyAmmo();
        NotifyProgress();
    }

    private void Update()
    {
        HandleShootCooldown();
        HandleShootInput();
        HandleManualReload();
        LerpOrbScale();
    }

    // =========================
    // Stats / identidad
    // =========================
    private void ApplyWeaponStats()
    {
        stats = weaponData.GetStatsSnapshot(weaponLevel);

        // La identidad viene del WeaponData
        weaponName = weaponData.weaponName;
        _rateOfFire = stats.rateOfFire;
        magazineSize = stats.magazineSize;
        startingReserve = stats.reserveAmmo;
    }

    private void RecalculateExpToNextLevel()
    {
        if (weaponData == null)
        {
            expToNextLevel = Mathf.Max(1, expToNextLevel);
            return;
        }

        // coste = base * (1 + xpIncreasePercent)^(nivel - 1)
        float baseExp = Mathf.Max(1, weaponData.baseExpToNextLevel);
        float inc = Mathf.Max(0f, weaponData.xpIncreasePercent);
        float factor = Mathf.Pow(1f + inc, Mathf.Max(0, weaponLevel - 1));
        expToNextLevel = Mathf.Max(1, Mathf.RoundToInt(baseExp * factor));
    }

    // =========================
    // Disparo
    // =========================
    private void HandleShootCooldown()
    {
        if (!_canShoot)
        {
            _shootCooldown -= Time.deltaTime;
            if (_shootCooldown <= 0f) _canShoot = true;
        }
    }

    private void HandleShootInput()
    {
        if (_inputManager == null) return;

        bool wantsShoot = stats.isAutomatic ? _inputManager.shoot : _inputManager.shootPressed;
        if (wantsShoot && _canShoot && !_isReloading)
            TryShoot();
    }

    private void TryShoot()
    {
        if (currentAmmo <= 0)
        {
            TryAutoReload();
            return;
        }

        _canShoot = false;
        _shootCooldown = _rateOfFire;

        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        NotifyAmmo();

        if (weaponData != null && weaponData.fireType == WeaponFireType.Raycast)
        {
            FireRaycast();
        }
        else
        {
            if (bulletOrigins != null && bulletOrigins.Length > 0)
            {
                foreach (var origin in bulletOrigins)
                    if (origin != null) FireProjectile(origin);
            }
            else if (bulletOrigin != null)
            {
                FireProjectile(bulletOrigin);
            }
        }

        if (currentAmmo <= 0)
            TryAutoReload();
    }

    private void FireProjectile(Transform origin)
    {
        GameObject bullet = BulletPool.Instance.RequestBasicBullet();
        if (bullet != null)
        {
            bullet.transform.position = origin.position;
            bullet.transform.rotation = origin.rotation;

            var projectile = bullet.GetComponent<IWeaponProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(this, stats.damage, stats.bulletSpeed);
            }
            else
            {
                var basic = bullet.GetComponent<BasicBullet>();
                if (basic != null)
                {
                    basic.ownerWeapon = this;
                    basic.damage = stats.damage;
                }
            }
        }
    }

    private void FireRaycast()
    {
        if (bulletOrigin == null) return;

        if (Physics.Raycast(bulletOrigin.position, bulletOrigin.forward, out RaycastHit hit, stats.raycastRange))
        {
            var z = hit.collider.GetComponent<ZombieBasicController>();
            if (z != null) z.TakeDamage(stats.damage, this);
        }
    }

    // =========================
    // Recarga
    // =========================
    private void HandleManualReload()
    {
        if (_inputManager != null && _inputManager.reloadPressed)
            ManualReload();
    }

    public void ManualReload()
    {
        if (_isReloading) return;
        if (currentAmmo >= magazineSize) return;
        if (reserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    private void TryAutoReload()
    {
        if (_isReloading) return;
        if (reserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;

        if (reloadParticles != null) reloadParticles.Play();

        if (useAnimatorReload && reloadAnimator != null)
        {
            reloadAnimator.SetTrigger(reloadTrigger);
            yield return new WaitForSeconds(reloadTime);
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        int needed = magazineSize - currentAmmo;
        int toLoad = Mathf.Clamp(needed, 0, reserveAmmo);

        currentAmmo += toLoad;
        reserveAmmo -= toLoad;

        _isReloading = false;
        NotifyAmmo();
    }

    // =========================
    // EXP / Nivel
    // =========================
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;

        // Si está en el tope, NO subir más. Conserva exp pero clamp al coste-1 para evitar overflow.
        if (IsAtLevelCap)
        {
            RecalculateExpToNextLevel();
            exp = Mathf.Min(exp, Mathf.Max(0, expToNextLevel - 1));
            NotifyProgress();
            return;
        }

        // Subidas encadenadas mientras no alcance el tope
        while (exp >= expToNextLevel && !IsAtLevelCap)
        {
            exp -= expToNextLevel;
            weaponLevel++;

            // Si al subir alcanza el tope, frena aquí.
            if (IsAtLevelCap)
            {
                // No perder exp; clamp para no superar el coste actual
                RecalculateExpToNextLevel();
                exp = Mathf.Min(exp, Mathf.Max(0, expToNextLevel - 1));
                break;
            }

            // Recalcula stats y el próximo coste
            if (weaponData != null) ApplyWeaponStats();
            RecalculateExpToNextLevel();
        }

        // Si no subió de nivel pero rebasamos expToNextLevel por alguna edición manual
        if (exp >= expToNextLevel && IsAtLevelCap)
            exp = Mathf.Min(exp, Mathf.Max(0, expToNextLevel - 1));

        NotifyProgress();
    }

    /// <summary>
    /// Evoluciona el arma a su siguiente dato si existe.
    /// Reinicia nivel a 1 y recalcula stats/exp.
    /// No instancia prefab nuevo; asume que la evolución es lógica/visual en el mismo GO.
    /// </summary>
    public void EvolveWeapon()
    {
        if (!CanEvolve) return;

        weaponData = weaponData.nextEvolution;
        evolutionStage++;
        weaponLevel = 1;

        // Reaplicar identidad y stats
        ApplyWeaponStats();

        // Opcional: rellenar munición al evolucionar
        currentAmmo = magazineSize;
        reserveAmmo = startingReserve;

        // Recalcular coste de XP para el nuevo árbol
        RecalculateExpToNextLevel();

        NotifyAmmo();
        NotifyProgress();
    }

    // =========================
    // Feedback de ammo
    // =========================
    private void UpdateAmmoVisuals()
    {
        bool hasAmmo = currentAmmo > 0;

        if (ammoVFX != null)
        {
            int target = hasAmmo ? originalParticleAmount : 0;
            ammoVFX.SetInt("particleAmmount", target);
        }

        if (ammoLight != null)
        {
            float targetIntensity = hasAmmo ? 1f : 0f;

            if (lightFadeRoutine != null) StopCoroutine(lightFadeRoutine);
            lightFadeRoutine = StartCoroutine(FadeLightRoutine(targetIntensity));
        }
    }

    private IEnumerator FadeLightRoutine(float target)
    {
        float start = ammoLight != null ? ammoLight.intensity : 0f;

        while (ammoLight != null && !Mathf.Approximately(ammoLight.intensity, target))
        {
            ammoLight.intensity = Mathf.MoveTowards(ammoLight.intensity, target, Time.deltaTime * lightFadeSpeed);
            yield return null;
        }
    }

    // =========================
    // Escala orbe
    // =========================
    private void LerpOrbScale()
    {
        if (orbVisualRoot == null) return;

        float t = (float)currentAmmo / magazineSize;
        float minPercent = Mathf.Clamp01(orbMinScalePercent);
        float finalPercent = Mathf.Lerp(minPercent, 1f, t);

        Vector3 target = orbOriginalScale * finalPercent;
        orbVisualRoot.localScale = Vector3.Lerp(orbVisualRoot.localScale, target, Time.deltaTime * orbScaleLerpSpeed);
    }

    private void ApplyOrbScaleInstant()
    {
        if (orbVisualRoot == null) return;

        float t = (float)currentAmmo / magazineSize;
        float minPercent = Mathf.Clamp01(orbMinScalePercent);
        float finalPercent = Mathf.Lerp(minPercent, 1f, t);
        orbVisualRoot.localScale = orbOriginalScale * finalPercent;
    }

    private static Vector3 orbVisualScaleSafe(Vector3 v)
    {
        if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) return Vector3.one;
        return v;
    }

    // =========================
    // Eventos UI
    // =========================
    private void NotifyAmmo()
    {
        OnAmmoChanged?.Invoke(weaponName, weaponLevel, currentAmmo, magazineSize, reserveAmmo);
        UpdateAmmoVisuals();
    }

    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(weaponLevel, exp, expToNextLevel, evolutionStage);
    }

    // Para refill desde la tienda
    public void ForceAmmoFull()
    {
        currentAmmo = magazineSize;
        reserveAmmo = startingReserve;
        NotifyAmmo();
    }
}
