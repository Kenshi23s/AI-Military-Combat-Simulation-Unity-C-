using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneLoader : MonoBehaviour
{
    public int scenesCount;
    [SerializeField] string[] extras;
    HashSet<AsyncOperation> operations;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadScenes());
    }

    IEnumerator LoadScenes() 
    {
        string levelName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < scenesCount; i++)
        {
            yield return SceneManager.LoadSceneAsync(levelName + "_" + i, LoadSceneMode.Additive);
            yield return new WaitForSeconds(0.2f);
        }

        for (int i = 0; i < extras.Length; i++)
        {
            yield return SceneManager.LoadSceneAsync(levelName + "_" + extras[i], LoadSceneMode.Additive);
            yield return new WaitForSeconds(0.2f);
        }

    }
}
