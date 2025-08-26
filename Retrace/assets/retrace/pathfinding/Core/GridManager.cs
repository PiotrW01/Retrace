using Retrace.assets.retrace.PathFinding.Behaviors;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Core
{
    internal class GridManager
    {
        private IBlockAccessor blockAccessor;

        public GridManager(IBlockAccessor blockAccessor)
        {
            this.blockAccessor = blockAccessor;
        }

        public Node GetNode(BlockPos pos)
        {
            bool isPassable = IsBlockPassable(pos);
            Node node = new(pos, blockAccessor.GetBlock(pos), isPassable);
            //(middle.Code.Path.Contains("door") || (middle.Code.Path.Contains("gate") && IsPassable(top)));
            Block middle = blockAccessor.GetBlock(pos.UpCopy());
            Block top = blockAccessor.GetBlock(pos.UpCopy(2));

            if (middle.Code.Path.Contains("door") || middle.Code.Path.Contains("gate"))
            {
                node.OnNodeSetAsNext = Behavior.OpenDoorLike;
            }
            else if (top.Code.Path.Contains("gate"))
            {
                node.OnNodeSetAsNext = Behavior.OpenOnlyUpperGate;
            }

            return node;
        }

        public List<BlockPos> GetNeighborPositions(BlockPos pos)
        {
            List<BlockPos> neighbors = new List<BlockPos>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        if (blockAccessor.GetBlock(pos).BlockMaterial == EnumBlockMaterial.Liquid)
                        {
                            if (dy == -1) continue;
                        }

                        BlockPos neighborPos = pos.AddCopy(dx, dy, dz);
                        if (IsBlockPassable(neighborPos))  // optional walkability check
                        {
                            neighbors.Add(neighborPos);
                        }
                    }
                }
            }
            return neighbors;
        }

        private bool IsBlockPassable(BlockPos pos)
        {
            Block below = blockAccessor.GetBlock(pos);
            Block middle = blockAccessor.GetBlock(pos.UpCopy(1));
            Block top = blockAccessor.GetBlock(pos.UpCopy(2));

            bool canStand = !IsAirLike(below);
            bool hasDoor = middle.Code.Path.Contains("door") ||
                           (middle.Code.Path.Contains("gate") || middle.BlockMaterial == EnumBlockMaterial.Air) &&
                           (IsPassable(top) || top.Code.Path.Contains("gate"));
            bool hasHeadroom = IsPassable(middle) && IsPassable(top);

            return canStand && (hasHeadroom || hasDoor);
        }

        private bool IsAirLike(Block block)
        {
            return block.BlockMaterial == EnumBlockMaterial.Air;
        }
        public bool IsAirLike(BlockPos pos)
        {
            return blockAccessor.GetBlock(pos).BlockMaterial == EnumBlockMaterial.Air;
        }

        private bool IsPassable(Block block)
        {
            return block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
        }
    }
}
