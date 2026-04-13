using UnityEngine;

public class ZombieAnimationEvents : MonoBehaviour
{
    [Tooltip("0 = collider Attack1, 1 = collider Attack2")]
    public ZombieDamageCollider[] damageColliders;

    // llamado desde Attack1
    public void EnableAttack1Hitbox()
    {
        if (damageColliders.Length > 0)
            damageColliders[0].ActivateDamage();
    }

    public void DisableAttack1Hitbox()
    {
        if (damageColliders.Length > 0)
            damageColliders[0].DeactivateDamage();
    }

    // llamado desde Attack2
    public void EnableAttack2Hitbox()
    {
        if (damageColliders.Length > 1)
            damageColliders[1].ActivateDamage();
    }

    public void DisableAttack2Hitbox()
    {
        if (damageColliders.Length > 1)
            damageColliders[1].DeactivateDamage();
    }
}
