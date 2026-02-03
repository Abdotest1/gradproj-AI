using UnityEngine;
using UnityEngine.UI; // Needed for standard UI elements like Sliders
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public Slider graphicsSlider;

    void Start()
    {
        // 1. Load saved values (if they exist), otherwise set defaults
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        graphicsSlider.value = PlayerPrefs.GetInt("Quality", 2); // Default to High (2)

        // 2. Add listeners so the code runs when you drag the handle
        volumeSlider.onValueChanged.AddListener(SetVolume);
        graphicsSlider.onValueChanged.AddListener(SetQuality);
    }

    public void SetVolume(float volume)
    {
        // Sets the global game volume
        AudioListener.volume = volume;
        
        // Save it for next time
        PlayerPrefs.SetFloat("Volume", volume);
    }

    public void SetQuality(float qualityIndex)
    {
        // 0 = Low, 1 = Medium, 2 = High (Matches Unity's Quality Settings)
        QualitySettings.SetQualityLevel((int)qualityIndex);

        // Save it
        PlayerPrefs.SetInt("Quality", (int)qualityIndex);
    }
}