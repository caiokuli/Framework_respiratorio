using System.IO;
using UnityEngine;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters;
using System.Collections.Generic;
using NWaves.Windows;
using NWaves.Filters.Fda;
using System;
using NWaves.Filters.Butterworth;

public class AudioAnalyzer : MonoBehaviour
{
    // Método para analisar arquivos na pasta especificada

    ConfigLoader configLoader = ConfigLoader.Instance;
    public RespirationDataAnalyzer respirationDataAnalyzer;
    private float frameDurationAux;
    private float HopDurationAux;
    private string oldfilepath;


    private Dictionary<string, Func<DiscreteSignal, DiscreteSignal>> availableFilters;
    private Dictionary<string, WindowType> windowTypeMap = new Dictionary<string, WindowType>
{
    { "Rectangular", WindowType.Rectangular },
    { "Triangular", WindowType.Triangular },
    { "Hamming", WindowType.Hamming },
    { "Blackman", WindowType.Blackman },
    { "Hanning", WindowType.Hann }, // Note: Hanning é às vezes chamado de Hann
    { "Hann", WindowType.Hann }, // Adicionando ambos por conveniência
    { "Gaussian", WindowType.Gaussian },
    { "Kaiser", WindowType.Kaiser },
    { "Kbd", WindowType.Kbd },
    { "BartlettHann", WindowType.BartlettHann },
    { "Lanczos", WindowType.Lanczos },
    { "PowerOfSine", WindowType.PowerOfSine },
    { "Flattop", WindowType.Flattop },
    { "Liftering", WindowType.Liftering }
};

    private WindowType GetWindowType(string windowName)
    {
        if (windowTypeMap.ContainsKey(windowName))
            return windowTypeMap[windowName];
        return WindowType.Hamming;  // Default window type if not specified or if there's an error
    }


    private Dictionary<string, Func<int, int, (double, double, double)[], float[][]>> filterBankMethods = new Dictionary<string, Func<int, int, (double, double, double)[], float[][]>>
{
    { "Triangular", (fftSize, sampleRate, frequencies) => FilterBanks.Triangular(fftSize, sampleRate, frequencies) },
    { "Rectangular", (fftSize, sampleRate, frequencies) => FilterBanks.Rectangular(fftSize, sampleRate, frequencies) },
    { "Trapezoidal", (fftSize, sampleRate, frequencies) => FilterBanks.Trapezoidal(fftSize, sampleRate, frequencies) },  
    { "BiQuad", (fftSize, sampleRate, frequencies) => FilterBanks.BiQuad(fftSize, sampleRate, frequencies) }            
};


    private DiscreteSignal ApplyHilbertFilter(DiscreteSignal signal)
    {
        // Você pode decidir o tamanho do filtro baseado em suas necessidades específicas ou configurações
        int filterSize = 128;  // Tamanho padrão; pode ser ajustado ou configurado
        var hilbertFilter = new HilbertFilter(filterSize);
        return hilbertFilter.ApplyTo(signal);
    }

    private DiscreteSignal BandPassFilter(DiscreteSignal signal)
    {
        // Extrai a faixa de frequência e a ordem do filtro do arquivo de configuração
        double lowFrequency = 1800;  // Frequência de corte inferior padrão
        double highFrequency = 4200; // Frequência de corte superior padrão
        int order = configLoader.configValues.ContainsKey("order") ? int.Parse(configLoader.configValues["order"]) : 4;

        if (configLoader.configValues.ContainsKey("faixa_frequencia"))
        {
            string[] frequencyRange = configLoader.configValues["faixa_frequencia"].Split('-');
            if (frequencyRange.Length == 2 &&
                double.TryParse(frequencyRange[0], out double lowFreq) &&
                double.TryParse(frequencyRange[1], out double highFreq) &&
                lowFreq < highFreq)
            {
                lowFrequency = lowFreq / (signal.SamplingRate / 2.0); // Normaliza em relação à frequência de Nyquist
                highFrequency = highFreq / (signal.SamplingRate / 2.0);
            }
            else
            {
                Debug.LogError("Configuração de faixa de frequência inválida. Usando valores padrão.");
            }
        }

        // Cria e aplica o filtro passa-banda Butterworth
        var filter = new BandPassFilter(lowFrequency, highFrequency, order);
        return filter.ApplyTo(signal);
    }

    private DiscreteSignal ApplySimpleBandPassFilter(DiscreteSignal signal)
    {
        double lowFrequency = 1800;  // Frequência de corte inferior padrão em Hz
        double highFrequency = 4200; // Frequência de corte superior padrão em Hz
        int order = configLoader.configValues.ContainsKey("order") ? int.Parse(configLoader.configValues["order"]) : 4;

        if (configLoader.configValues.ContainsKey("faixa_frequencia"))
        {
            string[] frequencyRange = configLoader.configValues["faixa_frequencia"].Split('-');
            if (frequencyRange.Length == 2 &&
                double.TryParse(frequencyRange[0], out double lowFreq) &&
                double.TryParse(frequencyRange[1], out double highFreq) &&
                lowFreq < highFreq)
            {
                lowFrequency = lowFreq;
                highFrequency = highFreq;
            }
            else
            {
                Debug.LogError("Configuração de faixa de frequência inválida. Usando valores padrão.");
            }
        }

        // Normalizar frequências em relação à frequência de Nyquist
        double normalizedLowFreq = lowFrequency / (signal.SamplingRate / 2.0);
        double normalizedHighFreq = highFrequency / (signal.SamplingRate / 2.0);

        // Aplicar o filtro passa-alta
        var highPassFilter = new HighPassFilter(normalizedLowFreq, order);
        var filteredSignal = highPassFilter.ApplyTo(signal);

        // Aplicar o filtro passa-baixa
        var lowPassFilter = new LowPassFilter(normalizedHighFreq, order);
        return lowPassFilter.ApplyTo(filteredSignal);
    }


    private DiscreteSignal ApplyChebyshevBandPassFilter(DiscreteSignal signal)
    {
        double lowFrequency = 1800;  // Frequência de corte inferior padrão em Hz
        double highFrequency = 4200; // Frequência de corte superior padrão em Hz
        int order = 4; //configLoader.configValues.ContainsKey("order") ? int.Parse(configLoader.configValues["order"]) : 4;
        double ripple = 0.1; // Ondulação na banda passante (padrão)

        if (configLoader.configValues.ContainsKey("faixa_frequencia"))
        {
            string[] frequencyRange = configLoader.configValues["faixa_frequencia"].Split('-');
            if (frequencyRange.Length == 2 &&
                double.TryParse(frequencyRange[0], out double lowFreq) &&
                double.TryParse(frequencyRange[1], out double highFreq) &&
                lowFreq < highFreq)
            {
                lowFrequency = lowFreq / (signal.SamplingRate / 2.0); // Normaliza em relação à frequência de Nyquist
                highFrequency = highFreq / (signal.SamplingRate / 2.0);
            }
            else
            {
                Debug.LogError("Configuração de faixa de frequência inválida. Usando valores padrão.");
            }
        }

        /*if (configLoader.configValues.ContainsKey("ripple"))
        {
            if (double.TryParse(configLoader.configValues["ripple"], out double rippleValue))
            {
                ripple = rippleValue;
            }
        }
        */
        var filter = new NWaves.Filters.ChebyshevI.BandPassFilter(lowFrequency, highFrequency, order,ripple);
        return filter.ApplyTo(signal);
    }


    private void Awake()
    {
        availableFilters = new Dictionary<string, Func<DiscreteSignal, DiscreteSignal>>
    {
        { "SemFiltro", signal => signal },
        { "ButterworthBandPassFilter", signal => BandPassFilter(signal) },
        { "Hilbert", signal => ApplyHilbertFilter(signal) },
        { "SimpleBandPassFilter", signal => ApplySimpleBandPassFilter(signal) },
        { "ChebyshevBandPassFilter", signal => ApplyChebyshevBandPassFilter(signal) },
        // Adicione mais filtros conforme necessário
    };
    }

    public float AnalyzeFolder(string folderPath)
    {
        this.oldfilepath = folderPath;
        Debug.Log("Analisando pasta: " + folderPath);
        string[] audioFiles = Directory.GetFiles(folderPath, "*.wav");
        float limiar = 0;
        foreach (string file in audioFiles)
        {
            Debug.Log("Processando arquivo: " + file);
            var mfccData = ProcessAudioFile(file);
            Debug.Log(HopDurationAux);
            limiar = respirationDataAnalyzer.AnalyzeRespirationData(file, mfccData, frameDurationAux,HopDurationAux);
            Debug.Log("Respiration data analyzer ok");
            


        }
        return limiar;
    }

    public void AnalyzeFolderWiththreshold(string folderPath,float limiar)
    {
        Debug.Log("Analisando pasta: " + folderPath);
        string[] audioFiles = Directory.GetFiles(folderPath, "*.wav");
        foreach (string file in audioFiles)
        {
            Debug.Log("Processando arquivo: " + file);
            var mfccData = ProcessAudioFile(file);
            Debug.Log(HopDurationAux);
            respirationDataAnalyzer.AnalyzeRespirationData(file,this.oldfilepath, mfccData, frameDurationAux, HopDurationAux,limiar);
            //respirationDataAnalyzer
            Debug.Log("Respiration data analyzer ok");


        }
    }

    // Método para carregar e processar cada arquivo WAV
    private List<float[]> ProcessAudioFile(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            var waveFile = new WaveFile(stream);
            DiscreteSignal signal = waveFile[Channels.Left];
            Debug.Log($" sample rate do audio file {signal.SamplingRate}");
            string filterType = configLoader.configValues.ContainsKey("filtro") ? configLoader.configValues["filtro"] : "SemFiltro";
            if (availableFilters.ContainsKey(filterType))
            {
                signal = availableFilters[filterType](signal);
            }
            else
            {
                Debug.LogWarning($"Filtro '{filterType}' não encontrado. Aplicação de filtro ignorada.");
            }
            // Exemplo de processamento - calcular os MFCCs
            var mfccVectors = ExtractMFCC(signal);
            Debug.Log($"Áudio processado: {filePath}. MFCCs calculados: {mfccVectors.Count} frames.");
            return mfccVectors;
        }
    }

    private List<float[]> ExtractMFCC(DiscreteSignal signal)
    {
        // Acesso ao ConfigLoader via Singleton
        

        int qtdAmostras = configLoader.configValues.ContainsKey("qtd_amostras") ? int.Parse(configLoader.configValues["qtd_amostras"]) : 1024;
        int samplingRate = signal.SamplingRate;// Rate/configLoader.configValues.ContainsKey("taxa_amostragem") ? int.Parse(configLoader.configValues["taxa_amostragem"]) : 44100;
        int fftSize = qtdAmostras;

 
        float frameDuration = qtdAmostras / (float)samplingRate;
        float auxh = configLoader.configValues.ContainsKey("hopsize") ? float.Parse(configLoader.configValues["hopsize"]) : 0.4f;
        
        float hopDuration = frameDuration * auxh;

        frameDurationAux = frameDuration ;
        HopDurationAux = hopDuration;
        Debug.Log(frameDuration);
        int lowFrequency = 20; // Valor padrão
        int highFrequency = samplingRate / 2; // Valor padrão

        if (configLoader.configValues.ContainsKey("faixa_frequencia"))
        {
            string[] frequencyRange = configLoader.configValues["faixa_frequencia"].Split('-');
            if (frequencyRange.Length == 2)
            {
                int.TryParse(frequencyRange[0], out lowFrequency);
                int.TryParse(frequencyRange[1], out highFrequency);
            }
        }



        string fftWindowName = configLoader.configValues.ContainsKey("fftWindow") ? configLoader.configValues["fftWindow"] : "Hamming";
        WindowType windowType = GetWindowType(fftWindowName);
        
        int max_frequency_filterbank = configLoader.configValues.ContainsKey("frequencia_max_mel") ? int.Parse(configLoader.configValues["frequencia_max_mel"]) : highFrequency;
        int qtd_mfcc = configLoader.configValues.ContainsKey("quantidade_filterbank") ? int.Parse(configLoader.configValues["quantidade_filterbank"]) : 13;
        var frequencies = FilterBanks.MelBands(qtd_mfcc, samplingRate, lowFrequency, highFrequency, true);


        string filterBankType = configLoader.configValues.ContainsKey("filterBankType") ? configLoader.configValues["filterBankType"] : "Triangular";
        if (!filterBankMethods.TryGetValue(filterBankType, out var filterBankFunc))
        {
            throw new InvalidOperationException($"Unsupported filter bank type: {filterBankType}");
        }
        Debug.Log("AQUI " + filterBankType);
        var filterBank = filterBankFunc(qtdAmostras, samplingRate, frequencies);

        // Extrair faixa de frequência se disponível


        var mfccOptions = new MfccOptions
        {
            SamplingRate = samplingRate,
            FeatureCount = qtd_mfcc,
            FrameDuration = frameDuration,
            HopDuration = hopDuration,
            FilterBankSize = qtd_mfcc,
            //PreEmphasis = configLoader.configValues.ContainsKey("delta") ? float.Parse(configLoader.configValues["delta"]) : 0.97f,
            LowFrequency = lowFrequency,
            HighFrequency = highFrequency,
            Window = windowType,
            FilterBank = filterBank
        };

        var mfccExtractor = new MfccExtractor(mfccOptions);
        var mfccVectors = mfccExtractor.ComputeFrom(signal);
        return mfccVectors;
    }
}



/*IEnumerator LoadAndProcessAudioClip(string filePath)
{
    string uri = "file://" + filePath;
    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
    {
        yield return www.SendWebRequest(); // Espera a conclusão do request

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Erro ao carregar o arquivo de áudio: " + www.error);
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip != null)
            {
                audioSource.clip = clip;
                ProcessAudioClip(clip); // Processa o áudio carregado
            }
            else
            {
                Debug.LogError("Falha ao carregar o AudioClip.");
            }
        }
    }
}
*/

