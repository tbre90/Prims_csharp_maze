using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze
{
    public interface IMaze
    {
        int[,] NewMaze(int rows, int columns);
    }

    public class Maze : IMaze
    {
        int DefaultRows = 21;
        int DefaultColumns = 21;

        public Maze(int rows, int columns)
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
