using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Utilities
{
    internal class Utils
    {
        public static double LerpYawRad(double fromYaw, double toYaw, double t)
        {
            double delta = (toYaw - fromYaw + Math.PI * 3) % (Math.PI * 2) - Math.PI;
            return (fromYaw + delta * t + Math.PI * 2) % (Math.PI * 2);
        }

        public static double GetYawBetweenPositions(Vec3d pos1, Vec3d pos2)
        {
            double dx = pos1.X - pos2.X;
            double dz = pos1.Z - pos2.Z;
            double yaw = Math.Atan2(dx, dz);
            return yaw;
        }
    }
}
