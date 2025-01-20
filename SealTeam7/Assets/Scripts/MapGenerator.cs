using UnityEngine;
using System;
using Unity.Mathematics;

[Serializable]
public struct MapSettings {
    public ushort width;
    public ushort length;
    public ushort chunks;
    public ushort chunkWidth;
    public ushort chunkLength;
    public ushort heightScale;
    public GameObject chunkPrefab;
}

public class MapGenerator : MonoBehaviour {
    [SerializeField] private MapSettings settings;

    private void OnEnable()
    {
        var prefabScript = settings.chunkPrefab.GetComponent<ChunkGenerator>();
        var spacing = (ushort)(settings.width / math.log2(settings.chunks) / settings.chunkWidth);
        
        ChunkSettings chunkSettings = new ChunkSettings
        {
            width = settings.chunkWidth,
            length = settings.chunkLength,
            spacing = spacing,
            heightScale = settings.heightScale
        };
        
        prefabScript.SetSettings(chunkSettings);
        
        for (float x = 0; x < settings.width; x += settings.chunkWidth * spacing) {
            for (float z = 0; z < settings.length; z += settings.chunkLength * spacing)
            {
                var chunk = Instantiate(settings.chunkPrefab, new Vector3(x, 0f, z), Quaternion.identity).GetComponent<ChunkGenerator>();
                chunkSettings.x = (ushort) (x / (settings.chunkWidth * spacing));
                chunkSettings.z = (ushort) (z / (settings.chunkWidth * spacing));
                chunk.SetSettings(chunkSettings);
            }
        }
    }
}