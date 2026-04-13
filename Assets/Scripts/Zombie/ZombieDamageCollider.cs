using UnityEngine;

public class ZombieDamageCollider : MonoBehaviour
{
    public int damage = 1;

    private bool active = false;
    private bool hasDealtDamage = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!active) return;
        if (hasDealtDamage) return;
        //Si colisiona con el jugador, llama al método para hacer daño al jugador y le pasa un int de daño
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                hasDealtDamage = true;
            }
        }
    }
    //Metodos encargados de evitar quq el zombie le de varios hits al jugador
    public void ActivateDamage()
    {
        active = true;
        hasDealtDamage = false;
    }

    public void DeactivateDamage()
    {
        active = false;
    }
}
