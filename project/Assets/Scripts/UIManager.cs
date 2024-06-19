using UnityEngine;
using UnityEngine.UI;  // Necessário para interagir com elementos UI
using UnityEngine.SceneManagement;  // Necessário para mudança de cenas

public class UIManager : MonoBehaviour
{
    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Button returnToMenuButton;

    public AudioCaptureManager audioManager;

    void Update()
    {
        if (audioManager.isRecording)
        {
            startRecordingButton.interactable = false;
            stopRecordingButton.interactable = audioManager.markerManager.CanStopRecording();
            returnToMenuButton.interactable = false;
        }
        else
        {
            startRecordingButton.interactable = true;
            stopRecordingButton.interactable = false;
            returnToMenuButton.interactable = true;
        }
    }

    public void StartRecording()
    {
        audioManager.StartRecording();
    }

    public void StopRecording()
    {
        audioManager.StopRecording();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");  // Substitua "MenuScene" pelo nome correto da cena do menu
    }
}
