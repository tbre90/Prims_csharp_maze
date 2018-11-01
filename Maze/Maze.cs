using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using MT = Maze.MazeData.Tile;
using MP = Maze.MazeData.Position;
using MN = Maze.MazeData.Neighbours;

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
        public enum Tile { Passage, Blocked };
        public struct Position { public int x; public int y; }
        public struct Neighbours {
            public Position? NorthNeighbour;
            public Position? SouthNeighbour;
            public Position? EastNeighbour;
            public Position? WestNeighbour; 
        }
    }

    public interface IMaze
    {
         MT[,] NewMaze();
         MT[,] GetCurrent();
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

        void CreateMaze()
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
            MN cellNeighbours = GetNeighbours(frontierCell, 2);
            AddNewFrontierCells(frontierSet, cellNeighbours);

            while (frontierSet.Count > 0)
            {
                frontierCell = PickRandomFrontierCell(frontierSet);
                MarkPassage(frontierCell);

                MN neighbouringPassages = GetNeighbourPassages(frontierCell);
                MP randomNeighbourPassage = GetRandomNeighbourPassage(neighbouringPassages);
                MarkPassage(randomNeighbourPassage);

                cellNeighbours = GetNeighbours(frontierCell, 2);
                AddNewFrontierCells(frontierSet, cellNeighbours);

                frontierSet.Remove(frontierCell);
            }
        }

        MP PickRandomFrontierCell(IEnumerable<MP> frontierSet)
        {
            return frontierSet.ElementAt(randomNumber.Next(frontierSet.Count()));
        }

        MN GetNeighbourPassages(MP coordinate)
        {
            MN neighbourPassages = new MN();

            MN neighbours = GetNeighbours(coordinate, 2);

            if (neighbours.NorthNeighbour.HasValue && CurrentMaze[neighbours.NorthNeighbour.Value] == MT.Passage)
                { neighbourPassages.NorthNeighbour = GetSouthNeighbour(neighbours.NorthNeighbour.Value); }

            if (neighbours.SouthNeighbour.HasValue && CurrentMaze[neighbours.SouthNeighbour.Value] == MT.Passage)
                { neighbourPassages.SouthNeighbour = GetNorthNeighbour(neighbours.SouthNeighbour.Value); }

            if (neighbours.EastNeighbour.HasValue  && CurrentMaze[neighbours.EastNeighbour.Value]  == MT.Passage)
                { neighbourPassages.EastNeighbour  = GetWestNeighbour(neighbours.EastNeighbour.Value); }

            if (neighbours.WestNeighbour.HasValue && CurrentMaze[neighbours.WestNeighbour.Value] == MT.Passage)
                { neighbourPassages.WestNeighbour = GetEastNeighbour(neighbours.WestNeighbour.Value); }

            return neighbourPassages;
        }

        MP GetRandomNeighbourPassage(MN passages)
        {
            var neighbours = new List<MP>();

            if (passages.NorthNeighbour.HasValue) { neighbours.Add(passages.NorthNeighbour.Value); }
            if (passages.SouthNeighbour.HasValue) { neighbours.Add(passages.SouthNeighbour.Value); }
            if (passages.EastNeighbour.HasValue)  { neighbours.Add(passages.EastNeighbour.Value); }
            if (passages.WestNeighbour.HasValue)  { neighbours.Add(passages.WestNeighbour.Value); }

            return neighbours[randomNumber.Next(neighbours.Count)];
        }

        void MarkPassage(MP coordinate)
        {
            CurrentMaze[coordinate.x, coordinate.y] = MT.Passage;
        }

        void AddNewFrontierCells(HashSet<MP> frontierSet, MN neighbouringCells)
        {
            if (neighbouringCells.NorthNeighbour.HasValue) { frontierSet.Add(neighbouringCells.NorthNeighbour.Value); }
            if (neighbouringCells.SouthNeighbour.HasValue) { frontierSet.Add(neighbouringCells.SouthNeighbour.Value); }
            if (neighbouringCells.EastNeighbour.HasValue)  { frontierSet.Add(neighbouringCells.EastNeighbour.Value); }
            if (neighbouringCells.WestNeighbour.HasValue)  { frontierSet.Add(neighbouringCells.WestNeighbour.Value); }
        }

        MN GetNeighbours(MP coordinate, int stepsAway = 1)
        {
            return new MN
            {
                NorthNeighbour = GetNorthNeighbour(coordinate, stepsAway),
                SouthNeighbour = GetSouthNeighbour(coordinate, stepsAway),
                EastNeighbour = GetEastNeighbour(coordinate, stepsAway),
                WestNeighbour = GetWestNeighbour(coordinate, stepsAway),
            };
        }

        MP? GetNorthNeighbour(MP coordinate, int stepsAway = 1)
        {
            if (coordinate.y - stepsAway < 0) { return null; }

            return new MP { x = coordinate.x, y = coordinate.y - stepsAway };
        }
        MP? GetSouthNeighbour(MP coordinate, int stepsAway = 1)
        {
            if (coordinate.y + stepsAway > CurrentMaze.GetLength(0)) { return null; }

            return new MP { x = coordinate.x, y = coordinate.y + stepsAway };
        }
        MP? GetEastNeighbour(MP coordinate, int stepsAway = 1)
        {
            if (coordinate.x + stepsAway > CurrentMaze.GetLength(1)) { return null; }

            return new MP { x = coordinate.x + stepsAway, y = coordinate.y };
        }
        MP? GetWestNeighbour(MP coordinate, int stepsAway = 1)
        {
            if (coordinate.x - stepsAway < 0) { return null; }

            return new MP { x = coordinate.x - stepsAway, y = coordinate.y };
        }

        void SeedMaze(TileCollection toSeed)
        {
            for (int row = 0; row < CurrentMaze.GetLength(0); row++)
            {
                for (int column = 0; column < CurrentMaze.GetLength(1); column++)
                {
                    CurrentMaze[row, column] = MT.Blocked;
                }
            }
        }
    }
}
