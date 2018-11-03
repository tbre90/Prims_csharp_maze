using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using MT = Maze.MazeData.Tile;
using MP = Maze.MazeData.Position;

namespace Maze
{
    public static class MazeInterface
    {
        public static IMaze NewInterface(int rows, int columns)
        {
            return new PrimsMaze(rows, columns);
        }

        public static IMaze NewInterface()
        {
            return new PrimsMaze();
        }
    }

    public static class MazeData
    {
        public enum Tile { Passage, Blocked, Exit };
        public struct Position
        { 
            public int x;
            public int y;
        }
    }

    public interface IMaze
    {
        MT[,] NewMaze();
        MT[,] GetCurrent();
        MT GetTile(int x, int y);
    }

    class TileCollection
    {
        MT[,] Collection;

        public TileCollection(int rows, int columns)
        {
            Collection = new MT[rows, columns];
        }

        public MT[,] GetTiles()
        {
            return Collection;
        }

        public int GetLength(int dimension)
        {
            return Collection.GetLength(dimension);
        }

        public MT this[MP index]
        {
            get
            {
                return Collection[index.x, index.y];
            }
            set
            {
                Collection[index.x, index.y] = value;
            }
        }

        public MT this[int x, int y]
        {
            get
            {
                return Collection[x, y];
            }
            set
            {
                Collection[x, y] = value;
            }
        }
    }

    class PrimsMaze : IMaze
    {
        readonly Random randomNumber = new Random();

        int Rows = 21;
        int Columns = 21;

        TileCollection CurrentMaze;

        public PrimsMaze(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public PrimsMaze()
        {
        }

        public MT[,] GetCurrent()
        {
            return CurrentMaze.GetTiles();
        }

        public MT[,] NewMaze()
        {
            CreateMaze();

            return CurrentMaze.GetTiles();
        }

        public MT[,] NewMaze(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;

            CreateMaze();

            return CurrentMaze.GetTiles();
        }

        private void CreateMaze()
        {
            /* 
             * Prim's algorithm
             *
             * A cell can be in two states: Passage or blocked.
             * CurrentMaze[0, 0] is set two be a Passage.
             *
             * After this an initial frontier list is created,
             * with frontier cells being neighbouring cells of
             * CurrentMaze[0, 0] at 2 steps away.
             * 
             * After this we enter a loop where a random frontier cell from the
             * frontier list is selected, and set to be a passage.
             * We then get a list of neighbouring passages, pick a random one,
             * and set it to also be a passage.
             * 
             * After this we get the neighbours of the last picked frontier cell,
             * and add them to the frontier list.
             * 
             * Remove the last picked frontier cell, and loop until the frontier list is empty.
             */

            CurrentMaze = new TileCollection(Rows, Columns);
            SeedMaze(CurrentMaze);

            // we'll always start with the 0,0 tile being a passage
            // and expand from there
            MP frontierCell = new MP { x = 0, y = 0 };
            MarkPassage(frontierCell);

            // use a hashset to avoid possible frontier cell duplicates
            // being added to the frontier list
            var frontierSet = new HashSet<MP>();

            // load first frontier cells
            var cellNeighbours = GetFrontier(frontierCell);
            AddNewFrontierCells(frontierSet, cellNeighbours);

            while (frontierSet.Count > 0)
            {
                frontierCell = PickRandomFrontierCell(frontierSet);
                MarkPassage(frontierCell);

                var neighbouringPassages = GetNeighbours(frontierCell);
                MP randomNeighbourPassage = RandomNeighbour(neighbouringPassages);
                MarkPassage(Between(frontierCell, randomNeighbourPassage));

                cellNeighbours = GetFrontier(frontierCell);
                AddNewFrontierCells(frontierSet, cellNeighbours);

                frontierSet.Remove(frontierCell);
            }

            MarkExit(frontierCell);
        }

        private MP Between(MP p1, MP p2)
        {
            int x = 0;
            int y = 0;

            int tx = p2.x - p1.x;
            switch (tx)
            {
                case 0:  { x = p1.x; } break;
                case 2:  { x = p1.x + 1; } break;
                case -2: { x = p1.x - 1; } break;
                default: throw new InvalidBetween("Invalid paramters given to Between()");
            }

            int ty = p2.y - p1.y;
            switch (ty)
            {
                case 0:  { y = p1.y; } break;
                case 2:  { y = p1.y + 1; } break;
                case -2: { y = p1.y - 1; } break;
                default: throw new InvalidBetween("Invalid paramters given to Between()");
            }

            return new MP { x = x, y = y };
        }

        private MP PickRandomFrontierCell(IEnumerable<MP> frontierSet)
        {
            return frontierSet.ElementAt(randomNumber.Next(frontierSet.Count()));
        }

        private MP RandomNeighbour(List<MP> passages)
        {
            return passages[randomNumber.Next(passages.Count)];
           // ((Func<int>)(() => { int t = randomNumber.Next(neighbours.Count); return (t > 0 ? t : 0); }))() 
        }

        private void AddNewFrontierCells(HashSet<MP> frontierSet, List<MP> neighbouringCells)
        {
            foreach (var position in neighbouringCells)
            {
                frontierSet.Add(position);
            }
        }

        private List<MP> GetNeighbours(MP coordinate)
        {
            var neighbours = new List<MP>()
            {
                new MP { x = coordinate.x - 2, y = coordinate.y },
                new MP { x = coordinate.x + 2, y = coordinate.y },
                new MP { x = coordinate.x, y = coordinate.y - 2 },
                new MP { x = coordinate.x, y = coordinate.y + 2 },
            };

            return FilterTile(neighbours, MT.Passage);
        }

        private List<MP> GetFrontier(MP coordinate)
        {
            var frontierCoords = new List<MP>()
            {
                new MP { x = coordinate.x - 2, y = coordinate.y },
                new MP { x = coordinate.x + 2, y = coordinate.y },
                new MP { x = coordinate.x, y = coordinate.y - 2 },
                new MP { x = coordinate.x, y = coordinate.y + 2 },
            };

            return FilterTile(frontierCoords, MT.Blocked);
        }

        private List<MP> FilterTile(List<MP> tiles, MT kind)
        {
            List<MP> newList =
                tiles.Where(p =>
                p.x >= 0 && p.x < (CurrentMaze.GetLength(1)) &&
                p.y >= 0 && p.y < (CurrentMaze.GetLength(0)) &&
                CurrentMaze[p.x, p.y] == kind).ToList();

            return newList;
        }

        private void MarkPassage(MP coordinate)
        {
            CurrentMaze[coordinate.x, coordinate.y] = MT.Passage;
        }

        private void MarkExit(MP coordinate)
        {
            CurrentMaze[coordinate.x, coordinate.y] = MT.Exit;
        }

        private void SeedMaze(TileCollection toSeed)
        {
            for (int row = 0; row < CurrentMaze.GetLength(0); row++)
            {
                for (int column = 0; column < CurrentMaze.GetLength(1); column++)
                {
                    CurrentMaze[row, column] = MT.Blocked;
                }
            }
        }

        public MT GetTile(int x, int y)
        {
            return CurrentMaze[x, y];
        }
    }

    class InvalidBetween : Exception
    {
        public InvalidBetween() { }

        public InvalidBetween(string message) : base(message) { }

        public InvalidBetween(string message, Exception inner) : base(message, inner) { }
    }
}
