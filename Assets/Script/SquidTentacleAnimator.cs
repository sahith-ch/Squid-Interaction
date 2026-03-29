using UnityEngine;
using System.Collections.Generic;

public class SquidSwimAnimator : MonoBehaviour
{
    [Header("Swim Stroke")]
    public float swimSpeed = 1.0f;
    public float openAngle = 30f;       // How far tentacles flare open
    public float closeAngle = -15f;     // How far they close inward
    public float strokeSharpness = 2f;

    [Header("Tip")]
    public float tipCurlAngle = 20f;
    public float lagBetweenSegments = 0.15f;

    // ROOT of each tentacle + its sibling child bone name
    // Structure: Bone.002 -> Bone.017, Bone.003 -> Bone.026, etc.
    private struct TentacleChain
    {
        public string root;
        public string child;
        public Vector3 localFlareAxis; // Which LOCAL axis to rotate around
    }

    private TentacleChain[] tentacles = new TentacleChain[]
    {
        new TentacleChain { root = "Bone.002", child = "Bone.017", localFlareAxis = Vector3.right },
        new TentacleChain { root = "Bone.003", child = "Bone.026", localFlareAxis = new Vector3(0.7f, 0f, 0.7f) },
        new TentacleChain { root = "Bone.007", child = "Bone.030", localFlareAxis = Vector3.forward },
        new TentacleChain { root = "Bone.016", child = null,       localFlareAxis = new Vector3(-0.7f, 0f, 0.7f) },
        new TentacleChain { root = "Bone.032", child = null,       localFlareAxis = Vector3.left },
        new TentacleChain { root = "Bone.034", child = null,       localFlareAxis = new Vector3(-0.7f, 0f, -0.7f) },
        new TentacleChain { root = "Bone.038", child = null,       localFlareAxis = Vector3.back },
        new TentacleChain { root = "Bone.046", child = null,       localFlareAxis = new Vector3(0.7f, 0f, -0.7f) },
    };

    private Dictionary<string, TentacleChain> boneToChain = new Dictionary<string, TentacleChain>();
    private Transform[] allBones;

    void Start()
    {
        allBones = GetComponentsInChildren<Transform>();

        // Map every bone name to its chain info
        foreach (var chain in tentacles)
        {
            boneToChain[chain.root] = chain;
            if (chain.child != null)
                boneToChain[chain.child] = chain;
        }
    }

    float GetStroke(float t)
    {
        float raw = Mathf.Sin(t * Mathf.PI * 2f);
        return Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw), strokeSharpness);
    }

    void Update()
    {
        float time = Time.time * swimSpeed;

        foreach (Transform b in allBones)
        {
            if (!boneToChain.ContainsKey(b.name)) continue;

            TentacleChain chain = boneToChain[b.name];
            bool isTip = (b.name == chain.child);

            // Tips lag behind root
            float delay = isTip ? lagBetweenSegments : 0f;
            float stroke = GetStroke(time - delay);

            // Stroke: positive = flare open, negative = close
            float angle = stroke > 0
                ? stroke * openAngle
                : stroke * Mathf.Abs(closeAngle);

            // Tips curl more
            if (isTip)
                angle += stroke * tipCurlAngle;

            // Rotate around THIS tentacle's local outward axis
            b.localRotation = Quaternion.AngleAxis(angle, chain.localFlareAxis);
        }
    }
}