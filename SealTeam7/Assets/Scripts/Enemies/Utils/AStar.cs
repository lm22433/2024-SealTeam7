using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Enemies.Utils
{
    public struct Node : IEquatable<Node>
    {
        public readonly int Index;
        public readonly float WorldX;
        public float WorldY;
        public readonly float WorldZ;
        public readonly Dictionary<int, float> Neighbours;

        public Node(int index, float worldX, float worldY, float worldZ, Dictionary<int, float> neighbours)
        {
            Index = index;
            WorldX = worldX;
            WorldY = worldY;
            WorldZ = worldZ;
            Neighbours = neighbours;
        }

        public bool Equals(Node other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is Node other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }
    }
    
    public class AStar
    {
        public HashSet<Node> Map;
        
        public AStar(int size, float mapSpacing)
        {
            Map = CreateMap(size, mapSpacing);
        }

        private HashSet<Node> CreateMap(int size, float mapSpacing)
        {
            var length = (size + 1) * (size + 1);
            var map = new HashSet<Node>(length);
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
                    
                    map.Add(new Node(index, x * mapSpacing, 0f, z * mapSpacing, neighbours));
                }
            }

            return map;
        }
        
        public void UpdateMap(float[] heights)
        {
            foreach (var node in Map.ToList())
            {
                var newNode = node;
                newNode.WorldY = heights[node.Index];
                Debug.Assert(Map.Remove(node));
                Map.Add(newNode);
            }
        }

        private static float Heuristic(Node current, Node goal)
        {
            return Math.Abs(current.WorldX - goal.WorldX) + Math.Abs(current.WorldZ - goal.WorldZ);// + current.WorldY - goal.WorldY;
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

        public List<Node> FindPath(Node start, Node goal)
        {
            var openSet = new List<Node> { start };
            var closedSet = new HashSet<Node>();

            var from = new Dictionary<int, Node>();

            var gScore = new Dictionary<int, float> { [start.Index] = 0 };
            var hScore = new Dictionary<int, float> { [start.Index] = Heuristic(start, goal) };

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(node => gScore[node.Index] + hScore[node.Index]).First();
                if (current.Equals(goal)) return ReconstructPath(from, current);

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbourIndex in current.Neighbours.Keys)
                {
                    var neighbour = Map.FirstOrDefault(node => node.Index == neighbourIndex);
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