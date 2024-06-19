using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Globalization;
using System;
using static UnityEditor.ShaderData;


public class RespirationDataAnalyzer : MonoBehaviour
{
    ConfigLoader configLoader = ConfigLoader.Instance;
    public Dictionary<string, List<(double time, string action, string originalLine)>> ReadRespirationMarkers(string directory)
    {
        var markerFiles = Directory.GetFiles(directory, "RespirationMarkers*.txt");
        if (markerFiles.Length == 0)
        {
            Debug.LogError("Nenhum arquivo de marcadores encontrado.");
            return null;
        }

        var markers = new Dictionary<string, List<(double time, string action, string originalLine)>>();

        foreach (var file in markerFiles)
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)  // Verificar se há pelo menos três partes
                {
                    try
                    {
                        // Assegurar que está lendo a parte correta da string para o tempo
                        string timeString = parts[0].Trim(); // Corrigido para o índice correto
                        //Debug.Log($"Time string before parsing: '{timeString}'"); // Depuração

                        double time = double.Parse(timeString, CultureInfo.InvariantCulture);
                        string eventType = parts[1].Trim();
                        string action = parts[2].Trim();

                        if (!markers.ContainsKey(eventType))
                            markers[eventType] = new List<(double time, string action, string originalLine)>();

                        markers[eventType].Add((time, action, line));
                        //Debug.Log($"Parsed: {time} for event {eventType} with action {action}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing line '{line}': {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid line format: {line}");
                }
            }
        }

        return markers;
    }



    public Dictionary<string, List<(double start, double end)>> ExtractManualEvents(string directory)
    {
        var markers = ReadRespirationMarkers(directory);
        var events = new Dictionary<string, List<(double start, double end)>>();

        foreach (var eventType in markers.Keys)
        {
            events[eventType] = new List<(double start, double end)>();
            double startTime = -1;

            foreach (var (time, action, _) in markers[eventType])
            {
                if (action == "start")
                {
                    startTime = time;
                }
                else if (action == "end" && startTime != -1)
                {
                    events[eventType].Add((startTime, time));
                    startTime = -1;
                }
            }
        }

        return events;
    }



    public Dictionary<string, List<(double start, double end)>> ExtractAutomatedEvents(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var events = new Dictionary<string, List<(double start, double end)>>();
        string lastType = null;
        double lastTime = 0;

        foreach (var line in lines.Skip(1)) // Pula o cabeçalho
        {
            var parts = line.Split(',');
            int frame = int.Parse(parts[0]);
            double time = double.Parse(parts[1], CultureInfo.InvariantCulture);
            string type = parts[2];

            if (type != lastType)
            {
                if (lastType != null && lastType != "apneia")
                {
                    if (!events.ContainsKey(lastType))
                    {
                        events[lastType] = new List<(double start, double end)>();
                    }
                    events[lastType].Add((lastTime, time));
                }
                lastTime = time;
                lastType = type;
            }
        }

        return events;
    }



    // Use esta função para processar 'automatedEvents' antes de passá-los para 'CalculateMetrics'


    public void CalculateMetrics(
    Dictionary<string, List<(double start, double end)>> manualEvents,
    Dictionary<string, List<(double start, double end)>> automatedEvents,
    string newfilePath, string oldfilePath)
    {

        string content = "\n";
        int correctMatches = 0;
        HashSet<(double, double)> matchedEvents = new HashSet<(double, double)>(); // Para rastrear eventos correspondidos

        // Calculando o total de eventos registrados no automatizado
        int totalEvents = automatedEvents.Sum(x => x.Value.Count);

        foreach (var eventType in manualEvents.Keys)
        {
            List<(double start, double end)> manualList = manualEvents[eventType];
            List<(double start, double end)> automatedList = automatedEvents.ContainsKey(eventType) ? automatedEvents[eventType] : new List<(double, double)>();

            foreach (var (startM, endM) in manualList)
            {
                foreach (var (startA, endA) in automatedList)
                {
                    // Verifica se há sobreposição e se o evento automático não foi ainda correspondido
                    if (startA <= endM && endA >= startM && !matchedEvents.Contains((startA, endA)))
                    {
                        correctMatches++;
                        matchedEvents.Add((startA, endA)); // Marca o evento automático como correspondido
                        Debug.Log($"Matching event found: Manual ({startM}, {endM}) with Automated ({startA}, {endA})");
                        content = content + "\n" + $"Found: Manual ({startM}, {endM}) with Automated ({startA}, {endA})";
                    }
                }
            }
        }

        // Calculando a métrica de acurácia como a razão de correspondências corretas pelo total de eventos
        double accuracy = totalEvents == 0 ? 0 : (double)correctMatches / totalEvents;
        Debug.Log($"Accuracy calculated: {accuracy:F2} (Correct Matches: {correctMatches}, Total Events: {totalEvents})");

        string lastFolderName = Path.GetFileName(oldfilePath.TrimEnd(Path.DirectorySeparatorChar));

        // Criando o nome do arquivo
        string fileName = $"Metricas_With_{lastFolderName}.txt";
        newfilePath = Path.GetDirectoryName(newfilePath);
        string filePath = Path.Combine(newfilePath, fileName);
        content = content +"\n"+ 
            $"Accuracy calculated: {accuracy:F2} (Correct Matches: {correctMatches}, Total Events: {totalEvents})\n" +
            $"Accuracy: {accuracy:F2}\n" +
            $"#################\n" +
            $"Config file:\n" +
            $"qtd_amostras = {int.Parse(configLoader.configValues["qtd_amostras"])}\n" +
            $"taxa_amostragem = {int.Parse(configLoader.configValues["taxa_amostragem"])}\n" +
            $"hopsize = {float.Parse(configLoader.configValues["hopsize"])}\n" +
            $"delta = {float.Parse(configLoader.configValues["delta"])}\n" +
            $"fftWindow = {configLoader.configValues["fftWindow"]}\n" +
            $"filtro = {configLoader.configValues["filtro"]}\n" +
            $"faixa_frequencia = {configLoader.configValues["faixa_frequencia"]}\n" +
            $"quantidade_filterbank = {int.Parse(configLoader.configValues["quantidade_filterbank"])}\n" +
            $"frequencia_max_mel = {int.Parse(configLoader.configValues["frequencia_max_mel"])}\n" +
            $"filterBankType = {configLoader.configValues["filterBankType"] }\n" +
            $"mfccMetric = {configLoader.configValues["mfccMetric"] }\n" +
            $"################\n\n\n";; ;

        // Escrevendo a acurácia no arquivo
        try
        {
            //File.WriteAllText(filePath, content);
            File.AppendAllText(filePath, content);
            Debug.Log($"Acurácia salva em: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Falha ao salvar o arquivo de acurácia: {e.Message}");
        }

    }


    public float AnalyzeRespirationData(string filePath, List<float[]> mfccData, float frameDuration,float hopDuration)
    {
        var directory = Path.GetDirectoryName(filePath);
        Debug.Log("Directory");
        Debug.Log(directory);
        var respirationMarkers = ReadRespirationMarkers(directory);

        if (respirationMarkers == null)
        {
            Debug.LogError("Nenhum marcador de respiração encontrado. Cheque o diretório e o formato do arquivo.");
            return 0f;
        }

        Dictionary<string, List<(int start, int end)>> eventIndices = new Dictionary<string, List<(int start, int end)>>();

        foreach (var eventType in respirationMarkers.Keys)
        {
            eventIndices[eventType] = new List<(int start, int end)>();
        }

        int startIndex = -1;

        foreach (var eventType in respirationMarkers.Keys)
        {
            foreach (var (time, action, originalLine) in respirationMarkers[eventType])
            {
                int mfccIndex = (int)(time / frameDuration);

                if (mfccIndex < 0 || mfccIndex >= mfccData.Count)
                {
                    Debug.LogWarning($"Tempo de evento {time} fora do intervalo de dados MFCC. Índice: {mfccIndex}");
                    continue;
                }

                if (action == "start")
                {
                    startIndex = mfccIndex;
                }
                else if (action == "end" && startIndex != -1)
                {
                    eventIndices[eventType].Add((startIndex, mfccIndex));
                    startIndex = -1; // Reset startIndex para o próximo evento
                }
            }
        }

        List<float> allMeans = new List<float>();
        List<float> allMaxima = new List<float>();
        List<float> allMinima = new List<float>();
        List<float> allMedians = new List<float>();

        // Processar cada evento para calcular as estatísticas
        foreach (var eventType in eventIndices.Keys)
        {
            foreach (var (start, end) in eventIndices[eventType])
            {
                var mfccSegment = mfccData.GetRange(start, end - start + 1).Select(a => a[5]).ToList();
                float mean = mfccSegment.Average();
                float max = mfccSegment.Max();
                float min = mfccSegment.Min();
                float median = GetMedian(mfccSegment);

                allMeans.Add(mean);
                allMaxima.Add(max);
                allMinima.Add(min);
                allMedians.Add(median);

                Debug.Log($"Evento: {eventType}, Início: {start}, Fim: {end}, Média: {mean:F7}, Máximo: {max:F7}, Mínimo: {min:F7}, Mediana: {median:F7}");
            }
        }

        float overallMean = allMeans.Average();
        float overallMax = allMaxima.Average();
        float overallMin = allMinima.Average();
        float overallMedian = allMedians.Average();

        Debug.Log($"Média de todas as médias: {overallMean:F7}, Média de todos os máximos: {overallMax:F7}, Média de todos os mínimos: {overallMin:F7}, Média de todas as medianas: {overallMedian:F7}");
        float returnValue;

        switch (configLoader.configValues.ContainsKey("mfccMetric") ? configLoader.configValues["mfccMetric"] : "mean")
        {
            case "max":
                returnValue = overallMax;
                break;
            case "min":
                returnValue = overallMin;
                break;
            case "median":
                returnValue = overallMedian;
                break;
            case "mean":
            default:
                returnValue = overallMean;
                break;
        }

        return returnValue;

        /*   
           GenerateRespirationEvents(filePath, mfccData, frameDuration,hopDuration ,overallMean,0.3f);



           var manualEvents = ExtractManualEvents(directory);

           foreach (var eventType in manualEvents.Keys)
           {
               Debug.Log($"Manual Event Type: {eventType}");
               foreach (var (start, end) in manualEvents[eventType])
               {
                   Debug.Log($"Start: {start}, End: {end}");
               }
           }



           var autoEvents = ExtractAutomatedEvents("C:\\Users\\Caio\\AppData\\LocalLow\\DefaultCompany\\framework-respiratorio\\recordings\\Session_20240614_114530\\AutoGeneratedRespirationMarkers_20240614_131956.txt");

           foreach (var eventType in autoEvents.Keys)
           {
               Debug.Log($"Automated Event Type: {eventType}");
               foreach (var (start, end) in autoEvents[eventType])
               {
                   Debug.Log($"Start: {start}, End: {end}");
               }
           }

           CalculateMetrics(manualEvents, autoEvents);
        */

    }

    public void AnalyzeRespirationData(string newfilePath,string oldfilePath, List<float[]> mfccData, float frameDuration, float hopDuration, float limiar) {

        var delta = float.Parse(configLoader.configValues["delta"]);

        var caminho = GenerateRespirationEvents(newfilePath,oldfilePath, mfccData, frameDuration, hopDuration, limiar,delta);

        Debug.Log($" new file path {newfilePath}");
        Debug.Log($" old file path {oldfilePath}");
        var manualEvents = ExtractManualEvents(Path.GetDirectoryName(newfilePath));

        foreach (var eventType in manualEvents.Keys)
        {
            Debug.Log($"Manual Event Type: {eventType}");
            foreach (var (start, end) in manualEvents[eventType])
            {
                Debug.Log($"Start: {start}, End: {end}");
            }
        }



        var autoEvents = ExtractAutomatedEvents(caminho);

        foreach (var eventType in autoEvents.Keys)
        {
            Debug.Log($"Automated Event Type: {eventType}");
            foreach (var (start, end) in autoEvents[eventType])
            {
                Debug.Log($"Start: {start}, End: {end}");
            }
        }

        CalculateMetrics(manualEvents, autoEvents,newfilePath,oldfilePath);
    }

    private float GetMedian(List<float> numbers)
    {
        var sortedNumbers = numbers.OrderBy(n => n).ToList();
        int middleIndex = sortedNumbers.Count / 2;
        if (sortedNumbers.Count % 2 == 0)
            return (sortedNumbers[middleIndex] + sortedNumbers[middleIndex - 1]) / 2;
        else
            return sortedNumbers[middleIndex];
    }


    public string GenerateRespirationEvents(string filePath,string oldfilePath, List<float[]> mfccData, float frameDuration,float hopDuration ,float threshold, float delta)
    {
        var directory = Path.GetDirectoryName(filePath);
        var oldsectionName  = Path.GetFileName(oldfilePath.TrimEnd(Path.DirectorySeparatorChar));
        string outputFile = Path.Combine(directory, "AutoGeneratedRespirationMarkers_WithLimiarFrom" + oldsectionName + ".txt");
        List<string> outputLines = new List<string>();

        Debug.Log($"mfccdata count {mfccData.Count} " );
        // Adiciona o cabeçalho no arquivo de saída
        outputLines.Add("frame,tempo,tipo");

        string lastState = "apneia"; // Estado inicial assumido como apneia
        float inspirationThreshold = threshold + delta;
        float expirationThreshold = threshold - delta;

        Debug.Log($"inspirationThreshold {inspirationThreshold} ");
        Debug.Log($"expirationThreshold {expirationThreshold} ");

        for (int i = 0; i < mfccData.Count; i++)
        {
            float currentMFCC6 = mfccData[i][5];
            //Debug.Log($" currentMFCC6 {currentMFCC6} ");
            string currentState;
           
            if (currentMFCC6 > inspirationThreshold)
            {
                currentState = "inspiration";
                Debug.Log($" entrei ins ");
            }
            else if (currentMFCC6 < expirationThreshold)
            {
                currentState = "expiration";
                Debug.Log($" entrei ex ");
            }
            else
            {
                currentState = "apneia";
                Debug.Log($" entrei ap ");
            }

            // Registra a mudança de estado quando ocorre
            if (currentState != lastState)
            {
                outputLines.Add($"{i},{((i * hopDuration)+frameDuration).ToString("F6", CultureInfo.InvariantCulture)},{currentState}");
                lastState = currentState;
            }
        }

        File.WriteAllLines(outputFile, outputLines);
        Debug.Log($"Arquivo de marcações gerado em: {outputFile}");
        return outputFile;
    }


    public void GenerateRespirationEvents4(string filePath, List<float[]> mfccData, float frameDuration, float threshold, float delta)
    {
        var directory = Path.GetDirectoryName(filePath);
        string outputFile = Path.Combine(directory, "AutoGeneratedRespirationMarkers_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
        List<string> outputLines = new List<string>();

        // Adiciona o cabeçalho no arquivo de saída
        outputLines.Add("frame,tempo,tipo");

        float inspirationThreshold = threshold + delta;
        float expirationThreshold = threshold - delta;
        string currentState = "apneia"; // Estado inicial

        for (int i = 0; i < mfccData.Count; i++)
        {
            float currentMFCC6 = mfccData[i][5];

            if (currentMFCC6 > inspirationThreshold)
            {
                currentState = "inspiration";
            }
            else if (currentMFCC6 < expirationThreshold)
            {
                currentState = "expiration";
            }
            else
            {
                currentState = "apneia";
            }

            // Escreve o estado atual para cada frame no arquivo de saída
            outputLines.Add($"{i},{(i * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},{currentState}");
        }

        File.WriteAllLines(outputFile, outputLines);
        Debug.Log($"Arquivo de marcações gerado em: {outputFile}");
    }



    public void GenerateRespirationEvents3(string filePath, List<float[]> mfccData, float frameDuration, float threshold, float delta)
    {
        var directory = Path.GetDirectoryName(filePath);
        string outputFile = Path.Combine(directory, "AutoGeneratedRespirationMarkers_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
        List<string> outputLines = new List<string>();

        // Adiciona o cabeçalho no arquivo de saída
        outputLines.Add("frame,tempo,tipo");

        string lastState = "apneia"; // Começa assumindo que está em apneia
        float inspirationThreshold = threshold + delta;
        float expirationThreshold = threshold - delta;

        for (int i = 0; i < mfccData.Count; i++)
        {
            float currentMFCC6 = mfccData[i][5];
            string currentState;

            if (currentMFCC6 > inspirationThreshold)
            {
                currentState = "inspiration";
            }
            else if (currentMFCC6 < expirationThreshold)
            {
                currentState = "expiration";
            }
            else
            {
                currentState = "apneia";
            }

            // Verifica se o estado atual é diferente do último estado para registrar a mudança
            if (currentState != lastState)
            {
                outputLines.Add($"{i},{(i * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},{currentState}");
                lastState = currentState;
            }
        }

        File.WriteAllLines(outputFile, outputLines);
        Debug.Log($"Arquivo de marcações gerado em: {outputFile}");
    }


    public void GenerateRespirationEvents2(string filePath, List<float[]> mfccData, float frameDuration, float threshold)
    {
        var directory = Path.GetDirectoryName(filePath);
        string outputFile = Path.Combine(directory, "AutoGeneratedRespirationMarkers2_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
        List<string> outputLines = new List<string>();

        bool currentlyInspiring = false; // Track whether we are in inspiration
        float lastEventTime = 0; // Time of the last event (either start or end)

        for (int i = 0; i < mfccData.Count; i++)
        {
            float currentMFCC6 = mfccData[i][5];
            float currentTime = i * frameDuration;

            if (currentMFCC6 > threshold && !currentlyInspiring)
            {
                // Transition to inspiration
                if (i > 0) // Ensure there's a previous event to end
                {
                    outputLines.Add($"{lastEventTime.ToString("F6", CultureInfo.InvariantCulture)},expiration,end");
                }
                outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},inspiration,start");
                currentlyInspiring = true;
                lastEventTime = currentTime;
            }
            else if (currentMFCC6 <= threshold && currentlyInspiring)
            {
                // End inspiration and start expiration
                outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},inspiration,end");
                outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},expiration,start");
                currentlyInspiring = false;
                lastEventTime = currentTime;
            }
        }

        // Close the last ongoing event
        if (currentlyInspiring)
        {
            outputLines.Add($"{(mfccData.Count * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},inspiration,end");
        }
        else
        {
            outputLines.Add($"{(mfccData.Count * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},expiration,end");
        }

        File.WriteAllLines(outputFile, outputLines);
        Debug.Log($"Generated respiration events saved to {outputFile}");
    }


    public void GenerateRespirationAndApneaEvents(string filePath, List<float[]> mfccData, float frameDuration, float inspirationThreshold, float apneaThreshold)
    {
        var directory = Path.GetDirectoryName(filePath);
        string outputFile = Path.Combine(directory, "AutoGeneratedRespirationMarkers3_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
        List<string> outputLines = new List<string>();

        bool currentlyInspiring = false;
        bool inApnea = false;
        float lastEventTime = 0;

        for (int i = 0; i < mfccData.Count; i++)
        {
            float currentMFCC6 = mfccData[i][5];
            float currentTime = i * frameDuration;

            // Check for apnea
            if (currentMFCC6 < apneaThreshold && !inApnea)
            {
                // Transition to apnea
                if (currentlyInspiring)
                {
                    outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},inspiration,end");
                    currentlyInspiring = false;
                }
                outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},apnea,start");
                inApnea = true;
                lastEventTime = currentTime;
            }
            else if (currentMFCC6 >= apneaThreshold && inApnea)
            {
                // End apnea
                outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},apnea,end");
                inApnea = false;
                lastEventTime = currentTime;
            }

            // Check for inspiration and expiration
            if (!inApnea)  // Only check if not in apnea
            {
                if (currentMFCC6 > inspirationThreshold && !currentlyInspiring)
                {
                    // Transition to inspiration
                    if (i > 0) // Ensure there's a previous event to end
                    {
                        outputLines.Add($"{lastEventTime.ToString("F6", CultureInfo.InvariantCulture)},expiration,end");
                    }
                    outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},inspiration,start");
                    currentlyInspiring = true;
                    lastEventTime = currentTime;
                }
                else if (currentMFCC6 <= inspirationThreshold && currentlyInspiring)
                {
                    // End inspiration and start expiration
                    outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},inspiration,end");
                    outputLines.Add($"{currentTime.ToString("F6", CultureInfo.InvariantCulture)},expiration,start");
                    currentlyInspiring = false;
                    lastEventTime = currentTime;
                }
            }
        }

        // Close the last ongoing event
        if (currentlyInspiring)
        {
            outputLines.Add($"{(mfccData.Count * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},inspiration,end");
        }
        else if (!inApnea)
        {
            outputLines.Add($"{(mfccData.Count * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},expiration,end");
        }
        else if (inApnea)
        {
            outputLines.Add($"{(mfccData.Count * frameDuration).ToString("F6", CultureInfo.InvariantCulture)},apnea,end");
        }

        File.WriteAllLines(outputFile, outputLines);
        Debug.Log($"Generated respiration and apnea events saved to {outputFile}");
    }
    public float CalculateOtsuThreshold(List<float[]> mfccData)
    {
        // Extrair o sexto coeficiente MFCC de cada frame
        float[] data = mfccData.Select(x => x[5]).ToArray();

        // Calcular o histograma dos dados
        int numBins = 256;
        int[] histogram = new int[numBins];
        float minData = data.Min();
        float maxData = data.Max();
        float range = maxData - minData;

        foreach (var value in data)
        {
            int bin = (int)((numBins - 1) * (value - minData) / range);
            histogram[bin]++;
        }

        // Implementação do método de Otsu
        int total = data.Length;
        float sum = 0;
        for (int t = 0; t < numBins; t++)
            sum += t * histogram[t];

        float sumB = 0, wB = 0, wF = 0, mB, mF, max = 0, between = 0;
        float threshold1 = 0, threshold2 = 0;

        for (int t = 0; t < numBins; t++)
        {
            wB += histogram[t];               // Weight Background
            if (wB == 0) continue;

            wF = total - wB;                  // Weight Foreground
            if (wF == 0) break;

            sumB += t * histogram[t];

            mB = sumB / wB;                   // Mean Background
            mF = (sum - sumB) / wF;           // Mean Foreground

            // Calculate Between Class Variance
            between = wB * wF * (mB - mF) * (mB - mF);

            // Check if new maximum found
            if (between > max)
            {
                threshold1 = t;
                if (between > max)
                {
                    threshold2 = t;
                }
                max = between;
            }
        }
        return (threshold1 + threshold2) / 2.0f;
    }

}