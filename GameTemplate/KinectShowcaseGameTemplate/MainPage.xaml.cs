using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KinectShowcaseGameTemplate
{
    public partial class MainPage : Page
    {
        #region Setup Code

        // Handles calling the main game loop X times per second
        private DispatcherTimer _gameTimer;
        // The time in between studentWork() calls
        private const int GAME_LOOP_INTERVAL_MILLISECONDS = 33; //translates to about 30 fps
        private int popupFrames = -1;

        public MainPage()
        {
            InitializeComponent();

            //make the background full blur
            ((App.Current as App).MainWindow as MainWindow).SkeletonView.SetPercents(0.0f, 1.0f);
            ((App.Current as App).MainWindow as MainWindow).SkeletonView.SetMode(KinectShowcaseCommon.UI_Elements.LiveBackground.BackgroundMode.Infrared);

            this.StudentInit();
            this.InitTimer();
        }

        private void InitTimer()
        {
            _gameTimer = new System.Windows.Threading.DispatcherTimer();
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Interval = new TimeSpan(0, 0, 0, 0, GAME_LOOP_INTERVAL_MILLISECONDS);
            _gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            MainLoop();

            if (popupFrames > 0)
            {
                popupFrames--;
            }
            else
            {
                popupLabel.Visibility = Visibility.Hidden;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            _gameTimer.Stop();
            SystemCanary.Default.AskForKill();
        }

        private void ShowPopup(string text, int frames)
        {
            popupLabel.Content = text;
            popupFrames = frames;
            popupLabel.Visibility = Visibility.Visible;
        }

        private void HidePopup()
        {
            popupLabel.Visibility = Visibility.Hidden;
        }

        #endregion

        // Constants for the values that go in the game grid
        private const int EMPTY_VALUE = 0;
        private const int PLAYER_VALUE = 1;
        private const int BOT_VALUE = 2;

        // Number of rows in the game grid
        private const int GAME_GRID_ROWS_COUNT = 3;
        // Number of columns in the game grid
        private const int GAME_GRID_COLUMNS_COUNT = 3;

        // Delays the game by a certain number of frames
        private int delayFrames = -1;
        // Holds if the game has finished
        private bool isGameOver = false;
        // Holds if the game is currently waiting for the BOT to move
        private bool waitingOnBot = false;

        /**
         * This is the initialization function. This function will be run when the game starts. Put setup code here.
         */
        private void StudentInit()
        {
            // TODO - configure to your heart's content

            // TIP - If you want to write something to the console (the text output on your computer), you can do it like this:
            Debug.WriteLine("StudentInit - Initializing student code!");

            // TODO - Set the title of the game
            titleText.Text = "Tic Tac Toe";
            titleText.Foreground = Brushes.White;

            // TODO - Set the instruction text
            // Newline => \r\n (Windows Format)
            instructionText.Text = "How to play: \r\n " +
                                   "1. Connect 3 in a row \r\n" +
                                   "2. Beat the Bot \r\n" +
                                   "3. Game repeats! \r\n ";
            instructionText.Foreground = Brushes.White;
            instructionText.FontSize = 48;

            // TODO - Set the authors
            byText.Text = "Doug Roeper";

            // Configure the popup label
            popupLabel.Foreground = Brushes.White;
            popupLabel.Background = Brushes.Black;
            popupLabel.FontSize = 100;
            popupLabel.BorderBrush = Brushes.White;
            popupLabel.BorderThickness = new Thickness(3);

            // Configure the game grid
            gameGrid.SetSize(GAME_GRID_ROWS_COUNT, GAME_GRID_COLUMNS_COUNT);
            // Set if the hand should attach to center of the buttons
            gameGrid.ButtonsShouldAttach = false;
            // Configure how the grid looks
            gameGrid.BorderColor = Brushes.Gray;
            gameGrid.HoverColor = Brushes.Blue;
            // Set the colors for each state
            gameGrid.stateFillColors[EMPTY_VALUE] = Brushes.White;
            gameGrid.stateFillColors[PLAYER_VALUE] = Brushes.Green;
            gameGrid.stateFillColors[BOT_VALUE] = Brushes.Red;
            // Don't modify this line
            gameGrid.Handler += GridWasClicked;
        }

        private void GridWasClicked(int row, int col)
        {
            // Check if the game is over
            if (isGameOver == true)
            {
                ResetGame();
            }
            else
            {
                // Check if it is the player's turn
                if (waitingOnBot == false)
                {
                    // Check if the (row, col) position is already filled
                    if (gameGrid.GetGrid(row, col) == EMPTY_VALUE)
                    {
                        // If the spot is empty, fill it with the player's value
                        gameGrid.SetGrid(row, col, PLAYER_VALUE);

                        // Check if we have a winnner
                        if (CheckWinner() == PLAYER_VALUE)
                        {
                            // Show a win message
                            ShowPopup("You win!", 90);
                            // Game is over
                            isGameOver = true;
                        }
                        else
                        {
                            // If game is not over, delay 15 frames. This will make the bot play after 15 frames, or 0.5 secs
                            delayFrames = 15;
                            // Waiting on bot, so player can't move
                            waitingOnBot = true;
                        }
                    }
                }
            }
        }

        /**
         * This is the main game loop. It is run many times per second so you can update things in the game.
         */
        private void MainLoop()
        {
            // Decrement delay frames
            delayFrames--;
            // See if delay frames <= 0,  meaning we are done waiting
            if (delayFrames <= 0)
            {
                // Check if game is not over and it's the bots turn
                if (isGameOver == false && waitingOnBot == true)
                {
                    // Make the bot play a move. You can edit the BotMove function to make it more smarterer
                    BotMove();
                    // Bot has moved, it's the players' turn
                    waitingOnBot = false;

                    // See if the bot won
                    if (CheckWinner() == BOT_VALUE)
                    {
                        // Show a message to the player
                        ShowPopup("You lost!", 90);
                        isGameOver = true;
                    }
                }
            }
        }

        /**
         * Checks if someone has one the game. Returns 0 if no one has won.
         */
        private int CheckWinner()
        {
            // TODO - Change to check for a winner in your game

            // Check diagonals
            if (gameGrid.GetGrid(0, 0) != 0 && gameGrid.GetGrid(0, 0) == gameGrid.GetGrid(1, 1) && gameGrid.GetGrid(1, 1) == gameGrid.GetGrid(2, 2))
            {
                return gameGrid.GetGrid(0, 0);
            }

            if (gameGrid.GetGrid(2, 0) != 0 && gameGrid.GetGrid(2, 0) == gameGrid.GetGrid(1, 1) && gameGrid.GetGrid(1, 1) == gameGrid.GetGrid(0, 2))
            {
                return gameGrid.GetGrid(2, 0);
            }

            // Check rows
            if (gameGrid.GetGrid(0, 0) != 0 && gameGrid.GetGrid(0, 0) == gameGrid.GetGrid(0, 1) && gameGrid.GetGrid(0, 1) == gameGrid.GetGrid(0, 2))
            {
                return gameGrid.GetGrid(0, 0);
            }

            if (gameGrid.GetGrid(1, 0) != 0 && gameGrid.GetGrid(1, 0) == gameGrid.GetGrid(1, 1) && gameGrid.GetGrid(1, 1) == gameGrid.GetGrid(1, 2))
            {
                return gameGrid.GetGrid(1, 0);
            }

            if (gameGrid.GetGrid(2, 0) != 0 && gameGrid.GetGrid(2, 0) == gameGrid.GetGrid(2, 1) && gameGrid.GetGrid(2, 1) == gameGrid.GetGrid(2, 2))
            {
                return gameGrid.GetGrid(2, 0);
            }

            // Check columns
            // Check rows
            if (gameGrid.GetGrid(0, 0) != 0 && gameGrid.GetGrid(0, 0) == gameGrid.GetGrid(1, 0) && gameGrid.GetGrid(1, 0) == gameGrid.GetGrid(2, 0))
            {
                return gameGrid.GetGrid(0, 0);
            }

            if (gameGrid.GetGrid(0, 1) != 0 && gameGrid.GetGrid(0, 1) == gameGrid.GetGrid(1, 1) && gameGrid.GetGrid(1, 1) == gameGrid.GetGrid(2, 1))
            {
                return gameGrid.GetGrid(0, 1);
            }

            if (gameGrid.GetGrid(0, 2) != 0 && gameGrid.GetGrid(0, 2) == gameGrid.GetGrid(1, 2) && gameGrid.GetGrid(1, 2) == gameGrid.GetGrid(2, 2))
            {
                return gameGrid.GetGrid(0, 2);
            }

            return 0;
        }

        /**
         * Call this function to make the computer play a move.
         */
        private void BotMove()
        {
            // TODO - Change this function so the computer can play against you

            // Enumerate through the grid, check for open spots
            HashSet<Tuple<int, int>> possibleMoves = new HashSet<Tuple<int, int>>();
            for (int i = 0; i < GAME_GRID_ROWS_COUNT; i++)
            {
                for (int j = 0; j < GAME_GRID_COLUMNS_COUNT; j++)
                {
                    if (gameGrid.GetGrid(i, j) == EMPTY_VALUE)
                    {
                        possibleMoves.Add(Tuple.Create(i, j));
                    }
                }
            }

            // Check if there are possible moves
            if (possibleMoves.Count == 0)
            {
                // No possible moves, game is a tie
                isGameOver = true;
                ShowPopup("Tie Game!", 90);
            }
            else
            {
                // Randomly choose a move
                int moveIndex = (new Random()).Next(possibleMoves.Count);
                Tuple<int, int> move = possibleMoves.ElementAt(moveIndex);
                gameGrid.SetGrid(move.Item1, move.Item2, BOT_VALUE);
            }
        }

        /**
         * This function is called when the reset button is clicked. You can also call this function
         * from within your code to reset the game.
         */
        private void ResetGame()
        {
            // TODO - Reset all game logic here
            waitingOnBot = false;
            isGameOver = false;
            gameGrid.ZeroGridArray();
            HidePopup();
        }
    }
}
