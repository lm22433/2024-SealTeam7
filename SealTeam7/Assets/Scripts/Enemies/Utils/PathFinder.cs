using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Enemies.Utils
{
    public class Node : IEquatable<Node>
    {
        public float GScore;
        public float HScore;
        public float FScore => GScore + HScore;

        public int2 Index;
        public Node Parent;
        public Vector3 WorldPos;
        public float GradientX;
        public float GradientZ;
        public readonly List<int2> NeighbourIndices;

        public Node(int2 index, Vector3 worldPos, List<int2> neighbours)
        {
            Index = index;
            WorldPos = worldPos;
            NeighbourIndices = neighbours;
        }

        public bool Equals(Node other)
        {
            return other != null && Index.Equals(other.Index);
        }
    }
    
    public class PathFinder
    {
        private readonly int _size;
        private readonly int _lodFactor;
        private readonly float _mapSpacing;
        private Node[,] _map;
            
        public PathFinder(int size, float mapSpacing, int lodFactor)
        {
            _size = size / lodFactor;
            _mapSpacing = mapSpacing;
            _lodFactor = lodFactor;
            
            CreateMap(_size, _mapSpacing, _lodFactor);
        }

        private void CreateMap(int size, float mapSpacing, int lodFactor)
        {
            _map = new Node[size + 1, size + 1];
            for (int z = 0; z < size + 1; z++)
            {
                for (int x = 0; x < size + 1; x++)
                {
                    var neighbours = new List<int2>();
                    
                    if (z - 1 >= 0)                           neighbours.Add(new int2(z - 1, x));
                    if (z + 1 < size + 1)                     neighbours.Add(new int2(z + 1, x));
                    if (x - 1 >= 0)                           neighbours.Add(new int2(z, x - 1));
                    if (x + 1 < size + 1)                     neighbours.Add(new int2(z, x + 1));
                    if (x + 1 < size + 1 && z + 1 < size + 1) neighbours.Add(new int2(z + 1, x + 1));
                    if (x + 1 < size + 1 && z - 1 >= 0)       neighbours.Add(new int2(z - 1, x + 1));
                    if (x - 1 >= 0 && z + 1 < size + 1)       neighbours.Add(new int2(z + 1, x - 1));
                    if (x - 1 >= 0 && z - 1 >= 0)             neighbours.Add(new int2(z - 1, x - 1));
                    
                    _map[z, x] = new Node(new int2(z, x), new Vector3(x * mapSpacing * lodFactor, 0f, z * mapSpacing * lodFactor), neighbours);
                }
            }
        }

        private static float Heuristic(Node current, Node goal, float flyHeight)
        {
            // var distance = goal.WorldPos - current.WorldPos;
            // var angle = Mathf.Atan2(distance.z, distance.x) * Mathf.Rad2Deg;
            // var gradientWeightOld = (current.GradientX * Mathf.Cos(angle) + current.GradientZ * Mathf.Sin(angle)) * 100f;

            float gradientWeight, distanceWeight;
            if (flyHeight == 0) gradientWeight = (current.WorldPos.y - current.Parent?.WorldPos.y ?? current.WorldPos.y) * 100f;
            else gradientWeight = goal.WorldPos.y > flyHeight ? 10000f : 0f;
            
            var dstX = Mathf.Abs(current.WorldPos.x - goal.WorldPos.x);
            var dstZ = Mathf.Abs(current.WorldPos.z - goal.WorldPos.z);
            if (dstX > dstZ) distanceWeight = 10f * (math.SQRT2 * dstZ + (dstX - dstZ));
            else distanceWeight = 10f * (math.SQRT2 * dstX + (dstZ - dstX));
            
            return distanceWeight + gradientWeight;
        }
        
        private static Vector3[] ReconstructPath(Node start, Node end)
        {
            var nodePath = new List<Node>();
            var current = end;

            while (!current.Equals(start))
            {
                nodePath.Add(current);
                current = current.Parent;
            }
            
            nodePath.Reverse();

            var vecPath = new Vector3[nodePath.Count];
            var index = 0;
            foreach (var node in nodePath)
            {
                vecPath[index++] = node.WorldPos;
            }
            
            return vecPath;
        }
        
        public void UpdateMap(ref float[,] heights)
        {
            for (int z = 0; z < _map.GetLength(0); z++)
            {
                for (int x = 0; x < _map.GetLength(1); x++)
                {
                    _map[z, x].WorldPos.y = heights[z * heights.GetLength(0) / _map.GetLength(0), x * heights.GetLength(1) / _map.GetLength(1)];
                }
            }
        }
        
        public void UpdateGradient(ref float2[,] grads)
        {
            for (int z = 0; z < _map.GetLength(0); z++)
            {
                for (int x = 0; x < _map.GetLength(1); x++)
                {
                    _map[z, x].GradientZ = grads[z * grads.GetLength(0) / _map.GetLength(0), x * grads.GetLength(1) / _map.GetLength(1)].x;
                    _map[z, x].GradientX = grads[z * grads.GetLength(0) / _map.GetLength(0), x * grads.GetLength(1) / _map.GetLength(1)].y;
                }
            }
        }

        private Node NodeFromWorldPos(Vector3 worldPos)
        {
            var percentX = worldPos.x / (_size * _mapSpacing * _lodFactor);
            var percentZ = worldPos.z / (_size * _mapSpacing * _lodFactor);
            percentX = Mathf.Clamp01(percentX);
            percentZ = Mathf.Clamp01(percentZ);
            return _map[Mathf.RoundToInt(percentZ * _size), Mathf.RoundToInt(percentX * _size)];
        }
        
        public void FindPathAsync(Vector3 startPos, Vector3 goalPos, float flyHeight, int depth, Action<Vector3[]> setPath)
        {
            var start = NodeFromWorldPos(startPos);
            var goal = NodeFromWorldPos(goalPos);
            
            var openSet = new List<Node> { start };
            var closedSet = new HashSet<Node>();

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(node => node.FScore).First();

                if (current.Equals(goal) || closedSet.Count == depth)
                {
                    setPath(ReconstructPath(start, current));
                    return;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbourIndex in current.NeighbourIndices)
                {
                    var neighbour = _map[neighbourIndex.x, neighbourIndex.y];
                    if (neighbour.Equals(null) || closedSet.Contains(neighbour)) continue;
                    
                    var tentativeGScore = current.GScore + Heuristic(current, neighbour, flyHeight);
                    if (openSet.Contains(neighbour) && tentativeGScore > neighbour.GScore) continue;
                    
                    neighbour.Parent = current;
                    neighbour.GScore = tentativeGScore;
                    neighbour.HScore = Heuristic(neighbour, goal, flyHeight);
                        
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
            
            setPath(Array.Empty<Vector3>());
        }
    }
}