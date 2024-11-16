using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MapGen;

namespace MapGen
{
    public class Node
    {
        public Node cameFrom = null; // Parent node
        public double priority = 0; // F value
        public double costSoFar = 0; // G value
        public Tile tile;

        public Node(Tile _tile, double _priority, Node _cameFrom, double _costSoFar)
        {
            cameFrom = _cameFrom;
            priority = _priority;
            costSoFar = _costSoFar;
            tile = _tile;
        }
    }

    public class PathFinder // Removed the semicolon here
    {
        List<Node> TODOList = new List<Node>();
        List<Node> DoneList = new List<Node>();
        Tile goalTile;

        // Constructor
        public PathFinder()
        {
            goalTile = null;
        }

        // A* Algorithm to find a path
        public Queue<Tile> FindPathAStar(Tile start, Tile goal)
        {
            TODOList = new List<Node>();
            DoneList = new List<Node>();

            TODOList.Add(new Node(start, 0, null, 0));
            goalTile = goal;

            while (TODOList.Count > 0)
            {
                TODOList.Sort((x, y) => x.priority.CompareTo(y.priority));
                Node current = TODOList[0];
                TODOList.RemoveAt(0);
                DoneList.Add(current);

                if (current.tile == goal)
                {
                    return RetracePath(current); // Goal reached, return the path
                }

                foreach (Tile nextTile in current.tile.Adjacents)
                {
                    if (DoneList.Exists(node => node.tile == nextTile))
                        continue; // Skip if already processed

                    double newCost = current.costSoFar + 10; // Assuming movement cost is 10
                    Node nextNode = TODOList.Find(node => node.tile == nextTile);

                    if (nextNode == null || newCost < nextNode.costSoFar)
                    {
                        double priority = newCost + HeuristicsDistance(nextTile, goal);
                        if (nextNode == null)
                        {
                            TODOList.Add(new Node(nextTile, priority, current, newCost));
                        }
                        else
                        {
                            nextNode.cameFrom = current;
                            nextNode.priority = priority;
                            nextNode.costSoFar = newCost;
                        }
                    }
                }
            }

            return new Queue<Tile>(); // No path found
        }

        // A* Algorithm with enemy avoidance
        public Queue<Tile> FindPathAStarEvadeEnemy(Tile start, Tile goal)
        {
            TODOList = new List<Node>();
            DoneList = new List<Node>();

            TODOList.Add(new Node(start, 0, null, 0));
            goalTile = goal;

            while (TODOList.Count > 0)
            {
                TODOList.Sort((x, y) => x.priority.CompareTo(y.priority));
                Node current = TODOList[0];
                TODOList.RemoveAt(0);
                DoneList.Add(current);

                if (current.tile == goal)
                {
                    return RetracePath(current); // Goal reached, return the path
                }

                foreach (Tile nextTile in current.tile.Adjacents)
                {
                    if (DoneList.Exists(node => node.tile == nextTile))
                        continue;

                    double newCost = current.costSoFar + 10;
                    if (IsNearEnemy(nextTile))
                        newCost += 30; // Add extra cost for tiles near enemies

                    Node nextNode = TODOList.Find(node => node.tile == nextTile);

                    if (nextNode == null || newCost < nextNode.costSoFar)
                    {
                        double priority = newCost + HeuristicsDistance(nextTile, goal);
                        if (nextNode == null)
                        {
                            TODOList.Add(new Node(nextTile, priority, current, newCost));
                        }
                        else
                        {
                            nextNode.cameFrom = current;
                            nextNode.priority = priority;
                            nextNode.costSoFar = newCost;
                        }
                    }
                }
            }

            return new Queue<Tile>(); // No path found
        }

        // Manhattan Distance with horizontal/vertical cost of 10
        double HeuristicsDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            return (xdist * 10 + ydist * 10); // Assuming cost to move horizontally and vertically is 10
        }

        // Retrace path from a given Node back to the start Node
        Queue<Tile> RetracePath(Node node)
        {
            List<Tile> tileList = new List<Tile>();
            Node nodeIterator = node;
            while (nodeIterator.cameFrom != null)
            {
                tileList.Insert(0, nodeIterator.tile);
                nodeIterator = nodeIterator.cameFrom;
            }
            return new Queue<Tile>(tileList);
        }

        // Generate a random path (used for enemies)
        public Queue<Tile> RandomPath(Tile start, int stepNumber)
        {
            List<Tile> tileList = new List<Tile>();
            Tile currentTile = start;
            for (int i = 0; i < stepNumber; i++)
            {
                Tile nextTile;
                if (currentTile.Adjacents.Count < 0)
                {
                    break;
                }
                else if (currentTile.Adjacents.Count == 1)
                {
                    nextTile = currentTile.Adjacents[0];
                }
                else
                {
                    nextTile = null;
                    List<Tile> adjacentList = new List<Tile>(currentTile.Adjacents);
                    ShuffleTiles(adjacentList);
                    if (tileList.Count <= 0) nextTile = adjacentList[0];
                    else
                    {
                        foreach (Tile tile in adjacentList)
                        {
                            if (tile != tileList[tileList.Count - 1])
                            {
                                nextTile = tile;
                                break;
                            }
                        }
                    }
                }
                tileList.Add(currentTile);
                currentTile = nextTile;
            }
            return new Queue<Tile>(tileList);
        }

        // Helper function to shuffle tiles (Knuth shuffle algorithm)
        private void ShuffleTiles<T>(List<T> list)
        {
            for (int t = 0; t < list.Count; t++)
            {
                T tmp = list[t];
                int r = UnityEngine.Random.Range(t, list.Count);
                list[t] = list[r];
                list[r] = tmp;
            }
        }

        // Helper function to check if a tile is near an enemy
        private bool IsNearEnemy(Tile tile)
        {
            foreach (Enemy enemy in GameObject.FindObjectsOfType<Enemy>())
            {
                if (Vector3.Distance(enemy.transform.position, tile.transform.position) < 1.5f) // Adjust radius as needed
                {
                    return true;
                }
            }
            return false;
        }
    }

    // Assuming you have a Tile class like this
    public class Tile : MonoBehaviour
    {
        public List<Tile> Adjacents;  // List of adjacent tiles
        public int indexX, indexY;    // Coordinates of the tile
        public Vector3 position;      // World position of the tile

        // Initialize Tile with position
        public Tile(Vector3 pos)
        {
            position = pos;
            Adjacents = new List<Tile>();
        }
    }

    // Assuming you have an Enemy class like this
    public class Enemy : MonoBehaviour
    {
        public Transform transform;
        // Add enemy-related logic here
    }
}
