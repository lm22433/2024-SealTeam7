using UnityEngine;

public class TerrainDeform : MonoBehaviour
{
    [SerializeField]
    private Texture2D heightMap;
    private TerrainData _terrainData;
    private float[,] _heights;

    private bool _updated; 
    
    void Awake()
    {
        _heights = new float[heightMap.width, heightMap.height];
        _terrainData = GetComponentInParent<Terrain>().terrainData;
    }
    
    void Update()
    {
        if (!_updated || !heightMap)
        {
            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    _heights[x, y] = heightMap.GetPixel(x, y).r;
                }
            }
            
            _terrainData.SetHeightsDelayLOD(0, 0, _heights);
            _terrainData.SyncHeightmap();
            _updated = true;
        }
    }

    
    private void OnValidate()
    {
        _updated = false;
    }
}
