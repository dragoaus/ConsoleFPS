using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ConsoleFPS
{
    internal class Program
    {
        static int nScreenWidth = 120;
        static int nScreenHeight = 40;

        #region WindowsAPIs
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="dwShareMode"></param>
        /// <param name="secutiryAttributes"></param>
        /// <param name="flags"></param>
        /// <param name="screenBufferData"></param>
        /// <returns>The return value is a handle to the new console screen buffer</returns>
        /// <see cref="https://learn.microsoft.com/en-us/windows/console/createconsolescreenbuffer"/>
        /// <remarks>More info on PInvoke</remarks>
        /// <see cref="https://www.pinvoke.net/default.aspx/kernel32.createconsolescreenbuffer"/>
        [DllImport("Kernel32.dll")]
        private static extern IntPtr CreateConsoleScreenBuffer(
            UInt32 dwDesiredAccess,
            UInt32 dwShareMode,
            IntPtr secutiryAttributes,
            UInt32 flags,
            IntPtr screenBufferData
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetConsoleActiveScreenBuffer(IntPtr hConsoleOutput);
        
        /// <summary>
        /// Write to console
        /// </summary>
        /// <param name="hConsoleOutput"></param>
        /// <param name="lpCharacter"></param>
        /// <param name="nLength"></param>
        /// <param name="dwWriteCoord"></param>
        /// <param name="lpNumberOfCharsWritten"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern bool WriteConsoleOutputCharacter(IntPtr hConsoleOutput,
            char[] lpCharacter, 
            uint nLength, 
            COORD dwWriteCoord,
            out uint lpNumberOfCharsWritten);

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;

            public COORD(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        #endregion

        #region StaticVariables

        static float playerX = 8.0f;
        static float playerY = 8.0f;
        static float playerA = 0.0f;

        static int mapHeight = 16;
        static int mapWidth = 16;


        static string map = "################" +
                            "#..............#" +
                            "#..........#...#" +
                            "#..........#...#" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "#......#########" +
                            "#..............#" +
                            "#..............#" +
                            "#..............#" +
                            "################";

        static float fOV = 3.14159f/4.0f;
        static float depth = 16.0f;

        static float elapsedTime;
        #endregion

        static void Main(string[] args)
        {

            var tp1 = DateTime.Now;
            var tp2 = DateTime.Now;

            Console.WindowWidth = nScreenWidth;
            Console.WindowHeight = nScreenHeight;
            
            UInt32 GENERIC_READ = Convert.ToUInt32(0x40000000L);
            UInt32 GENERIC_WRITE = Convert.ToUInt32(0x80000000L);
            
            var hConsole = CreateConsoleScreenBuffer(GENERIC_READ | GENERIC_WRITE,0, IntPtr.Zero, 1, IntPtr.Zero);
            SetConsoleActiveScreenBuffer(hConsole);


            char[] screen = new char[nScreenWidth * nScreenHeight];
            UInt32 nLength = Convert.ToUInt32(nScreenWidth * nScreenHeight);
            uint _ = 0;


            //Start user input thread
            //Thread userInputThread = new Thread(UserInputThreaded);
            //userInputThread.Start();

            while (true)
            {
                UserInput();

                tp2 = DateTime.Now;
                elapsedTime = (float) (tp2 - tp1).TotalMilliseconds;
                tp1 = tp2;

                for (int x = 0; x < nScreenWidth; x++)
                {
                    float rayAngle = (playerA - fOV/2.0f) + ((float)x / (float)nScreenWidth)* fOV;

                    float distanceToTheWall = 0.0f;
                    
                    bool hitWall = false;
                    bool boundary = false;

                    float eyeX = (float)Math.Sin(rayAngle);
                    float eyeY = (float)Math.Cos(rayAngle);

                    while (!hitWall && (distanceToTheWall < depth) )
                    {
                        distanceToTheWall += 0.1f;

                        int testX = (int)(playerX + eyeX * distanceToTheWall);
                        int testY = (int)(playerY + eyeY * distanceToTheWall);

                        // is ray out of bounds
                        if (testX < 0 || testX >= mapWidth || testY < 0 || testY >= mapHeight)
                        {
                            hitWall = true;
                            distanceToTheWall = depth;
                        }
                        else
                        {
                            if (map[testX * mapWidth + testY] == '#')
                            {
                                // Ray has hit wall
                                hitWall = true;

                                List<Tuple<float,float>> p = new List<Tuple<float,float>>();

                                for (int tx = 0; tx < 2; tx++)
                                {
                                    for (int ty = 0; ty < 2; ty++)
                                    {
                                        float vx = (float)testX + tx - playerX;
                                        float vy = (float)testY + ty - playerY;
                                        float d = (float) Math.Sqrt( (vx * vx) + (vy * vy) );
                                        float dotProduct = (eyeX * vx/d) + (eyeY * vy/d);
                                        p.Add(new Tuple<float, float>(d,dotProduct));
                                    }
                                }
                                p = p.OrderBy(x => x.Item1).ToList();

                                float bound = 0.01f;
                                if (Math.Acos(p[0].Item2) < bound)
                                {
                                    boundary = true;
                                }
                                if (Math.Acos(p[1].Item2) < bound)
                                {
                                    boundary = true;
                                }
                                if (Math.Acos(p[2].Item2) < bound)
                                {
                                    boundary = true;
                                }
                            }
                        }
                    }

                    int ceiling =  (int) ( (nScreenHeight/2.0f) - nScreenHeight / distanceToTheWall);
                    int floor =  (nScreenHeight - ceiling);

                    for (int y = 0; y < nScreenHeight; y++)
                    {
                        if (y < ceiling)
                        {
                            screen[y * nScreenWidth + x] = ' ';
                        }
                        else if (y > ceiling && y <= floor)
                        {
                            var wallShade = ' ';
                            if (distanceToTheWall <= depth/4f)
                                wallShade = (char)219;
                            else if (distanceToTheWall < depth/3f)
                                wallShade = (char)178;
                            else if (distanceToTheWall < depth/2f)
                                wallShade = (char)177;
                            else if (distanceToTheWall < depth)
                                wallShade = (char)176;

                            if (boundary)
                                wallShade = ' ';
                            
                            screen[y * nScreenWidth + x] = wallShade;
                        }
                        else
                        {
                            var floorShade = ' ';
                            float b = 1f - (( (float)y - nScreenHeight/2f)/( (float)nScreenHeight/2f));
                            if (b < 0.25)
                                floorShade = '#';
                            else if (b < 0.5)
                                floorShade = 'x';
                            else if (b < 0.75)
                                floorShade = '.';
                            else if (b < 0.9)
                                floorShade = '_';

                            screen[y * nScreenWidth + x] = floorShade;
                        }
                    }
                }


                float fps = 1.0f / (elapsedTime/1000);
                string header = $"X={playerX:##.##} Y={playerY:##.##} A={playerA:##.##} FPS={fps:###.##} ";

                for (int i = 0; i < header.Length; i++)
                {
                    screen[i] = header[i];
                }

                for (int mx = 0; mx < mapWidth; mx++)
                {
                    for (int my = 0; my < mapHeight; my++)
                    {
                        screen[(mx +1) * nScreenWidth + my] = map[mx*mapWidth + my];
                    }
                }

                screen[(int)playerX * nScreenWidth + (int)(playerY)] = 'P';

                WriteConsoleOutputCharacter(hConsole, screen, nLength, new COORD(0, 0), out _);
            }
        }

        /// <summary>
        /// Experimental version for threading
        /// </summary>
        private static void UserInputThreaded()
        {
            while (true)
            {
                UserInput();
            }
            
        }

        /// <summary>
        /// Handles user input for A=>turn left, D=>turn right, W=>move forward, S=>move backward, Q=>strafe left, E=>strafe left.
        /// Use for running on main thread 
        /// </summary>
        private static void UserInput()
        {
            while (Console.KeyAvailable)
            {
                float step = 0.05f;
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.A or ConsoleKey.LeftArrow:
                        playerA -= step * elapsedTime;
                        break;
                    case ConsoleKey.D or ConsoleKey.RightArrow:
                        playerA += step * elapsedTime;
                        break;
                    case ConsoleKey.W or ConsoleKey.UpArrow:
                        playerX += (float)Math.Sin(playerA) * elapsedTime * step;
                        playerY += (float)Math.Cos(playerA) * elapsedTime * step;
                        if (map[(int)playerX * mapWidth + (int)playerY] == '#')
                        {
                            playerX -= (float)Math.Sin(playerA) * elapsedTime * step;
                            playerY -= (float)Math.Cos(playerA) * elapsedTime * step;
                        }
                        break;
                    case ConsoleKey.S or ConsoleKey.DownArrow:
                        playerX -= (float)Math.Sin(playerA) * elapsedTime * step;
                        playerY -= (float)Math.Cos(playerA) * elapsedTime * step;
                        if (map[(int)playerX * mapWidth + (int)playerY] == '#')
                        {
                            playerX += (float)Math.Sin(playerA) * elapsedTime * step;
                            playerY += (float)Math.Cos(playerA) * elapsedTime * step;
                        }
                        break;
                    case ConsoleKey.E:
                        playerX += (float)Math.Cos(playerA) * elapsedTime * step;
                        playerY -= (float)Math.Sin(playerA) * elapsedTime * step;
                        if (map[(int)playerX * mapWidth + (int)playerY] == '#')
                        {
                            playerX -= (float)Math.Cos(playerA) * elapsedTime * step;
                            playerY += (float)Math.Sin(playerA) * elapsedTime * step;
                        }
                        break;
                    case ConsoleKey.Q:
                        playerX -= (float)Math.Cos(playerA) * elapsedTime * step;
                        playerY += (float)Math.Sin(playerA) * elapsedTime * step;
                        if (map[(int)playerX * mapWidth + (int)playerY] == '#')
                        {
                            playerX += (float)Math.Cos(playerA) * elapsedTime * step;
                            playerY -= (float)Math.Sin(playerA) * elapsedTime * step;
                        }
                        break;
                }
            }

        }
    }
}