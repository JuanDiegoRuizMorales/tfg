using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using UnityEngine.VFX;

public class ZombieBasicController : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    [HideInInspector] public float baseHealth;
    public float walkSpeed = 2.2f;
    public float runSpeed = 5.2f;

    [Header("Configuración de puntuación")]
    [SerializeField] private PointsConfig _pointsConfig;

    [Header("Componentes")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;
    [SerializeField] private CapsuleCollider _collider;

    public event Action<GameObject> OnDeath;

    private int _speedHash;
    private bool _hasDied = false;
    private bool _spawning = false;

    private bool _pendingRunMode = false;

    [Header("Dissolve")]
    public SkinnedMeshRenderer skinnedMesh;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;

    private Material skinnedMaterial;
    private const string DISSOLVE_PARAM = "_DissolveAmount";

    public VisualEffect vfxgraph;

    private ZombieAttackController _attackController;
    private BaseWeaponController _lastHitWeapon;


    private void Awake()
    {
        baseHealth = Mathf.Max(1f, health);

        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponent<Animator>();
        _attackController = GetComponent<ZombieAttackController>();

        if (_collider != null) _collider.enabled = true;

        if (skinnedMesh != null)
            skinnedMaterial = skinnedMesh.material;
    }

    private void Start()
    {
        if (_agent != null)
            _agent.speed = walkSpeed;

        _speedHash = Animator.StringToHash("Speed");

        if (_pointsConfig == null)
            _pointsConfig = Resources.Load<PointsConfig>("PointsConfig");
    }

    private void Update()
    {
        if (_hasDied || _spawning) return;

        if (_attackController != null)
            _attackController.TryAttack();

        if (health <= 0)
        {
            Death();
            return;
        }

        UpdateAnimationSpeed();
    }
    /// <summary>
    /// Metodo encargado de actualizar los valores que se le pasan al blend tree del animator del zombi
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (_spawning || _animator == null || _agent == null) return;

        float velocity = _agent.velocity.magnitude;
        float normalized = Mathf.InverseLerp(0f, runSpeed, velocity);

        float finalValue =
            (normalized < 0.1f) ? 0f :
            (normalized < 0.6f) ? 0.5f :
            1f;

        _animator.SetFloat(_speedHash, finalValue, 0.1f, Time.deltaTime);
    }
    /// <summary>
    /// Método encargado de aplicar la velocidad de movimiento en base a la ronda determinada por el wave controller
    /// </summary>
    /// <param name="useRunSpeed"></param>
    public void SetRunningMode(bool useRunSpeed)
    {
        _pendingRunMode = useRunSpeed;

        if (!_spawning && _agent != null)
            _agent.speed = useRunSpeed ? runSpeed : walkSpeed;
    }

    //Aplica la velocidad de movimiento en base a si está asignada la velocidad de caminar o correr
    private void ApplyPendingSpeed()
    {
        if (_agent != null)
            _agent.speed = _pendingRunMode ? runSpeed : walkSpeed;
    }
    /// <summary>
    /// Resta una cantidad float de daño a la vida del zombie, además, registra de qué arma recibio el daño
    /// </summary>
    /// <param name="dmg">Daño recibido</param>
    public void TakeDamage(float dmg, BaseWeaponController sourceWeapon)
    {
        if (_hasDied || _spawning) return;

        // registra arma que hizo daño
        if (sourceWeapon != null)
            _lastHitWeapon = sourceWeapon;

        health -= dmg;

        ScoreManager.Instance?.AddPoints(
            _pointsConfig != null ? _pointsConfig.pointsPerDamage : 10
        );

        if (_animator != null)
            _animator.SetTrigger("Hit");

        if (health <= 0)
            Death();
    }


    /// <summary>
    /// Método encargado de la muerte del zombie, además, suma experiencia al arma que lo mató
    /// </summary>
    private void Death()
    {
        if (_hasDied) return;
        _hasDied = true;

        StartCoroutine(DissolveCo());

        if (_collider != null)
            _collider.enabled = false;

        ScoreManager.Instance?.AddPoints(
            _pointsConfig != null ? _pointsConfig.pointsPerKill : 100
        );

        // da exp al arma que mato al zombie
        if (_lastHitWeapon != null)
        {
            int expAmount = Mathf.FloorToInt(baseHealth * 0.25f);
            if (expAmount > 0)
                _lastHitWeapon.AddExp(expAmount);
        }


        OnDeath?.Invoke(gameObject);
        StopAgentSafely();

        if (_animator != null && _animator.isActiveAndEnabled)
            _animator.SetTrigger("Die");
        else
            Deactivate();
    }

    /// <summary>
    /// Método encargado de la desactivación del navmesh agent
    /// </summary>
    private void StopAgentSafely()
    {
        if (_agent == null) return;

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            _agent.updateRotation = false;
            _agent.updatePosition = false;
        }
    }

    /// <summary>
    /// Método encargado de la desactivación del zombie
    /// </summary>
    public void Deactivate()
    {
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            _agent.updateRotation = true;
            _agent.updatePosition = true;
        }

        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Método encargado de reproducir la animación de aparición
    /// </summary>
    public void StartSpawnAnimation()
    {
        _spawning = true;

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.updateRotation = false;
            _agent.updatePosition = false;
        }

        StartCoroutine(MaterializeCo());

        if (_animator != null)
            _animator.SetTrigger("Spawn");
    }

    public void EndSpawn()
    {
        _spawning = false;

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.updateRotation = true;
            _agent.updatePosition = true;
        }

        ApplyPendingSpeed();
    }
    /// <summary>
    /// Resetea toda la lógica del zombie y recalcula su vida en base a la vida de los zombies de esta ronda
    /// </summary>
    /// <param name="healthMultiplier"></param>
    public void ResetZombie(float newHealth)
    {
        _hasDied = false;
        _spawning = true;

        if (_collider != null)
            _collider.enabled = true;

        // asigna la vida final exacta que viene desde el wave controller
        health = Mathf.Max(1f, newHealth);
        baseHealth = health;

        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }

        if (_agent != null)
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
                _agent.ResetPath();
            }

            _agent.speed = walkSpeed;
            _agent.updateRotation = false;
            _agent.updatePosition = false;
        }

        if (skinnedMaterial == null && skinnedMesh != null)
            skinnedMaterial = skinnedMesh.material;

        if (skinnedMaterial != null)
            skinnedMaterial.SetFloat(DISSOLVE_PARAM, 1f);

        StartSpawnAnimation();
    }


    //Reproduce la animación de disolverse
    IEnumerator DissolveCo()
    {
        if (skinnedMaterial == null) yield break;

        if (vfxgraph != null)
            vfxgraph.Play();

        float d = 0f;
        skinnedMaterial.SetFloat(DISSOLVE_PARAM, d);

        while (d < 1f)
        {
            d += dissolveRate;
            skinnedMaterial.SetFloat(DISSOLVE_PARAM, d);
            yield return new WaitForSeconds(refreshRate);
        }
    }

    //Reproduce la animación de disolverse a la inversa
    IEnumerator MaterializeCo()
    {
        if (skinnedMaterial == null) yield break;

        if (vfxgraph != null)
            vfxgraph.Play();

        float d = 1f;
        skinnedMaterial.SetFloat(DISSOLVE_PARAM, d);

        while (d > 0f)
        {
            d -= dissolveRate;
            skinnedMaterial.SetFloat(DISSOLVE_PARAM, d);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
