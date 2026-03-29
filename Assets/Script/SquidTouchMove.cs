using UnityEngine;

public class SquidTouchMove : MonoBehaviour
{
    public float moveDistance = 0.5f;
    public float moveSpeed = 2f;

    [Header("Ink Settings")]
    public int maxInkParticles = 150;
    public float inkSpread = 0.3f;

    Vector3 targetPosition;
    bool moving = false;
    Quaternion targetRotation;
    ParticleSystem inkPS;

    void Start()
    {
        CreateInkParticleSystem();
    }

    void Update()
    {
        HandleTouch();
        if (moving)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                moving = false;
        }
    }

    void HandleTouch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    // ✅ Shoot ink on tap
                    ShootInk();

                    Vector3 cameraDir = (Camera.main.transform.position - transform.position).normalized;
                    Vector3 dir;
                    do
                    {
                        dir = new Vector3(
                            Random.Range(-1f, 1f),
                            0,
                            Random.Range(-1f, 1f)
                        ).normalized;
                    } while (Vector3.Dot(dir, cameraDir) > 0);

                    targetPosition = transform.position + dir * moveDistance;
                    targetRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
                    moving = true;
                }
            }
        }

        // 🟢 MOBILE touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        // ✅ Shoot ink on tap
                        ShootInk();

                        Vector3 cameraDir = (Camera.main.transform.position - transform.position).normalized;
                        Vector3 dir;
                        do
                        {
                            dir = new Vector3(
                                Random.Range(-1f, 1f),
                                0,
                                Random.Range(-1f, 1f)
                            ).normalized;
                        } while (Vector3.Dot(dir, cameraDir) > 0);

                        targetPosition = transform.position + dir * moveDistance;
                        targetRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
                        moving = true;
                    }
                }
            }
        }
    }

    void ShootInk()
    {
        if (inkPS == null) return;
        inkPS.Stop();
        inkPS.Clear();
        inkPS.Play();
        Debug.Log("🦑 Squid shot ink!");
    }

void CreateInkParticleSystem()
{
    GameObject inkGO = new GameObject("InkBurst");
    inkGO.transform.SetParent(transform);
    inkGO.transform.localPosition = new Vector3(0f, 0f, -0.5f);

    inkPS = inkGO.AddComponent<ParticleSystem>();
    inkPS.Stop();

    var psRenderer = inkPS.GetComponent<ParticleSystemRenderer>();
    psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
    Material inkMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
    inkMat.SetFloat("_Surface", 1f);
    inkMat.SetFloat("_Blend", 0f);
    inkMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    inkMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    inkMat.SetInt("_ZWrite", 0);
    inkMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    inkMat.renderQueue = 3000;
    psRenderer.material = inkMat;

    var main = inkPS.main;
    main.loop = false;
    main.duration = 0.2f;                                               // ✅ very short burst
    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);        // ✅ lasts longer
    main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);     // ✅ spreads slowly
    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);      // ✅ bigger particles
    main.startColor = new Color(0.02f, 0.02f, 0.05f, 1f);              // ✅ deep black ink
    main.maxParticles = maxInkParticles;
    main.simulationSpace = ParticleSystemSimulationSpace.World;
    main.gravityModifier = 0f;                                          // ✅ floats in water

    // ✅ all at once — splat!
    var emission = inkPS.emission;
    emission.rateOverTime = 0f;
    emission.SetBursts(new ParticleSystem.Burst[]
    {
        new ParticleSystem.Burst(0f, maxInkParticles)
    });

    // ✅ tight sphere — ink blob not scattered
    var shape = inkPS.shape;
    shape.shapeType = ParticleSystemShapeType.Sphere;
    shape.radius = 0.05f;                                               // ✅ tight spawn point

    // ✅ slow drift, no wild movement
    var velocityOverLife = inkPS.velocityOverLifetime;
    velocityOverLife.enabled = true;
    velocityOverLife.space = ParticleSystemSimulationSpace.World;
    velocityOverLife.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
    velocityOverLife.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
    velocityOverLife.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

    // ✅ stays dark, fades out slowly at the end
    var colorOverLife = inkPS.colorOverLifetime;
    colorOverLife.enabled = true;
    Gradient grad = new Gradient();
    grad.SetKeys(
        new GradientColorKey[] {
            new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0f),
            new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1f)
        },
        new GradientAlphaKey[] {
            new GradientAlphaKey(1f, 0f),       // ✅ fully opaque at start
            new GradientAlphaKey(0.9f, 0.6f),   // ✅ holds opacity for a long time
            new GradientAlphaKey(0f, 1f)        // ✅ only fades at the very end
        }
    );
    colorOverLife.color = grad;

    // ✅ blooms out quickly then holds size
    var sizeOverLife = inkPS.sizeOverLifetime;
    sizeOverLife.enabled = true;
    AnimationCurve sizeCurve = new AnimationCurve();
    sizeCurve.AddKey(0f, 0.1f);     // starts tiny
    sizeCurve.AddKey(0.1f, 1f);     // ✅ blooms fast
    sizeCurve.AddKey(0.8f, 0.9f);   // ✅ holds size
    sizeCurve.AddKey(1f, 0f);       // fades at end
    sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
}
}