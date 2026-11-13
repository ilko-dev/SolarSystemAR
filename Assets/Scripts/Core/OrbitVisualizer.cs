
using UnityEngine;
using UnityEngine.Rendering;

public class OrbitVisualizer : MonoBehaviour
{
    [SerializeField] private Transform sun;
    [SerializeField] private Transform[] planets;
    [SerializeField] private Material orbitMaterial;

    [SerializeField, Range(32, 256)]
    private int segments = 128;

    [SerializeField, Min(0.005f)]
    private float lineWidth = 0.035f;

    [SerializeField]
    private float heightOffset = -0.02f;

    private void Start()
    {
        if (sun == null || orbitMaterial == null)
        {
            Debug.LogError("OrbitVisualizer is not configured.", this);
            return;
        }

        Vector3 center = transform.InverseTransformPoint(sun.position);

        foreach (Transform planet in planets)
        {
            if (planet == null)
                continue;

            Vector3 position = transform.InverseTransformPoint(
                planet.position);

            float radius = Vector2.Distance(
                new Vector2(center.x, center.z),
                new Vector2(position.x, position.z));

            CreateOrbit(planet.name, center, radius,
                position.y + heightOffset);
        }
    }

    
    private void CreateOrbit(
        string planetName,
        Vector3 center,
        float radius,
        float height)
    {
        GameObject orbit = new GameObject($"Orbit_{planetName}");
        orbit.transform.SetParent(transform, false);

        orbit.transform.localPosition =
            new Vector3(center.x, height, center.z);

        orbit.transform.localRotation =
            Quaternion.Euler(90f, 0f, 0f);

        LineRenderer line = orbit.AddComponent<LineRenderer>();

        line.sharedMaterial = orbitMaterial;
        line.useWorldSpace = false;
        line.alignment = LineAlignment.TransformZ;
        line.loop = true;
        line.positionCount = segments;

        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.widthMultiplier = lineWidth;

        line.startColor = Color.white;
        line.endColor = Color.white;

        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

}
