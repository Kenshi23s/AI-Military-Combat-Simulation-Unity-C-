using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the sub-scenes of the active scene additively, one after the other.
/// The sub-scenes are named after the active scene: "Name_0" to "Name_(scenesCount - 1)", then "Name_extra" for each extra.
/// </summary>
public class SubSceneLoader : MonoBehaviour
{
    /// <summary>Number of numbered sub-scenes.</summary>
    public int scenesCount;

    [SerializeField] private string[] extras;

    // Seconds between two scene loads.
    private const float DelayBetweenLoads = 0.2f;

    private void Start()
    {
        StartCoroutine(LoadScenes());
    }

    private IEnumerator LoadScenes()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < scenesCount; i++)
            yield return LoadIfNotLoaded(sceneName + "_" + i);

        for (int i = 0; i < extras.Length; i++)
            yield return LoadIfNotLoaded(sceneName + "_" + extras[i]);
    }

    // Loads the scene additively when it is not loaded yet, then waits DelayBetweenLoads seconds.
    private IEnumerator LoadIfNotLoaded(string subSceneName)
    {
        if (SceneManager.GetSceneByName(subSceneName).IsValid())
            yield break;

        yield return SceneManager.LoadSceneAsync(subSceneName, LoadSceneMode.Additive);
        yield return new WaitForSeconds(DelayBetweenLoads);
    }
}
