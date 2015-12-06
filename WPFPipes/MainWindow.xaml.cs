using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFPipes.Engine;

namespace WPFPipes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IPipesWindow
    {
        private readonly GraphicsEngine graphicsEngine;

        public MainWindow()
        {
            InitializeComponent();

            graphicsEngine = new GraphicsEngine(this, (int)ImageControl.Width, (int)ImageControl.Height);
            ImageControl.Source = graphicsEngine.bitmap;
        }

        public void DisplayMessage(string message1, string message2)
        {
            tbMessage1.Text = message1;
            tbMessage2.Text = message2;
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ImageControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(ImageControl);

            graphicsEngine.onCanvasClick((int)position.X, (int)position.Y);
        }
    }
}
