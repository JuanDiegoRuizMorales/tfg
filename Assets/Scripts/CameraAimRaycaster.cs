using UnityEngine;

public class CameraAimRaycaster : MonoBehaviour
{
    [Tooltip("Distancia máxima del raycast de apuntado")]
    [SerializeField] private float _aimDistance = 100f;

    [Tooltip("Capa de colisión que puede ser apuntada")]
    [SerializeField] private LayerMask _aimLayerMask = Physics.DefaultRaycastLayers;

    public static Vector3 aimPoint { get; private set; }

    private void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _aimDistance, _aimLayerMask))
        {
            aimPoint = hit.point;
        }
        else
        {
            // Si no impacta nada, apunta a un punto imaginario frente a la cámara
            aimPoint = ray.origin + ray.direction * _aimDistance;
        }

        
#if UNITY_EDITOR
        Debug.DrawLine(ray.origin, aimPoint, Color.red);
#endif
    }
}