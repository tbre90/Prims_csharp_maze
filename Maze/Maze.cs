using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public enum Tile { Wall, Path };
    }

    public interface IMaze
    {
         MazeData.Tile[,] NewMaze();
         MazeData.Tile[,] GetCurrent();
    }

    class PrimsMaze : IMaze
    {
        int Rows = 21;
        int Columns = 21;

        MazeData.Tile[,] CurrentMaze;

        public PrimsMaze(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public PrimsMaze()
        {
        }

        public MazeData.Tile[,] GetCurrent()
        {
            return CurrentMaze;
        }

        public MazeData.Tile[,] NewMaze(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;

            CurrentMaze = new MazeData.Tile[Rows, Columns];

            for (int row = 0; row < CurrentMaze.GetLength(0); row++)
            {
                for (int column = 0; column < CurrentMaze.GetLength(1); column++)
                {
                    CurrentMaze[row, column] = MazeData.Tile.Path;
                }
            }

            return CurrentMaze;
        }

        public MazeData.Tile[,] NewMaze()
        {
            CurrentMaze = new MazeData.Tile[Rows, Columns];

            for (int row = 0; row < CurrentMaze.GetLength(0); row++)
            {
                for (int column = 0; column < CurrentMaze.GetLength(1); column++)
                {
                    CurrentMaze[row, column] = MazeData.Tile.Path;
                }
            }

            return CurrentMaze;
        }
    }
}
