using UnityEngine;
using UnityEngine.SceneManagement;
public class RestartButton : MonoBehaviour
{
  public void Restart()
  {
      Time.timeScale = 1f;
      SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }
}