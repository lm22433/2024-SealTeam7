using UnityEngine;

public class TerrainDeform : MonoBehaviour
{
    [SerializeField]
    private Texture2D heightMap;
    private TerrainData _terrainData;
    private float[,] _heights;
    
    void Awake()
    {
        _terrainData = GetComponentInParent<Terrain>().terrainData;
    }
    
    void Update()
    {
        _heights = new float[heightMap.width, heightMap.height];
        if (!heightMap) return;
        
        for (int y = 0; y < heightMap.height; y++)
        {
            for (int x = 0; x < heightMap.width; x++)
            {
                _heights[x, y] = heightMap.GetPixel(x, y).a;
                //Debug.Log(heightMap.GetPixel(x, y).a);
                //Debug.Log(_heights[x, y]);
            }
        }
        
        _terrainData.SetHeightsDelayLOD(0, 0, _heights);
        _terrainData.SyncHeightmap();
        enabled = false;
    }

    
    private void OnValidate()
    {
        enabled = true;
    }
}
