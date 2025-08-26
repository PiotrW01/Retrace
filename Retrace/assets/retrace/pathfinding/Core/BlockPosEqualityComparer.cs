using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Core
{
    internal class BlockPosEqualityComparer : IEqualityComparer<BlockPos>
    {
        public bool Equals(BlockPos a, BlockPos b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public int GetHashCode(BlockPos pos)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + pos.X;
                hash = hash * 31 + pos.Y;
                hash = hash * 31 + pos.Z;
                return hash;
            }
        }
    }
}