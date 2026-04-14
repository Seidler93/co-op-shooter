using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnLane : MonoBehaviour
{
    [Header("Lane Identity")]
    [SerializeField] private string laneName = "North Entry";

    [Header("Spawn Setup")]
    [SerializeField] private Transform[] exteriorSpawnPoints;
    [SerializeField] private Transform entryFocusPoint;
    [SerializeField] private int maxSpawnPerWave = 99;

    [Header("Debug")]
    [SerializeField] private Color gizmoColor = new Color(0.47f, 0.96f, 0.64f, 0.95f);
    [SerializeField] private bool drawGizmos = true;

    public string LaneName => laneName;
    public int MaxSpawnPerWave => Mathf.Max(1, maxSpawnPerWave);
    public bool HasValidSpawnPoint => GetSpawnPointCount() > 0;

    public int GetSpawnPointCount()
    {
        if (exteriorSpawnPoints == null)
            return 0;

        int count = 0;
        for (int i = 0; i < exteriorSpawnPoints.Length; i++)
        {
            if (exteriorSpawnPoints[i] != null)
                count++;
        }

        return count;
    }

    public Transform GetRandomSpawnPoint()
    {
        if (exteriorSpawnPoints == null || exteriorSpawnPoints.Length == 0)
            return transform;

        int validCount = GetSpawnPointCount();
        if (validCount == 0)
            return transform;

        int targetIndex = Random.Range(0, validCount);
        int seen = 0;

        for (int i = 0; i < exteriorSpawnPoints.Length; i++)
        {
            Transform point = exteriorSpawnPoints[i];
            if (point == null)
                continue;

            if (seen == targetIndex)
                return point;

            seen++;
        }

        return transform;
    }

    public Quaternion GetSpawnRotation(Vector3 spawnPosition)
    {
        if (entryFocusPoint == null)
            return transform.rotation;

        Vector3 flatDirection = entryFocusPoint.position - spawnPosition;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = gizmoColor;

        if (entryFocusPoint != null)
        {
            Gizmos.DrawWireSphere(entryFocusPoint.position, 0.45f);
        }

        if (exteriorSpawnPoints == null)
            return;

        for (int i = 0; i < exteriorSpawnPoints.Length; i++)
        {
            Transform point = exteriorSpawnPoints[i];
            if (point == null)
                continue;

            Gizmos.DrawSphere(point.position, 0.22f);

            if (entryFocusPoint != null)
            {
                Gizmos.DrawLine(point.position, entryFocusPoint.position);
            }
        }
    }
}
