using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BookWeaponDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WeaponInventoryManager weaponInventory;

    [Header("UI básica")]
    [SerializeField] private TextMeshProUGUI weaponNameAndLevelTMP;
    [SerializeField] private Slider weaponXpSlider;
    [SerializeField] private TextMeshProUGUI evolutionTMP;

    [Header("UI munición")]
    [SerializeField] private TextMeshProUGUI clipTMP;
    [SerializeField] private TextMeshProUGUI reserveTMP;
    [SerializeField] private TextMeshProUGUI ammoWarningTMP;

    [Header("Colores aviso")]
    [SerializeField] private Color lowAmmoColor = new Color(1f, 0.92f, 0.16f);
    [SerializeField] private Color noAmmoColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private string lowAmmoText = "LOW AMMO";
    [SerializeField] private string noAmmoText = "NO AMMO";

    private BaseWeaponController weapon;

    private void Awake()
    {
        if (weaponInventory == null)
            weaponInventory = FindAnyObjectByType<WeaponInventoryManager>();
    }

    private void OnEnable()
    {
        if (weaponInventory != null)
        {
            weaponInventory.OnWeaponChanged += OnWeaponSwitched;
            OnWeaponSwitched(weaponInventory.currentWeapon);
        }
    }

    private void OnDisable()
    {
        if (weaponInventory != null)
            weaponInventory.OnWeaponChanged -= OnWeaponSwitched;

        if (weapon != null)
            UnhookWeaponEvents(weapon);
    }

    private void OnWeaponSwitched(BaseWeaponController newWeapon)
    {
        if (weapon != null) UnhookWeaponEvents(weapon);

        weapon = newWeapon;

        if (weapon != null) HookWeaponEvents(weapon);

        RefreshAll();
    }

    private void HookWeaponEvents(BaseWeaponController w)
    {
        w.OnAmmoChanged += OnAmmoChanged;
        w.OnProgressChanged += OnProgressChanged;
    }

    private void UnhookWeaponEvents(BaseWeaponController w)
    {
        w.OnAmmoChanged -= OnAmmoChanged;
        w.OnProgressChanged -= OnProgressChanged;
    }

    private void RefreshAll()
    {
        if (weapon == null) return;

        OnAmmoChanged(
            weapon.weaponName,
            weapon.weaponLevel,
            weapon.currentAmmo,
            weapon.magazineSize,
            weapon.reserveAmmo
        );

        OnProgressChanged(
            weapon.weaponLevel,
            weapon.exp,
            weapon.expToNextLevel,
            weapon.evolutionStage
        );
    }

    private void OnAmmoChanged(string wName, int wLevel, int current, int mag, int reserve)
    {
        if (weaponNameAndLevelTMP != null)
            weaponNameAndLevelTMP.text = $"{wName}  Lvl {wLevel}";

        if (clipTMP != null) clipTMP.text = current.ToString();
        if (reserveTMP != null) reserveTMP.text = reserve.ToString();

        UpdateAmmoWarning();
    }

    private void OnProgressChanged(int lvl, int exp, int expToNext, int evo)
    {
        if (weaponXpSlider != null)
        {
            weaponXpSlider.minValue = 0;
            weaponXpSlider.maxValue = Mathf.Max(1, expToNext);
            weaponXpSlider.value = Mathf.Clamp(exp, 0, weaponXpSlider.maxValue);
        }

        if (evolutionTMP != null)
            evolutionTMP.text = $"Evo stage: {evo}";
    }

    private void UpdateAmmoWarning()
    {
        if (ammoWarningTMP == null || weapon == null) return;

        int totalAmmo = weapon.currentAmmo + weapon.reserveAmmo;

        if (totalAmmo <= 0)
        {
            ammoWarningTMP.text = noAmmoText;
            ammoWarningTMP.color = noAmmoColor;
            ammoWarningTMP.enabled = true;
        }
        else if (totalAmmo <= Mathf.CeilToInt(weapon.magazineSize * weapon.lowAmmoPercent))
        {
            ammoWarningTMP.text = lowAmmoText;
            ammoWarningTMP.color = lowAmmoColor;
            ammoWarningTMP.enabled = true;
        }
        else
        {
            ammoWarningTMP.enabled = false;
        }
    }
}
