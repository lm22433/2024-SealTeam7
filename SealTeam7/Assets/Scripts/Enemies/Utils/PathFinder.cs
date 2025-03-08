using System;
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
        public readonly Dictionary<int2, float> Neighbours;

        public Node(int2 index, Vector3 worldPos, Dictionary<int2, float> neighbours)
        {
            Index = index;
            WorldPos = worldPos;
            Neighbours = neighbours;
        }

        public bool Equals(Node other)
        {
            return other != null && Index.Equals(other.Index);
        }
    }
    
    public class PathFinder
    {
        private readonly int _size;
        private readonly float _mapSpacing;
        private Node[,] _map;
        
        public PathFinder(int size, float mapSpacing)
        {
            _size = size;
            _mapSpacing = mapSpacing;
            
            CreateMap(size, mapSpacing);
        }

        private void CreateMap(int size, float mapSpacing)
        {
            _map = new Node[size + 1, size + 1];
            for (int z = 0; z < size + 1; z++)
            {
                for (int x = 0; x < size + 1; x++)
                {
                    var neighbours = new Dictionary<int2, float>();
                    
                    if (z - 1 >= 0)                           neighbours.Add(new int2(z - 1, x), mapSpacing * 10f);
                    if (z + 1 < size + 1)                     neighbours.Add(new int2(z + 1, x), mapSpacing * 10f);
                    if (x - 1 >= 0)                           neighbours.Add(new int2(z, x - 1), mapSpacing * 10f);
                    if (x + 1 < size + 1)                     neighbours.Add(new int2(z, x + 1), mapSpacing * 10f);
                    if (x + 1 < size + 1 && z + 1 < size + 1) neighbours.Add(new int2(z + 1, x + 1), mapSpacing * math.SQRT2 * 10f);
                    if (x + 1 < size + 1 && z - 1 >= 0)       neighbours.Add(new int2(z - 1, x + 1), mapSpacing * math.SQRT2 * 10f);
                    if (x - 1 >= 0 && z + 1 < size + 1)       neighbours.Add(new int2(z + 1, x - 1), mapSpacing * math.SQRT2 * 10f);
                    if (x - 1 >= 0 && z - 1 >= 0)             neighbours.Add(new int2(z - 1, x - 1), mapSpacing * math.SQRT2 * 10f);
                    
                    _map[z, x] = new Node(new int2(x, z), new Vector3(x * mapSpacing, 0f, z * mapSpacing), neighbours);
                }
            }
        }

        private static float Heuristic(Node current, Node goal)
        {
            return (Math.Abs(current.WorldPos.x - goal.WorldPos.x) + Math.Abs(current.WorldPos.z - goal.WorldPos.z) + (current.WorldPos.y - goal.WorldPos.y)) * 10f;
        }
        
        private static List<Node> ReconstructPath(Node start, Node end)
        {
            var path = new List<Node>();
            var current = end;

            while (!current.Equals(start))
            {
                path.Add(current);
                current = current.Parent;
            }
            
            path.Reverse();
            return path;
        }
        
        public void UpdateMap(ref float[,] heights)
        {
            for (int z = 0; z < heights.GetLength(0); z++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    _map[z, x].WorldPos.y = heights[z, x];
                }
            }
        }

        private Node NodeFromWorldPos(Vector3 worldPos)
        {
            var percentX = worldPos.x / (_size * _mapSpacing);
            var percentZ = worldPos.z / (_size * _mapSpacing);
            percentX = Mathf.Clamp01(percentX);
            percentZ = Mathf.Clamp01(percentZ);
            return _map[Mathf.RoundToInt(percentX * _size), Mathf.RoundToInt(percentZ * _size)];
        }
        
        public Vector3[] FindPath(Vector3 start, Vector3 goal, int depth)
        {
            var startNode = NodeFromWorldPos(start);
            var goalNode = NodeFromWorldPos(goal);
            
            var nodePath = FindPath(startNode, goalNode, depth);
            
            var vecPath = new Vector3[nodePath.Count];
            var index = 0;
            foreach (var node in nodePath)
            {
                vecPath[index++] = node.WorldPos;
            }
            
            return vecPath;
        }

        private List<Node> FindPath(Node start, Node goal, int depth)
        {
            var openSet = new List<Node> { start };
            var closedSet = new HashSet<Node>();

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(node => node.FScore).First();

                openSet.Remove(current);
                closedSet.Add(current);
                
                if (current.Equals(goal) || closedSet.Count == depth) return ReconstructPath(start, current);

                foreach (var neighbourIndex in current.Neighbours.Keys)
                {
                    var neighbour = _map[neighbourIndex.x, neighbourIndex.y];
                    if (neighbour.Equals(null) || closedSet.Contains(neighbour)) continue;
                    
                    var tentativeGScore = current.GScore + current.Neighbours[neighbourIndex];
                    if (openSet.Contains(neighbour) && tentativeGScore > neighbour.GScore) continue;
                    
                    neighbour.GScore = tentativeGScore;
                    neighbour.HScore = Heuristic(neighbour, goal);
                    neighbour.Parent = current;
                        
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }

            return null;
        }
    }
}