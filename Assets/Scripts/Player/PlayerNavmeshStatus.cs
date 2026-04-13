using UnityEngine;
using UnityEngine.AI;

public class PlayerNavmeshStatus : MonoBehaviour
{
    public static bool IsPlayerOnNavmesh { get; private set; }
    public static Vector3 LastValidPosition { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private float sampleRadius = 2f;

    private void Update()
    {
        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            IsPlayerOnNavmesh = true;
            LastValidPosition = hit.position;
        }
        else
        {
            IsPlayerOnNavmesh = false;
        }
    }
}
