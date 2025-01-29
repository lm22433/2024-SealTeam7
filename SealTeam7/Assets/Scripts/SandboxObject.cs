public abstract class SandboxObject
{
    private float _x;
    private float _y;
    
    public float GetX()
    {
        return _x;
    }
    
    public float GetY()
    {
        return _y;
    }

    public class Bunker : SandboxObject
    {
        public Bunker(float x, float y)
        {
            _x = x;
            _y = y;
        }

        public override string ToString()
        {
            return $"Bunker(x={_x}, y={_y})";
        }
    }

    public class Spawner : SandboxObject
    {
        public Spawner(float x, float y)
        {
            _x = x;
            _y = y;
        }

        public override string ToString()
        {
            return $"Spawner(x={_x}, y={_y})";
        }
    }
}