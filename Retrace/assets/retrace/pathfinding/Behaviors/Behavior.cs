using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Retrace.assets.retrace.PathFinding.Behaviors
{
    internal class Behavior
    {
        public static void OpenDoorLike(ICoreClientAPI api, BlockPos nextBlock)
        {
            BlockSelection bSel = new();
            bSel.Face = BlockFacing.EAST;
            bSel.Block = api.World.BlockAccessor.GetBlock(nextBlock.UpCopy());
            bSel.Position = nextBlock.UpCopy();
            bSel.HitPosition = new Vec3d(1, 0.4, 0.5);
            if (bSel.Block.Code.Path.Contains("door"))
            {
                BEBehaviorDoor behaviorDoor = api.World.BlockAccessor.GetBlockEntity(bSel.Position)?.GetBehavior<BEBehaviorDoor>();
                if (behaviorDoor == null || behaviorDoor.Opened) return;
            }
            // gate
            else if (bSel.Block.Code.Path.Contains("opened"))
            {
                return;
            }
            SendPackets(api, bSel);

            api.Event.EnqueueMainThreadTask(() =>
            {
                // send additional packet for upper gate
                bSel.Block = api.World.BlockAccessor.GetBlock(bSel.Position.Up());
                if (bSel.Block.Code.Path.Contains("gate"))
                {
                    SendPackets(api, bSel);
                }
            }, "UpperGate");
        }

        public static void OpenOnlyUpperGate(ICoreClientAPI api, BlockPos nextBlock)
        {
            BlockSelection bSel = new();
            bSel.Face = BlockFacing.EAST;
            bSel.Block = api.World.BlockAccessor.GetBlock(nextBlock.UpCopy(2));
            bSel.Position = nextBlock.UpCopy(2);
            bSel.HitPosition = new Vec3d(1, 0.4, 0.5);

            if (bSel.Block.Code.Path.Contains("opened"))
            {
                return;
            }
            SendPackets(api, bSel);
        }

        public static void MineBlock(ICoreClientAPI api)
        {

        }

        private static void SendPackets(ICoreClientAPI api, BlockSelection bSel)
        {
            bSel.Block.OnBlockInteractStart(
                api.World, api.World.Player,
                bSel
                );

            api.Network.SendHandInteraction(2,
                bSel, api.World.Player.CurrentEntitySelection,
                EnumHandInteract.BlockInteract, (int)EnumHandInteractNw.StartBlockUse, true, EnumItemUseCancelReason.MovedAway);

            api.Network.SendHandInteraction(2,
                bSel, api.World.Player.CurrentEntitySelection,
                EnumHandInteract.BlockInteract, (int)EnumHandInteractNw.StopBlockUse, true, EnumItemUseCancelReason.MovedAway);
        }

    }

}
