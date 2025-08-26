using Retrace.assets.retrace.PathFinding.Core;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Rendering
{
    internal class PathRenderer : IRenderer
    {
        private ICoreClientAPI _api;

        public int step { get; set; } = 0;
        public List<Node> path { get; set; }

        public PathRenderer(ICoreClientAPI api)
        {
            _api = api;
        }

        public double RenderOrder => 3.0;
        public int RenderRange => 999;


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (path == null) return;

            int pathLinesDrawAmount = 10;
            for (int i = step; i < step + pathLinesDrawAmount; i++)
            {
                if (i + 1 >= path.Count) break;
                Vec3f pos1 = path[i].pos.ToVec3f();
                Vec3f pos2 = path[i + 1].pos.ToVec3f();
                Vec3f dpos = pos2 - pos1;

                float xcenter = 0.5f;
                float zcenter = 0.5f;
                if (pos1.X < 0) xcenter = -0.5f;
                if (pos1.Z < 0) zcenter = -0.5f;

                _api.Render.RenderLine(pos1.AsVec3i.AsBlockPos,
                    xcenter, 1, zcenter,
                    xcenter + dpos.X, 1 + dpos.Y, zcenter + dpos.Z,
                    ColorUtil.ToRgba(255, 255, 255, 255));
            }
        }
        public void Dispose() { }
    }
}
