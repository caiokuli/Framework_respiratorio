using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ConfigLoader : MonoBehaviour
{
    public static ConfigLoader Instance { get; private set; }

    public Dictionary<string, string> configValues = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destrua a instância extra que foi criada.
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém o GameObject quando carregar uma nova cena.
            LoadConfig(); // Carrega a configuração logo que o script é inicializado.
        }
    }

    private void LoadConfig()
    {
        // Suba um nível a partir da pasta 'Assets', que é o que 'Application.dataPath' retorna
        string filePath = Path.Combine(Application.dataPath, "../configs.txt");

        // Normaliza o caminho para lidar com diferentes sistemas operacionais
        filePath = Path.GetFullPath(filePath);

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        configValues[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Arquivo de configuração não encontrado em: " + filePath);
        }
    }
}
