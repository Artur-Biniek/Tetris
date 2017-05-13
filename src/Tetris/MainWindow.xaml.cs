using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PIXEL_SIZE = 15;

        const int WIDTH = 32;
        const int HEIGHT = 32;

        DispatcherTimer _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        Rectangle[,] _pixels = new Rectangle[32, 32];

        uint[] _screenMemory = new uint[32];
        uint[] TotalLines = new uint[1];

        long _programStart;
        uint _gamePad;

        private TetrisGame _game;

        public MainWindow()
        {
            InitializeComponent();
            initializeScreen();
            initializeTimer();
            InitilizeKeyboard();

            _game = new TetrisGame(_screenMemory, TotalLines, getTime, () => _gamePad);

            _game.InitGame();
        }

        private void InitilizeKeyboard()
        {
            this.KeyDown += (o, e) =>
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Left:
                        _gamePad |= TetrisGame.KEY_LEFT;
                        break;
                    case System.Windows.Input.Key.Right:
                        _gamePad |= TetrisGame.KEY_RIGHT;
                        break;
                    case System.Windows.Input.Key.Up:
                        _gamePad |= TetrisGame.KEY_UP;
                        break;
                    case System.Windows.Input.Key.Down:
                        _gamePad |= TetrisGame.KEY_DOWN;
                        break;
                    case System.Windows.Input.Key.Escape:
                        _gamePad |= TetrisGame.KEY_ESC;
                        break;
                }
            };

            this.KeyUp += (o, e) =>
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Left:
                        _gamePad &= ~TetrisGame.KEY_LEFT;
                        break;
                    case System.Windows.Input.Key.Right:
                        _gamePad &= ~TetrisGame.KEY_RIGHT;
                        break;
                    case System.Windows.Input.Key.Up:
                        _gamePad &= ~TetrisGame.KEY_UP;
                        break;
                    case System.Windows.Input.Key.Down:
                        _gamePad &= ~TetrisGame.KEY_DOWN;
                        break;
                    case System.Windows.Input.Key.Escape:
                        _gamePad &= ~TetrisGame.KEY_ESC;
                        break;
                }
            };
        }

        private void initializeTimer()
        {
            _programStart = DateTime.Now.Ticks;

            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(0.2);
            _dispatcherTimer.Tick += gameLoop;

            _dispatcherTimer.IsEnabled = true;
        }

        private void gameLoop(object sender, EventArgs e)
        {
            _game.Run();

            screenRefresh();

            if (_game.GameOver)
            {
                Title = "Game Over! " + TotalLines[0].ToString();
            }
            else
            {
                Title = TotalLines[0].ToString();
            }
        }

        private void initializeScreen()
        {
            for (int i = 0; i < 32; i++)
            {
                var def = new RowDefinition();
                def.Height = GridLength.Auto;

                ScreenGrid.RowDefinitions.Add(def);

                var colDef = new ColumnDefinition();
                colDef.Width = GridLength.Auto;
                ScreenGrid.ColumnDefinitions.Add(colDef);
            }

            for (int i = 0; i < 32; i++)
            {
                ScreenGrid.RowDefinitions.Add(new RowDefinition());

                for (int j = 0; j < 32; j++)
                {
                    var rect = new Rectangle();

                    rect.Width = PIXEL_SIZE;
                    rect.Height = PIXEL_SIZE;
                    rect.Fill = new SolidColorBrush(Colors.Black);

                    rect.Stroke = new SolidColorBrush(Colors.DarkRed);
                    rect.StrokeThickness = 1;

                    Grid.SetRow(rect, i);
                    Grid.SetColumn(rect, j);

                    ScreenGrid.Children.Add(rect);

                    _pixels[i, j] = rect;
                }
            }
        }

        private void screenRefresh()
        {
            for (int row = 0; row < 32; row++)
            {
                var bit = (uint)1 << 31;

                for (var col = 0; col < 32; col++, bit >>= 1)
                {
                    if ((_screenMemory[row] & bit) != 0)
                    {
                        _pixels[row, col].Fill = new SolidColorBrush(Colors.Lime);
                    }
                    else
                    {
                        _pixels[row, col].Fill = new SolidColorBrush(Colors.Black);
                    }
                }
            }
        }

        private uint getTime()
        {
            return (uint)TimeSpan.FromTicks(DateTime.Now.Ticks - _programStart).TotalMilliseconds;
        }
    }
}
