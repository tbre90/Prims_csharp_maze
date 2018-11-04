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

namespace Platform
{
    public class MainProgram
    {
        const int NumberOfRows = 21;
        const int NumberOfCollumns = 21;

        [STAThread]
        public static void Main()
        {
            var window = new Platform(NumberOfRows * 32, NumberOfCollumns * 32, "Prim's Maze",
                GameInterface.NewInterface(
                        Maze.MazeInterface.NewInterface(NumberOfRows, NumberOfCollumns)
                    )
                );
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
        long PreviousTimeInMsec = 0;
        long CurrentTimeInMsec = 0;

        const long UpdatesPerSecond = (long) (1.0 / 30.0 * 1000.0);

        IGame GameInterface;
        List<Tuple<Keys, PDKey>> InputToHandle = new List<Tuple<Keys, PDKey>>();
        Dictionary<int, Bitmap> TaggedBitmaps;
        List<PDTBC> DrawList = new List<PDTBC>();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        public Platform(int x, int y, string windowText, IGame gameInterface)
        {
            GameInterface = gameInterface;
            ShowIcon = false; // remove default form symbol/icon
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);  // --||--
            FormBorderStyle = FormBorderStyle.FixedSingle; // do not want a resizeable window
            UpdateStyles();
            Text = windowText;
            MinimizeBox = false; // don't want to handle max/min-imize
            MaximizeBox = false; // --||--
            BackColor = Color.Black;
            ClientSize = new Size(x, y);
            KeyDown += new KeyEventHandler(HandleInput);

            GameInterface.WindowSizeChanged(ClientSize.Width, ClientSize.Height);

            RegisterWorkKeys(GameInterface.WhenPressedNotify());

            TaggedBitmaps = GameInterface.LoadTaggedBitmaps();

            DrawList = GameInterface.Initialize();

            var drawRefresh = new System.Windows.Forms.Timer();
            drawRefresh.Interval = (int)UpdatesPerSecond / 2;
            drawRefresh.Tick += new EventHandler(DrawCallback);
            drawRefresh.Start();
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
                GameInterface.DoWork(maybeAction.Value);
            }
        }

        void DrawCallback(object sender, EventArgs e)
        {
            CurrentTimeInMsec += DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (CurrentTimeInMsec - PreviousTimeInMsec > UpdatesPerSecond)
            {
                Debug.WriteLine("Fuaw9odja wpdj");
                DrawList = GameInterface.ToDraw();
                Invalidate();
            }
            PreviousTimeInMsec = CurrentTimeInMsec;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            foreach (var bm in DrawList)
            {
                g.DrawImage(TaggedBitmaps[bm.tag], bm.x, bm.y);
            }
        }
    }
}
