using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class WaveController : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> availableZombieSpawnPoints = new List<Transform>();
    public List<Transform> activeZombieSpawnPoints = new List<Transform>();
    public List<Transform> inactiveZombieSpawnPoints = new List<Transform>();

    [Header("Zombie Settings")]
    public GameObject zombiePrefab;
    public int maxZombiePoolSize = 50;
    public int baseZombiesPerRound = 10;
    public int zombiesPerRoundIncrement = 5;
    public float minSpawnCooldown = 3f;
    public float maxSpawnCooldown = 6f;
    public float cooldownReductionPerRound = 0.1f;

    [Header("Round Settings")]
    public float interRoundDelay = 5f;

    private Dictionary<Transform, float> spawnTimers = new Dictionary<Transform, float>();
    private Queue<GameObject> zombiePool = new Queue<GameObject>();
    private List<GameObject> activeZombies = new List<GameObject>();

    private int currentRound = 0;
    private int totalZombiesThisRound;
    private int zombiesSpawnedThisRound;
    private bool roundActive = false;
    private bool waitingForNextRound = false;
    [SerializeField] private Transform zombiePoolParent;

    [Header("UI Ronda")]
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Round Debug Parameters")]
    [SerializeField]private int fastRoundStart;     //Ronda aleatoria entre 5 y 9
    [SerializeField] private int fastRound_Most;     //fastRoundStart + 1
    [SerializeField] private int fastRound_All;      //fastRoundStart + 2

    void Start()
    {
        // Selecciona una ronda aleatoria donde empieza la progresión
        fastRoundStart = Random.Range(5, 10); // 5–9
        fastRound_Most = fastRoundStart + 1;
        fastRound_All = fastRoundStart + 2;

        Debug.Log($"  - Algunos rápidos: ronda {fastRoundStart}");
        Debug.Log($"  - Mayoría rápidos: ronda {fastRound_Most}");
        Debug.Log($"  - Todos rápidos: ronda {fastRound_All}");

        CreateZombiePool();
        NextRound();
    }


    void Update()
    {
        //Si la ronda no está activa, o no hay spawnpoints para los zombies, o se está esperando para la siguiente ronda, no hace nada.
        if (!roundActive || activeZombieSpawnPoints.Count == 0 || waitingForNextRound)
            return;
        //Por cada spawnpoint activo, les asigna un cooldown aleatorio para hacer aparecer un zombi. Si ese cooldown llega a 0, intenta hacer aparecer un zombi en ese spawnpoint
        //Luego, reinicia los tiempos de cooldown de ese spawnpoint
        foreach (Transform spawnPoint in activeZombieSpawnPoints)
        {
            if (!spawnTimers.ContainsKey(spawnPoint))
                spawnTimers[spawnPoint] = Random.Range(minSpawnCooldown, maxSpawnCooldown);

            spawnTimers[spawnPoint] -= Time.deltaTime;

            if (spawnTimers[spawnPoint] <= 0f)
            {
                TrySpawnZombie(spawnPoint);
                spawnTimers[spawnPoint] = Random.Range(minSpawnCooldown, maxSpawnCooldown);
            }
        }

        if (zombiesSpawnedThisRound >= totalZombiesThisRound && activeZombies.Count == 0)
        {
            StartCoroutine(WaitAndStartNextRound());
        }
    }


    
    void NextRound()
    {
        currentRound++;
        StartNewRound();
    }
    //Comienza una nueva ronda
    void StartNewRound()
    {
        roundActive = true;
        waitingForNextRound = false;

        zombiesSpawnedThisRound = 0;
        totalZombiesThisRound = CalculateZombieCountForRound(currentRound);

        minSpawnCooldown = Mathf.Max(0.5f, minSpawnCooldown - cooldownReductionPerRound);
        maxSpawnCooldown = Mathf.Max(1f, maxSpawnCooldown - cooldownReductionPerRound);

        Debug.Log($"Ronda {currentRound}  Zombies: {totalZombiesThisRound}");

        UpdateRoundUI();
    }

    private int CalculateZombieCountForRound(int round)
    {
        float factor =
            (round == 1) ? 0.25f :
            (round == 2) ? 0.30f :
            (round == 3) ? 0.50f :
            (round == 4) ? 0.70f :
            (round == 5) ? 0.90f :
            1.0f;

        float scaled = Mathf.Max(1f, round / 5f);

        float baseCount = 24f + (0.5f * 6f * scaled);

        return Mathf.RoundToInt(baseCount * factor);
    }

    private void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = $"{currentRound}";
    }


    //Espera cierto tiempo de espera hasta comenzar la siguiente ronda
    IEnumerator WaitAndStartNextRound()
    {
        if (waitingForNextRound) yield break;
        waitingForNextRound = true;
        roundActive = false;

        Debug.Log($"Ronda {currentRound} completada. Esperando {interRoundDelay}s...");

        yield return new WaitForSeconds(interRoundDelay);

        NextRound();
    }


    /// <summary>
    /// Intenta spawnear un zombi escogido por el GetZombieFrom pool, si devuelve null, no hace aparecer nada.
    /// Se le asigna la posición y rotación.
    /// Le asgina el controlador al zombi y lo activa, luego aplica su vida y se determina si corre o no.
    /// Luego añade un contador a la cantidad de zombies que han aparecido en esta ronda.
    /// </summary>
    /// <param name="spawnPoint">Punto de aparición elegido para aparecer</param>
    void TrySpawnZombie(Transform spawnPoint)
    {
        if (zombiesSpawnedThisRound >= totalZombiesThisRound) return;
        if (activeZombies.Count >= maxZombiePoolSize) return;

        GameObject zombie = GetZombieFromPool();
        if (zombie != null)
        {
            zombie.transform.position = spawnPoint.position;
            zombie.transform.rotation = spawnPoint.rotation;

            float targetHealth = CalculateZombieHealthForRound(currentRound);


            ZombieBasicController controller = zombie.GetComponent<ZombieBasicController>();

            zombie.SetActive(true);

            if (controller != null)
            {
                controller.ResetZombie(targetHealth);

                //Calcula dependiendo de la rondas elegidas aleatoriamente, cuantas probabilidades tiene el zombi de correr
                bool useRunSpeed = false;
                //Si la ronda es la ronda es igual o mayor a la ronda en la que todos los zombis corren, todos los zombies corren.
                if (currentRound >= fastRound_All)
                {
                    useRunSpeed = true; // 100%
                }
                //Si la ronda es la ronda en la que la mayoría de zombis corren: 60% de probabilidades de que el zombi corra.
                else if (currentRound == fastRound_Most)
                {
                    useRunSpeed = Random.value < 0.60f; // 60% rápidos
                }
                //Si la ronda es la ronda en la que unos pocos zombis corren: 20% de probabilidades de que el zombi corra.
                else if (currentRound == fastRoundStart)
                {
                    useRunSpeed = Random.value < 0.20f; // 20% rápidos
                }
                //Si la ronda es inferior a el numero a partir del cual los zombis corren, ninguno corre.
                else
                {
                    useRunSpeed = false;
                }

                controller.SetRunningMode(useRunSpeed);
            }

            zombiesSpawnedThisRound++;
            activeZombies.Add(zombie);
        }
    }

    private float CalculateZombieHealthForRound(int round)
    {
        // ronda 1: 150
        if (round <= 1)
            return 150f;

        // rondas 2 a 9: +100 por ronda
        if (round <= 9)
            return 150f + 100f * (round - 1);

        // vida base en ronda 9
        float health = 150f + 100f * 8f; // 950

        // a partir de ronda 10, incremento multiplicativo del 10%
        for (int r = 10; r <= round; r++)
            health *= 1.10f;

        return health;
    }


    /// <summary>
    /// Método encargado de crear la pool de zombies al inicio de la partida
    /// </summary>
    void CreateZombiePool()
    {
        if (zombiePoolParent == null)
        {
            GameObject parentObj = new GameObject("ZombiePool");
            zombiePoolParent = parentObj.transform;
        }

        for (int i = 0; i < maxZombiePoolSize; i++)
        {
            GameObject zombie = Instantiate(zombiePrefab, zombiePoolParent);
            zombie.SetActive(false);

            ZombieBasicController z = zombie.GetComponent<ZombieBasicController>();

            if (z != null)
            {
                if (z.baseHealth <= 0f)
                    z.baseHealth = Mathf.Max(1f, z.health);

                z.OnDeath -= OnZombieDeath;
                z.OnDeath += OnZombieDeath;
            }

            zombiePool.Enqueue(zombie);
        }

        Debug.Log($"Pool de zombis creado con {maxZombiePoolSize} instancias.");
    }

    /// <summary>
    /// Clase que obtiene a un zombie del pool.
    /// </summary>
    /// <returns>Devuelve a un zombi si no se supera la cantidad máxima del pool, si no, devuelve null.</returns>
    GameObject GetZombieFromPool()
    {
        if (zombiePool.Count > 0)
            return zombiePool.Dequeue();

        return null;
    }

    void OnZombieDeath(GameObject zombie)
    {
        activeZombies.Remove(zombie);
        zombiePool.Enqueue(zombie);
    }
}
