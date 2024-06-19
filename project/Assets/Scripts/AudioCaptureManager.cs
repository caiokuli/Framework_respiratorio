/*using UnityEngine;
using System.IO;
using System;

public class AudioCaptureManager : MonoBehaviour
{
    private AudioClip audioClip;
    private int sampleRate;
    private ConfigLoader configLoader; // Ajuste para usar ConfigLoader
    private string sessionFolderPath;
    public RespirationMarkerManager markerManager;

    // Propriedade p�blica para verificar o estado da grava��o
    public bool isRecording { get; private set; } = false;
    private void Awake()
    {
        configLoader = FindObjectOfType<ConfigLoader>(); // Encontra o ConfigLoader na cena
        if (configLoader != null && configLoader.configValues.ContainsKey("taxa_amostragem"))
        {
            sampleRate = int.Parse(configLoader.configValues["taxa_amostragem"]); // Obt�m sampleRate do arquivo de configura��o
        }
        else
        {
            Debug.LogError("ConfigLoader n�o encontrado ou taxa_amostragem n�o definida.");
            sampleRate = 44100; // Valor padr�o caso n�o encontre a configura��o
        }
    }

    public void StartRecording()
    {
        if (!isRecording)
        {
            audioClip = Microphone.Start(null, true, 3600-1, sampleRate);
            
            sessionFolderPath = CreateSessionFolder();
            markerManager.sessionFolderPath = sessionFolderPath;
            markerManager.StartRecording();
            isRecording = true;
            Debug.Log("Grava��o iniciada");
        }
    }


    public void StopRecording()
    {
        if (isRecording && Microphone.IsRecording(null))
        {
            // Verifica se os marcadores de inspira��o e expira��o est�o balanceados
            if (markerManager.CanStopRecording())
            {
                int lastSample = Microphone.GetPosition(null);
                Microphone.End(null);
                if (lastSample > 0)
                {
                    // Criar um AudioClip ajustado com a dura��o real da grava��o
                    AudioClip newClip = AudioClip.Create("TrimmedClip", lastSample, audioClip.channels, audioClip.frequency, false);
                    float[] data = new float[lastSample * audioClip.channels];
                    audioClip.GetData(data, 0);
                    newClip.SetData(data, 0);

                    string uniqueFileName = "audio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    SaveAudioClip(newClip, uniqueFileName);
                    SaveAudioConfig(uniqueFileName);
                }
                markerManager.StopRecording();
                isRecording = false;
                Debug.Log("Grava��o finalizada e dados salvos.");
            }
            else
            {
                Debug.LogWarning("N�o � poss�vel parar a grava��o. N�mero de inspira��es e expira��es n�o coincide.");
            }
        }
        else
        {
            Debug.LogWarning("Nenhuma grava��o ativa para ser parada.");
        }
    }



    private void SaveAudioConfig(string baseFileName)
    {
        string configPath = Path.Combine(sessionFolderPath, baseFileName + "_config.txt");
        string[] configLines = {
            $"taxa_amostragem={sampleRate}",
            // Adicione outras configura��es de �udio conforme necess�rio
        };
        File.WriteAllLines(configPath, configLines);
        Debug.Log($"Configura��es de �udio salvas em: {configPath}");
    }

    private string CreateSessionFolder()
    {
        string basePath = Application.persistentDataPath;  // Caminho base para dados persistentes
        string recordingsPath = Path.Combine(basePath, "recordings");  // Caminho para a pasta recordings
        Directory.CreateDirectory(recordingsPath);  // Garante que a pasta recordings exista

        string folderName = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(recordingsPath, folderName);
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }




    private void SaveAudioClip(AudioClip clip, string fileName)
    {
        try
        {
            string path = Path.Combine(sessionFolderPath, fileName + ".wav");
            using (var fileStream = CreateEmptyWAV(path))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }
            Debug.Log($"Audio salvo em: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Falha ao salvar o arquivo WAV: {e.Message}");
        }
    }

    private FileStream CreateEmptyWAV(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++) // Prepara o cabe�alho do arquivo WAV
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    private void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        // Bytes por amostra
        Byte[] bytesData = new Byte[samples.Length * 2];

        const float rescaleFactor = 32767; // Para converter float para Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;
        UInt16 two = 2;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * numChannels * bytesPerSample
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        // O restante do arquivo j� foi escrito no m�todo ConvertAndWrite
    }



}
*/

using UnityEngine;
using System.IO;
using System;
using NAudio.Wave;
using NWaves.Audio;

public class AudioCaptureManager : MonoBehaviour
{
    private WaveInEvent waveSource;
    private WaveFileWriter waveFile;
    private ConfigLoader configLoader;
    private string sessionFolderPath;
    public RespirationMarkerManager markerManager;
    public bool isRecording { get; private set; } = false;

    private void Awake()
    {
        configLoader = FindObjectOfType<ConfigLoader>();
        if (configLoader != null && configLoader.configValues.ContainsKey("taxa_amostragem"))
        {
            int sampleRate = int.Parse(configLoader.configValues["taxa_amostragem"]);
            waveSource = new WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(sampleRate, 1) };
        }
        else
        {
            Debug.LogError("ConfigLoader n�o encontrado ou taxa_amostragem n�o definida.");
            waveSource = new WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(44100, 1) };
        }
    }

    public void StartRecording()
    {
        if (!isRecording)
        {
            sessionFolderPath = CreateSessionFolder();
            markerManager.sessionFolderPath = sessionFolderPath;

            string filePath = Path.Combine(sessionFolderPath, "audio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");
            waveFile = new WaveFileWriter(filePath, waveSource.WaveFormat);

            waveSource.DataAvailable += OnDataAvailable;
            waveSource.RecordingStopped += OnRecordingStopped;

            waveSource.StartRecording();
            markerManager.StartRecording();
            isRecording = true;
            Debug.Log("Grava��o iniciada");
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            if (markerManager.CanStopRecording())
            {
                waveSource.StopRecording();
                markerManager.StopRecording();
                isRecording = false;
                Debug.Log("Grava��o finalizada e dados salvos.");
            }
            else
            {
                Debug.LogWarning("N�o � poss�vel parar a grava��o. N�mero de inspira��es e expira��es n�o coincide.");
            }
        }
        else
        {
            Debug.LogWarning("Nenhuma grava��o ativa para ser parada.");
        }
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (waveFile != null)
        {
            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
            waveFile.Flush();
        }
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        waveFile?.Dispose();
        waveFile = null;
        waveSource.DataAvailable -= OnDataAvailable;
        waveSource.RecordingStopped -= OnRecordingStopped;
    }

    private string CreateSessionFolder()
    {
        string basePath = Application.persistentDataPath;
        string recordingsPath = Path.Combine(basePath, "recordings");
        Directory.CreateDirectory(recordingsPath);

        string folderName = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(recordingsPath, folderName);
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }
}