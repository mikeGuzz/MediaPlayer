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
        private readonly BitmapSource scaledVolumeHighImg, scaledPauseImg, scaledReplayImg;
        private BitmapSource? startBitmap;

        private bool isEnd
        {
            get
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                    return mediaElement.Position >= mediaElement.NaturalDuration.TimeSpan;
                return false;
            }
        }
        private bool? isPlay;
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

            scaledVolumeHighImg = new TransformedBitmap(volumeHighImg, new ScaleTransform(1.25, 1.25));
            scaledPauseImg = new TransformedBitmap(pauseImg, new ScaleTransform(1.25, 1.25));
            scaledReplayImg = new TransformedBitmap(replayImg, new ScaleTransform(1.25, 1.25));

            positionTimer.Interval = TimeSpan.FromSeconds(1);
            positionTimer.Tick += UpdatePositionSlider;
        }

        private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message, "Media failed", MessageBoxButton.OK, MessageBoxImage.Error);
            stateImage.Source = new TransformedBitmap(new BitmapImage(new Uri(@"pack://application:,,,/Resources/load_error.png")), new ScaleTransform(1.25, 1.25));
        }

        private void UpdateTitle()
        {
            Title = fileName + $" - Media Player";
        }

        private void UpdatePositionSlider()
        {
            position_slider.Value = mediaElement.Position.TotalSeconds;
        }

        private void UpdatePositionSlider(object? sender, EventArgs e)
        {
            UpdatePositionSlider();
        }

        private void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;
            mediaElement.Source = new Uri(filePath);
            this.filePath = filePath;
            position_slider.Value = mediaElement.Position.TotalSeconds;
            UpdateTitle();
            mediaElement.Stop();
            Play();
        }

        private void Play()
        {
            isPlay = true;
            stateImage.Source = startBitmap;
            pauseButtonImage.Source = pauseImg;
            UpdatePositionSlider();
            mediaElement.Play();
            positionTimer.Start();
        }

        private void Pause()
        {
            if (!mediaElement.CanPause)
                return;
            isPlay = false;
            stateImage.Source = scaledPauseImg;
            pauseButtonImage.Source = playImg;
            mediaElement.Pause();
            positionTimer.Stop();
        }

        private void position_slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (mediaElement.CanPause)
                mediaElement.Pause();
            positionTimer.Stop();
        }

        private void position_slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (!e.Canceled)
            {
                mediaElement.Position = TimeSpan.FromSeconds(position_slider.Value);
                if (isEnd)
                {
                    mediaElement.Play();
                    return;
                }
            }
            if (isPlay is null || (bool)isPlay || e.Canceled)
                Play();
        }

        private void moveR_button_Click(object sender, RoutedEventArgs e)
        {
            if (isPlay is null)
                return;
            mediaElement.Position += TimeSpan.FromSeconds(5);
            if (isEnd)
                mediaElement.Play();
            else if ((bool)isPlay)
                Play();
            else 
                UpdatePositionSlider();
        }

        private void moveL_button_Click(object sender, RoutedEventArgs e)
        {
            if (isPlay is null)
            {
                mediaElement.Position = mediaElement.NaturalDuration.TimeSpan - TimeSpan.FromSeconds(5);
                Play();
                return;
            }
            else
                mediaElement.Position -= TimeSpan.FromSeconds(5);
            if((bool)isPlay)
                Play();
            else
                UpdatePositionSlider();
        }

        private void MediaElementClick()
        {
            if (mediaElement.Source == null)
                return;
            if (isPlay is not null && (bool)isPlay)
            {
                if(mediaElement.CanPause)
                    Pause();
            }
            else
                Play();
        }

        private void stateImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MediaElementClick();
        }

        private void mediaElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MediaElementClick();
        }

        private void position_slider_KeyUp(object sender, KeyEventArgs e)
        {
            if (mediaElement.Source == null)
                return;
            switch (e.Key)
            {
                case Key.Space:
                    MediaElementClick();
                    break;
                case Key.Left:
                    moveL_button_Click(sender, e);
                    break;
                case Key.Right:
                    moveR_button_Click(sender, e);
                    break;
            }
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
            stateImage.Source = startBitmap = !mediaElement.HasVideo ? scaledVolumeHighImg : null;

            pause_button.IsEnabled = true;
            moveL_button.IsEnabled = true;
            moveR_button.IsEnabled = true;
            position_slider.IsEnabled = true;

            position_slider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            UpdatePositionSlider(sender, e);

            if (recent_stackPanel.Children.Cast<RadioButton>().Any(i => ((FileInfo)i.Tag).FullName == filePath))
                return;
            var btn = new RadioButton()
            {
                Content = filePath,
                Tag = new FileInfo(filePath),
            };
            btn.Click += RecentFileClick;
            recent_stackPanel.Children.Insert(0, btn);
            ((RadioButton)recent_stackPanel.Children[0]).IsChecked = true;
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            if (repeat_checkBox.IsChecked is not null && (bool)repeat_checkBox.IsChecked)
            {
                Play();
                return;
            }
            isPlay = null;
            positionTimer.Stop();
            stateImage.Source = scaledReplayImg;
            pauseButtonImage.Source = replayImg;
            position_slider.Value = position_slider.Maximum;
            
        }

        private void pause_button_Click(object sender, RoutedEventArgs e)
        {
            MediaElementClick();
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
