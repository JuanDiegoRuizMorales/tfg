using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Ajustes")]
    public float interactRange = 3f;
    public LayerMask interactMask;

    [Header("UI")]
    public TextMeshProUGUI promptTMP;

    public Camera cam;
    private WeaponInventoryManager weaponInventory;

    private void Awake()
    {
        weaponInventory = GetComponent<WeaponInventoryManager>();
    }

    private void Update()
    {
        ShowPrompt();

        if (Input.GetKeyDown(KeyCode.F))
            TryInteract();
    }

    private void ShowPrompt()
    {
        if (promptTMP != null)
            promptTMP.text = "";

        if (weaponInventory == null)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            // 1. WeaponBuyStation (con lógica dinámica)
            WeaponBuyStation station = hit.collider.GetComponent<WeaponBuyStation>();
            if (station != null)
            {
                if (promptTMP != null)
                {
                    string prompt = station.GetPrompt(
                        weaponInventory,
                        ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0
                    );

                    promptTMP.text = prompt;
                }

                return;
            }

            // 2. Cualquier otro interactuable
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (promptTMP != null)
                    promptTMP.text = interactable.GetPrompt();

                return;
            }
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(transform);
            }
        }
    }
}
