using Nekalypse.Manager;
using System;
using System.Collections;
using UnityEngine;

public class WeaponInventoryManager : MonoBehaviour
{
    public event Action<BaseWeaponController> OnWeaponChanged;

    [Header("Referencias")]
    [SerializeField] private InputManager input;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private Transform weaponHolder;

    [Header("Bullet Origins compartidos")]
    [SerializeField] private Transform[] sharedBulletOrigins;
    [SerializeField] private bool setPrimaryOriginFromList = true;

    [Header("Inventario (2 slots)")]
    public BaseWeaponController primaryWeapon;
    public BaseWeaponController secondaryWeapon;
    public BaseWeaponController currentWeapon;

    [Header("Animación cambio de arma")]
    public float switchDuration = 0.25f;
    public float weaponRaiseAmount = 1.0f;
    public AnimationCurve switchCurve;

    [Header("Costes")]
    public int ammoRefillCost = 500;

    private bool isSwitching = false;
    private Vector3 camOriginalLocalPos;

    private void Start()
    {
        if (input == null) input = GetComponent<InputManager>();
        if (weaponCamera != null) camOriginalLocalPos = weaponCamera.transform.localPosition;

        if (primaryWeapon != null)
        {
            InjectToWeapon(primaryWeapon);
            currentWeapon = primaryWeapon;
            primaryWeapon.gameObject.SetActive(true);
        }

        if (secondaryWeapon != null)
        {
            InjectToWeapon(secondaryWeapon);
            secondaryWeapon.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isSwitching || input == null) return;
        if (input.switchWeapon1) SwitchToSlot(1);
        if (input.switchWeapon2) SwitchToSlot(2);
    }

    // ===== Cambio de arma =====
    public void SwitchToSlot(int slot)
    {
        if (isSwitching) return;

        if (slot == 1 && primaryWeapon == null) return;
        if (slot == 2 && secondaryWeapon == null) return;

        BaseWeaponController newWeapon = (slot == 1) ? primaryWeapon : secondaryWeapon;
        if (newWeapon == currentWeapon) return;

        StartCoroutine(SwitchWeaponRoutine(newWeapon));
    }

    private IEnumerator SwitchWeaponRoutine(BaseWeaponController newWeapon)
    {
        isSwitching = true;

        float t = 0f;
        while (t < switchDuration)
        {
            t += Time.deltaTime;
            float p = (switchCurve != null) ? switchCurve.Evaluate(t / switchDuration) : t / switchDuration;
            if (weaponCamera != null)
                weaponCamera.transform.localPosition = camOriginalLocalPos + Vector3.up * weaponRaiseAmount * p;
            yield return null;
        }

        if (currentWeapon != null) currentWeapon.gameObject.SetActive(false);

        newWeapon.gameObject.SetActive(true);
        currentWeapon = newWeapon;

        OnWeaponChanged?.Invoke(currentWeapon);

        t = 0f;
        while (t < switchDuration)
        {
            t += Time.deltaTime;
            float p = (switchCurve != null) ? switchCurve.Evaluate(1f - (t / switchDuration)) : 1f - (t / switchDuration);
            if (weaponCamera != null)
                weaponCamera.transform.localPosition = camOriginalLocalPos + Vector3.up * weaponRaiseAmount * p;
            yield return null;
        }

        if (weaponCamera != null) weaponCamera.transform.localPosition = camOriginalLocalPos;
        isSwitching = false;
    }

    // ===== Comprar / añadir =====
    public bool TryBuyWeapon(BaseWeaponController weaponPrefab, int cost, int playerPoints, out int finalPoints)
    {
        finalPoints = playerPoints;
        if (weaponPrefab == null || weaponPrefab.WeaponData == null) return false;

        // 1) Ya tienes el arma → refill
        if (HasWeapon(weaponPrefab.WeaponData))
        {
            if (playerPoints < ammoRefillCost) return false;

            var existing = GetWeaponByData(weaponPrefab.WeaponData);
            existing.ForceAmmoFull();

            finalPoints -= ammoRefillCost;
            return true;
        }

        // 2) No la tienes → compra
        if (playerPoints < cost) return false;

        AddWeapon(weaponPrefab);
        finalPoints -= cost;
        return true;
    }

    public void AddWeapon(BaseWeaponController newWeaponPrefab)
    {
        if (newWeaponPrefab == null) return;

        BaseWeaponController spawned = Instantiate(newWeaponPrefab, weaponHolder);
        spawned.gameObject.SetActive(false);
        InjectToWeapon(spawned);

        if (primaryWeapon == null)
        {
            primaryWeapon = spawned;
            SwitchToSlot(1);
            return;
        }

        if (secondaryWeapon == null)
        {
            secondaryWeapon = spawned;
            SwitchToSlot(2);
            return;
        }

        // Inventario lleno → reemplaza la equipada
        if (currentWeapon == primaryWeapon)
        {
            Destroy(primaryWeapon.gameObject);
            primaryWeapon = spawned;
            SwitchToSlot(1);
        }
        else
        {
            Destroy(secondaryWeapon.gameObject);
            secondaryWeapon = spawned;
            SwitchToSlot(2);
        }
    }

    // ===== Helpers =====
    public bool HasFreeSlot() => primaryWeapon == null || secondaryWeapon == null;

    public bool HasWeapon(WeaponData data) => GetWeaponByData(data) != null;

    public BaseWeaponController GetWeaponByData(WeaponData data)
    {
        if (data == null) return null;

        if (primaryWeapon != null && primaryWeapon.WeaponData == data) return primaryWeapon;
        if (secondaryWeapon != null && secondaryWeapon.WeaponData == data) return secondaryWeapon;
        return null;
    }

    // Mantengo compatibilidad si en alguna parte se usa por nombre
    public BaseWeaponController GetWeaponByName(string name)
    {
        if (primaryWeapon != null && primaryWeapon.weaponName == name) return primaryWeapon;
        if (secondaryWeapon != null && secondaryWeapon.weaponName == name) return secondaryWeapon;
        return null;
    }

    private void InjectToWeapon(BaseWeaponController weapon)
    {
        if (weapon == null) return;

        weapon.SetInput(input);

        if (sharedBulletOrigins != null && sharedBulletOrigins.Length > 0)
        {
            weapon.SetBulletOrigins(sharedBulletOrigins);
            if (setPrimaryOriginFromList && weapon.bulletOrigin == null)
                weapon.bulletOrigin = sharedBulletOrigins[0];
        }
    }

    public void ReplaceCurrentWeapon(BaseWeaponController prefab)
    {
        if (prefab == null) return;

        if (currentWeapon != null)
        {
            if (currentWeapon == primaryWeapon) primaryWeapon = null;
            if (currentWeapon == secondaryWeapon) secondaryWeapon = null;
            Destroy(currentWeapon.gameObject);
        }

        BaseWeaponController newWeapon = Instantiate(prefab, weaponHolder);
        InjectToWeapon(newWeapon);

        if (primaryWeapon == null) primaryWeapon = newWeapon;
        else secondaryWeapon = newWeapon;

        StartCoroutine(SwitchWeaponRoutine(newWeapon));
    }
}
