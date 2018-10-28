using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using PDKey = Maze.PlatformData.Key;
using PDTBC = Maze.PlatformData.TaggedBitmapCoordinates;

namespace Maze
{
    public static class GameInterface
    {
        public static IGame NewInterface(IMaze mazeInterface)
        {
            return new Game(mazeInterface);
        }
    }

    public interface IGame
    {
        List<PDKey> WhenPressedNotify(); /* list of keys that the game wants the platform layer to listen for */
        Dictionary<int, Bitmap> LoadTaggedBitmaps(); /* bitmaps with tags to more easily refer to them after being loaded by the platform layer */
        List<PDTBC> Initialize(); /* set initialize game state */
        bool DoWork(PDKey keyPressed); /* did the game do any work? */
        List<PDTBC> ToDraw(); /* if it did, call ToDraw() for a list of bitmap tags and their new positions */
        void WindowSizeChanged(int newWidth, int newHeight); /* only used right after a Game object is instantiated, to set the window dimension (bounds checking) */
    }

    class Game : IGame
    {
        const int TileSize = 32;
        readonly Tuple<int, Bitmap> Goblin = Tuple.Create(1, Properties.Resources.Goblin);
        readonly Tuple<int, Bitmap> Path = Tuple.Create(2, Properties.Resources.Wooden_wall);
        readonly Tuple<int, Bitmap> Exit = Tuple.Create(3, Properties.Resources.Exit);
        readonly Dictionary<PDKey, Action> MovementKeys;
        readonly IMaze MazeInterface;

        int WindowWidth = 0;
        int WindowHeight = 0;
        TilePosition OldPlayerPosition = new TilePosition { x = 0, y = 0, scale = TileSize } ;
        TilePosition CurrentPlayerPosition;
        List<PDTBC> EmptyDrawList = new List<PDTBC>();

        public Game(IMaze mazeInterface)
        {
            MovementKeys = new Dictionary<PDKey, Action>
            {
                { PDKey.W, MoveUp },
                { PDKey.Up, MoveUp },
                { PDKey.A, MoveLeft },
                { PDKey.Left, MoveLeft },
                { PDKey.S, MoveDown },
                { PDKey.Down, MoveDown },
                { PDKey.D, MoveRight },
                { PDKey.Right, MoveRight },
            };

            MazeInterface = mazeInterface;
        }

        public void WindowSizeChanged(int newWidth, int newHeight)
        {
            WindowWidth = newWidth;
            WindowHeight = newHeight;
        }

        public bool DoWork(PDKey keyPressed)
        {
            bool didWork = false;

            MovementKeys[keyPressed]();

            // player moved, need to draw
            if (!(OldPlayerPosition.Equals(CurrentPlayerPosition)))
            {
                didWork = true;
            }

            return didWork;
        }

        public void MoveUp()
        {
            if (CurrentPlayerPosition.y * CurrentPlayerPosition.scale - TileSize >= 0)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.y -= 1;
            }
        }

        void MoveDown()
        {
            if (CurrentPlayerPosition.y * CurrentPlayerPosition.scale + TileSize < WindowHeight)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.y += 1;
            }
        }

        void MoveLeft()
        {
            if (CurrentPlayerPosition.x * CurrentPlayerPosition.scale - TileSize >= 0)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.x -= 1;
            }
        }

        void MoveRight()
        {
            if (CurrentPlayerPosition.x * CurrentPlayerPosition.scale + TileSize < WindowWidth)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.x += 1;
            }
        }

        /*
         * Might be called by platform player if window needs to be repainted
         * i.e. not necessarily right after the platform layer has called DoWork()
         */
        public List<PDTBC> ToDraw()
        {
            if (OldPlayerPosition.Equals(CurrentPlayerPosition))
            {
                return EmptyDrawList;
            }
            else
            {
                return new List<PDTBC>
                {
                    new PDTBC { tag = Goblin.Item1, x = CurrentPlayerPosition.x * CurrentPlayerPosition.scale, y = CurrentPlayerPosition.y * CurrentPlayerPosition.scale },
                    new PDTBC { tag = Path.Item1, x = OldPlayerPosition.x * CurrentPlayerPosition.scale, y = OldPlayerPosition.y * CurrentPlayerPosition.scale },
                };
            }
        }

        public List<PDKey> WhenPressedNotify()
        {
            return MovementKeys.Select(m => m.Key).ToList();
        }

        public Dictionary<int, Bitmap> LoadTaggedBitmaps()
        {
            return new Dictionary<int, Bitmap>
            {
                { Goblin.Item1, Goblin.Item2 },
                { Path.Item1, Path.Item2 },
                { Exit.Item1, Exit.Item2 },
            };
        }

        public List<PDTBC> Initialize()
        {
            return new List<PDTBC>
            {
                new PDTBC { tag = Goblin.Item1, x = CurrentPlayerPosition.x * CurrentPlayerPosition.scale, y = CurrentPlayerPosition.y * CurrentPlayerPosition.scale }
            };
        }

        struct TilePosition
        {
            public int x;
            public int y;
            public int scale;
        }
    }
}
