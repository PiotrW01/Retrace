using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Core
{
    internal class Node
    {
        public Node Parent { get; set; }
        public BlockPos Pos { get; set; }
        public Block Block { get; set; }

        public int gCost { get; set; }
        public int hCost { get; set; }
        public int fCost => gCost + hCost;

        public bool Walkable { get; set; }

        public Action<ICoreClientAPI, BlockPos> OnNodeSetAsNext;
        public Action<ICoreClientAPI, BlockPos> OnNodeReached;

        public event Action<ICoreClientAPI, BlockPos> OnTick;

        public Node(BlockPos pos, Block block, bool walkable = true)
        {
            this.Pos = pos;
            this.Block = block;
            this.Walkable = walkable;
        }

        public void TriggerOnTick(ICoreClientAPI api, BlockPos blockPos)
        {
            OnTick?.Invoke(api, blockPos);
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
            return obj is Node other && Pos.Equals(other.Pos);
        }

        public override int GetHashCode()
        {
            return Pos.GetHashCode();
        }
    }
}
