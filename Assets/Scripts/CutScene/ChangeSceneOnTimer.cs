using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ChangeSceneOnTimer : MonoBehaviour
{
    // The amount of time (in seconds) to wait before changing the scene.
    public float changeTime;

    // The name of the scene you want to load.
    public string sceneName;

    // The Update method is called once every frame.
    private void Update()
    {
        // Subtract the time that has passed since the last frame.
        changeTime -= Time.deltaTime;

        // When the timer reaches zero or less...
        if (changeTime <= 0)
        {
            // ...load the new scene.
            SceneManager.LoadScene(sceneName);
        }
    }
}
