using System;

namespace Tetris
{
    public class TetrisGame
    {
        public const uint KEY_LEFT = 1 << 0;
        public const uint KEY_UP = 1 << 1;
        public const uint KEY_RIGHT = 1 << 2;
        public const uint KEY_DOWN = 1 << 3;
        public const uint KEY_ESC = 1 << 4;

        private readonly uint[] BLOCKS_DATA = new[]
        {
               (uint)0x0F00, (uint)0x2222, (uint)0x00f0, (uint)0x4444, // I-block
               (uint)0x8E00, (uint)0x6440, (uint)0x0E20, (uint)0x44C0, // J-block
               (uint)0x2E00, (uint)0x4460, (uint)0x0E80, (uint)0xC440, // L-block
               (uint)0x6600, (uint)0x6600, (uint)0x6600, (uint)0x6600, // O-block
               (uint)0x6C00, (uint)0x4620, (uint)0x06C0, (uint)0x8C40, // S-block
               (uint)0x4E00, (uint)0x4640, (uint)0x0E40, (uint)0x4C40, // T-block
               (uint)0xC600, (uint)0x2640, (uint)0x0C60, (uint)0x4C80  // Z-block
        };

        private readonly int[] START_LOCATION_DATA = new[]
        {
            -1, 0, -2, 0,
             0, 0, -1, 0,
             0, 0, -1, 0,
             0, 0,  0, 0,
             0, 0, -1, 0,
             0, 0, -1, 0,
             0, 0, -1, 0
        };

        const int BOARD_HORIZONTAL_SHIFT = 11;
        const int BOARD_VERTICAL_SHIFT = 6;

        const uint FULL_LINE_MASK = 0x03FF;
        const uint COPY_LINE_MASK = ~(FULL_LINE_MASK << BOARD_HORIZONTAL_SHIFT);

        const uint REPEAT_RATE_SLOW = 200;
        const uint REPEAT_RATE_FAST = 100;
        const uint REPEAT_RATE_RAPID = 40;

        const int BOARD_WIDTH = 10;
        const int BOARD_HEIGHT = 20;

        private readonly Random _rnd = new Random();

        uint[] _board = new uint[BOARD_HEIGHT];

        int _curRow, _curCol;
        int _curDelay = 1000;

        int _curBlock, _curRotation;
        int _nextBlock, _nextRotation;

        bool _playing;

        uint _oldKeys;
        uint _lastKeyboardTime;
        uint _lastUpdateTime;

        private uint[] _screenMemory;
        private uint[] _sevenSegDisplay;
        private Func<uint> _getTime;
        private Func<uint> _getKeys;

        public bool GameOver
        {
            get { return !_playing; }
        }

        public TetrisGame(uint[] screenMemory, uint[] sevenSegDisplay, Func<uint> getElapsedMiliseconds, Func<uint> getGamepadState)
        {
            _screenMemory = screenMemory;
            _sevenSegDisplay = sevenSegDisplay;
            _getTime = getElapsedMiliseconds;
            _getKeys = getGamepadState;
        }

        public void InitGame()
        {
            _sevenSegDisplay[0] = 0;

            _nextBlock = _rnd.Next(7) * 4;
            _nextRotation = _rnd.Next(4);

            createBoard();
            generateTetromino();

            _playing = true;
        }

        private void createBoard()
        {
            for (int i = 0; i < 32; i += 1)
            {
                if (i < BOARD_HEIGHT)
                {
                    _board[i] = 0;
                    _screenMemory[i + BOARD_VERTICAL_SHIFT] = 0x00200400;
                }                
            }

            _screenMemory[BOARD_VERTICAL_SHIFT + BOARD_HEIGHT] = 0x003FFC00;
        }

        private void generateTetromino()
        {
            _curBlock = _nextBlock;
            _curRotation = _nextRotation;

            _nextBlock = _rnd.Next(7) * 4;
            _nextRotation = _rnd.Next(4);

            _curRow = START_LOCATION_DATA[_curBlock + _curRotation];
            _curCol = 3;

            if (!canMoveBlock(_curCol, _curRow, _curRotation))
            {
                _playing = false;
            }
        }

        public void Run()
        {
            var keystate = _getKeys();

            if ((keystate & KEY_ESC) != 0)
            {
                InitGame();
            }

            if (!_playing) return;

            var time = _getTime();

            bool moveLeft = false, moveRight = false, rotate = false, moveDown = false;
            int ncol = _curCol, nrow = _curRow, nrot = _curRotation;

            #region Key Handling
            if ((keystate & KEY_LEFT) != 0)
            {
                if ((_oldKeys & KEY_LEFT) == 0)
                {
                    moveLeft = true;
                    _lastKeyboardTime = time + REPEAT_RATE_SLOW;
                }
                else
                {
                    if (time > _lastKeyboardTime)
                    {
                        _lastKeyboardTime = time + REPEAT_RATE_FAST;
                        moveLeft = true;
                    }
                }
            }

            if ((keystate & KEY_RIGHT) != 0)
            {
                if ((_oldKeys & KEY_RIGHT) == 0)
                {
                    moveRight = true;
                    _lastKeyboardTime = time + REPEAT_RATE_SLOW;
                }
                else
                {
                    if (time > _lastKeyboardTime)
                    {
                        _lastKeyboardTime = time + REPEAT_RATE_FAST;
                        moveRight = true;
                    }
                }
            }

            if ((keystate & KEY_UP) != 0)
            {
                if ((_oldKeys & KEY_UP) == 0)
                {
                    rotate = true;
                    _lastKeyboardTime = time + REPEAT_RATE_SLOW;
                }
                else
                {
                    if (time > _lastKeyboardTime)
                    {
                        _lastKeyboardTime = time + REPEAT_RATE_FAST;
                        rotate = true;
                    }
                }
            }

            if ((keystate & KEY_DOWN) != 0)
            {
                if ((_oldKeys & KEY_DOWN) == 0)
                {
                    moveDown = true;
                    _lastKeyboardTime = time + REPEAT_RATE_SLOW;
                }
                else
                {
                    if (time > _lastKeyboardTime)
                    {
                        _lastKeyboardTime = time + REPEAT_RATE_RAPID;
                        moveDown = true;
                    }
                }
            }
            #endregion

            _oldKeys = keystate;

            if (moveLeft)
            {
                ncol--;
            }
            if (moveRight)
            {
                ncol++;
            }
            if (rotate)
            {
                nrot = (_curRotation + 1) % 4;
            }
            if (moveDown)
            {
                nrow++;
            }

            if (moveLeft || moveRight || rotate || moveDown)
            {
                if (canMoveBlock(ncol, nrow, nrot))
                {
                    _curCol = ncol;
                    _curRow = nrow;
                    _curRotation = nrot;
                }
            }

            if (time > _lastUpdateTime + _curDelay)
            {
                if (canMoveBlock(_curCol, _curRow + 1, _curRotation))
                {
                    _curRow++;
                }
                else
                {
                    mergeBlock();
                    _sevenSegDisplay[0] += checkLines();
                    generateTetromino();
                }

                _lastUpdateTime = time;
            }

            blitBlock(_curRow, _curCol, _curRotation);
        }

        private uint checkLines()
        {
            uint res = 0;

            for (int row = BOARD_HEIGHT - 1; row >= 0; row--)
            {
                if ((_board[row] & FULL_LINE_MASK) == FULL_LINE_MASK)
                {
                    for (int next = row; next > 1; next--)
                    {
                        _board[next] = (_board[next - 1] & COPY_LINE_MASK);
                    }

                    row++; // recheck current row;
                    res++; // add line to result
                }
            }

            return res;
        }

        private void mergeBlock()
        {
            uint mask = 0xF000;
            int offset = 6;
            int shft = 0;

            uint mem = 0u;
            uint strip = 0u;
            uint block = BLOCKS_DATA[_curBlock + _curRotation];
            int i = 0;

            for (i = _curRow; i < _curRow + 4; i++, mask >>= 4, offset -= 4)
            {
                if (i < 0 || i >= BOARD_HEIGHT) continue;

                mem = _board[i];

                strip = (uint)(block & mask);
                shft = offset + _curCol;

                if (shft < 0)
                {
                    strip <<= (-shft);
                }
                else
                {
                    strip >>= shft;
                }

                mem |= strip;

                _board[i] = mem;
            }
        }

        private bool canMoveBlock(int dcol, int drow, int drot)
        {
            uint mask = 0x8000;
            uint block = BLOCKS_DATA[_curBlock + drot];
            int x, y;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if ((mask & block) != 0)
                    {
                        x = c + dcol;
                        y = r + drow;
                        if (x < 0) return false;
                        if (x >= BOARD_WIDTH) return false;
                        if (y < 0) return false;
                        if (y >= BOARD_HEIGHT) return false;

                        var line = _board[y];

                        var bit = BOARD_WIDTH - 1 - x;

                        if ((line & (1 << bit)) != 0) return false;
                    }

                    mask >>= 1;
                }
            }

            return true;
        }

        private void blitBlock(int row, int col, int rot)
        {
            uint mask = 0xF000;
            int offset = 6;

            uint mem = 0u;
            uint strip = 0u;
            uint block = BLOCKS_DATA[_curBlock + rot];

            int i = 0;
            int shft = 0;

            for (i = 0; i < row; i++)
            {
                var maskedLine = _screenMemory[i + BOARD_VERTICAL_SHIFT] & COPY_LINE_MASK;
                _screenMemory[i + BOARD_VERTICAL_SHIFT] = maskedLine | (_board[i] << BOARD_HORIZONTAL_SHIFT);
            }

            for (i = row; i < row + 4; i++, mask >>= 4, offset -= 4)
            {
                if (i < 0 || i >= BOARD_HEIGHT) continue;

                mem = _board[i];

                strip = (uint)(block & mask);
                shft = offset + col;

                if (shft < 0)
                {
                    strip <<= (-shft);
                }
                else
                {
                    strip >>= shft;
                }

                mem |= strip;

                var maskedLine = _screenMemory[i + BOARD_VERTICAL_SHIFT] & COPY_LINE_MASK;
                _screenMemory[i + BOARD_VERTICAL_SHIFT] = maskedLine | (mem << BOARD_HORIZONTAL_SHIFT);
            }

            for (i = row + 4; i < BOARD_HEIGHT; i++)
            {
                var maskedLine = _screenMemory[i + BOARD_VERTICAL_SHIFT] & COPY_LINE_MASK;
                _screenMemory[i + BOARD_VERTICAL_SHIFT] = maskedLine | (_board[i] << BOARD_HORIZONTAL_SHIFT);
            }
        }
    }
}
