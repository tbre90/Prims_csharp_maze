using System;
using System.Collections.Generic;
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
    }

    public interface IMaze
    {
        int[,] NewMaze(int rows, int columns);
    }

    class PrimsMaze : IMaze
    {
        int DefaultRows = 21;
        int DefaultColumns = 21;

        public PrimsMaze(int rows, int columns)
        {
            DefaultRows = rows;
            DefaultColumns = columns;
        }

        public int[,] NewMaze(int rows, int columns)
        {
            throw new NotImplementedException();
        }
    }
}
