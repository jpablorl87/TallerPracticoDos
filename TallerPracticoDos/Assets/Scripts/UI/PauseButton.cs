using UnityEngine;

public class PauseButton : MonoBehaviour
{
    public void OnPausePressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
    }
}
