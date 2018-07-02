using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sample_NAudio;

namespace AudioPlayerControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class AudioPlayer : UserControl
    {
        public AudioPlayer()
        {
            InitializeComponent();
            NAudioEngine soundEngine = NAudioEngine.Instance;
            
            UIHelper.Bind(soundEngine, "CanStop", StopButton, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanPlay", PlayButton, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanPause", PauseButton, IsEnabledProperty);
            soundEngine.PropertyChanged += NAudioEngine_PropertyChanged;


            waveformTimeline.RegisterSoundPlayer(soundEngine);

            Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }
        
        public PlayerParam DataSource
        {
            get { return (PlayerParam)this.GetValue(DataSourceProperty); }
            set { this.SetValue(DataSourceProperty, value); }
        }
        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register("DataSource",
            typeof(PlayerParam), typeof(AudioPlayer), new PropertyMetadata(null,null,new CoerceValueCallback(DataSourceChanged)));

        private static object DataSourceChanged(DependencyObject d, object basevalue)
        {
              if(basevalue!=null)
              {
                  AudioPlayer audioPlayer = (AudioPlayer) d;
                  PlayerParam param = (PlayerParam)basevalue;
                  //using (FileStream fs=new FileStream("File",FileMode.Create))
                  //{
                  //    fs.Write(param.ForwardChannelData, 0, param.ForwardChannelData.Length);
                  //}
                  if (param.BackwardChannelData != null)
                  {
                      byte[] forward = param.ForwardChannelData;
                      byte[] backward = param.BackwardChannelData;
                      audioPlayer.SetData(ref forward, ref backward, param.Rate, param.Bits, param.Channels);
                  }
                  else
                  {
                      byte[] forward = param.ForwardChannelData;
                      audioPlayer.SetData(ref forward, param.Rate, param.Bits, param.Channels);
                  }
              }
            return basevalue;
        }

     

        private void NAudioEngine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NAudioEngine instance = NAudioEngine.Instance;
            string propertyName = e.PropertyName;
            if (propertyName != null)
                clockDisplay.Time = TimeSpan.FromSeconds(instance.ChannelPosition);
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            NAudioEngine.Instance.Dispose();
        }

        private void SetData(ref byte[] forwardData, ref byte[] backData,int rate,int bits,int channels)
        {
            NAudioEngine.Instance.SetData(ref forwardData,ref backData, rate, bits, channels);
            NAudioEngine.Instance.SetVollume(VollumeSlider.Value);
        }

        private void SetData(ref byte[] forwardData,  int rate,int bits,int channels)
        {
            NAudioEngine.Instance.SetData(ref forwardData, rate, bits, channels);
            NAudioEngine.Instance.SetVollume(VollumeSlider.Value);
        }


        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanPlay)
                NAudioEngine.Instance.Play();
        }

        public void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanPause)
                NAudioEngine.Instance.Pause();
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanStop)
                NAudioEngine.Instance.Stop();
            
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            NAudioEngine.Instance.SetVollume(e.NewValue);

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanStop)
            {
                NAudioEngine.Instance.Stop();
            }

            Application.Current.Dispatcher.ShutdownStarted -= Dispatcher_ShutdownStarted;
            NAudioEngine.Instance.Dispose();
        }

        public void PlayOrPause()
        {
            if (NAudioEngine.Instance.ChannelPosition == NAudioEngine.Instance.ChannelLength
                && NAudioEngine.Instance.CanStop)
            {
                NAudioEngine.Instance.Stop();
            }

            if (NAudioEngine.Instance.IsPlaying
                && NAudioEngine.Instance.CanPause)
            {
                NAudioEngine.Instance.Pause();
            }
            else if (NAudioEngine.Instance.CanPlay)
            {
                NAudioEngine.Instance.Play();
            }
        }
    }
}
