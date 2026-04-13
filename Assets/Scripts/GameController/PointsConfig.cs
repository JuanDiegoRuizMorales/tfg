using UnityEngine;

[CreateAssetMenu(menuName = "Game/PointsConfig")]
public class PointsConfig : ScriptableObject
{
    public int pointsPerDamage = 10;
    public int pointsPerKill = 100;
}
