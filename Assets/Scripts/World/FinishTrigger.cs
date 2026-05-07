using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class FinishTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject winUi;
    [SerializeField] private bool freezeTime = false;
    [SerializeField] private bool unlockCursor = true;
    [SerializeField] private string nextSceneName = "";

    public UnityEvent OnFinish;

    private bool finished;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (finished) return;
        if (!other.CompareTag(playerTag)) return;

        finished = true;
        if (winUi != null) winUi.SetActive(true);
        if (freezeTime) Time.timeScale = 0f;
        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        OnFinish?.Invoke();
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            SceneManager.LoadScene("MainMenu");
    }
}
