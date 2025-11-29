using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // Cargar valores actuales
        masterSlider.value = AudioSettingsManager.Instance.MasterVolume;
        musicSlider.value = AudioSettingsManager.Instance.MusicVolume;
        sfxSlider.value = AudioSettingsManager.Instance.SFXVolume;

        masterSlider.onValueChanged.AddListener(v => AudioSettingsManager.Instance.SetMasterVolume(v));
        musicSlider.onValueChanged.AddListener(v => AudioSettingsManager.Instance.SetMusicVolume(v));
        sfxSlider.onValueChanged.AddListener(v => AudioSettingsManager.Instance.SetSFXVolume(v));
    }

    public void OnContinue()
    {
        GameManager.Instance.Resume();
    }

    public void OnQuit()
    {
        //Me aseguro que el juego NO quede pausado al cambiar escena (no más Pablonadas!)
        GameManager.Instance.Resume();
        // Si está en minijuego
        if (MiniGameManager.Instance != null)
        {
            // No pierde monedas; las pasa automáticamente MiniGameManager
            MiniGameManager.Instance.FinishMiniGame(
                TheHuntGameManager.Instance != null ? TheHuntGameManager.Instance.Coins : 0
            );
            return;
        }

        // Si no es minijuego -> cerrar app
        GameManager.Instance.QuitGame();
    }
}
