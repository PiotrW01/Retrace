using Retrace.assets.retrace.PathFinding.Algorithms;
using Retrace.assets.retrace.PathFinding.Core;
using Retrace.assets.retrace.PathFinding.Rendering;
using Retrace.assets.retrace.PathFinding.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Retrace.assets.retrace.pathfinding
{
    internal class PathFinder
    {
        private ICoreClientAPI _api;
        private IClientPlayer _player => _api.World.Player;
        private GridManager _gridManager;
        private Settings _settings = new();
        private List<Node> _path;
        private AStar3D _aStar;

        private long _id;
        private bool _isFollowingPath = false;
        private bool _isSearching = false;

        private CancellationTokenSource _cts;

        public PathFinder(ICoreClientAPI api)
        {
            _path = new();
            _api = api;
            _gridManager = new(api.World.BlockAccessor);
            _aStar = new(_gridManager);
            SetMouseEvents();
            SetHokeys();
            SetCommands();
        }

        public void FindPath(BlockPos start, BlockPos end, bool startPosIsPlayer = true)
        {
            if(_isSearching || _isFollowingPath)
            {
                CancelPathfinding();
                return;
            }
            if(startPosIsPlayer)
            {
                start.Y -= 1; // accounting for player's camera height
            }

            _cts = new();
            CancellationToken token = _cts.Token;
            Task.Run(() =>
            {
                ShowChatMessage($"Searching for path from {start.AsVec3i} to {end.AsVec3i}...");

                _isSearching = true;
                _path = _aStar.FindPath(start, end, token);
                _isSearching = false;
                
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (_path == null)
                {
                    ShowChatMessage("No path found.");
                    return;
                }

                ShowChatMessage("Path found, following path now.");
                FollowPath(token);
                
            }, token);
        }
        
        public void FollowPath(CancellationToken token)
        {
            _isFollowingPath = true;
            int step = 0;

            PathRenderer pathRenderer = new(_api);
            pathRenderer.path = _path;

            _api.Event.EnqueueMainThreadTask(() =>
            {
                _api.Event.RegisterRenderer(pathRenderer, EnumRenderStage.AfterBlit);
            }, "RegisterRenderer");

            _id = _api.World.RegisterGameTickListener((dt) =>
            {
                pathRenderer.step = step;
                Vec3d nextPos = _path[step].pos.ToVec3d();

                AdjustYOffset(nextPos, _path[step].block);
                OffsetToBlockCenter(nextPos);

                // TODO: if over nextPos stop all input
                // also check that for the future positions part
                ApplyMovement(nextPos);
                ApplyRotation(nextPos);
                ApplySprintAndJump(nextPos);
                

                if (IsNextPosReached(nextPos, ref step))
                {
                    _path[step].ExecuteOnNodeReached(_api, _path[step].pos);
                    step++;
                    
                    if (step == _path.Count)
                    {
                        _api.World.UnregisterGameTickListener(_id);
                        _api.Event.UnregisterRenderer(pathRenderer, EnumRenderStage.AfterBlit);
                        _isFollowingPath = false;
                        ShowChatMessage("Path following completed.");
                        return;
                    }
                    _path[step].ExecuteOnNodeSetAsNext(_api, _path[step].pos);
                }

                if (token.IsCancellationRequested)
                {
                    _api.World.UnregisterGameTickListener(_id);
                    _api.Event.UnregisterRenderer(pathRenderer, EnumRenderStage.AfterBlit);
                    CancelPathfinding();
                    return;
                }
            }, 0);
        }
        
        private void SetCommands()
        {
            IChatCommand command = _api.ChatCommands.Create("retrace");

            command.BeginSubCommand("goto")
                .WithArgs(_api.ChatCommands.Parsers.WorldPosition("position"))
                .HandleWith((args) =>
                {
                    Vec3i pos = ((Vec3d)args[0]).AsVec3i;

                    // local to world position
                    pos.X += (int)_api.World.DefaultSpawnPosition.X;
                    pos.Z += (int)_api.World.DefaultSpawnPosition.Z;

                    FindPath(_player.Entity.Pos.AsBlockPos, new BlockPos(pos, 0));
                    return TextCommandResult.Success();
                });

            command.BeginSubCommand("canSprint")
                .WithArgs(_api.ChatCommands.Parsers.Bool("sprint"))
                .HandleWith((args) =>
                {
                    bool canSprint = (bool)args[0];
                    _settings.CanSprint = canSprint;
                    ShowChatMessage($"Updated canSprint to {canSprint}");
                    return TextCommandResult.Success();
                });

            command.BeginSubCommand("canMine")
                .WithArgs(_api.ChatCommands.Parsers.Bool("mine"))
                .HandleWith((args) =>
                {
                    bool canMine = (bool)args[0];
                    _settings.CanSprint = canMine;
                    ShowChatMessage($"Updated canMine to {canMine}");
                    return TextCommandResult.Success();
                });
        }
        
        private void SetHokeys()
        {
            _api.Input.RegisterHotKey("retrace:findpath", "find path", GlKeys.N, HotkeyType.GUIOrOtherControls);
            _api.Input.SetHotKeyHandler(_api.Input.GetHotKeyByCode("retrace:findpath").Code, (a) =>
            {
                if (_isFollowingPath || _isSearching)
                {
                    CancelPathfinding();
                    return true;
                }
                if (_player.CurrentBlockSelection == null)
                {
                    Vec3d startPos = _player.Entity.Pos.XYZ.Add(0, _player.Entity.LocalEyePos.Y, 0);
                    Vec3d lookDir = _player.Entity.SidedPos.GetViewVector().Normalize().ToVec3d();
                    float maxDistance = 160;

                    Vec3d endPos = startPos + (lookDir * maxDistance);
                    BlockSelection sel = null;
                    EntitySelection eSel = null;
                    _api.World.RayTraceForSelection(startPos, endPos, ref sel, ref eSel);
                    if (sel == null) return true;

                    FindPath(_player.Entity.Pos.AsBlockPos, sel.Position);
                    return true;
                }
                FindPath(_player.Entity.Pos.AsBlockPos, _player.CurrentBlockSelection.Position);
                return true;
            });
        }
        
        private void SetMouseEvents()
        {
            _api.Event.MouseDown += (args) =>
            {
                GuiDialog mapGui = _api.Gui.LoadedGuis.FirstOrDefault((g) => g.DebugName == "GuiDialogWorldMap");
                if (mapGui != null && mapGui.IsOpened())
                {
                    if (args.Button == EnumMouseButton.Middle)
                    {
                        GuiElementMap mapElement = mapGui.SingleComposer.LastAddedElement as GuiElementMap;
                        GuiElementHoverText textElement = mapGui.SingleComposer.GetElement("hoverText") as GuiElementHoverText;
                        if (textElement != null && textElement.IsVisible)
                        {
                            Console.WriteLine(textElement.Text);
                            var parts = textElement.Text.Split(',');

                            int x = int.Parse(parts[0].Trim());
                            int y = int.Parse(parts[1].Trim());
                            int z = int.Parse(parts[2].Trim());
                            Vec3i local = new(x, y - 1, z);
                            Vec3i world = new Vec3i(
                                local.X + (int)_api.World.DefaultSpawnPosition.X,
                                local.Y,
                                local.Z + (int)_api.World.DefaultSpawnPosition.Z
                                );
                            
                            FindPath(_player.Entity.Pos.AsBlockPos, new BlockPos(world, 0));
                        }
                    }
                }
            };
        }

        private void OffsetToBlockCenter(Vec3d pos)
        {
            if (pos.X < 0)
            {
                pos.X -= 0.5;
            }
            else
            {
                pos.X += 0.5;
            }

            if (pos.Z < 0)
            {
                pos.Z -= 0.5;
            }
            else
            {
                pos.Z += 0.5;
            }
        }

        private bool IsNextPosReached(Vec3d nextPos, ref int step)
        {
            bool posReached = _player.Entity.Pos.AsBlockPos == nextPos.AsBlockPos;
            for (int i = 1; i <= 3; i++)
            {
                if (step + i >= _path.Count) break;
                Vec3d futurePos = _path[step + i].pos.ToVec3d();

                AdjustYOffset(futurePos, _path[step + i].block);
                if (_player.Entity.Pos.AsBlockPos == futurePos.AsBlockPos)
                {
                    step = step + i;
                    posReached = true;
                    break;
                }
            }
            return posReached;
        }

        private void ApplySprintAndJump(Vec3d nextPos)
        {
            _player.Entity.Controls.Sprint = _settings.CanSprint;
            if (nextPos.Y - _player.Entity.Pos.Y > 0.3 || _player.Entity.FeetInLiquid)
            {
                _player.Entity.Controls.Jump = true;
                _player.Entity.Controls.Sprint = false;
            }
        }

        private void ApplyMovement(Vec3d nextPos)
        {
            //double dy = _player.Entity.Pos.Y - _player.Entity.PositionBeforeFalling.Y;
            Vec3d deltaBlockPos = nextPos - new Vec3d(_player.Entity.Pos);
            double tolerance = 0.3;
            if(Math.Abs(deltaBlockPos.X) <= tolerance && 
               Math.Abs(deltaBlockPos.Z) <= tolerance && 
               deltaBlockPos.Y < 0)
            {
                _player.Entity.Controls.Forward = false;
                return;
            }
            else
            {
                _player.Entity.Controls.Forward = true;
            }
/*            if (!_player.Entity.OnGround && dy < 0)
            {
                _player.Entity.Controls.Forward = false;
            }
            else
            {
                _player.Entity.Controls.Forward = true;
            }*/
        }

        private void ApplyRotation(Vec3d nextPos)
        {
            _player.CameraYaw = (float)Utils.GetYawBetweenPositions(nextPos, new Vec3d(_player.Entity.Pos));
        }

        private void AdjustYOffset(Vec3d nextPos, Block nextPosBlock)
        {
            bool isSlab = nextPosBlock.Code.Path.Contains("slab");
            bool isTopSlab = nextPosBlock.Shape.Base.Path.Contains("up");
            if (!isSlab || (isSlab && isTopSlab))
            {
                nextPos.Y += 1; // accounting for player camera height
            }
        }

        private void CancelPathfinding()
        {
            ShowChatMessage("Cancelling pathfinding...");
            _isFollowingPath = false;
            _isSearching = false;
            _cts.Cancel();
        }

        private void ShowChatMessage(string message)
        {
            _api.Event.EnqueueMainThreadTask(() =>
            {
                _api.ShowChatMessage(message);
            }, "ShowChatMessage");
        }
    }
}
