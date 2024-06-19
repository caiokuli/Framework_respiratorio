using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class RespirationMarkerManager : MonoBehaviour
{
    private List<string> markers = new List<string>();
    private int countInspiration = 0;
    private int countExpiration = 0;
    private float startTime;
    private bool isRecording = false;
    public string sessionFolderPath { get; set; }

    void Update()
    {
        if (isRecording)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                string timeString = (Time.time - startTime).ToString(CultureInfo.InvariantCulture);
                markers.Add($"{timeString},inspiration,start");
                countInspiration++;
            }
            else if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                string timeString = (Time.time - startTime).ToString(CultureInfo.InvariantCulture);
                markers.Add($"{timeString},inspiration,end");
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                string timeString = (Time.time - startTime).ToString(CultureInfo.InvariantCulture);
                markers.Add($"{timeString},expiration,start");
                countExpiration++;
            }
            else if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                string timeString = (Time.time - startTime).ToString(CultureInfo.InvariantCulture);
                markers.Add($"{timeString},expiration,end");
            }
        }
    }

    public bool CanStopRecording()
    {
        return countInspiration == countExpiration;
    }

    public void StartRecording()
    {
        startTime = Time.time;
        markers.Clear();
        countInspiration = 0;
        countExpiration = 0;
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
        SaveMarkers();
    }

    private void SaveMarkers()
    {
        string fileName = "RespirationMarkers_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string filePath = Path.Combine(sessionFolderPath, fileName); // Usa o caminho da pasta da sessão
        File.WriteAllLines(filePath, markers.ToArray());
        Debug.Log($"Markers saved to {filePath}");
    }
}
