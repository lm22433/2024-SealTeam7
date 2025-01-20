using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct ChunkSettings {
    public ushort width;
    public ushort length;
    public ushort spacing;
    public ushort heightScale;
}

public class ChunkGenerator : MonoBehaviour {
    
    private ChunkSettings _settings;
    private Texture2D _heightMap;
    private NativeArray<float3> _vertices;
    private Mesh _mesh;

    public ChunkGenerator(ChunkSettings s, Texture2D t) {
        _settings = s;
        _heightMap = t;
    }

    private void OnEnable() {
        _mesh = CreateMesh();
    }

    private void OnDisable() {
        _vertices.Dispose();
    }

    private void Update() {
        if (_heightMap) UpdateMesh();
    }

    private Mesh CreateMesh() {
        Mesh mesh = new Mesh {name = "Generated Mesh"};
        _vertices = new NativeArray<float3>(_settings.width * _settings.length, Allocator.Persistent);

        // generate grid
        var index = 0;
        for (int x = 0; x < _settings.width; x++) {
            for (int z = 0; z < _settings.length; z++) {
                _vertices[index++] = new Vector3(x * _settings.spacing, 0f, z * _settings.spacing);
            }
        }
        
        // index triangles from vertices
        var indices = new int[(_settings.width - 1) * (_settings.length - 1) * 6];
        index = 0;
        for (var y = 0; y < _settings.length - 1; y++)
        {		
            for (var x = 0; x < _settings.width - 1; x++)
            {
                var baseIndex = y * _settings.width + x;
                indices[index++] = baseIndex;
                indices[index++] = baseIndex + 1;
                indices[index++] = baseIndex + _settings.width + 1;
                indices[index++] = baseIndex; 
                indices[index++] = baseIndex + _settings.width + 1;
                indices[index++] = baseIndex + _settings.width;
            }
        }
        
        mesh.SetVertices(_vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        return mesh;
    }

    private void UpdateMesh() {
        // choose appropriate mipmap level from mesh size
        //var mip = (int) math.clamp(math.log2(math.min((float) heightMap.height / settings.length, (float) heightMap.width / settings.width)), 0f, 10f); 
        //var heights = heightMap.GetPixelData<Color32>(mip);

        var heights = _heightMap.GetPixelData<Color32>(0);
        
        var posHandle = new HeightSampler {
            Vertices = _vertices,
            Heights = heights,
            Scale = _settings.heightScale
        }.Schedule(_settings.width * _settings.length, 1);
        posHandle.Complete();
        
        _mesh.MarkDynamic();
        _mesh.SetVertices(_vertices);
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();
        heights.Dispose();
    }
}

[BurstCompile]
public struct HeightSampler : IJobParallelFor {

    public NativeArray<float3> Vertices;
    public NativeArray<Color32> Heights;
    public float Scale;
    
    public void Execute(int index) {
        var step = (float) Heights.Length / Vertices.Length;
        var p = Vertices[index];
        var y = p.y;
        y = Heights[index].r / 255f * Scale;
        p.y = y;
        Vertices[index] = p;
    }
}
