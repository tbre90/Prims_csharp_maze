using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.IO;

using PDKey = Maze.PlatformData.Key;
using PDTBC = Maze.PlatformData.TaggedBitmapCoordinates;

namespace Maze
{
    public class MainProgram
    {
        [STAThread]
        public static void Main()
        {
            var window = new Platform("Prim's Maze", new Game());
            Application.Run(window);
        }
    }

    public interface IPlatform
    {
    }

    public static class PlatformData
    {
        public enum Key { W, A, S, D, Up, Down, Left, Right };
        public struct TaggedBitmapCoordinates { public int tag; public int x; public int y; };
    }

    public class Platform : Form
    {
        IGame GameInterface;
        List<Tuple<Keys, PDKey>> InputToHandle = new List<Tuple<Keys, PDKey>>();
        Dictionary<int, Bitmap> TaggedBitmaps;
        List<PDTBC> DrawList = new List<PDTBC>();

        public Platform(string windowText, IGame gameInterface)
        {
            GameInterface = gameInterface;
            ShowIcon = false; // remove default form symbol/icon
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true); // for double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);  // --||--
            FormBorderStyle = FormBorderStyle.FixedSingle; // do not want a resizeable window
            Text = windowText;
            MinimizeBox = false; // don't want to handle max/min-imize
            MaximizeBox = false; // --||--
            BackColor = Color.Black;
            ClientSize = new Size(25 * 32, 20 * 32);
            //Width = 800;
            //Height = 640;
            Paint += new PaintEventHandler(Draw);
            KeyDown += new KeyEventHandler(HandleInput);

            GameInterface.WindowSizeChanged(ClientSize.Width, ClientSize.Height);

            RegisterWorkKeys(GameInterface.WhenPressedNotify());

            TaggedBitmaps = GameInterface.LoadTaggedBitmaps();
        }

        void RegisterWorkKeys(List<PDKey> whenPressedNotify)
        {
            foreach (var key in whenPressedNotify)
            {
                InputToHandle.Add(Tuple.Create(KeyToPlatformSpecific(key), key));
            }
        }

        Keys KeyToPlatformSpecific(PDKey actionKey)
        {
            var returnKey = Keys.None;

            switch (actionKey)
            {
                case PDKey.W:     { returnKey = Keys.W; }       break;
                case PDKey.Up:    { returnKey = Keys.Up; }      break;
                case PDKey.A:     { returnKey = Keys.A; }       break;
                case PDKey.Left:  { returnKey = Keys.Left; }    break;
                case PDKey.S:     { returnKey = Keys.S; }       break;
                case PDKey.Down:  { returnKey = Keys.Down; }    break;
                case PDKey.D:     { returnKey = Keys.D; }       break;
                case PDKey.Right: { returnKey = Keys.Right; }   break;
                default:          { }                           break;
            }

            return returnKey;
        }

        void HandleInput(object sender, KeyEventArgs e)
        {
            var maybeAction = InputToHandle.FirstOrDefault(elem => elem.Item1 == e.KeyCode)?.Item2;
            if (maybeAction.HasValue)
            {
                if (GameInterface.DoWork(maybeAction.Value))
                {
                    DrawList = GameInterface.ToDraw();
                    this.Refresh();
                }
            }
        }

        void Draw(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            foreach (var bm in DrawList)
            {
                graphics.DrawImage(TaggedBitmaps[bm.tag], bm.x, bm.y);
            }
        }
    }

    public interface IGame
    {
        List<PDKey> WhenPressedNotify(); /* list of keys that the game wants the platform layer to listen for */
        Dictionary<int, Bitmap> LoadTaggedBitmaps(); /* bitmaps with tags to more easily refer to them after being loaded by the platform layer */
        bool DoWork(PDKey keyPressed); /* did the game do any work? */
        List<PDTBC> ToDraw(); /* if it did, call ToDraw() for a list of bitmap tags and their new positions */
        void WindowSizeChanged(int newWidth, int newHeight); /* only used right after a Game object is instantiated, to set the window dimension (bounds checking) */
    }

    public class Game : IGame
    {
        readonly int TileWidth = 32;
        readonly int TileHeight = 32;
        readonly Tuple<int, Bitmap> Goblin = Tuple.Create(1, Properties.Resources.Goblin);
        readonly Tuple<int, Bitmap> Path = Tuple.Create(2, Properties.Resources.Wooden_wall);
        readonly Tuple<int, Bitmap> Exit = Tuple.Create(3, Properties.Resources.Exit);

        int WindowWidth = 0;
        int WindowHeight = 0;
        Position OldPlayerPosition;
        Position CurrentPlayerPosition;
        readonly Dictionary<PDKey, Action> MovementKeys;
        List<PDTBC> EmptyDrawList = new List<PDTBC>();

        public Game()
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
            if (CurrentPlayerPosition.y - TileHeight >= 0)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.y -= TileHeight;
            }
        }

        void MoveDown()
        {
            if (CurrentPlayerPosition.y + TileHeight < WindowHeight)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.y += TileHeight;
            }
        }

        void MoveLeft()
        {
            if (CurrentPlayerPosition.x - TileWidth >= 0)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.x -= TileWidth;
            }
        }

        void MoveRight()
        {
            if (CurrentPlayerPosition.x + TileWidth < WindowWidth)
            {
                OldPlayerPosition = CurrentPlayerPosition;
                CurrentPlayerPosition.x += TileWidth;
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
                    new PDTBC { tag = Goblin.Item1, x = CurrentPlayerPosition.x, y = CurrentPlayerPosition.y },
                    new PDTBC { tag = Path.Item1, x = OldPlayerPosition.x, y = OldPlayerPosition.y },
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

        struct Position
        {
            public int x;
            public int y;
        }
    }
}
