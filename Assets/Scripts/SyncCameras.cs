using UnityEngine;

[ExecuteAlways]
public class SyncCameras : MonoBehaviour
{
    public Camera mainCamera;
    public Camera weaponCamera;

    void OnPreCull()
    {
        if (!mainCamera || !weaponCamera) return;

        weaponCamera.transform.SetPositionAndRotation(
            mainCamera.transform.position,
            mainCamera.transform.rotation
        );
    }
}
