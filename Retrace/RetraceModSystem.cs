using Retrace.assets.retrace.pathfinding;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Retrace
{
    public class RetraceModSystem : ModSystem
    {
        PathFinder _pathFinder;

        public override void StartClientSide(ICoreClientAPI api)
        {
            _pathFinder = new(api);
        }
        public override double ExecuteOrder()
        {
            return 0.9;
        }
    }
}
