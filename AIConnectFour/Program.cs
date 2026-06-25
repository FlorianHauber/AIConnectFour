using System;
using System.Collections.Generic;
using System.Threading;

namespace ConnectFour
{
    internal class Program
    {
        static string[,] board = new string[11, 11];
        public const int MIN_SIZE = 4;
        public const int MAX_SIZE = 10;
        static int playerTurn = 1;

        // Q-Table to store expected rewards for each state-action pair
        static Dictionary<string, double[]> qTable = new Dictionary<string, double[]>();
        // Stores game steps to backpropagate rewards after a match
        static List<(string state, int move)> gameHistory = new List<(string, int)>();

        static void Main(string[] args)
        {
            AskForBoardSize();

            // 500,000 games is the sweet spot for string-keys before running out of RAM
            int trainingGames = 500000;
            Console.WriteLine($"AI is training over {trainingGames:N0} games... please wait...");

            for (int i = 0; i < trainingGames; i++)
            {
                TrainAI();
            }

            Console.Write("\nTraining complete! Press any key to play against the AI... ");
            Console.ReadKey();

            PlayAgainstHuman();
        }

        static void TrainAI()
        {
            bool gameOver = false;
            int currentTrainingPlayer = 1;
            gameHistory.Clear();

            Random rand = new Random();

            while (!gameOver)
            {
                string stateKey = GetStateKey();
                int availableCols = board.GetLength(1);
                int moveCol = 0;

                // Epsilon-Greedy: 10% chance to explore randomly, 90% to use best move
                if (rand.NextDouble() < 0.10)
                {
                    moveCol = rand.Next(0, availableCols);
                }
                else
                {
                    moveCol = GetBestMove(stateKey);
                }

                // If chosen column is full, fallback to a random valid column
                if (board[0, moveCol] != " ")
                {
                    List<int> openCols = new List<int>();
                    for (int c = 0; c < availableCols; c++)
                    {
                        if (board[0, c] == " ") openCols.Add(c);
                    }

                    if (openCols.Count == 0) break; // Tie / Board completely full
                    moveCol = openCols[rand.Next(0, openCols.Count)];
                }

                gameHistory.Add((stateKey, moveCol));

                string token = (currentTrainingPlayer == 1) ? "X" : "O";
                DropTokenInColumn(moveCol, token);

                string nextStateKey = GetStateKey();

                if (CheckWin())
                {
                    // Winner gets 1.0, Loser gets 0.0
                    for (int i = gameHistory.Count - 1; i >= 0; i--)
                    {
                        var step = gameHistory[i];
                        bool isWinnerStep = (i % 2 == 0 && currentTrainingPlayer == 1) || (i % 2 != 0 && currentTrainingPlayer == 2);
                        qTable[step.state][step.move] = isWinnerStep ? 1.0 : 0.0;
                    }
                    gameOver = true;
                }
                else if (CheckDraw())
                {
                    // Split points on a draw
                    foreach (var step in gameHistory)
                    {
                        qTable[step.state][step.move] = 0.5;
                    }
                    gameOver = true;
                }

                currentTrainingPlayer = (currentTrainingPlayer == 1) ? 2 : 1;
            }

            ResetBoard();
        }

        static void DropTokenInColumn(int col, string token)
        {
            for (int r = board.GetLength(0) - 1; r >= 0; r--)
            {
                if (board[r, col] == " ")
                {
                    board[r, col] = token;
                    break;
                }
            }
        }

        static void UndoTokenInColumn(int col)
        {
            for (int r = 0; r < board.GetLength(0); r++)
            {
                if (board[r, col] != " ")
                {
                    board[r, col] = " ";
                    break;
                }
            }
        }

        private static int GetBestMove(string stateKey)
        {
            int cols = board.GetLength(1);

            // --- 1. LOOKAHEAD BRAIN (Look for immediate wins/blocks) ---
            // Can AI (Player 2, "O") win on this exact turn?
            for (int c = 0; c < cols; c++)
            {
                if (board[0, c] == " ")
                {
                    DropTokenInColumn(c, "O");
                    bool isWin = CheckWin();
                    UndoTokenInColumn(c);
                    if (isWin) return c;
                }
            }

            // Can Human (Player 1, "X") win on their next turn? Block them!
            for (int c = 0; c < cols; c++)
            {
                if (board[0, c] == " ")
                {
                    DropTokenInColumn(c, "X");
                    bool isWin = CheckWin();
                    UndoTokenInColumn(c);
                    if (isWin) return c;
                }
            }

            // --- 2. Q-TABLE BRAIN (Fall back to trained database) ---
            double bestScore = double.MinValue;
            List<int> bestMoves = new List<int>();

            for (int i = 0; i < qTable[stateKey].Length; i++)
            {
                if (qTable[stateKey][i] > bestScore)
                {
                    bestScore = qTable[stateKey][i];
                    bestMoves.Clear();
                    bestMoves.Add(i);
                }
                else if (qTable[stateKey][i] == bestScore)
                {
                    bestMoves.Add(i);
                }
            }

            Random r = new Random();
            return bestMoves[r.Next(0, bestMoves.Count)];
        }

        static string GetStateKey()
        {
            // Using a StringBuilder here dramatically speeds up execution time
            var sb = new System.Text.StringBuilder();
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(board[i, j]);
                }
            }

            string stateKey = sb.ToString();

            if (!qTable.ContainsKey(stateKey))
            {
                qTable[stateKey] = new double[cols];
            }

            return stateKey;
        }

        private static bool CheckWin()
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (board[i, j] == " ") continue;

                    // Horizontal
                    if (j + 3 < cols && board[i, j] == board[i, j + 1] && board[i, j + 1] == board[i, j + 2] && board[i, j + 2] == board[i, j + 3])
                        return true;
                    // Vertical
                    if (i + 3 < rows && board[i, j] == board[i + 1, j] && board[i + 1, j] == board[i + 2, j] && board[i + 2, j] == board[i + 3, j])
                        return true;
                    // Diagonal Down-Right
                    if (i + 3 < rows && j + 3 < cols && board[i, j] == board[i + 1, j + 1] && board[i + 1, j + 1] == board[i + 2, j + 2] && board[i + 2, j + 2] == board[i + 3, j + 3])
                        return true;
                    // Diagonal Up-Right
                    if (i - 3 >= 0 && j + 3 < cols && board[i, j] == board[i - 1, j + 1] && board[i - 1, j + 1] == board[i - 2, j + 2] && board[i - 2, j + 2] == board[i - 3, j + 3])
                        return true;
                }
            }
            return false;
        }

        private static bool CheckDraw()
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[0, j] == " ") return false;
            }
            return true;
        }

        static void ResetBoard()
        {
            playerTurn = 1;
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    board[i, j] = " ";
                }
            }
        }

        static void PlayAgainstHuman()
        {
            bool win = false;
            bool draw = false;

            DisplayLogo();

            while (!win && !draw)
            {
                DisplayBoard(board.GetLength(0), board.GetLength(1));

                if (playerTurn == 1)
                {
                    // Human Turn
                    GetInputAndPlacePiece();
                }
                else
                {
                    // AI Turn (Player 2)
                    Console.WriteLine("AI is calculating its move...");
                    Thread.Sleep(800);

                    string stateKey = GetStateKey();
                    int aiMove = GetBestMove(stateKey);

                    // Extra emergency fallback if column happens to be filled
                    if (board[0, aiMove] != " ")
                    {
                        for (int c = 0; c < board.GetLength(1); c++)
                        {
                            if (board[0, c] == " ") { aiMove = c; break; }
                        }
                    }

                    DropTokenInColumn(aiMove, "O");
                    playerTurn = 1; // Hand control back to human
                }

                win = CheckWin();
                draw = CheckDraw();
            }

            DisplayBoard(board.GetLength(0), board.GetLength(1));
            PrintEndMessage(win, draw);
        }

        static void DisplayLogo()
        {
            Console.Clear();
            Console.WriteLine("========================");
            Console.WriteLine("Welcome to Connect Four!");
            Console.WriteLine("========================");
            Console.Write("\nPress any key to start the match... ");
            Console.ReadKey();
            Console.Clear();
        }

        static void AskForBoardSize()
        {
            Console.Clear();
            Console.Write("Enter number of rows (4-10): ");
            int rows = GetValidInput();
            PrintSuccess();

            Console.Write("Enter number of columns (4-10): ");
            int cols = GetValidInput();
            PrintSuccess();

            Console.Write("Game system preparing...");
            Thread.Sleep(1500);
            InitializeBoard(rows, cols);
        }

        static void PrintSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("SUCCESS!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static int GetValidInput()
        {
            int input = 0;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out input) && input >= MIN_SIZE && input <= MAX_SIZE)
                {
                    return input;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"Invalid setup. Enter a value between {MIN_SIZE} and {MAX_SIZE}: ");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static void InitializeBoard(int rows, int cols)
        {
            board = new string[rows, cols];
            ResetBoard();
        }

        static void DisplayBoard(int rows, int cols)
        {
            Console.Clear();
            for (int i = 0; i < cols; i++)
            {
                Console.Write("  " + (i + 1) + " ");
            }
            Console.WriteLine();

            for (int i = 0; i < rows; i++)
            {
                PrintHorizontalLines(cols);
                Console.Write("\n| ");

                for (int j = 0; j < cols; j++)
                {
                    if (board[i, j] == "X") Console.ForegroundColor = ConsoleColor.Red;
                    else if (board[i, j] == "O") Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.Write($"{board[i, j]}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" | ");
                }
                Console.WriteLine();
            }
            PrintHorizontalLines(cols);
            Console.WriteLine("\n");
        }

        static void PrintHorizontalLines(int cols)
        {
            for (int i = 0; i < cols; i++)
            {
                Console.Write(" ---");
            }
        }

        static void GetInputAndPlacePiece()
        {
            int cols = board.GetLength(1);
            int chosenColumn = 0;

            while (true)
            {
                Console.Write($"Player 1 (Human), pick a column (1-{cols}): ");
                if (int.TryParse(Console.ReadLine(), out chosenColumn) && chosenColumn > 0 && chosenColumn <= cols)
                {
                    if (board[0, chosenColumn - 1] == " ")
                    {
                        DropTokenInColumn(chosenColumn - 1, "X");
                        playerTurn = 2; // Switch to AI
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Column full. Pick a different one.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid input. Choose between 1 and {cols}.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        static void PrintEndMessage(bool win, bool draw)
        {
            Console.Clear();
            if (win)
            {
                // Note: Turn switches right after slotting a token.
                if (playerTurn == 2)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Congratulations! You beat the AI!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The AI wins! Better luck next time.");
                }
            }
            else if (draw)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("It's a draw! The board is full.");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
