using System.Windows;

namespace SnakeGame
{
    internal class SnakeTile
    {
        public UIElement UiElement { get; set; }

        public Point Position { get; set; }

        public bool IsHead { get; set; }
    }
}
