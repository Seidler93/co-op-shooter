using System.Collections;
using UnityEngine;

public class WeaponTracer : MonoBehaviour
{
    [Header("Local Tracer Travel (Owner Only)")]
    [SerializeField] private bool useLocalTracer = true;
    [SerializeField] private float tracerWidth = 0.02f;
    [SerializeField] private float tracerSpeed = 120f;
    [SerializeField] private float tracerMaxLifetime = 0.20f;
    [SerializeField] private float tracerTailLength = 1.5f;
    [SerializeField] private Material tracerMaterial;

    public void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (!useLocalTracer) return;

        GameObject go = new GameObject("LocalTracer");
        go.transform.position = start;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth * 0.6f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = tracerMaterial != null
            ? tracerMaterial
            : new Material(Shader.Find("Sprites/Default"));

        lr.SetPosition(0, start);
        lr.SetPosition(1, start);

        StartCoroutine(TravelTracerRoutine(lr, start, end));
    }

    private IEnumerator TravelTracerRoutine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        float totalDist = Vector3.Distance(start, end);
        if (totalDist < 0.001f)
        {
            if (lr != null) Destroy(lr.gameObject);
            yield break;
        }

        float speed = Mathf.Max(1f, tracerSpeed);
        float travelTime = totalDist / speed;
        travelTime = Mathf.Min(travelTime, tracerMaxLifetime);

        float t = 0f;
        Vector3 dir = (end - start).normalized;

        while (t < 1f && lr != null)
        {
            t += Time.deltaTime / travelTime;

            Vector3 head = Vector3.Lerp(start, end, t);
            float tailDist = Mathf.Min(tracerTailLength, Vector3.Distance(start, head));
            Vector3 tail = head - dir * tailDist;

            lr.SetPosition(0, tail);
            lr.SetPosition(1, head);

            yield return null;
        }

        if (lr != null)
            Destroy(lr.gameObject);
    }
}