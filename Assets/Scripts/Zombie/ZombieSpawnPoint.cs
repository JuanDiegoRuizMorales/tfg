using UnityEngine;

[ExecuteAlways]
public class ZombieSpawnPoint : MonoBehaviour
{
    [Header("Radios de activación y exclusión")]
    public float activationRadius = 20f;
    public float deactivationRadius = 5f;

    private Transform player;
    private bool lastStateActive = false;
    private WaveController waveController;

    private void OnEnable()
    {
        waveController = FindAnyObjectByType<WaveController>();

        if (waveController == null)
        {
            Debug.LogError("No se encontró un WaveController en la escena.");
            enabled = false;
            return;
        }

        // Al activarse el objeto, se registra
        if (!waveController.availableZombieSpawnPoints.Contains(transform))
            waveController.availableZombieSpawnPoints.Add(transform);

        if (!waveController.inactiveZombieSpawnPoints.Contains(transform))
            waveController.inactiveZombieSpawnPoints.Add(transform);

        Debug.Log($"{name} registrado en WaveController (activo en escena)");
    }

    private void OnDisable()
    {
        if (waveController == null) return;

        // Al desactivarse el objeto, se elimina de las listas activas/inactivas
        waveController.activeZombieSpawnPoints.Remove(transform);
        waveController.inactiveZombieSpawnPoints.Remove(transform);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                return;
            }
        }

        float distance = Vector3.Distance(player.position, transform.position);
        bool isActive = distance <= activationRadius && distance > deactivationRadius;

        if (isActive != lastStateActive)
        {
            lastStateActive = isActive;

            if (isActive)
                ActivateSpawnPoint();
            else
                DeactivateSpawnPoint();
        }
    }

    void ActivateSpawnPoint()
    {
        if (!waveController.activeZombieSpawnPoints.Contains(transform))
            waveController.activeZombieSpawnPoints.Add(transform);

        waveController.inactiveZombieSpawnPoints.Remove(transform);
    }

    void DeactivateSpawnPoint()
    {
        if (!waveController.inactiveZombieSpawnPoints.Contains(transform))
            waveController.inactiveZombieSpawnPoints.Add(transform);

        waveController.activeZombieSpawnPoints.Remove(transform);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, deactivationRadius);
    }
}
