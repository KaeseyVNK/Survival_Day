using UnityEngine;

[System.Serializable]
public class TreeSettings
{
    public GameObject treePrefab;
    [Range(0, 1)] public float treeProb = 0.08f;
    public float treeBiasHeightMin = 0.45f;
    public float treeBiasHeightMax = 0.80f;
}

[System.Serializable]
public class ResourceSettings
{
    public Transform resourceParent;

    [Header("Grass Resources")]
    public GameObject[] grassResourcePrefabs;
    [Range(0, 1)] public float grassResourceSpawnChance = 0.1f;

    [Header("Sand Resources")]
    public GameObject[] sandResourcePrefabs;
    [Range(0, 1)] public float sandResourceSpawnChance = 0.05f;
}
