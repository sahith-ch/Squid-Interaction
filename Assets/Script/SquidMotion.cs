using UnityEngine;

public class SquidMotion : MonoBehaviour
{
    public float swimSpeed = 0.2f;
    public float swayAmount = 10f;
    public float swaySpeed = 2f;
    public float pulseAmount = 0.05f;

    Vector3 originalScale;
    Quaternion originalRotation;

    void Start()
    {
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        transform.rotation = originalRotation * Quaternion.Euler(sway, 0, 0);

        float pulse = 1 + Mathf.Sin(Time.time * 4f) * pulseAmount;
        transform.localScale = originalScale * pulse;

        transform.Translate(Vector3.forward * swimSpeed * Time.deltaTime);
    }
}