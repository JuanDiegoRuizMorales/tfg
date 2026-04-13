using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Componentes")]
    public NavMeshAgent navMeshAgent;
    public Transform player;

    [Header("Horda (radios globales)")]
    [SerializeField] private HordeSettingsSO hordeSettings; // Asset compartido
    [Tooltip("Si está activo y no se asigna asset, intentará cargar 'HordeSettings' de Resources.")]
    [SerializeField] private bool autoLoadFromResources = true;

    [Header("Rotación hacia el jugador")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float lookDistanceBuffer = 0.2f;

    [Header("Frecuencia de actualización de punto aleatorio")]
    [Tooltip("Cada cuántos segundos se recalcula un nuevo punto relativo al jugador.")]
    [SerializeField] private float randomPointUpdateRate = 1.5f;

    private Vector3 currentRandomTarget;
    private float nextRandomUpdateTime = 0f;
    private Vector3 lastKnownPlayerNavPos;
    private bool playerOnNavMesh = false;

    private void Awake()
    {
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        // Autocargar el asset si no está asignado
        if (hordeSettings == null && autoLoadFromResources)
        {
            hordeSettings = Resources.Load<HordeSettingsSO>("HordeSettings");
        }
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (player == null || navMeshAgent == null) return;

        float chaseRadius = GetChaseRadius();
        float innerRadius = GetInnerRadius();

        // Verifica si el jugador está sobre el NavMesh
        playerOnNavMesh = NavMesh.SamplePosition(player.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (playerOnNavMesh)
        {
            lastKnownPlayerNavPos = playerHit.position; // guardamos su última posición válida

            // Si está fuera del área se mueve a un punto aleatorio relativo al jugador
            if (distanceToPlayer > chaseRadius)
            {
                if (Time.time >= nextRandomUpdateTime)
                {
                    currentRandomTarget = GetRandomPointNearPlayer(chaseRadius);
                    navMeshAgent.SetDestination(currentRandomTarget);
                    nextRandomUpdateTime = Time.time + randomPointUpdateRate;
                }

                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.2f)
                {
                    currentRandomTarget = GetRandomPointNearPlayer(chaseRadius);
                    navMeshAgent.SetDestination(currentRandomTarget);
                    nextRandomUpdateTime = Time.time + randomPointUpdateRate;
                }
            }
            else
            {
                // Dentro del área sigue al jugador directamente
                navMeshAgent.SetDestination(player.position);

                // Si ya está muy cerca, gira hacia el jugador
                if (distanceToPlayer <= navMeshAgent.stoppingDistance + lookDistanceBuffer)
                {
                    RotateTowardsPlayer();
                }
            }
        }
        else
        {
            // Si el jugador sale del navmesh se queda quieto
            navMeshAgent.SetDestination(transform.position);
        }
    }

    private float GetChaseRadius()
    {
        return (hordeSettings != null) ? hordeSettings.GlobalChaseRadius : 8f;
    }

    private float GetInnerRadius()
    {
        return (hordeSettings != null) ? hordeSettings.GlobalInnerRadius : 3f;
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private Vector3 GetRandomPointNearPlayer(float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidate = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return lastKnownPlayerNavPos != Vector3.zero ? lastKnownPlayerNavPos : transform.position;
    }


    private void OnDrawGizmos()
    {
        if (player == null) return;

        float chaseRadius = GetChaseRadius();
        float innerRadius = GetInnerRadius();

        // Radios alrededor del jugador
        Gizmos.color = Color.midnightBlue;
        Gizmos.DrawWireSphere(player.position, chaseRadius);

        // Línea hacia el destino aleatorio actual
        if (currentRandomTarget != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentRandomTarget);
            Gizmos.DrawSphere(currentRandomTarget, 0.2f);
        }

        // Última posición conocida del jugador
        if (lastKnownPlayerNavPos != Vector3.zero)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
            Gizmos.DrawSphere(lastKnownPlayerNavPos + Vector3.up * 0.1f, 0.15f);
        }
    }

}
