using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample.HandLandmarkDetection; // 👈 THIS LINE
public class ThumbsUpGestureDetector : MonoBehaviour
{
    [Header("References")]
    public HandLandmarkerRunner handLandmarkerRunner;
    public Animator squidAnimator;
    public SquidHideController squidHideController;

    [Header("Settings")]
    public string animationTriggerName = "ThumbsUpReaction";
    public float detectionCooldown = 2f;

    private float lastThumbsUpTime = -99f;
    private float lastFistTime = -99f;

    void OnEnable()
    {
        handLandmarkerRunner.OnLandmarkDetected += OnLandmarkDetected;
    }

    void OnDisable()
    {
        handLandmarkerRunner.OnLandmarkDetected -= OnLandmarkDetected;
    }

    void OnLandmarkDetected(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            return;

        foreach (var hand in result.handLandmarks)
        {
            var lm = hand.landmarks;

            if (IsThumbsUp(lm))
            {
                if (Time.time - lastThumbsUpTime < detectionCooldown) continue;
                lastThumbsUpTime = Time.time;
                TriggerSquidReaction();
            }
            else if (IsFist(lm))
            {
                if (Time.time - lastFistTime < detectionCooldown) continue;
                lastFistTime = Time.time;
                TriggerSquidHide();
            }
        }
    }

bool IsThumbsUp(IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> lm)
{
    bool thumbUp = lm[4].y < lm[0].y - 0.1f;

    bool fingersDown =
        lm[8].y > lm[6].y &&
        lm[12].y > lm[10].y &&
        lm[16].y > lm[14].y &&
        lm[20].y > lm[18].y;

    return thumbUp && fingersDown;
}

bool IsFist(IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> lm)
{
    bool indexCurled  = lm[8].y  > lm[5].y;
    bool middleCurled = lm[12].y > lm[9].y;
    bool ringCurled   = lm[16].y > lm[13].y;
    bool pinkyCurled  = lm[20].y > lm[17].y;
    bool thumbCurled  = lm[4].x  > lm[3].x;

    return indexCurled && middleCurled && ringCurled && pinkyCurled && thumbCurled;
}

    void TriggerSquidReaction()
    {
        if (squidAnimator != null)
            squidAnimator.SetTrigger(animationTriggerName);
        Debug.Log("👍 Thumbs up detected!");
    }

    void TriggerSquidHide()
    {
        if (squidHideController != null)
            squidHideController.TriggerHide();
        Debug.Log("✊ Fist detected — squid hiding!");
    }
}
