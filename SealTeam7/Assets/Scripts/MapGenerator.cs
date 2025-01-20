using UnityEngine;
using System;

[Serializable]
public struct MapSettings {
    public ushort width;
    public ushort length;
    public ushort chunks;
    public GameObject chunkPrefab;
}

public class MapGenerator : MonoBehaviour {
    [SerializeField] private MapSettings settings;

    private void OnEnable() {
        ChunkSettings cSettings = new ChunkSettings {length = 64, width = 64, spacing = 8, heightScale = 100};
        
        for (int i = 0; i < settings.chunks; i++) {
            Instantiate(settings.chunkPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}