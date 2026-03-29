using UnityEngine;

public class OceanFloorSpawner : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject oceanFloorPrefab;
    public UnityEngine.Rendering.Volume underwaterVolume;
    public GameObject godRaysObject;

    [Header("Floor Settings")]
    public float floorScale = 1.5f;

    private GameObject spawnedFloor;

    // Call from OceanPlacementManager after anchor is ready
    public void BuildOceanFloor(Transform anchor)
    {
        SpawnFloor(anchor);
        EnableUnderwaterEffects();
    }

    void SpawnFloor(Transform anchor)
    {
        spawnedFloor = Instantiate(oceanFloorPrefab, anchor);
        spawnedFloor.transform.localPosition = Vector3.zero;
        spawnedFloor.transform.localScale = Vector3.one * floorScale;
    }

    void EnableUnderwaterEffects()
    {
        if (underwaterVolume != null)
            underwaterVolume.gameObject.SetActive(true);

        if (godRaysObject != null)
            godRaysObject.SetActive(true);
    }
}