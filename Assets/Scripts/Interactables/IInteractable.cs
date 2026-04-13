using UnityEngine;

public interface IInteractable
{
    string GetPrompt(); // Texto para mostrar en UI (ej: “Comprar arma 1500”)
    void Interact(Transform player); // Acción al pulsar F
}
