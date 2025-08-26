using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Core
{
    internal class Node
    {
        public Node parent { get; set; }
        public BlockPos pos { get; set; }
        public Block block { get; set; }

        public int gCost { get; set; }
        public int hCost { get; set; }
        public int fCost => gCost + hCost;

        public bool walkable { get; set; }

        public Action<ICoreClientAPI, BlockPos> OnNodeSetAsNext;
        public Action<ICoreClientAPI, BlockPos> OnNodeReached;

        public Node(BlockPos pos, Block block, bool walkable)
        {
            this.pos = pos;
            this.block = block;
            this.walkable = walkable;
        }

        public void ExecuteOnNodeSetAsNext(ICoreClientAPI api, BlockPos blockPos)
        {
            OnNodeSetAsNext?.Invoke(api, blockPos);
        }

        public void ExecuteOnNodeReached(ICoreClientAPI api, BlockPos blockPos)
        {
            OnNodeReached?.Invoke(api, blockPos);
        }

        public override bool Equals(object obj)
        {
            return obj is Node other && pos.Equals(other.pos);
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode();
        }
    }
}
