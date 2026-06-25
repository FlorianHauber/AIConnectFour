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

        // Higher depth = smarter AI, but takes longer to think. 5-6 is perfect for console.
        const int MAX_DEPTH = 5;

        static void Main(string[] args)
        {
            AskForBoardSize();

            Console.WriteLine("AI Engine loaded successfully!");
            Console.Write("Press any key to play against the AI... ");
            Console.ReadKey();

            PlayAgainstHuman();
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
                    Console.WriteLine("AI is calculating optimal strategy...");

                    int aiMove = GetBestMoveLookahead();

                    DropTokenInColumn(aiMove, "O");
                    playerTurn = 1; // Hand control back to human
                }

                win = CheckWin();
                draw = CheckDraw();
            }

            DisplayBoard(board.GetLength(0), board.GetLength(1));
            PrintEndMessage(win, draw);
        }

        // Uses Minimax with Alpha-Beta Pruning to find the best move
        private static int GetBestMoveLookahead()
        {
            int cols = board.GetLength(1);
            int bestMove = 0;
            int bestScore = int.MinValue;

            // Simple heuristic: evaluate middle columns first for massive speed boost in pruning
            List<int> colOrder = new List<int>();
            for (int i = 0; i < cols; i++) colOrder.Add(i);
            colOrder.Sort((a, b) => Math.Abs(cols / 2 - a).CompareTo(Math.Abs(cols / 2 - b)));

            foreach (int c in colOrder)
            {
                if (board[0, c] == " ")
                {
                    DropTokenInColumn(c, "O");
                    int score = Minimax(MAX_DEPTH, int.MinValue, int.MaxValue, false);
                    UndoTokenInColumn(c);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = c;
                    }
                }
            }
            return bestMove;
        }

        static int Minimax(int depth, int alpha, int beta, bool isMaximizing)
        {
            if (CheckWin()) return isMaximizing ? -10000 - depth : 10000 + depth; // Prefer quicker wins / delayed losses
            if (CheckDraw() || depth == 0) return EvaluateBoard();

            int cols = board.GetLength(1);

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                for (int c = 0; c < cols; c++)
                {
                    if (board[0, c] == " ")
                    {
                        DropTokenInColumn(c, "O");
                        int eval = Minimax(depth - 1, alpha, beta, false);
                        UndoTokenInColumn(c);
                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);
                        if (beta <= alpha) break; // Beta Pruning
                    }
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                for (int c = 0; c < cols; c++)
                {
                    if (board[0, c] == " ")
                    {
                        DropTokenInColumn(c, "X");
                        int eval = Minimax(depth - 1, alpha, beta, true);
                        UndoTokenInColumn(c);
                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);
                        if (beta <= alpha) break; // Alpha Pruning
                    }
                }
                return minEval;
            }
        }

        // Evaluates the current board state dynamically (scoring positioning)
        static int EvaluateBoard()
        {
            int score = 0;
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            int midCol = cols / 2;

            // Prioritize center control
            for (int r = 0; r < rows; r++)
            {
                if (board[r, midCol] == "O") score += 3;
                else if (board[r, midCol] == "X") score -= 3;
            }

            // Simple line assessment heuristics
            score += ScoreWindows("O") - ScoreWindows("X");
            return score;
        }

        static int ScoreWindows(string token)
        {
            int score = 0;
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // Row checking for structural advantages (3 in a row open, 2 in a row open)
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols - 3; c++)
                {
                    int count = 0; int empty = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (board[r, c + i] == token) count++;
                        else if (board[r, c + i] == " ") empty++;
                    }
                    if (count == 3 && empty == 1) score += 50;
                    else if (count == 2 && empty == 2) score += 10;
                }
            }

            // Column checking
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows - 3; r++)
                {
                    int count = 0; int empty = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (board[r + i, c] == token) count++;
                        else if (board[r + i, c] == " ") empty++;
                    }
                    if (count == 3 && empty == 1) score += 50;
                    else if (count == 2 && empty == 2) score += 10;
                }
            }

            return score;
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

        private static bool CheckWin()
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (board[i, j] == " ") continue;

                    if (j + 3 < cols && board[i, j] == board[i, j + 1] && board[i, j + 1] == board[i, j + 2] && board[i, j + 2] == board[i, j + 3])
                        return true;
                    if (i + 3 < rows && board[i, j] == board[i + 1, j] && board[i + 1, j] == board[i + 2, j] && board[i + 2, j] == board[i + 3, j])
                        return true;
                    if (i + 3 < rows && j + 3 < cols && board[i, j] == board[i + 1, j + 1] && board[i + 1, j + 1] == board[i + 2, j + 2] && board[i + 2, j + 2] == board[i + 3, j + 3])
                        return true;
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
