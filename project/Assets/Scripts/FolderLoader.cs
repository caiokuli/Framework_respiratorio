using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FolderLoader : MonoBehaviour
{
    public Dropdown foldersDropdown; // Referência ao componente Dropdown

    private string recordingsPath; // Caminho para a pasta recordings

    void Start()
    {
        SetupPaths();
        LoadFolders();
    }

    void SetupPaths()
    {
        string basePath = Application.persistentDataPath;  // Caminho base para dados persistentes
        recordingsPath = Path.Combine(basePath, "recordings");  // Configura o caminho para a pasta recordings
        Debug.Log("Caminho para as gravações: " + recordingsPath);
    }

    void LoadFolders()
    {
        if (Directory.Exists(recordingsPath))
        {
            var directories = Directory.GetDirectories(recordingsPath);
            foldersDropdown.ClearOptions(); // Limpa as opções existentes
            foreach (var dir in directories)
            {
                foldersDropdown.options.Add(new Dropdown.OptionData(Path.GetFileName(dir)));
            }
            foldersDropdown.RefreshShownValue();
        }
        else
        {
            Debug.LogError("Caminho especificado não existe: " + recordingsPath);
        }
    }

    public void OnFolderSelected()
    {
        int index = foldersDropdown.value;
        string selectedFolder = foldersDropdown.options[index].text;
        string fullPath = Path.Combine(recordingsPath, selectedFolder);
        Debug.Log("Pasta selecionada: " + fullPath);
        // Aqui você pode adicionar a chamada para analisar os arquivos dessa pasta
    }
}
