# audio-player-control

Визуализированный аудио плеер

## ОПИСАНИЕ

Визуализированный аудио плеер, отображающий спектрограмму аудио данных и позволяющий управлять воспроизведением.

Формат аудио воспроизводимого плеером:

* PCM
* Rate = 8000
* Bits = 16
* Channels = 1

Данные на плеер передаются в структуре PlayerParam. Входные данные подаются сразу полностью. Тестовые данные находятся в папке TestAudio.


## ЗАВИСИМОСТИ

* NAudio
* WPFSoundVisualizationLib


## ПРИМЕР ПОДКЛЮЧЕНИЯ

**В XAML**
```
<audioPlayerControl:AudioPlayer x:Name="AudioPlayer" 
                                HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>
```

**В Code-Behind**
```
public MainWindow()
{
	InitializeComponent();

	AudioPlayer.DataContext = new PlayerParam();
}
```
