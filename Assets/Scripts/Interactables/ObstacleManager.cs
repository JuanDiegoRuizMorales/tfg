using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

[RequireComponent(typeof(Collider))]
public class ObstacleManager : MonoBehaviour, IInteractable
{
    [Header("Coste")]
    public int cost = 500;

    [Header("Zonas a desbloquear")]
    public List<string> areasToUnlock_FromFront = new List<string>();
    public List<string> areasToUnlock_FromBack = new List<string>();
    public List<string> areasToUnlock_Always = new List<string>();

    [Header("Dirección frontal del obstáculo")]
    public Transform frontDirection;

    [Header("Disolver")]
    public Renderer[] renderersToDissolve;
    public VisualEffect[] dissolveFXPrefab;
    public float dissolveRate = 0.02f;
    public float dissolveTick = 0.03f;

    [Header("Opcional")]
    public NavMeshObstacle navMeshObstacle;
    public Collider obstacleCollider;

    private Material[] _materialsInstanced;
    private bool _isOpen = false;

    private const string DISSOLVE_PARAM = "_DissolveAmount";

    private void Reset()
    {
        obstacleCollider = GetComponent<Collider>();
        navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    private void Awake()
    {
        if (obstacleCollider == null)
            obstacleCollider = GetComponent<Collider>();

        if (frontDirection == null)
            frontDirection = this.transform;

        if (renderersToDissolve != null)
        {
            _materialsInstanced = new Material[renderersToDissolve.Length];

            for (int i = 0; i < renderersToDissolve.Length; i++)
            {
                if (renderersToDissolve[i] != null)
                {
                    _materialsInstanced[i] = Instantiate(renderersToDissolve[i].sharedMaterial);
                    renderersToDissolve[i].material = _materialsInstanced[i];

                    if (_materialsInstanced[i].HasProperty(DISSOLVE_PARAM))
                        _materialsInstanced[i].SetFloat(DISSOLVE_PARAM, 0f);
                }
            }
        }
    }

    public string GetPrompt()
    {
        return $"Press F to CLEAR OBSTACLE ({cost})";
    }

    public void Interact(Transform player)
    {
        TryOpen(player);
    }

    public void TryOpen(Transform player)
    {
        if (_isOpen) return;
        if (ScoreManager.Instance == null) return;

        if (ScoreManager.Instance.Score < cost)
            return;

        ScoreManager.Instance.AddPoints(-cost);
        OpenObstacle(player);
    }

    public void OpenObstacle(Transform player)
    {
        if (_isOpen) return;
        _isOpen = true;

        StartCoroutine(DissolveAndDisable(player));
    }

    private IEnumerator DissolveAndDisable(Transform player)
    {
        float val = 0f;

        // Lanzar VFX desde el primer frame del dissolve
        if (dissolveFXPrefab != null)
        {
            foreach (var fx in dissolveFXPrefab)
            {
                if (fx == null) continue;

                // Sacamos los VFX del padre para que no se apaguen al desactivar el obstáculo
                fx.transform.SetParent(null, true);

                fx.Reinit();
                fx.SendEvent("OnPlay");

                // Programar destrucción automática al terminar
                float duration = fx.HasFloat("Duration") ? fx.GetFloat("Duration") : 1.5f;
                Destroy(fx.gameObject, duration + 0.1f);
            }
        }

        // Proceso visual del dissolve
        while (val < 1f)
        {
            val += dissolveRate;

            if (_materialsInstanced != null)
            {
                foreach (var mat in _materialsInstanced)
                    if (mat != null)
                        mat.SetFloat(DISSOLVE_PARAM, val);
            }

            yield return new WaitForSeconds(dissolveTick);
        }

        // Ejecutar lógica cuando ya se ha disuelto
        DisableNavmeshAndCollider();
        UnlockCorrectAreas(player);

        // El obstáculo ya no es necesario
        gameObject.SetActive(false);
    }




    private void DisableNavmeshAndCollider()
    {
        if (obstacleCollider != null)
            obstacleCollider.enabled = false;

        if (navMeshObstacle != null)
            navMeshObstacle.enabled = false;
    }

    private void UnlockCorrectAreas(Transform player)
    {
        var mgr = FindAnyObjectByType<LevelProgressionManager>();
        if (mgr == null)
            return;

        foreach (var a in areasToUnlock_Always)
            mgr.UnlockArea(a);

        Vector3 toPlayer = (player.position - transform.position).normalized;
        float dot = Vector3.Dot(frontDirection.forward, toPlayer);
        bool playerIsInFront = dot > 0f;

        if (playerIsInFront)
        {
            foreach (var a in areasToUnlock_FromFront)
                mgr.UnlockArea(a);
        }
        else
        {
            foreach (var a in areasToUnlock_FromBack)
                mgr.UnlockArea(a);
        }
    }
}
