using System;

namespace Enemies.Utils
{
    public static class LaplaceDistribution
    {
        public static float Sample(float location, float scale)
        {
            Random random = new Random();
            double u = random.NextDouble();
            if (u < 0.5)
            {
                return (float)(location - scale * Math.Log(2 * u));
            }
            else
            {
                return (float)(location + scale * Math.Log(2 * (1 - u)));
            }
        }

        public static float ProbabilityDensity(float x, float location, float scale)
        {
            return (float)((1.0 / (2.0 * scale)) * Math.Exp(-Math.Abs(x - location) / scale));
        }
    }
}