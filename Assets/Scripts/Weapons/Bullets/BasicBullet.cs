using UnityEngine;

public class BasicBullet : MonoBehaviour, IWeaponProjectile
{
    [Header("Bullet settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private Rigidbody _rb;
    public BaseWeaponController ownerWeapon;
    public float damage = 5f;
    public float lifeTime = 5f;
    private float _lifeTimer;

    [Header("VFX")]
    [SerializeField] private GameObject spawnVFX;
    [SerializeField] private GameObject impactVFX;
    [SerializeField] private float impactVFXLife = 2f;
    [SerializeField] private float spawnVFXLife = 1f;

    public void Initialize(BaseWeaponController owner, float newDamage, float newSpeed)
    {
        ownerWeapon = owner;
        damage = newDamage;
        _speed = newSpeed;

        if (_rb != null && gameObject.activeInHierarchy)
        {
            _rb.linearVelocity = transform.forward * _speed;
        }
    }

    private void Awake()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
    }

    [SerializeField] private GameObject spawnVFXPrefab;

    private void OnEnable()
    {
        _lifeTimer = lifeTime;

        // Reiniciar movimiento
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.linearVelocity = transform.forward * _speed;
        }

        //correccion
        if (spawnVFXPrefab != null)
        {
            var vfx = Instantiate(spawnVFXPrefab, transform.position, transform.rotation);
            // si quieres destruirlo luego:
            Destroy(vfx, 1.5f);
        }
    }


    private void OnDisable()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (_rb != null && this.gameObject.activeInHierarchy)
        {
            _rb.linearVelocity = transform.forward * _speed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // -------- IMPACT VFX --------
        if (impactVFX != null)
        {
            GameObject fx = Instantiate(
                impactVFX,
                collision.contacts[0].point,
                Quaternion.LookRotation(collision.contacts[0].normal)
            );

            Destroy(fx, impactVFXLife);
        }

        gameObject.SetActive(false);

        if (collision.collider.CompareTag("Zombie"))
        {
            ZombieBasicController zombie = collision.collider.GetComponent<ZombieBasicController>();
            if (zombie != null)
                zombie.TakeDamage(damage, ownerWeapon);
        }
    }
}
