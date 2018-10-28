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
            var window = new Platform("Prim's Maze", new Game(new Maze(21, 21)));
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

            DrawList = GameInterface.Initialize();
            Refresh();
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
                    Refresh();
                }
            }
        }

        void Draw(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            foreach (var bm in DrawList)
            {
                Debug.WriteLine("{0}:{1}", bm.x, bm.y);
                graphics.DrawImage(TaggedBitmaps[bm.tag], bm.x, bm.y);
            }
        }
    }
}
