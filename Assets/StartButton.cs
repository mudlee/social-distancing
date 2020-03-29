using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void BeginGame()
    {
        SceneManager.LoadScene("Level1");
    }
}
