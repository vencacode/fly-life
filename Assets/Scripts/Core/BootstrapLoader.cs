using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        // GameManager.Awake() has already run and called DontDestroyOnLoad
        // Just load the main scene
        SceneManager.LoadScene("MainMap");
    }
}
