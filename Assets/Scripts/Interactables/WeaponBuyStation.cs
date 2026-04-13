using UnityEngine;

public class WeaponBuyStation : MonoBehaviour, IInteractable
{
    [Header("Venta de arma")]
    public BaseWeaponController weaponPrefab;

    [Header("Costes")]
    public int buyCost = 1000;
    public int ammoRefillCost = 500;

    // Requisito de IInteractable (prompt base genérico)
    public string GetPrompt()
    {
        if (weaponPrefab == null || weaponPrefab.WeaponData == null) return "ERROR: No weaponPrefab";
        return $"{weaponPrefab.WeaponData.weaponName}\nComprar arma ({buyCost})";
    }

    // Prompt avanzado usado por PlayerInteractor
    public string GetPrompt(WeaponInventoryManager inventory, int playerPoints)
    {
        if (weaponPrefab == null || weaponPrefab.WeaponData == null) return "ERROR: No weaponPrefab";
        string arma = weaponPrefab.WeaponData.weaponName;

        // 1) Ya tiene el arma → SOLO recargar
        if (inventory.HasWeapon(weaponPrefab.WeaponData))
        {
            if (playerPoints < ammoRefillCost)
                return $"{arma}\nNot enough points ({ammoRefillCost})";

            return $"{arma}\nReload mana ({ammoRefillCost})";
        }

        // 2) No la tiene + hay hueco → comprar
        if (inventory.HasFreeSlot())
        {
            if (playerPoints < buyCost)
                return $"{arma}\nNot enough points ({buyCost})";

            return $"{arma}\nBuy weapon ({buyCost})";
        }

        // 3) No la tiene + inventario lleno → reemplazar equipada
        if (playerPoints < buyCost)
            return $"{arma}\nNot enough points ({buyCost})";

        return $"{arma}\nReplace current weapon ({buyCost})";
    }

    public void Interact(Transform player)
    {
        if (ScoreManager.Instance == null) return;
        if (weaponPrefab == null || weaponPrefab.WeaponData == null) return;

        WeaponInventoryManager inv = player.GetComponent<WeaponInventoryManager>();
        if (inv == null) return;

        int points = ScoreManager.Instance.Score;

        // 1) Ya tiene el arma → recarga
        if (inv.HasWeapon(weaponPrefab.WeaponData))
        {
            if (points < ammoRefillCost)
            {
                Debug.Log("Not enough points.");
                return;
            }

            ScoreManager.Instance.AddPoints(-ammoRefillCost);

            var existing = inv.GetWeaponByData(weaponPrefab.WeaponData);
            if (existing != null) existing.ForceAmmoFull();

            Debug.Log("Munición recargada.");
            return;
        }

        // 2) Hay slot libre → comprar
        if (inv.HasFreeSlot())
        {
            if (points < buyCost)
            {
                Debug.Log("Not enough points.");
                return;
            }

            ScoreManager.Instance.AddPoints(-buyCost);
            inv.AddWeapon(weaponPrefab); // auto-equip por SwitchToSlot
            Debug.Log("Arma añadida.");
            return;
        }

        // 3) Inventario lleno → reemplazar equipada
        if (points < buyCost)
        {
            Debug.Log("Not enough points.");
            return;
        }

        ScoreManager.Instance.AddPoints(-buyCost);
        inv.ReplaceCurrentWeapon(weaponPrefab);
        Debug.Log("Arma reemplazada.");
    }
}
