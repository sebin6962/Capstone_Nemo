using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneManager : MonoBehaviour
{
    private bool clicked = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !clicked)
        {
            clicked = true;
            FadeManager.Instance.FadeToScene("SaveSelectScene");
        }
    }
}
