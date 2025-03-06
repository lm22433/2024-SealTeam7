using UnityEngine;

namespace Python
{
    public struct HandLandmarks
    {
        public Vector3[] Left;
        public Vector3[] Right;
        
        public override string ToString()
        {
            var left = Left == null ? "null" : $"[{string.Join(", ", Left)}]";
            var right = Right == null ? "null" : $"[{string.Join(", ", Right)}]";
            return $"HandLandmarks(Left={left}, Right={right})";
        }
    }
}