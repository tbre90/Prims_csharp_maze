using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

using PDKey = Platform.PlatformData.Key;
using PDTBC = Platform.PlatformData.TaggedBitmapCoordinates;

using Maze;

using MT = Maze.MazeData.Tile;
using Platform;

namespace Game
{
    public static class GameInterface
    {
        public static IGame NewInterface(IMaze mazeInterface)
        {
            return new GameInstance(mazeInterface);
        }
    }

    public interface IGame
    {
        List<PDKey> WhenPressedNotify(); /* list of keys that the game wants the platform layer to listen for */
        Dictionary<int, Bitmap> LoadTaggedBitmaps(); /* bitmaps with tags to more easily refer to them after being loaded by the platform layer */
        List<PDTBC> Initialize(); /* set initialize game state */
        List<PDTBC> AllTiles(); /* called by platform layer if it ever needs to repaint more than just previous and current tile */
        void DoWork(PDKey keyPressed);
        List<PDTBC> ToDraw(); /* if it did, call ToDraw() for a list of bitmap tags and their new positions */
        void WindowSizeChanged(int newWidth, int newHeight); /* only used right after a Game object is instantiated, to set the window dimension (bounds checking) */
    }

    class GameInstance : IGame
    {
        enum GameState { Running, GameOver, IllegalMove };

        GameState CurrentState = GameState.Running;

        const int TileSize = 32;
        readonly Tuple<int, Bitmap> Goblin = Tuple.Create(1, Properties.Resources.Goblin);
        readonly Tuple<int, Bitmap> Path = Tuple.Create(2, Properties.Resources.Wooden_path);
        readonly Tuple<int, Bitmap> Exit = Tuple.Create(3, Properties.Resources.Exit);
        readonly Dictionary<PDKey, Action> MovementKeys;
        readonly IMaze MazeInterface;

        readonly List<Tuple<int, Bitmap>> GameOverText = new List<Tuple<int, Bitmap>>
        {
            Tuple.Create(4, Properties.Resources.Letter_Y),
            Tuple.Create(5, Properties.Resources.Letter_O),
            Tuple.Create(6, Properties.Resources.Letter_U),
            null,
            Tuple.Create(7, Properties.Resources.Letter_W),
            Tuple.Create(8, Properties.Resources.Letter_O),
            Tuple.Create(9, Properties.Resources.Letter_N),
        };

        int WindowWidth = 0;
        int WindowHeight = 0;
        TilePosition OldPlayerPosition = new TilePosition { x = 0, y = 0, scale = TileSize };
        TilePosition CurrentPlayerPosition = new TilePosition { x = 0, y = 0, scale = TileSize };
        List<PDTBC> EmptyDrawList = new List<PDTBC>();

        public GameInstance(IMaze mazeInterface)
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

        public void DoWork(PDKey keyPressed)
        {
            if (CurrentState == GameState.Running)
            {
                MovementKeys[keyPressed]();
            }
        }

        public void MoveUp()
        {
            if (CurrentPlayerPosition.y * CurrentPlayerPosition.scale - TileSize >= 0)
            {
                var move = PerformMove(CurrentPlayerPosition.x, CurrentPlayerPosition.y - 1);
                if (move == GameState.Running)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.y -= 1;
                }
                else if (move == GameState.GameOver)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.y -= 1;
                    CurrentState = GameState.GameOver;
                }
            }
        }

        void MoveDown()
        {
            if (CurrentPlayerPosition.y * CurrentPlayerPosition.scale + TileSize < WindowHeight)
            {
                var move = PerformMove(CurrentPlayerPosition.x, CurrentPlayerPosition.y + 1);
                if (move == GameState.Running)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.y += 1;
                }
                else if (move == GameState.GameOver)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.y += 1;
                    CurrentState = GameState.GameOver;
                }
            }
        }

        void MoveLeft()
        {
            if (CurrentPlayerPosition.x * CurrentPlayerPosition.scale - TileSize >= 0)
            {
                var move = PerformMove(CurrentPlayerPosition.x - 1, CurrentPlayerPosition.y);
                if (move == GameState.Running)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.x -= 1;
                }
                else if (move == GameState.GameOver)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.x -= 1;
                    CurrentState = GameState.GameOver;
                }
            }
        }

        void MoveRight()
        {
            if (CurrentPlayerPosition.x * CurrentPlayerPosition.scale + TileSize < WindowWidth)
            {
                var move = PerformMove(CurrentPlayerPosition.x + 1, CurrentPlayerPosition.y);
                if (move == GameState.Running)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.x += 1;
                }
                else if (move == GameState.GameOver)
                {
                    OldPlayerPosition = CurrentPlayerPosition;
                    CurrentPlayerPosition.x += 1;
                    CurrentState = GameState.GameOver;
                }
            }
        }

        GameState PerformMove(int x, int y)
        {
            var tile = MazeInterface.GetTile(x, y);

            if      (tile == MT.Passage) { return GameState.Running; }
            else if (tile == MT.Exit)    { return GameState.GameOver; }
            else                         { return GameState.IllegalMove; }
        }

        /*
         * Might be called by platform player if window needs to be repainted
         * i.e. not necessarily right after the platform layer has called DoWork()
         */
        public List<PDTBC> ToDraw()
        {
            return AllTiles();
        }

        private List<PDTBC> MakeTextList(int x, int y, List<Tuple<int, Bitmap>> gameOverText)
        {
            int numberOfLetters = 0;
            int letterAvgWidth = 0;

            foreach (var b in gameOverText)
            {
                if (b != null)
                {
                    numberOfLetters++;
                    letterAvgWidth += b.Item2.Width;
                }
            }

            letterAvgWidth /= numberOfLetters;

            var textList = new List<PDTBC>();
            int offset = x;

            bool addSpace = false;
            foreach (var t in gameOverText)
            {
                if (t == null)
                {
                    addSpace = true;
                }
                else
                {
                    if (addSpace)
                    {
                        textList.Add(new PDTBC { tag = t.Item1, x = offset + letterAvgWidth, y = y });
                        offset += letterAvgWidth;
                        offset += t.Item2.Width;
                        addSpace = false;
                    }
                    else
                    {
                        textList.Add(new PDTBC { tag = t.Item1, x = offset, y = y });
                        offset += t.Item2.Width;
                    }
                }
            }

            return textList;
        }

        public List<PDKey> WhenPressedNotify()
        {
            return MovementKeys.Select(m => m.Key).ToList();
        }

        public Dictionary<int, Bitmap> LoadTaggedBitmaps()
        {
            var taggedBitmaps = new Dictionary<int, Bitmap>();

            taggedBitmaps.Add(Goblin.Item1, Goblin.Item2);
            taggedBitmaps.Add(Path.Item1, Path.Item2);
            taggedBitmaps.Add(Exit.Item1, Exit.Item2);

            foreach (var letter in GameOverText)
            {
                if (letter != null)
                {
                    taggedBitmaps.Add(
                        letter.Item1, letter.Item2
                    );
                }
            }

            return taggedBitmaps;
        }

        public List<PDTBC> Initialize()
        {
            var maze = MazeInterface.NewMaze();

            return AllTiles();
        }

        public List<PDTBC> AllTiles()
        {
            var maze = MazeInterface.GetCurrent();
            var drawList = new List<PDTBC>();

            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (maze[i, j] == MT.Passage)
                    {
                        drawList.Add(
                            new PDTBC { tag = Path.Item1, x = i * TileSize, y = j * TileSize }
                        );
                    }
                    else if (maze[i, j] == MT.Exit)
                    {
                        drawList.Add(
                            new PDTBC { tag = Path.Item1, x = i * TileSize, y = j * TileSize }
                        );
                        drawList.Add(
                            new PDTBC { tag = Exit.Item1, x = i * TileSize, y = j * TileSize }
                        );
                    }
                }
            }


            drawList.Add(new PDTBC { tag = Path.Item1, x = CurrentPlayerPosition.x * CurrentPlayerPosition.scale, y = CurrentPlayerPosition.y * CurrentPlayerPosition.scale });
            drawList.Add(new PDTBC { tag = Goblin.Item1, x = CurrentPlayerPosition.x * CurrentPlayerPosition.scale, y = CurrentPlayerPosition.y * CurrentPlayerPosition.scale });

            if (CurrentState == GameState.GameOver)
            {
                drawList.AddRange(MakeTextList(WindowWidth / 2, WindowHeight / 2, GameOverText));
            }

            return drawList;
        }

        struct TilePosition
        {
            public int x;
            public int y;
            public int scale;
        }
    }
}
