// GodRaysController.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GodRaysController : MonoBehaviour
{
    [Header("God Ray Settings")]
    public int rayCount = 8;
    public float rayLength = 2.0f;
    public float rayWidth = 0.04f;
    public float raySpread = 0.6f;
    public Color rayColor = new Color(0.5f, 0.85f, 1f, 0.08f);
    public float animSpeed = 0.3f;

    private GameObject[] rays;
    private Material rayMat;

    void Start()
    {
        rayMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        rayMat.color = rayColor;
        rayMat.SetFloat("_Surface", 1); // transparent

        rays = new GameObject[rayCount];
        for (int i = 0; i < rayCount; i++)
            rays[i] = CreateRay(i);
    }

    GameObject CreateRay(int index)
    {
        GameObject ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ray.name = $"GodRay_{index}";
        ray.transform.SetParent(transform);
        Destroy(ray.GetComponent<BoxCollider>());

        // Spread rays in a cone below the light
        float angle = (index / (float)rayCount) * 360f;
        float spread = Random.Range(0f, raySpread);
        ray.transform.localPosition = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * spread,
            0,
            Mathf.Sin(angle * Mathf.Deg2Rad) * spread
        );

        ray.transform.localRotation = Quaternion.Euler(
            Random.Range(-10f, 10f), angle, Random.Range(-5f, 5f)
        );

        ray.transform.localScale = new Vector3(
            rayWidth * Random.Range(0.5f, 1.5f),
            rayLength * Random.Range(0.7f, 1.3f),
            rayWidth
        );

        ray.GetComponent<Renderer>().material = rayMat;
        return ray;
    }

    void Update()
    {
        // Slowly sway rays like real underwater light shafts
        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i] == null) continue;
            float sway = Mathf.Sin(Time.time * animSpeed + i * 1.3f) * 3f;
            rays[i].transform.localRotation = Quaternion.Euler(
                sway, (i / (float)rayCount) * 360f, sway * 0.5f
            );
        }
    }
}