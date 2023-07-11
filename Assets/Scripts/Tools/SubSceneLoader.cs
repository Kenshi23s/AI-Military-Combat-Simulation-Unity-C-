using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneLoader : MonoBehaviour
{
    public int scenesCount;
    [SerializeField] string[] extras;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadScenes());
    }

    IEnumerator LoadScenes() 
    {
        string sceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < scenesCount; i++)
        {
            string currentSceneName = sceneName + "_" + i;

            // Si la escena ya esta cargada, pasar a la siguiente
            if (SceneManager.GetSceneByName(currentSceneName).IsValid())
                continue;

            // Cargar la escena y esperar para cargar la siguiente. ¿Por que? No estoy seguro.
            yield return SceneManager.LoadSceneAsync(currentSceneName, LoadSceneMode.Additive);
            yield return new WaitForSeconds(0.2f);
        }


        for (int i = 0; i < extras.Length; i++)
        {
            string currentSceneName = sceneName + "_" + i;

            // Si la escena ya esta cargada, pasar a la siguiente
            if (SceneManager.GetSceneByName(currentSceneName).IsValid())
                continue;

            // Cargar la escena y esperar para cargar la siguiente. ¿Por que? No estoy seguro.
            yield return SceneManager.LoadSceneAsync(sceneName + "_" + extras[i], LoadSceneMode.Additive);
            yield return new WaitForSeconds(0.2f);
        }

    }
}
