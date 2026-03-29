using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SquidHideController : MonoBehaviour
{
    [Header("Settings")]
    public float sinkDepth = 0.3f;
    public float sinkDuration = 0.8f;
    public float hiddenDuration = 4f;
    public float riseDuration = 0.8f;

    private Vector3 originalPosition;
    private List<Renderer> renderers = new List<Renderer>();
    private bool isHidden = false;
    private bool isAnimating = false;

    void Start()
    {
        originalPosition = transform.position;

        foreach (var r in GetComponentsInChildren<Renderer>())
            renderers.Add(r);

        SetupTransparentMaterials();
    }

    void SetupTransparentMaterials()
    {
        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var mat in mats)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                SetAlpha(mat, 1f);
            }
            r.materials = mats;
        }
    }

    public void TriggerHide()
    {
        if (isHidden || isAnimating) return;
        StartCoroutine(HideCycle());
    }

    IEnumerator HideCycle()
    {
        isAnimating = true;

        // Sink + fade out
        Vector3 startPos = transform.position;
        Vector3 sinkPos  = startPos - new Vector3(0, sinkDepth, 0);
        float elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / sinkDuration);
            transform.position = Vector3.Lerp(startPos, sinkPos, t);
            SetAllAlpha(Mathf.Lerp(1f, 0f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = sinkPos;
        SetAllAlpha(0f);
        isHidden = true;
        isAnimating = false;

        // Wait hidden
        yield return new WaitForSeconds(hiddenDuration);

        // Rise + fade in
        isAnimating = true;
        elapsed = 0f;

        while (elapsed < riseDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);
            transform.position = Vector3.Lerp(sinkPos, startPos, t);
            SetAllAlpha(Mathf.Lerp(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
        SetAllAlpha(1f);
        isHidden = false;
        isAnimating = false;

        Debug.Log("Squid reappeared! 🦑");
    }

    void SetAllAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var mat in mats)
                SetAlpha(mat, alpha);
            r.materials = mats;
        }
    }

    void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
        else if (mat.HasProperty("_Color"))
        {
            Color c = mat.GetColor("_Color");
            c.a = alpha;
            mat.SetColor("_Color", c);
        }
    }
}
