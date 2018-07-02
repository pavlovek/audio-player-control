namespace AudioPlayerControl
{
    /// <summary>
    /// Класс, представляющий входные параметры плеера
    /// </summary>
    public class PlayerParam
    {
        /// <summary>
        /// Данные прямого канала
        /// </summary>
        public byte[] ForwardChannelData { get; set; }

        /// <summary>
        /// Данные обратного канала. null если нет данных обратного канала. NOTE: должны быть одного размера с ForwardChannelData
        /// </summary>
        public byte[] BackwardChannelData { get; set; }

        /// <summary>
        /// Число выборок в секунду
        /// </summary>
        public int Rate { get; set; }

        /// <summary>
        /// Бит в выборке
        /// </summary>
        public int Bits { get; set; }

        /// <summary>
        /// Число каналов
        /// </summary>
        public int Channels { get; set; }


        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="forwardChannelData">Данные прямого канала</param>
        /// <param name="backwardChannelData">Данные обратного канала. null если нет данных обратного канала. NOTE: должны быть одного размера с ForwardChannelData</param>
        /// <param name="rate">Число выборок в секунду</param>
        /// <param name="bits">Бит в выборке</param>
        /// <param name="channels">Число каналов</param>
        public PlayerParam(byte[] forwardChannelData,byte[] backwardChannelData,int rate,int bits,int channels )
        {
            ForwardChannelData = forwardChannelData;
            BackwardChannelData = backwardChannelData;
            Rate = rate;
            Bits = bits;
            Channels = channels;
        }
    }
}
