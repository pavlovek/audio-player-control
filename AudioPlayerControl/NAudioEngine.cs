using System.Linq;
using System.Text;

namespace Sample_NAudio
{
    using NAudio.Wave;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;

    using WPFSoundVisualizationLib;

    internal class NAudioEngine :  IWaveformPlayer, ISoundPlayer, INotifyPropertyChanged, IDisposable
    {
        private WaveStream activeStream;
        private bool canPause;
        private bool canPlay;
        private bool canStop;
        private double channelLength;
        private double channelPosition;
        private bool disposed;


        private bool inChannelSet;
        private bool inChannelTimerUpdate;
        private WaveChannel32 _streamToPlay;
        private bool inRepeatSet;
        private static NAudioEngine instance;
        private bool isPlaying;
        private byte[] pendingWaveformPath;
        private byte[] pendingBackWaveformPath;
        private WaveFormat pendimgWaveFormat;
        private readonly DispatcherTimer positionTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
        private TimeSpan repeatStart;
        private TimeSpan repeatStop;
        private const int repeatThreshold = 200;
        private SampleAggregator waveformAggregator;
        private const int waveformCompressedPointCount = 0x7d0;
        private const float verticalScale = 1.5f;
        private float[] waveformData;
        private readonly BackgroundWorker waveformGenerateWorker = new BackgroundWorker();
        private WaveOut waveOutDevice;

        public event PropertyChangedEventHandler PropertyChanged;

        private NAudioEngine()
        {
            this.positionTimer.Interval = TimeSpan.FromMilliseconds(50.0);
            this.positionTimer.Tick += positionTimer_Tick;
            this.waveformGenerateWorker.DoWork += waveformGenerateWorker_DoWork;
            this.waveformGenerateWorker.RunWorkerCompleted += waveformGenerateWorker_RunWorkerCompleted;
            this.waveformGenerateWorker.WorkerSupportsCancellation = true;
        }

        public void SetVollume(double vollume)
        {
            if (_streamToPlay != null && vollume<=1&& vollume>=0)
                _streamToPlay.Volume = (float)vollume;
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.StopAndCloseStream();
                }
                this.disposed = true;
            }
        }

       

        private void GenerateWaveformData(byte[] path,byte[] backPath, WaveFormat waveFormat)
        {
            if (waveformGenerateWorker.IsBusy)
            {
                pendingWaveformPath = path;
                pendingBackWaveformPath = backPath;
                pendimgWaveFormat = waveFormat;
                waveformGenerateWorker.CancelAsync();
            }
            else if (!waveformGenerateWorker.IsBusy)
            {
                waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(2000, ref path,ref backPath,waveFormat));
            }
        }

        private void ForwardStreamSample(object sender, SampleEventArgs e)
        {
            
            long num = (long)((this.SelectionBegin.TotalSeconds / this.ActiveStream.TotalTime.TotalSeconds) * this.ActiveStream.Length);
            long num2 = (long)((this.SelectionEnd.TotalSeconds / this.ActiveStream.TotalTime.TotalSeconds) * this.ActiveStream.Length);
            if (((this.SelectionEnd - this.SelectionBegin) >= TimeSpan.FromMilliseconds(200.0)) && (this.ActiveStream.Position >= num2))
            {
                ActiveStream.Position = num;
            }
        }

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void SetWaveHeader(ref byte[] rawData,WaveFormat waveFormat,out byte[] result)
        {

            result=new byte[rawData.Length+44];
            Array.Copy(rawData,0,result,44,rawData.Length);

            BinaryWriter writer = new BinaryWriter(new MemoryStream(result));

            writer.Write(EncodeChars("RIFF"));
             							                            // placeholder for (filesize - 8)
            writer.Write(rawData.Length+36);						// byte position = 4
            writer.Write(EncodeChars("WAVE"));

            // fmt chunk (without extra format bytes)
            writer.Write(EncodeChars("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)waveFormat.Channels);
            writer.Write(waveFormat.SampleRate);
            writer.Write(waveFormat.AverageBytesPerSecond);
            writer.Write((short)waveFormat.BlockAlign);
            writer.Write((short)waveFormat.BitsPerSample);

            // data chunk
            writer.Write(EncodeChars("data"));
           							                            // placeholder for data length
            writer.Write(rawData.Length);						// byte position 0x28, 40 decimal


        }

        private byte[] EncodeChars(string s)
        {
            byte[] es = new byte[s.Length];
            es = Encoding.ASCII.GetBytes(s);
            return es;
        }

        public void SetData(ref byte[] path,ref byte[] pathToBack,int rate ,int bits,int channel)
        {
            WaveFormat waveFormat=new WaveFormat(rate,bits,channel);
            Stop();
            
            StopAndCloseStream();
            
            try
            {
                WaveOut outDevice = new WaveOut { DesiredLatency = 500 };   //100
                waveOutDevice = outDevice;
                byte[] waveData = null;
                SetWaveHeader(ref path, waveFormat, out waveData);

                byte[] backWaveData = null;
                SetWaveHeader(ref pathToBack, waveFormat, out backWaveData);

                List<WaveStream> waveProviders = new List<WaveStream>();

                waveProviders.Add(new WaveChannel32(new RawSourceWaveStream(new MemoryStream(waveData), waveFormat)));
                waveProviders.Add(new WaveChannel32(new RawSourceWaveStream(new MemoryStream(backWaveData), waveFormat)));

                ActiveStream = new WaveMixerStream32(waveProviders, false);

                _streamToPlay = new WaveChannel32(ActiveStream);
                _streamToPlay.Sample += ForwardStreamSample;
                waveOutDevice.Init(_streamToPlay);

                ChannelLength = _streamToPlay.TotalTime.TotalSeconds;

                GenerateWaveformData(waveData, backWaveData, waveFormat);
                CanPlay = true;
            }
            catch
            {
                ActiveStream = null;
                CanPlay = false;
            }
        }

        public void SetData(ref byte[] path,int rate,int bits ,int channel)
        {
            WaveFormat waveFormat=new WaveFormat(rate,bits,channel);
            Stop();
            StopAndCloseStream();

            try
            {
                WaveOut outDevice = new WaveOut { DesiredLatency = 500 };    //100
                waveOutDevice = outDevice;
                byte[] waveData = null;
                SetWaveHeader(ref path,waveFormat, out waveData);
                ActiveStream = new WaveFileReader(new MemoryStream(waveData));
                
                _streamToPlay = new WaveChannel32(ActiveStream);
                _streamToPlay.Sample += ForwardStreamSample;
                waveOutDevice.Init(_streamToPlay);
                ChannelLength = _streamToPlay.TotalTime.TotalSeconds;

                GenerateWaveformData(waveData, null, waveFormat);
                CanPlay = true;
            }
            catch
            {
                ActiveStream = null;
                CanPlay = false;
            }
        }

        public void Pause()
        {
            if (IsPlaying && CanPause)
            {
                waveOutDevice.Pause();
                IsPlaying = false;
                CanPlay = true;
                CanPause = false;
            }
        }

        public void Play()
        {
            if (CanPlay)
            {
                waveOutDevice.Play();
                IsPlaying = true;
                CanPause = true;
                CanPlay = false;
                CanStop = true;
            }
        }

        private void positionTimer_Tick(object sender, EventArgs e)
        {
            this.inChannelTimerUpdate = true;
            this.ChannelPosition = (((double)this.ActiveStream.Position) / ((double)this.ActiveStream.Length)) * this.ActiveStream.TotalTime.TotalSeconds;
            this.inChannelTimerUpdate = false;
        }

        public void Stop()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }

            if (ActiveStream != null)
            {
                SelectionBegin = TimeSpan.Zero;
                SelectionEnd = TimeSpan.Zero;
                ChannelPosition = 0.0;
            }
            IsPlaying = false;
            CanStop = false;
            CanPlay = true;
            CanPause = false;
        }

        private void StopAndCloseStream()
        {
            if (waveOutDevice != null)
                waveOutDevice.Stop();
           
            if (activeStream != null)
            {
                _streamToPlay.Close();
                _streamToPlay = null;
                ActiveStream.Close();
                ActiveStream = null;
            }
            if (waveOutDevice != null)
            {
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }

        private void waveformGenerateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WaveformGenerationParams argument = e.Argument as WaveformGenerationParams;
            bool isCancel = false;
            if (argument.BackPath == null)
                isCancel=ProcessSingleWaveForm(argument);
            else
                isCancel=ProcessPairWaveForm(argument);
            if (isCancel)
                e.Cancel = true;
        }
        private bool ProcessPairWaveForm(WaveformGenerationParams argument)
        {
            bool isCancel = false;
            WaveFileReader forwardStream = new WaveFileReader(new MemoryStream(argument.Path));
            WaveFileReader backStream = new WaveFileReader(new MemoryStream(argument.BackPath));


            WaveChannel32 forwardChannel = new WaveChannel32(forwardStream);
            WaveChannel32 backChannel = new WaveChannel32(backStream);
           
            backChannel.Sample += waveStream_Sample;
            forwardChannel.Sample += waveStream_Sample;

            long frameLength = 2 * backChannel.Length / argument.Points;
            frameLength = frameLength - frameLength % backChannel.WaveFormat.BlockAlign;

            waveformAggregator = new SampleAggregator((int)(frameLength / backChannel.WaveFormat.BlockAlign));

            float[] numArray = new float[argument.Points];
            byte[] buffer = new byte[frameLength];

            int factPointsCount = argument.Points / 2;
            for (int i = 0; i < factPointsCount; i++)
            {
                backChannel.Read(buffer, 0, buffer.Length);
                numArray[i * 2] = waveformAggregator.LeftMaxVolume * verticalScale;

                forwardChannel.Read(buffer, 0, buffer.Length);
                numArray[i * 2 + 1] = waveformAggregator.LeftMaxVolume* verticalScale;

                if (this.waveformGenerateWorker.CancellationPending)
                {
                    isCancel = true;
                    break;
                }
            }

            float[] finalClonedData = (float[])numArray.Clone();
            Application.Current.Dispatcher.Invoke(new Action(() => this.WaveformData = finalClonedData));
            
            forwardChannel.Close();
            forwardChannel.Dispose();
            forwardChannel = null;

            backChannel.Close();
            backChannel.Dispose();
            backChannel = null;

            forwardStream.Close();
            forwardStream.Dispose();
            forwardStream = null;

            backStream.Close();
            backStream.Dispose();
            backStream = null;

            return isCancel;
        }
        private bool ProcessSingleWaveForm(WaveformGenerationParams argument)
        {
            bool isCancel = false;
            WaveFileReader sourceStream = new WaveFileReader(new MemoryStream(argument.Path));
            WaveChannel32 channel = new WaveChannel32(sourceStream);
            channel.Sample += waveStream_Sample;

            long frameLength = 2 * channel.Length / argument.Points;
            frameLength = frameLength - frameLength % channel.WaveFormat.BlockAlign;

            this.waveformAggregator = new SampleAggregator((int)(frameLength / channel.WaveFormat.BlockAlign));

            float[] numArray = new float[argument.Points];
            byte[] buffer = new byte[frameLength];

            int factPointsCount = argument.Points / 2;
            for (int i = 0; i < factPointsCount; i++)
            {
                channel.Read(buffer, 0, buffer.Length);
                numArray[i * 2] = waveformAggregator.LeftMaxVolume * verticalScale;
                numArray[i * 2 + 1] = waveformAggregator.RightMaxVolume * verticalScale;

                if (this.waveformGenerateWorker.CancellationPending)
                {
                    isCancel = true;
                    break;
                }
            }

            float[] finalClonedData = (float[])numArray.Clone();
            Application.Current.Dispatcher.Invoke(new Action(() => this.WaveformData = finalClonedData));
            channel.Close();
            channel.Dispose();
            channel = null;
            sourceStream.Close();
            sourceStream.Dispose();
            sourceStream = null;
            return isCancel;
        }

        private void waveformGenerateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled && !this.waveformGenerateWorker.IsBusy)
            {
                this.waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(2000, ref pendingWaveformPath, ref pendingBackWaveformPath, pendimgWaveFormat));
            }
        }

        private void waveStream_Sample(object sender, SampleEventArgs e)
        {
            this.waveformAggregator.Add(e.Left, e.Right);
        }

        public WaveStream ActiveStream
        {
            get
            {
                return this.activeStream;
            }
            protected set
            {
                WaveStream activeStream = this.activeStream;
                this.activeStream = value;
                if (activeStream != this.activeStream)
                {
                    this.NotifyPropertyChanged("ActiveStream");
                }
            }
        }

        public bool CanPause
        {
            get
            {
                return this.canPause;
            }
            protected set
            {
                bool canPause = this.canPause;
                this.canPause = value;
                if (canPause != this.canPause)
                {
                    this.NotifyPropertyChanged("CanPause");
                }
            }
        }

        public bool CanPlay
        {
            get
            {
                return this.canPlay;
            }
            protected set
            {
                bool canPlay = this.canPlay;
                this.canPlay = value;
                if (canPlay != this.canPlay)
                {
                    this.NotifyPropertyChanged("CanPlay");
                }
            }
        }

        public bool CanStop
        {
            get
            {
                return this.canStop;
            }
            protected set
            {
                bool canStop = this.canStop;
                this.canStop = value;
                if (canStop != this.canStop)
                {
                    this.NotifyPropertyChanged("CanStop");
                }
            }
        }

        public double ChannelLength
        {
            get
            {
                return this.channelLength;
            }
            protected set
            {
                double channelLength = this.channelLength;
                this.channelLength = value;
                if (channelLength != this.channelLength)
                {
                    this.NotifyPropertyChanged("ChannelLength");
                }
            }
        }

        public double ChannelPosition
        {
            get
            {
                return this.channelPosition;
            }
            set
            {
                if (!this.inChannelSet)
                {
                    this.inChannelSet = true;
                    double channelPosition = this.channelPosition;
                    double num2 = Math.Max(0.0, Math.Min(value, this.ChannelLength));
                    if (!this.inChannelTimerUpdate && (this.ActiveStream != null))
                    {
                        this.ActiveStream.Position = (long)((num2 / this.ActiveStream.TotalTime.TotalSeconds) * this.ActiveStream.Length);
                    }
                    this.channelPosition = num2;
                    if (channelPosition != this.channelPosition)
                    {
                        this.NotifyPropertyChanged("ChannelPosition");
                    }
                    this.inChannelSet = false;
                }
            }
        }

       

        public static NAudioEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NAudioEngine();
                }
                return instance;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return this.isPlaying;
            }
            protected set
            {
                bool isPlaying = this.isPlaying;
                this.isPlaying = value;
                if (isPlaying != this.isPlaying)
                {
                    this.NotifyPropertyChanged("IsPlaying");
                }
                this.positionTimer.IsEnabled = value;
            }
        }

        public TimeSpan SelectionBegin
        {
            get
            {
                return this.repeatStart;
            }
            set
            {
                if (!this.inRepeatSet)
                {
                    this.inRepeatSet = true;
                    TimeSpan repeatStart = this.repeatStart;
                    this.repeatStart = value;
                    if (repeatStart != this.repeatStart)
                    {
                        this.NotifyPropertyChanged("SelectionBegin");
                    }
                    this.inRepeatSet = false;
                }
            }
        }

        public TimeSpan SelectionEnd
        {
            get
            {
                return this.repeatStop;
            }
            set
            {
                if (!this.inChannelSet)
                {
                    this.inRepeatSet = true;
                    TimeSpan repeatStop = this.repeatStop;
                    this.repeatStop = value;
                    if (repeatStop != this.repeatStop)
                    {
                        this.NotifyPropertyChanged("SelectionEnd");
                    }
                    this.inRepeatSet = false;
                }
            }
        }

        public float[] WaveformData
        {
            get
            {
                return this.waveformData;
            }
            protected set
            {
                float[] waveformData = this.waveformData;
                this.waveformData = value;
                if (waveformData != this.waveformData)
                {
                    this.NotifyPropertyChanged("WaveformData");
                }
            }
        }

        private class WaveformGenerationParams
        {
            public WaveformGenerationParams(int points,ref  byte[] path,ref byte[] backPath, WaveFormat waveFormat)
            {
                Points = points;
                BackPath = backPath;
                Path = path;
                WaveFormatOfData = waveFormat;
            }

            public byte[] Path { get; protected set; }
            public byte[] BackPath { get; protected set; }
            public WaveFormat WaveFormatOfData { get; protected set; }
            public int Points { get; protected set; }
        }
    }
}

