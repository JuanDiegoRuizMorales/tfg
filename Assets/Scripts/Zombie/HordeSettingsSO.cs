using UnityEngine;

[CreateAssetMenu(fileName = "HordeSettings", menuName = "Game/Horde Settings")]
public class HordeSettingsSO : ScriptableObject
{
    [Tooltip("Radio global de persecución compartido por todos los zombies")]
    public float GlobalChaseRadius = 8f;

    [Tooltip("Radio interno compartido (zona de persecución directa)")]
    public float GlobalInnerRadius = 3f;
}
