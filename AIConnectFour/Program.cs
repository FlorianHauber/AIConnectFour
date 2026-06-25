using System;
using System.Threading; //used for the thread.sleep function to make the program wait for a certain amount of time before executing the next line of code

/***************
 * Florian Hauber
 * 1IHIF
 * 29.4.2026
 * The program allows two players to play a game of Connect Four. The players take turns placing their pieces on the board and the first player to get four in a row wins. The program also checks for a draw if the board is full and no one has won.
 * ****************/

namespace ConnectFour
{
    internal class Program
    {
        static string[,] board = new string[11, 11];
        public const int MIN_SIZE = 4;
        public const int MAX_SIZE = 10;
        static int playerTurn = 1;
        static Dictionary<string, double[]> qTable = new Dictionary<string, double[]>(); // Q-Table to store the expected rewards for each state-action pair
        static List<(string state, int move)> gameHistory = new List<(string, int)>(); // This will store the states and moves of the current game for later updating the Q-Table

        static void Main(string[] args)
        {
            AskForBoardSize(); //asks the user for the size of the board and initializes the board with the specified size

            Console.WriteLine("AI is training a few games ... please wait");

            for (int i = 0; i < 100000; i++)
            {
                TrainAI();
            }

            Console.Write("Training complete! Press any key to play against the AI ... ");
            Console.ReadKey();

            PlayAgainstHuman();
        }

        static void TrainAI()
        {
            bool gameOver = false;
            int currentTrainingPlayer = 1;

            // Clear history from the previous game
            gameHistory.Clear();

            while (!gameOver)
            {
                string stateKey = GetStateKey();
                int availableCols = board.GetLength(1);

                int moveCol = 0;

                // Epsilon-Greedy: 10% chance to explore randomly, 90% chance to exploit best known move
                Random rand = new Random();
                if (rand.NextDouble() < 0.10)
                {
                    moveCol = rand.Next(0, availableCols);
                }
                else
                {
                    moveCol = GetBestMove(stateKey);
                }

                // Validate if column is full; if full, pick a random valid column
                if (board[0, moveCol] != " ")
                {
                    List<int> openCols = new List<int>();
                    for (int c = 0; c < availableCols; c++)
                        if (board[0, c] == " ") openCols.Add(c);

                    if (openCols.Count == 0) break; // Board is completely full
                    moveCol = openCols[rand.Next(0, openCols.Count)];
                }

                // Save state and action to history before executing the move
                gameHistory.Add((stateKey, moveCol));

                // Drop the piece
                string token = (currentTrainingPlayer == 1) ? "X" : "O";
                DropTokenInColumn(moveCol, token);

                // Check conditions
                if (CheckWin(GetStateKey()))
                {
                    // Backpropagate rewards through game history
                    // Winner gets 1.0, Loser gets 0.0
                    for (int i = gameHistory.Count - 1; i >= 0; i--)
                    {
                        var step = gameHistory[i];
                        bool isWinnerStep = (i % 2 == 0 && currentTrainingPlayer == 1) || (i % 2 != 0 && currentTrainingPlayer == 2);
                        qTable[step.state][step.move] = isWinnerStep ? 1.0 : 0.0;
                    }
                    gameOver = true;
                }
                else if (CheckDraw(GetStateKey()))
                {
                    // Both get 0.5 for a draw
                    foreach (var step in gameHistory)
                    {
                        qTable[step.state][step.move] = 0.5;
                    }
                    gameOver = true;
                }

                // Switch turns
                currentTrainingPlayer = (currentTrainingPlayer == 1) ? 2 : 1;
            }

            ResetBoard();
        }

        // Helper method specifically for the AI to drop pieces into a column index (0-indexed)
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

        private static bool CheckDraw(string stateKey)
        {
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    if (board[i, j] == " ")
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool CheckWin(string stateKey)
        {
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    if (board[i, j] == " ")
                    {
                        continue;
                    }
                    if (i + 3 < board.GetLength(0) && board[i, j] == board[i + 1, j] && board[i + 1, j] == board[i + 2, j] && board[i + 2, j] == board[i + 3, j])
                    {
                        return true;
                    }
                    if (j + 3 < board.GetLength(1) && board[i, j] == board[i, j + 1] && board[i, j + 1] == board[i, j + 2] && board[i, j + 2] == board[i, j + 3])
                    {
                        return true;
                    }
                    if (i + 3 < board.GetLength(0) && j + 3 < board.GetLength(1) && board[i, j] == board[i + 1, j + 1] && board[i + 1, j + 1] == board[i + 2, j + 2] && board[i + 2, j + 2] == board[i + 3, j + 3])
                    {
                        return true;
                    }
                    if (i + 3 < board.GetLength(0) && j - 3 >= 0 && board[i, j] == board[i + 1, j - 1] && board[i + 1, j - 1] == board[i + 2, j - 2] && board[i + 2, j - 2] == board[i + 3, j - 3])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static int GetBestMove(string stateKey)
        {
            int bestMove = 0;
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
            string stateKey = "";

            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    stateKey = stateKey + board[i, j].ToString();
                }
            }

            if (qTable.ContainsKey(stateKey) == false)
            {
                qTable[stateKey] = new double[board.GetLength(1)]; // Matches chosen board width
            }

            return stateKey;
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

            DisplayLogo(); //displays the logo and waits for the user to press a key before starting the game

            while (!win && !draw)
            {
                DisplayBoard(board.GetLength(0), board.GetLength(1)); //displays the current state of the board with the specified number of rows and columns and the pieces that have been placed on the board
                GetInputAndPlacePiece(); //asks the current player for the column they want to place their piece in, validates the input, and places the piece on the board
                win = CheckForWin(); //checks if the current player has won the game by checking for four in a row horizontally, vertically, diagonally, or anti-diagonally
                draw = CheckForDraw(); //checks if the board is full and no one has won, which would result in a draw
            }

            DisplayBoard(board.GetLength(0), board.GetLength(1)); //displays the final state of the board after the game has ended
            PrintEndMessage(win, draw); //prints a message indicating which player won or if the game ended in a draw, and waits for the user to press a key before exiting the program
        }
        static void DisplayLogo()
        {
            char dummy = '\0';

            Console.Clear();
            Console.WriteLine("========================");
            Console.WriteLine("Welcome to Connect Four!");
            Console.WriteLine("========================");
            Console.Write("\nPress any key to start the game... ");
            dummy = Console.ReadKey().KeyChar;
            Console.Clear();
        }

        static void AskForBoardSize()
        {
            int rows = 0;
            int cols = 0;

            Console.Write("Please enter the number of rows for the board (between 4 and 10): ");
            rows = GetValidInput(); //validates the input for the number of rows and ensures that it is an integer between 4 and 10

            PrintSuccess(); //prints a success message in green text to indicate that the input was valid

            Console.Write("Please enter the number of columns for the board (between 4 and 10): ");
            cols = GetValidInput();

            PrintSuccess();

            Console.Write("The game will start in 3 seconds...");
            Thread.Sleep(3000);

            InitializeBoard(rows, cols);
        }

        static void PrintSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("SUCCESS! ");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine();
        }

        static int GetValidInput()
        {
            int input = 0;
            bool stop = false;

            while (!stop)
            {
                if (int.TryParse(Console.ReadLine(), out input) && input >= MIN_SIZE && input <= MAX_SIZE) //checks if the input is a valid integer and within the specified range
                {
                    stop = true;
                }

                else
                {
                    PrintErrorMessage(); //prints an error message in red text to indicate that the input was invalid and prompts the user to enter a valid input
                }
            }

            return input;
        }

        static void PrintErrorMessage()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"Invalid input. Please enter a number between {MIN_SIZE} and {MAX_SIZE}: ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void InitializeBoard(int rows, int cols)
        {
            board = new string[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    board[i, j] = " "; //initializes each cell of the board with a space character to indicate that it is empty and so we can fill in the "x" and "o" pieces later on
                }
            }
        }

        static void DisplayBoard(int rows, int cols)
        {
            Console.Clear();

            for (int i = 0; i < cols; i++)
            {
                Console.Write("  " + (i + 1) + " "); //prints the column numbers at the top of the board to help the players identify which column they want to place their piece in
            }

            Console.WriteLine();

            for (int i = 0; i < rows; i++)
            {
                PrintHorizontalLines(cols); //prints the horizontal lines that separate the rows of the board

                Console.Write("\n| ");

                for (int j = 0; j < cols; j++)
                {
                    if (board[i, j] == "X")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    //prints current state of the board
                    Console.Write($"{board[i, j]}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" | ");
                }
                Console.WriteLine();
            }

            PrintHorizontalLines(cols);

            Console.WriteLine();
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
            int input = 0;

            input = GetInputAndValidate(); //asks the current player for the column they want to place their piece in and validates the input to ensure that it is a valid column number and that the column is not full

            PlaceStoneOnBoard(input); //places the current player's piece on the board in the specified column
        }

        static int GetInputAndValidate()
        {
            int input = 0;
            bool validPlace = false;

            while (!validPlace)
            {
                Console.Write($"Player {playerTurn} where do you want to enter your piece (e.g. {board.GetLength(1) / 2}): ");
                input = GetInputForStone();

                if (board[0, input - 1] == "X" || board[0, input - 1] == "O") //checks if the top cell of the specified column is already occupied by a piece
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("That column is full. Please choose another column.");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                else
                {
                    validPlace = true;
                }
            }

            return input;
        }

        static int GetInputForStone()
        {
            int input = 0;
            bool isValid = false;

            while (!isValid)
            {
                if (int.TryParse(Console.ReadLine(), out input) && input > 0 && input <= board.GetLength(1))
                {
                    isValid = true;
                }

                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"Invalid input. Please enter a number between 1 and {board.GetLength(1)}: ");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            return input;
        }

        static void PlaceStoneOnBoard(int input)
        {
            int row = 0;
            bool end = false;

            while (board[row, input - 1] == " " && row != board.GetLength(0) - 1 && !end)
            {
                row++;

                if (board[row, input - 1] == "X" || board[row, input - 1] == "O")
                {
                    end = true;
                    row--;
                }
            }

            if (playerTurn == 1)
            {
                board[row, input - 1] = "X";
                playerTurn = 2;
            }

            else
            {
                board[row, input - 1] = "O";
                playerTurn = 1;
            }
        }

        static bool CheckForWin()
        {
            bool win = false;

            win = CheckForHorizontalWin() || CheckForVerticalWin() || CheckForDiagonalWin() || CheckForAntiDiagonalWin(); //checks for a win by checking for four in a row horizontally, vertically, diagonally, or anti-diagonally

            return win;
        }

        static bool CheckForHorizontalWin()
        {
            bool win = false;

            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1) - 3; j++)
                {
                    if (board[i, j] != " " && board[i, j] == board[i, j + 1] && board[i, j] == board[i, j + 2] && board[i, j] == board[i, j + 3])
                    {
                        win = true;
                    }
                }
            }

            return win;
        }

        static bool CheckForVerticalWin()
        {
            bool win = false;

            for (int i = 0; i < board.GetLength(0) - 3; i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    if (board[i, j] != " " && board[i, j] == board[i + 1, j] && board[i, j] == board[i + 2, j] && board[i, j] == board[i + 3, j])
                    {
                        win = true;
                    }
                }
            }

            return win;
        }

        static bool CheckForDiagonalWin()
        {
            bool win = false;
            for (int i = 0; i < board.GetLength(0) - 3; i++)
            {
                for (int j = 0; j < board.GetLength(1) - 3; j++)
                {
                    if (board[i, j] != " " && board[i, j] == board[i + 1, j + 1] && board[i, j] == board[i + 2, j + 2] && board[i, j] == board[i + 3, j + 3])
                    {
                        win = true;
                    }
                }
            }
            return win;
        }

        static bool CheckForAntiDiagonalWin()
        {
            bool win = false;
            for (int i = 0; i < board.GetLength(0) - 3; i++)
            {
                for (int j = 3; j < board.GetLength(1); j++)
                {
                    if (board[i, j] != " " && board[i, j] == board[i + 1, j - 1] && board[i, j] == board[i + 2, j - 2] && board[i, j] == board[i + 3, j - 3])
                    {
                        win = true;
                    }
                }
            }
            return win;
        }

        static bool CheckForDraw()
        {
            bool draw = true;

            for (int i = 0; i < board.GetLength(0) && draw; i++)
            {
                for (int j = 0; j < board.GetLength(1) && draw; j++)
                {
                    if (board[i, j] == " ")
                    {
                        draw = false;
                    }
                }
            }

            return draw;
        }

        static void PrintEndMessage(bool win, bool draw)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Clear();

            if (playerTurn == 1 && win) //says player 2 wins because the player turn is switched after placing the piece
            {
                Console.Write($"Player 2 wins! Press any key to exit... ");
            }

            else if (playerTurn == 2 && win)
            {
                Console.Write($"Player 1 wins! Press any key to exit... ");
            }

            if (draw)
            {
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write("The board is full and no one won! Press any key to exit... ");
            }

            Console.ForegroundColor = ConsoleColor.White; //resets the console text color to white before exiting the program

            Console.ReadKey();
        }
    }
}
