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

using PDKey = Platform.PlatformData.Key;
using PDTBC = Platform.PlatformData.TaggedBitmapCoordinates;

using Game;
using Maze;

namespace Platform
{

    public class MainProgram
    {
        [STAThread]
        public static void Main()
        {
            var window = new Platform("Prim's Maze", GameInterface.NewInterface(Maze.MazeInterface.NewInterface()));
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
            ClientSize = new Size(21 * 32, 21 * 32);
            Paint += new PaintEventHandler(Draw);
            KeyDown += new KeyEventHandler(HandleInput);

            GameInterface.WindowSizeChanged(ClientSize.Width, ClientSize.Height);

            RegisterWorkKeys(GameInterface.WhenPressedNotify());

            TaggedBitmaps = GameInterface.LoadTaggedBitmaps();

            DrawList = GameInterface.Initialize();
            //Refresh();
            Update();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
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
                    foreach (var item in DrawList)
                    {
                        Invalidate(
                            new Rectangle(
                                item.x,
                                item.y,
                                TaggedBitmaps[item.tag].Width,
                                TaggedBitmaps[item.tag].Height)
                        );
                    }
                    Update();
                }
            }
        }

        void Draw(object sender, PaintEventArgs e)
        {
            // check if window is outside monitor, i.e. whole/part of window might need to be refreshed
            var monitorLeft   = SystemInformation.VirtualScreen.Left;
            var monitorTop    = SystemInformation.VirtualScreen.Top;
            var monitorWidth  = SystemInformation.VirtualScreen.Width;
            var monitorHeight = SystemInformation.VirtualScreen.Height;

            var bounds = DesktopBounds;

            List<PDTBC> oldDrawList = null;

            if (bounds.X < monitorLeft)
            {
                oldDrawList = DrawList;
                DrawList = GameInterface.AllTiles();
            }
            else if (bounds.Y < monitorTop)
            {
                oldDrawList = DrawList;
                DrawList = GameInterface.AllTiles();
            }
            else if (bounds.Right > monitorWidth)
            {
                oldDrawList = DrawList;
                DrawList = GameInterface.AllTiles();
            }
            else if (bounds.Bottom > monitorHeight)
            {
                oldDrawList = DrawList;
                DrawList = GameInterface.AllTiles();
            }

            if (oldDrawList != null)
            {
                DrawList.AddRange(oldDrawList);
                Invalidate();
            }

            Graphics graphics = e.Graphics;
            foreach (var bm in DrawList)
            {
                graphics.DrawImage(TaggedBitmaps[bm.tag], bm.x, bm.y);
            }
        }
    }
}
