using Retrace.assets.retrace.PathFinding.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace Retrace.assets.retrace.PathFinding.Algorithms
{
    internal class AStar3D
    {
        private GridManager _grid;
        private Dictionary<BlockPos, Node> _nodeCache = new Dictionary<BlockPos, Node>(new BlockPosEqualityComparer());

        private int _moveStraightCost = 10;
        private int _moveDiagonal2DCost = 14;
        private int _moveDiagonal3DCost = 17;

        public AStar3D(GridManager grid)
        {
            _grid = grid;
        }

        public List<Node> FindPath(BlockPos startPos, BlockPos endPos, CancellationToken token)
        {
            _nodeCache.Clear();
            if (_grid.IsAirLike(endPos))
            {
                // we also check 3 blocks below for a valid target position
                bool foundValid = false;
                for (int i = 0; i < 3; i++)
                {
                    endPos = endPos.Down();
                    if (!_grid.IsAirLike(endPos))
                    {
                        foundValid = true;
                        break;
                    }
                }
                if (!foundValid)
                {
                    return null;
                }
            }

            Node startNode = GetCachedNode(startPos);
            Node endNode = GetCachedNode(endPos);

            var openSet = new List<Node> { startNode };
            var closedSet = new HashSet<Node>();

            startNode.gCost = 0;
            startNode.hCost = GetHeuristic(startNode, endNode);

            while (openSet.Count > 0)
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }
                Node currentNode = openSet.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();

                if (currentNode.Pos.Equals(endNode.Pos))
                    return RetracePath(startNode, endNode);

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                foreach (var neighborPos in _grid.GetValidNeighborPositions(currentNode.Pos))
                {
                    Node neighbor = GetCachedNode(neighborPos);
                    if (closedSet.Contains(neighbor))
                        continue;
                    //int tentativeGCost = currentNode.gCost + 1;

                    int tentativeGCost = currentNode.gCost + GetMoveCost(currentNode, neighbor);
                    if (!openSet.Contains(neighbor) || tentativeGCost < neighbor.gCost)
                    {
                        neighbor.gCost = tentativeGCost;
                        neighbor.hCost = GetHeuristic(neighbor, endNode);
                        neighbor.Parent = currentNode;

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }
            return null;
        }

        private Node GetCachedNode(BlockPos pos)
        {
            if (!_nodeCache.TryGetValue(pos, out Node node))
            {
                node = _grid.GetNode(pos);
                _nodeCache[pos] = node;
            }
            return node;
        }

        private int GetHeuristic(Node a, Node b)
        {
            double dx = a.Pos.X - b.Pos.X;
            double dy = a.Pos.Y - b.Pos.Y;
            double dz = a.Pos.Z - b.Pos.Z;

            return (int)(Math.Sqrt(dx * dx + dy * dy + dz * dz) * 10); // scale up to match gCost units

            //return Math.Abs(a.pos.X - b.pos.X) + Math.Abs(a.pos.Y - b.pos.Y) + Math.Abs(a.pos.Z - b.pos.Z);
        }

        private int GetMoveCost(Node currentNode, Node neighbor)
        {
            int dx = Math.Abs(neighbor.Pos.X - currentNode.Pos.X);
            int dy = Math.Abs(neighbor.Pos.Y - currentNode.Pos.Y);
            int dz = Math.Abs(neighbor.Pos.Z - currentNode.Pos.Z);

            int moveCost = _moveStraightCost;
            if (dx + dy + dz == 2) moveCost = _moveDiagonal2DCost;
            else if (dx + dy + dz == 3) moveCost = _moveDiagonal3DCost;
            return moveCost;
        }

        private List<Node> RetracePath(Node start, Node end)
        {
            List<Node> path = new List<Node>();
            Node current = end;

            while (current != null && current != start)
            {
                path.Add(current);
                current = current.Parent;
            }

            if (current == null)
                return null; // path was broken

            path.Add(start);
            path.Reverse();
            return path;
        }
    }
}
