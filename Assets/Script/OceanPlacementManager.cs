using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Rendering;

public class OceanPlacementManager : MonoBehaviour
{
    [Header("Floor")]
    public GameObject oceanFloor;
    public float floorScale = 1.5f;

    [Header("Squid")]
    public GameObject squidPrefab;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARAnchorManager anchorManager;
    public ARCameraBackground arCameraBackground;

    [Header("Underwater Effects")]
    public Volume underwaterVolume;

    [Header("Bubble Settings")]
    public int maxBubbles = 80;
    public float bubbleSpread = 2f;

    [Header("Placement Settings")]
    public float minPlaneSize = 0.4f;
    public float positionSmoothSpeed = 5f;

    [Header("Debug")]
    public bool showTrackingState = false;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private bool placed = false;
    private ARAnchor mainAnchor;
    private GameObject spawnedFloor;
    private GameObject spawnedSquid;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    void Update()
    {
        if (placed)
        {
            if (mainAnchor == null || spawnedFloor == null)
            {
                placed = false;
                return;
            }

            if (!RenderSettings.fog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.02f, 0.25f, 0.28f, 1f);
                RenderSettings.fogDensity = 0.6f;
            }

            SmoothFollowAnchor();
            return;
        }

        // MOBILE (touch)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                TryPlace(touch.position);
        }

        // PC (mouse)
        if (Input.GetMouseButtonDown(0))
            TryPlace(Input.mousePosition);
    }

    void TryPlace(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, raycastHits, TrackableType.PlaneWithinPolygon))
            return;

        Pose hitPose = raycastHits[0].pose;
        ARPlane plane = raycastHits[0].trackable as ARPlane;

        if (plane == null) return;

        if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize)
        {
            Debug.Log("Plane too small — keep scanning.");
            return;
        }

        mainAnchor = CreateWorldAnchor(hitPose);
        if (mainAnchor == null || mainAnchor.gameObject == null)
        {
            Debug.LogError("Anchor creation failed or was immediately destroyed.");
            return;
        }

        spawnedFloor = Instantiate(oceanFloor, hitPose.position, hitPose.rotation);
        spawnedFloor.transform.localScale = Vector3.one * floorScale;
        frozenPosition = hitPose.position;
        frozenRotation = hitPose.rotation;

        SpawnSquid(hitPose);
        placed = true;

        // Defer plane manager shutdown by one frame so TrackableSpawner
        // finishes its current Update before we pull the rug out.
        // anchorManager is intentionally NOT disabled — disabling it destroys
        // anchor transforms mid-frame and causes MissingReferenceException.
        StartCoroutine(DeferredShutdownManagers());

        EnableUnderwaterEffects();
        Debug.Log("Ocean floor placed!");
    }

    IEnumerator DeferredShutdownManagers()
    {
        yield return null; // Wait one frame for TrackableSpawner to finish

        planeManager.requestedDetectionMode = PlaneDetectionMode.None;
        foreach (var p in planeManager.trackables)
            p.gameObject.SetActive(false);

        // planeManager itself is left enabled so it can be cleanly
        // re-enabled on reset without needing to re-register subsystems.
    }

    void SpawnSquid(Pose hitPose)
    {
        if (squidPrefab == null)
        {
            Debug.LogWarning("Squid prefab not assigned!");
            return;
        }

        Vector3 squidOffset = new Vector3(0f, 1f, 3f);
        Vector3 squidWorldPos = hitPose.position + hitPose.rotation * squidOffset;
        Quaternion squidRotation = Quaternion.Euler(-120f, 120f, 0f);
        spawnedSquid = Instantiate(squidPrefab, squidWorldPos, squidRotation);

        var gesture = FindObjectOfType<ThumbsUpGestureDetector>();
        if (gesture != null && spawnedSquid != null)
        {
            gesture.squidAnimator = spawnedSquid.GetComponent<Animator>();
            gesture.squidHideController = spawnedSquid.GetComponent<SquidHideController>();
        }

        Debug.Log("Squid spawned!");
    }

    ARAnchor CreateWorldAnchor(Pose pose)
    {
        GameObject anchorGO = new GameObject("OceanAnchor");
        anchorGO.transform.SetPositionAndRotation(pose.position, pose.rotation);
        ARAnchor anchor = anchorGO.AddComponent<ARAnchor>();

        if (anchor == null)
        {
            Destroy(anchorGO);
            return null;
        }

        return anchor;
    }

    void EnableUnderwaterEffects()
    {
        if (arCameraBackground != null)
            arCameraBackground.enabled = false;
        else
            Debug.LogError("arCameraBackground is NULL!");

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.25f, 0.28f, 1f);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.25f, 0.28f, 1f);
        RenderSettings.fogDensity = 0.4f;

        SpawnBubbles();

        if (underwaterVolume != null)
            underwaterVolume.gameObject.SetActive(true);
    }

    void SpawnBubbles()
    {
        if (spawnedFloor == null)
        {
            Debug.LogWarning("Cannot spawn bubbles — spawnedFloor is null.");
            return;
        }

        GameObject bubbleGO = new GameObject("Bubbles");
        bubbleGO.transform.SetParent(spawnedFloor.transform);
        bubbleGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = bubbleGO.AddComponent<ParticleSystem>();
        ps.Stop();

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material bubbleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        bubbleMat.SetFloat("_Surface", 1f);
        bubbleMat.SetFloat("_Blend", 0f);
        bubbleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bubbleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bubbleMat.SetInt("_ZWrite", 0);
        bubbleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bubbleMat.renderQueue = 3000;
        psRenderer.material = bubbleMat;

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
        main.startColor = new Color(0.6f, 0.85f, 1f, 0.4f);
        main.maxParticles = maxBubbles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bubbleSpread, 0.1f, bubbleSpread);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.7f, 0.9f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.2f),
                new GradientAlphaKey(0.5f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = grad;

        ps.Play();
    }

    void SmoothFollowAnchor()
    {
        if (spawnedFloor == null || mainAnchor == null)
        {
            placed = false;
            return;
        }

        Transform anchorTransform = mainAnchor.transform;
        if (anchorTransform == null || !mainAnchor.gameObject.activeInHierarchy)
        {
            placed = false;
            return;
        }

        if (showTrackingState)
            Debug.Log($"Anchor state: {mainAnchor.trackingState}");

        switch (mainAnchor.trackingState)
        {
            case TrackingState.Tracking:
                frozenPosition = anchorTransform.position;
                frozenRotation = anchorTransform.rotation;
                spawnedFloor.transform.position = Vector3.Lerp(
                    spawnedFloor.transform.position,
                    frozenPosition,
                    Time.deltaTime * positionSmoothSpeed
                );
                spawnedFloor.transform.rotation = Quaternion.Slerp(
                    spawnedFloor.transform.rotation,
                    frozenRotation,
                    Time.deltaTime * positionSmoothSpeed
                );
                break;

            case TrackingState.Limited:
            case TrackingState.None:
                spawnedFloor.transform.position = Vector3.Lerp(
                    spawnedFloor.transform.position,
                    frozenPosition,
                    Time.deltaTime * 2f
                );
                break;
        }
    }

    public void ResetPlacement()
    {
        if (arCameraBackground != null)
            arCameraBackground.enabled = true;

        Camera cam = Camera.main;
        if (cam != null)
            cam.clearFlags = CameraClearFlags.Depth;

        RenderSettings.fog = false;
        placed = false;

        if (spawnedFloor != null) { Destroy(spawnedFloor); spawnedFloor = null; }
        if (spawnedSquid != null) { Destroy(spawnedSquid); spawnedSquid = null; }
        if (mainAnchor != null)   { Destroy(mainAnchor.gameObject); mainAnchor = null; }

        if (underwaterVolume != null)
            underwaterVolume.gameObject.SetActive(false);

        // anchorManager was never disabled, so no need to re-enable it.
        // Just restore plane detection for the next placement.
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        planeManager.enabled = true;
    }
}