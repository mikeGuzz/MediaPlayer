using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer positionTimer = new DispatcherTimer();

        private readonly double[] playbackSpeedValues = new double[] { 0.5, 1, 1.25, 1.5, 1.75, 2 };
        private readonly BitmapSource pauseImg, playImg, volumeHighImg, volumeLowImg, volumeOffImg, replayImg;
        private readonly BitmapSource scaledPauseImg, scaledReplayImg;

        private bool isMediaPlay;
        private bool isEnd;
        private string filePath = string.Empty;
        private string fileName => File.Exists(filePath) ? System.IO.Path.GetFileNameWithoutExtension(filePath) : "Untitled";

        public MainWindow()
        { 
            InitializeComponent();
            UpdateTitle();

            foreach(var ob in playbackSpeedValues)
                speed_comboBox.Items.Add(ob.ToString() + 'x');
            speed_comboBox.SelectedIndex = 1;

            pauseImg = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
            playImg = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
            volumeHighImg = new BitmapImage(new Uri("pack://application:,,,/Resources/volume_high.png"));
            volumeLowImg = new BitmapImage(new Uri("pack://application:,,,/Resources/volume_low.png"));
            volumeOffImg = new BitmapImage(new Uri("pack://application:,,,/Resources/volume_off.png"));
            replayImg = new BitmapImage(new Uri("pack://application:,,,/Resources/replay.png"));

            scaledPauseImg = new TransformedBitmap(pauseImg, new ScaleTransform(1.25, 1.25));
            scaledReplayImg = new TransformedBitmap(replayImg, new ScaleTransform(1.25, 1.25));

            positionTimer.Interval = new TimeSpan(0, 0, 1);
            positionTimer.Tick += UpdatePostionSlider;
        }

        private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message, "Media failed", MessageBoxButton.OK, MessageBoxImage.Error);
            stateImage.Source = new TransformedBitmap(new BitmapImage(new Uri(@"pack://application:,,,/Resources/load_error.png")), new ScaleTransform(1.25, 1.25));
            stateImage.Visibility = Visibility.Visible;
        }

        private void UpdateTitle()
        {
            Title = fileName + $" - Media Player";
        }

        private void UpdatePostionSlider(object? sender, EventArgs e)
        {
            position_slider.Value = mediaElement.Position.TotalSeconds;
        }

        private void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;
            mediaElement.Source = new Uri(filePath);
            this.filePath = filePath;
            UpdateTitle();
            position_slider.Value = mediaElement.Position.TotalSeconds;
            mediaElement.Play();
        }

        private void Play()
        {
            stateImage.Visibility = Visibility.Hidden;
            pauseButtonImage.Source = pauseImg;
            mediaElement.Play();
            positionTimer.Start();
            isMediaPlay = true;
        }

        private void Pause()
        {
            if (!mediaElement.CanPause)
                return;
            stateImage.Source = scaledPauseImg;
            stateImage.Visibility = Visibility.Visible;
            pauseButtonImage.Source = playImg;
            mediaElement.Pause();
            positionTimer.Stop();
            isMediaPlay = false;
        }

        private void Reset()
        {
            isEnd = false;
            mediaElement.Position = TimeSpan.Zero;
            position_slider.Value = 0;
            Play();
        }

        private void position_slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (mediaElement.CanPause)
                mediaElement.Pause();
            positionTimer.Stop();
        }

        private void position_slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            isEnd = false;
            if (isMediaPlay)
                Play();
            if (e.Canceled)
            {
                UpdatePostionSlider(sender, e);
                return;
            }
            mediaElement.Position = TimeSpan.FromSeconds(position_slider.Value);          
        }

        private void moveR_button_Click(object sender, RoutedEventArgs e)
        {
            if (isEnd)
                return;
            if ((mediaElement.Position += TimeSpan.FromSeconds(5)) > mediaElement.NaturalDuration)
                mediaElement.Position = TimeSpan.FromSeconds(mediaElement.NaturalDuration.TimeSpan.TotalSeconds);
            Play();
            UpdatePostionSlider(sender, e);
        }

        private void moveL_button_Click(object sender, RoutedEventArgs e)
        {
            if ((mediaElement.Position -= TimeSpan.FromSeconds(5)) < TimeSpan.Zero)
                mediaElement.Position = TimeSpan.Zero;
            isEnd = false;
            Play();
            UpdatePostionSlider(sender, e);
        }

        private void speed_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mediaElement.SpeedRatio = playbackSpeedValues[speed_comboBox.SelectedIndex];
        }

        private void volume_slider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            mediaElement.Volume = volume_slider.Value;
            volume_image.Source = volume_slider.Value == 0 ? volumeOffImg : (volume_slider.Value < 0.33 ? volumeLowImg : volumeHighImg);
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            pause_button.IsEnabled = true;
            moveL_button.IsEnabled = true;
            moveR_button.IsEnabled = true;
            position_slider.IsEnabled = true;

            position_slider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            UpdatePostionSlider(sender, e);
            Reset();

            if (!File.Exists(filePath) || recent_stackPanel.Children.Cast<RadioButton>().Any(i => ((FileInfo)i.Tag).FullName == filePath))
                return;
            var btn = new RadioButton();
            btn.Content = filePath;
            btn.Click += RecentFileClick;
            btn.Tag = new FileInfo(filePath);
            recent_stackPanel.Children.Insert(0, btn);
            ((RadioButton)recent_stackPanel.Children[0]).IsChecked = true;
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            isEnd = true;
            isMediaPlay = false;
            stateImage.Source = scaledReplayImg;
            stateImage.Visibility = Visibility.Visible;
            pauseButtonImage.Source = replayImg;
            positionTimer.Stop();
            position_slider.Value = position_slider.Maximum;
        }

        private void pause_button_Click(object sender, RoutedEventArgs e)
        {
            if (isEnd)
                Reset();
            else if (isMediaPlay)
                Pause();
            else
                Play();
        }

        private void openFile_button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "MOV Files (*.mov)|*.mov|MP4 Files (*.mp4)|*.mp4|AAC Files (*.aac)|*.aac|MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav|MP2 Files (*.mp2)|*.mp2|All Files (*.*)|*.*";
            if(dialog.ShowDialog() == true)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void RecentFileClick(object sender, EventArgs e)
        {
            if(sender is FrameworkElement ob && ob.Tag is FileInfo file)
                LoadFile(file.FullName);
        }
    }
}
