using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FolderListManager : MonoBehaviour
{
    public GameObject folderItemPrefab; // Prefab que cont�m o UI de cada item da lista
    public Transform contentPanel; // O transform do conte�do dentro do Scroll View
    public AudioAnalyzer audioAnalyzer; // Refer�ncia ao AudioAnalyzer
    public Text headText;
    private string olddir;
    private string newdir;
    private float limiar;

    void Start()
    {
        LoadFoldersIntoScrollView();
    }

    void LoadFoldersIntoScrollView()
    {
        string basePath = Application.persistentDataPath;
        string recordingsPath = Path.Combine(basePath, "recordings");

        if (Directory.Exists(recordingsPath))
        {
            var directories = Directory.GetDirectories(recordingsPath);
            foreach (var dir in directories)
            {
                GameObject item = Instantiate(folderItemPrefab, contentPanel);
                item.GetComponentInChildren<Text>().text = Path.GetFileName(dir);
                item.GetComponent<Button>().onClick.AddListener(() => OnFolderButtonClick(dir));
            }
        }
        else
        {
            Debug.LogError("Directory not found: " + recordingsPath);
        }
    }

    void OnFolderButtonClick(string dir)
    {
        olddir = dir;
        // A��o 1: Analisar a pasta
        limiar = audioAnalyzer.AnalyzeFolder(dir);

        // A��o 2: Limpar o ScrollView e carregar as pastas com nova a��o
        LoadFoldersIntoScrollViewAfterSelectedFolder();
    }

    void ClearScrollView()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }

    public void LoadFoldersIntoScrollViewAfterSelectedFolder()
    {
        ClearScrollView(); // Limpa o ScrollView
        headText.text = "Selecione uma pasta abaixo para escolher qual base de dados de �udios ser� para aplicar o limiar.";
        string basePath = Application.persistentDataPath;
        string recordingsPath = Path.Combine(basePath, "recordings");

        if (Directory.Exists(recordingsPath))
        {
            var directories = Directory.GetDirectories(recordingsPath);
            foreach (var dir in directories)
            {
                GameObject item = Instantiate(folderItemPrefab, contentPanel);
                item.GetComponentInChildren<Text>().text = Path.GetFileName(dir);
                // Nova a��o ao clicar nos diret�rios
                item.GetComponent<Button>().onClick.AddListener(() => OnNewFolderButtonClick(dir));
            }
        }
        else
        {
            Debug.LogError("Directory not found: " + recordingsPath);
        }
    }

    void OnNewFolderButtonClick(string dir)
    {
        newdir = dir;
        ClearScrollView(); // Limpa o ScrollView
        // Implemente a nova a��o aqui
        Debug.Log("Nova a��o executada para a pasta: " + dir);
        // Por exemplo, voc� pode chamar um novo m�todo do audioAnalyzer
        audioAnalyzer.AnalyzeFolderWiththreshold(dir,limiar);
        headText.text = "Analise feita procure pelo o arquivo gerado em %appdata%";
    }
}
