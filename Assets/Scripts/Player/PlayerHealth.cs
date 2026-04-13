using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 4;
    public int currentHealth = 4;

    [Header("Regeneración")]
    public float regenDelay = 5f;
    public float regenSpeed = 1f;

    [Header("Invulnerabilidad")]
    public bool isInvulnerable = false;

    private float sinceLastDamage = 0f;
    private bool regenerating = false;
    private bool dead = false;

    [Header("Vignette")]
    public Image damageVignette;

    [Tooltip("Incremento de opacidad temporal durante el latido")]
    public float heartbeatBoost = 0.15f;

    [Tooltip("Tiempo que tarda en bajar despues del pico del latido")]
    public float heartbeatFadeDuration = 0.4f;

    [Tooltip("Tiempo entre latidos mientras el jugador esta herido")]
    public float heartbeatInterval = 1f;

    [Tooltip("Duracion del desvanecimiento al regenerar vida")]
    public float fadeDuration = 0.4f;

    private Coroutine heartbeatRoutine;
    private Coroutine heartbeatLoopRoutine;
    private Coroutine fadeRoutine;

    
    void Start()
    {
        UpdateVignetteOpacityImmediate();
    }

    void Update()
    {
        if (dead) return;

        if (!regenerating)
        {
            sinceLastDamage += Time.deltaTime;

            if (sinceLastDamage >= regenDelay)
            {
                StartCoroutine(Regenerate());
            }
        }
    }

    /// <summary>
    /// Método encargado de aplicar daño a la vida del jugador
    /// </summary>
    /// <param name="amount"></param>
    public void TakeDamage(int amount)
    {
        if (dead) return;
        if (isInvulnerable) return;

        currentHealth -= amount;
        sinceLastDamage = 0f;
        regenerating = false;

        Debug.Log("El jugador recibió daño, vida actual: " + currentHealth);

        // Cancelar fade si estaba en curso
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        // Opacidad base
        UpdateVignetteOpacityImmediate();

        // Pulso y loop de pulso
        PlayHeartbeat();
        StartHeartbeatLoop();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    //Regenera vida a lo largo del tiempo si lleva un tiempo sin recibir golpes
    private IEnumerator Regenerate()
    {
        regenerating = true;

        while (currentHealth < maxHealth)
        {
            currentHealth++;
            Debug.Log("Regenerando... vida actual: " + currentHealth);

            // Fade suave hacia la nueva opacidad
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeVignetteToBase());

            if (currentHealth == maxHealth)
            {
                StopHeartbeatLoop();
            }

            yield return new WaitForSeconds(regenSpeed);
        }

        regenerating = false;
    }

    /// <summary>
    /// Método encargado de la lógica de la muerte del jugador
    /// </summary>
    private void Die()
    {
        dead = true;
        Debug.Log("PLAYER MUERTO");
        StopHeartbeatLoop();
    }

    
    private float ComputeBaseOpacity()
    {
        return 1f - (float)currentHealth / maxHealth;
    }

    private void UpdateVignetteOpacityImmediate()
    {
        if (damageVignette == null) return;

        Color c = damageVignette.color;
        c.a = ComputeBaseOpacity();
        damageVignette.color = c;
    }

    //Reproduce una animación de opacidad cada cierto tiempo
    private void PlayHeartbeat()
    {
        if (heartbeatRoutine != null)
            StopCoroutine(heartbeatRoutine);

        heartbeatRoutine = StartCoroutine(HeartbeatPulse());
    }

    private IEnumerator HeartbeatPulse()
    {
        float baseOpacity = ComputeBaseOpacity();
        float startA = damageVignette.color.a;

        // Si la opacidad actual es distinta de la base se iguala suavemente
        float normalizeTime = 0f;
        while (normalizeTime < 0.1f)
        {
            normalizeTime += Time.deltaTime;
            float lerp = normalizeTime / 0.1f;

            Color n = damageVignette.color;
            n.a = Mathf.Lerp(startA, baseOpacity, lerp);
            damageVignette.color = n;

            yield return null;
        }

        float targetOpacity = Mathf.Clamp01(baseOpacity + heartbeatBoost);

        // subida
        float tUp = 0f;
        while (tUp < 0.1f)
        {
            tUp += Time.deltaTime;
            float lerp = tUp / 0.1f;

            Color c = damageVignette.color;
            c.a = Mathf.Lerp(baseOpacity, targetOpacity, lerp);
            damageVignette.color = c;

            yield return null;
        }

        // bajada lenta
        float tDown = 0f;
        while (tDown < heartbeatFadeDuration)
        {
            tDown += Time.deltaTime;
            float lerp = tDown / heartbeatFadeDuration;

            Color c = damageVignette.color;
            c.a = Mathf.Lerp(targetOpacity, baseOpacity, lerp);
            damageVignette.color = c;

            yield return null;
        }
    }

    private void StartHeartbeatLoop()
    {
        if (currentHealth >= maxHealth) return;

        if (heartbeatLoopRoutine == null)
            heartbeatLoopRoutine = StartCoroutine(HeartbeatLoop());
    }

    private void StopHeartbeatLoop()
    {
        if (heartbeatLoopRoutine != null)
        {
            StopCoroutine(heartbeatLoopRoutine);
            heartbeatLoopRoutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (currentHealth < maxHealth)
        {
            PlayHeartbeat();
            yield return new WaitForSeconds(heartbeatInterval);
        }
    }

    //Se hace fade a la viñeta si se ha recuperado la vida perdida al recibir daño
    private IEnumerator FadeVignetteToBase()
    {
        if (damageVignette == null) yield break;

        float startA = damageVignette.color.a;
        float targetA = ComputeBaseOpacity();

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;

            Color c = damageVignette.color;
            c.a = Mathf.Lerp(startA, targetA, lerp);
            damageVignette.color = c;

            yield return null;
        }

        
        Color finalC = damageVignette.color;
        finalC.a = targetA;
        damageVignette.color = finalC;
    }
}
