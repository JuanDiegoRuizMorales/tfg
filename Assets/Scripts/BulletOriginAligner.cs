using UnityEngine;

public class BulletOriginAligner : MonoBehaviour
{
    [Tooltip("Si está activado, solo se rotará el eje Y (horizontal)")]
    [SerializeField] private bool onlyRotateY = true;

    private void LateUpdate()
    {
        // Verifica que el punto de la cámara existe
        Vector3 target = CameraAimRaycaster.aimPoint;
        if (target == Vector3.zero) return;

        // Calcula la dirección hacia ese punto
        Vector3 direction = (target - transform.position).normalized;

        // Aplica la rotación
        if (onlyRotateY)
        {
            // Solo rota horizontalmente
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            //Rotacion completa en todos los ejes
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
