using Retrace.assets.retrace.PathFinding.Behaviors;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Retrace.assets.retrace.PathFinding.Core
{
    internal class GridManager
    {
        private ICoreClientAPI _api;
        private IBlockAccessor _blockAccessor;

        public GridManager(ICoreClientAPI api)
        {
            _api = api;
            _blockAccessor = api.World.BlockAccessor;
        }

        public Node GetNode(BlockPos pos)
        {
            //bool isPassable = IsBlockPassable(pos);
            Node node = new(pos, _blockAccessor.GetBlock(pos));
            ApplyBehavior(node);

            return node;
        }

        public List<BlockPos> GetValidNeighborPositions(BlockPos pos)
        {
            List<BlockPos> neighbors = new List<BlockPos>();
            Block block = _blockAccessor.GetBlock(pos);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        if (IsNeighborBelowWaterBlockPos(block.BlockMaterial, dy)) continue;

                        BlockPos neighborPos = pos.AddCopy(dx, dy, dz);
                        if (IsBlockPassable(neighborPos))
                        {
                            neighbors.Add(neighborPos);
                        }
                    }
                }
            }
            return neighbors;
        }

        private bool IsNeighborBelowWaterBlockPos(EnumBlockMaterial blockMaterial, int dy)
        {
            return (blockMaterial == EnumBlockMaterial.Liquid && dy == -1);
        }

        private void ApplyBehavior(Node node)
        {
            Block middle = _blockAccessor.GetBlock(node.Pos.UpCopy());
            Block top = _blockAccessor.GetBlock(node.Pos.UpCopy(2));

            if (middle.Code.Path.Contains("door") || middle.Code.Path.Contains("gate"))
            {
                node.OnNodeSetAsNext = Behavior.OpenDoorLike;
            }
            else if (top.Code.Path.Contains("gate"))
            {
                node.OnNodeSetAsNext = Behavior.OpenOnlyUpperGate;
            }
        }

        private bool IsBlockPassable(BlockPos pos)
        {
            Block below = _blockAccessor.GetBlock(pos);
            Block middle = _blockAccessor.GetBlock(pos.UpCopy(1));
            Block top = _blockAccessor.GetBlock(pos.UpCopy(2));

            bool canStand = !IsAirLike(below);
            bool hasDoor = middle.Code.Path.Contains("door");
            bool hasGate = (middle.Code.Path.Contains("gate") ||
                           (IsAirLike(middle) && top.Code.Path.Contains("gate")));

            // TODO: properly handle land claims
            /* if (hasDoor)
            {
                var doorPos = pos.UpCopy();
                LandClaim[] claims = _api.World.Claims.Get(doorPos);
                if (claims.Length > 0 && claims[0].OwnedByPlayerUid == _api.World.Player.PlayerUID)
                {
                    BEBehaviorDoor behaviorDoor = _api.World.BlockAccessor.GetBlockEntity(doorPos)?.GetBehavior<BEBehaviorDoor>();
                    if (behaviorDoor == null || !behaviorDoor.Opened) return false;
                }
            }
            if (hasGate)
            {
                LandClaim[] claims = _api.World.Claims.Get(pos.UpCopy());
                if (claims.Length > 0 && claims[0].OwnedByPlayerUid == _api.World.Player.PlayerUID)
                {
                    if (middle.Code.Path.Contains("gate") && !middle.Code.Path.Contains("opened")) return false;
                    if (top.Code.Path.Contains("gate") && !middle.Code.Path.Contains("opened")) return false;
                }
            } */
            bool hasHeadroom = (IsPassable(middle) || middle.Code.PathStartsWith("ladder")) &&
                               IsPassable(top) || top.Code.PathStartsWith("ladder");

            return canStand && (hasHeadroom || hasDoor || hasGate);
        }

        private bool IsAirLike(Block block)
        {
            return block.BlockMaterial == EnumBlockMaterial.Air;
        }
        
        public bool IsAirLike(BlockPos pos)
        {
            return _blockAccessor.GetBlock(pos).BlockMaterial == EnumBlockMaterial.Air;
        }

        private bool IsPassable(Block block)
        {
            return block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
        }
    }
}
