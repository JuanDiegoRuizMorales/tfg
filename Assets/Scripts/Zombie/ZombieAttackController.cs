using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackController : MonoBehaviour
{
    [Header("Ajuste de ataque")]
    public float attackRange = 2f;
    public float attackCooldown = 1.4f;

    private float lastAttackTime = -999f;

    private Animator animator;
    private NavMeshAgent agent;
    private Transform player;

    private int attackHash;
    private int attackIndexHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        attackHash = Animator.StringToHash("Attack");
        attackIndexHash = Animator.StringToHash("AttackIndex");
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /// <summary>
    /// Método que se llama cada frame, en el que se comprueba si se cumplen las condiciones para ejecutar el ataque
    /// </summary>
    public void TryAttack()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        PerformAttack();
    }
    /// <summary>
    /// Método que reproduce 2 animaciones. Estas animaciones tienen triggers que activan y desactivan las hitboxes de daño.
    /// </summary>
    private void PerformAttack()
    {
        int index = Random.Range(0, 2); // 0 = Reproduce animación Attack1, 1 = Reproduce animación Attack2

        animator.SetInteger(attackIndexHash, index);
        animator.SetTrigger(attackHash);

        lastAttackTime = Time.time;
    }
}
