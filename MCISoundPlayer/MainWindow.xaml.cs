using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
using Microsoft.Win32;

namespace MCISoundPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private StringBuilder sb;
        private const string ALIAS = "mcisoundplayer";
        private const string PLAYING = "playing";
        private const string STOPPED = "stopped";
        private const string PAUSED = "paused";
        private const char PLAY = '▶';
        private const string PAUSE = "||";
        private const string SPEAKER = "🔊";
        private const string MUTE = "🔇";
        private bool muted;
        private string path;
        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                path = dialog.FileName;
                mciSendStringA("CLOSE " + ALIAS, null, 0, 0);
                sldPosition.Value = 0;
                sldPosition.Maximum = 999;
                btnPlay.Content = PLAY;
            }
        }

        [DllImport("WINMM.DLL", EntryPoint="mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int mciSendStringA(string cmd, StringBuilder rtn, int len, int hwnd);


        public MainWindow()
        {
            InitializeComponent();

            sb = new StringBuilder(128);

            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(999), DispatcherPriority.SystemIdle, new EventHandler(tick), Dispatcher)
            { IsEnabled = false };

            sldVolume.ValueChanged += volumeChanged;
        }

        private void tick(object sender, EventArgs e)
        {
            if (sldPosition.IsMouseCaptureWithin) return;

            if (mciSendStringA("status " + ALIAS + " mode", sb, sb.Capacity, 0) == 0)
            {
                string mode = sb.ToString();
                mciSendStringA("status " + ALIAS + " position", sb, sb.Capacity, 0);
                sldPosition.Value = int.Parse(sb.ToString());
                switch (mode)
                {
                    case PAUSED:
                        timer.Stop();
                        btnPlay.Content = PLAY;
                        break;
                    case STOPPED:
                        timer.Stop();
                        btnPlay.Content = PLAY;
                        break;
                }
            }
            else
            {
                btnPlay.Content = PLAY;
                timer.Stop();
            }
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            Slider s = sender as Slider;
            if (s == null) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var thumb = (s.Template.FindName("PART_Track", s) as Track).Thumb;
                thumb.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent,
                    Source = e.Source
                });
            }
        }



        private void playClick(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if(b==null) return;
            if (string.IsNullOrEmpty(path)) return;

            if (mciSendStringA("status " + ALIAS + " mode", sb, sb.Capacity, 0) == 0)
            {
                switch (sb.ToString())
                {
                    case PAUSED:
                        b.Content = PAUSE;
                        mciSendStringA("resume " + ALIAS, null, 0, 0);
                        timer.Start();
                        break;
                    case PLAYING:
                        b.Content = PLAY;
                        mciSendStringA("pause " + ALIAS, null, 0, 0);
                        timer.Stop();
                        break;
                    case STOPPED:
                        b.Content = PAUSE;
                        mciSendStringA("STATUS " + ALIAS + " POSITION", sb, sb.Capacity, 0);
                        int position = int.Parse(sb.ToString());
                        mciSendStringA("STATUS " + ALIAS + " Length", sb, sb.Capacity, 0);
                        int length = int.Parse(sb.ToString());
                        if (position == length)
                        {
                            mciSendStringA("PLAY " + ALIAS + " FROM 0", null, 0, 0);
                            sldPosition.Value = 0;
                        }
                        else
                        {
                            mciSendStringA("PLAy " + ALIAS, null, 0, 0);
                        }
                        timer.Start();
                        break;
                }
            }
            else
            {
                b.Content = PAUSE;
                mciSendStringA("open \"" + path + "\" alias " + ALIAS, null, 0, 0);
                mciSendStringA("status " + ALIAS + " length", sb, sb.Capacity, 0);
                sldPosition.Maximum = int.Parse(sb.ToString());
                mciSendStringA("setaudio " + ALIAS + " volume to " + ((int)sldVolume.Value).ToString(), null, 0, 0);
                if (muted)
                {
                    mciSendStringA("SETAUDIO " + ALIAS + " OFF", null, 0, 0);
                }
                mciSendStringA("play " + ALIAS, null, 0, 0);
                timer.Start();
            }
        }

        private void volumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mciSendStringA("SETAUDIO " + ALIAS + " VOLUME TO " + ((int)e.NewValue).ToString(), null, 0, 0);
        }

        private void sldPosition_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if(mciSendStringA("STATUS " + ALIAS + " MODE", sb, sb.Capacity, 0) == 0)
            {
                switch (sb.ToString())
                {
                    case PAUSED:
                    case STOPPED:
                        mciSendStringA("seek " + ALIAS + " to " + (int)sldPosition.Value, null, 0, 0);
                        break;
                    case PLAYING:
                        mciSendStringA("play " + ALIAS + " from " + (int)sldPosition.Value, null, 0, 0);
                        break;
                }
            }
            
        }

        private void stopClick(object sender, RoutedEventArgs e)
        {
            mciSendStringA("SEEK " + ALIAS + " TO 0", null, 0, 0);
            btnPlay.Content = PLAY;
            sldPosition.Value = 0;
        }

        private void volumeClick(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;

            if (muted)
            {
                mciSendStringA("SETAUDIO " + ALIAS + " ON", null, 0, 0);
                b.Content = SPEAKER;
            }
            else
            {
                mciSendStringA("SETAUDIO " + ALIAS + " OFF", null, 0, 0);
                b.Content = MUTE;
            }
            muted = !muted;
        }
    }
    public class MillisecondsConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
            int milliseconds = (int)(double)value;
                return string.Format("{0}:{1:d2}", milliseconds / 1000 / 60, milliseconds / 1000 % 60);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    public class OneTenth : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)((double)value / 10);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
