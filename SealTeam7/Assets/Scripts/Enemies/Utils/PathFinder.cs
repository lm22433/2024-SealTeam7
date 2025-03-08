using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Enemies.Utils
{
    public struct Node : IEquatable<Node>
    {
        public float GScore;
        public float HScore;
        public float FScore => GScore + HScore;
        
        public readonly int X;
        public readonly int Z;
        public Vector3 WorldPos;
        public readonly Dictionary<int, float> Neighbours;

        public Node(int x, int z, Vector3 worldPos, Dictionary<int, float> neighbours)
        {
            X = x;
            Z = z;
            WorldPos = worldPos;
            Neighbours = neighbours;
        }

        public bool Equals(Node other)
        {
            return X == other.X && Z == other.Z;
        }
    }
    
    public class PathFinder
    {
        private readonly int _size;
        private readonly float _mapSpacing;
        private readonly Node[,] _map;
        
        public PathFinder(int size, float mapSpacing)
        {
            _size = size;
            _mapSpacing = mapSpacing;
            
            _map = CreateMap(size, mapSpacing);
        }

        private static Node[,] CreateMap(int size, float mapSpacing)
        {
            var length = (size + 1) * (size + 1);
            var map = new Node[size + 1, size + 1];
            for (int z = 0; z < size + 1; z++)
            {
                for (int x = 0; x < size + 1; x++)
                {
                    var index = z * (size + 1) + x;
                    var neighbours = new Dictionary<int, float>();

                    if (index + size < length)     neighbours.Add(index + size, mapSpacing * math.SQRT2);
                    if (index + size + 1 < length) neighbours.Add(index + size + 1, mapSpacing);
                    if (index + size + 2 < length) neighbours.Add(index + size + 2, mapSpacing * math.SQRT2);
                    if (index + 1 < length)        neighbours.Add(index + 1, mapSpacing);
                    if (index - size >= 0)         neighbours.Add(index - size, mapSpacing * math.SQRT2);
                    if (index - size - 1 >= 0)     neighbours.Add(index - size - 1, mapSpacing);
                    if (index - size - 2 >= 0)     neighbours.Add(index - size - 2, mapSpacing * math.SQRT2);
                    if (index - 1 >= 0)            neighbours.Add(index - 1, mapSpacing);
                    
                    map[z, x] = new Node(x, z, new Vector3(x * mapSpacing, 0f, z * mapSpacing), neighbours);
                }
            }

            return map;
        }

        private static float Heuristic(Node current, Node goal)
        {
            return Math.Abs(current.WorldPos.x - goal.WorldPos.x) + Math.Abs(current.WorldPos.z - goal.WorldPos.z);// + current.WorldY - goal.WorldY;
        }
        
        private static List<Node> ReconstructPath(Dictionary<int, Node> from, Node current)
        {
            var path = new List<Node> { current };

            while (from.ContainsKey(current.Index))
            {
                current = from[current.Index];
                path.Add(current);
            } 
            
            path.Reverse();
            return path;
        }
        
        public void UpdateMap(float[,] heights)
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
            return _map[Mathf.RoundToInt(worldPos.z / _mapSpacing), Mathf.RoundToInt(worldPos.x / _mapSpacing)];
        }
        
        public Vector3[] FindPath(Vector3 start, Vector3 goal)
        {
            var startNode = NodeFromWorldPos(start);
            var goalNode = NodeFromWorldPos(goal);
            
            var nodePath = FindPath(startNode, goalNode);
            
            var vecPath = new Vector3[nodePath.Count];
            var index = 0;
            foreach (var node in nodePath)
            {
                vecPath[index++] = node.WorldPos;
            }
            
            return vecPath;
        }

        private List<Node> FindPath(Node start, Node goal)
        {
            var openSet = new List<Node> { start };
            var closedSet = new HashSet<Node>();

            var from = new Dictionary<int, Node>();

            var gScore = new Dictionary<int, float> { [start.Index] = 0 };
            var hScore = new Dictionary<int, float> { [start.Index] = Heuristic(start, goal) };

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(node => node.FScore).First();

                openSet.Remove(current);
                closedSet.Add(current);
                
                if (current.Equals(goal)) return ReconstructPath(from, current);

                foreach (var neighbourIndex in current.Neighbours.Keys)
                {
                    var neighbour = _map.FirstOrDefault(node => node.Index == neighbourIndex);
                    if (neighbour.Equals(null) || closedSet.Contains(neighbour)) continue;
                    
                    var tentativeGScore = gScore[current.Index] + current.Neighbours[neighbourIndex];
                    if (gScore.ContainsKey(neighbour.Index) && tentativeGScore > gScore[neighbourIndex]) continue;
                    
                    from[neighbourIndex] = current;
                    gScore[neighbour.Index] = tentativeGScore;
                    hScore[neighbour.Index] = Heuristic(neighbour, goal);
                        
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }

            return null;
        }
    }
}