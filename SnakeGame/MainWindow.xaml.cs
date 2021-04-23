using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SnakeGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SnakeGameWindow : Window
    {
        const int TITLE_SIZE = 25;
        const int SNAKE_SQUARE_SIZE = 20;
        const int SNAKE_START_LENGTH = 3;
        const int SNAKE_START_SPEED = 400;
        const int SNAKE_SPEED_THRESHOLDS = 100;

        public enum SnakeDirection { Left, Right, Up, Down };

        private UIElement SnakeFood = null;
        private SolidColorBrush FoodBrush = Brushes.Red;
        private SolidColorBrush SnakeBodyBrush = Brushes.DarkGray;
        private SolidColorBrush SnakeHeadBrush = Brushes.Black;
        private List<SnakeTile> SnakeParts = new List<SnakeTile>();
        private Random Rnd = new Random();

        private int SnakeLength;
        private int CurrentScore = 0;

        private SnakeDirection CurrentSnakeDirection = SnakeDirection.Right;
        private DispatcherTimer GameTickTimer = new DispatcherTimer();

        public SnakeGameWindow()
        {
            InitializeComponent();
            GameTickTimer.Tick += GameTickTimer_Tick;
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            StartNewGame();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            this.ChangeSnakeDirection(e.Key);
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Draw snake on board
        /// </summary>
        private void DrawSnake()
        {
            foreach (SnakeTile snakePart in SnakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = TITLE_SIZE,
                        Height = TITLE_SIZE,
                        Fill = (snakePart.IsHead ? SnakeHeadBrush : SnakeBodyBrush)
                    };

                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        /// <summary>
        /// Draw game board
        /// </summary>
        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (doneDrawingBackground == false)
            {
                Rectangle rect = new Rectangle
                {
                    Width = TITLE_SIZE,
                    Height = TITLE_SIZE,
                    Fill = nextIsOdd ? Brushes.Green : Brushes.Green
                };
                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += TITLE_SIZE;
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += TITLE_SIZE;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if (nextY >= GameArea.ActualHeight)
                {
                    doneDrawingBackground = true;
                }
            }
        }
                
        private void MoveSnake()
        {
            // Remove the last part of the snake, in preparation of the new part added below  
            while (SnakeParts.Count >= SnakeLength)
            {
                GameArea.Children.Remove(SnakeParts[0].UiElement);
                SnakeParts.RemoveAt(0);
            }

            // Next up, we'll add a new element to the snake, which will be the (new) head  
            // Therefore, we mark all existing parts as non-head (body) elements and then  
            // we make sure that they use the body brush  
            foreach (SnakeTile snakePart in SnakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = SnakeBodyBrush;
                snakePart.IsHead = false;
            }

            // Determine in which direction to expand the snake, based on the current direction  
            SnakeTile snakeHead = SnakeParts[SnakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (CurrentSnakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= TITLE_SIZE;
                    break;
                case SnakeDirection.Right:
                    nextX += TITLE_SIZE;
                    break;
                case SnakeDirection.Up:
                    nextY -= TITLE_SIZE;
                    break;
                case SnakeDirection.Down:
                    nextY += TITLE_SIZE;
                    break;
            }

            // Now add the new head part to our list of snake parts...  
            SnakeParts.Add(new SnakeTile()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            //Refresh position on board
            DrawSnake();

            //Check collision
            CollisionCheck();
        }

        private void StartNewGame()
        {
            // Remove potential dead snake parts and leftover food...
            foreach (SnakeTile snakeBodyPart in SnakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
            }

            SnakeParts.Clear();

            if (SnakeFood != null)
            {
                GameArea.Children.Remove(SnakeFood);
            }

            // Reset stuff
            CurrentScore = 0;
            SnakeLength = SNAKE_START_LENGTH;
            CurrentSnakeDirection = SnakeDirection.Right;
            SnakeParts.Add(new SnakeTile() { Position = new Point(SNAKE_SQUARE_SIZE * 5, SNAKE_SQUARE_SIZE * 5) });
            GameTickTimer.Interval = TimeSpan.FromMilliseconds(SNAKE_START_SPEED);

            // Draw the snake again and some new food...
            DrawSnake();
            DrawSnakeFood();

            // Update status
            UpdateGameInfoTitleBar();

            // Go!        
            GameTickTimer.IsEnabled = true;
        }

        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SNAKE_SQUARE_SIZE);
            int maxY = (int)(GameArea.ActualHeight / SNAKE_SQUARE_SIZE);
            int foodX = Rnd.Next(0, maxX) * SNAKE_SQUARE_SIZE;
            int foodY = Rnd.Next(0, maxY) * SNAKE_SQUARE_SIZE;

            foreach (SnakeTile snakePart in SnakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    return GetNextFoodPosition();
                }
            }

            return new Point(foodX, foodY);
        }

        /// <summary>
        /// Set a new position for food and draw it on board
        /// </summary>
        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            SnakeFood = new Ellipse()
            {
                Width = SNAKE_SQUARE_SIZE,
                Height = SNAKE_SQUARE_SIZE,
                Fill = FoodBrush
            };

            //Add food to board
            GameArea.Children.Add(SnakeFood);
            Canvas.SetTop(SnakeFood, foodPosition.Y);
            Canvas.SetLeft(SnakeFood, foodPosition.X);
        }

        private void ChangeSnakeDirection(Key keyPressed)
        {
            SnakeDirection originalSnakeDirection = CurrentSnakeDirection;
            switch (keyPressed)
            {
                case Key.Up:
                    if (CurrentSnakeDirection != SnakeDirection.Down)
                    {
                        CurrentSnakeDirection = SnakeDirection.Up;
                    }
                    break;
                case Key.Down:
                    if (CurrentSnakeDirection != SnakeDirection.Up)
                    {
                        CurrentSnakeDirection = SnakeDirection.Down;
                    }
                    break;
                case Key.Left:
                    if (CurrentSnakeDirection != SnakeDirection.Right)
                    {
                        CurrentSnakeDirection = SnakeDirection.Left;
                    }
                    break;
                case Key.Right:
                    if (CurrentSnakeDirection != SnakeDirection.Left)
                    {
                        CurrentSnakeDirection = SnakeDirection.Right;
                    }
                    break;
                case Key.Space:
                    {
                        StartNewGame();
                    }
                    break;
            }

            if (CurrentSnakeDirection != originalSnakeDirection)
            {
                MoveSnake();
            }
        }

        /// <summary>
        /// Perform checks about collisions if food has been eaten, the snake touch walls...
        /// </summary>
        private void CollisionCheck()
        {
            SnakeTile snakeHead = SnakeParts[SnakeParts.Count - 1];

            if ((snakeHead.Position.X == Canvas.GetLeft(SnakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(SnakeFood)))
            {
                SnakeEatenFood();
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
            (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }

            foreach (SnakeTile snakeBodyPart in SnakeParts.Take(SnakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    EndGame();
                }
            }
        }

        private void UpdateGameInfoTitleBar()
        {
            this.Title = "SnakeInWPF - Score: " + CurrentScore + " - Game speed: " + GameTickTimer.Interval.TotalMilliseconds;
        }

        /// <summary>
        /// Snake has eaten a food tile
        /// </summary>
        private void SnakeEatenFood()
        {
            //Update
            SnakeLength++;
            CurrentScore++;

            //Increment speed threshold
            int timerInterval = Math.Max(SNAKE_SPEED_THRESHOLDS, (int)GameTickTimer.Interval.TotalMilliseconds - (CurrentScore * 2));
            GameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(SnakeFood);

            //Draw a new food tile
            DrawSnakeFood();

            //Refresh board
            UpdateGameInfoTitleBar();
        }

        /// <summary>
        /// End of the game
        /// </summary>
        private void EndGame()
        {
            GameTickTimer.IsEnabled = false;
            MessageBox.Show("You died!\n\nTo start a new game, just press the Space bar...", "SnakeWPF");
        }

    }
}
