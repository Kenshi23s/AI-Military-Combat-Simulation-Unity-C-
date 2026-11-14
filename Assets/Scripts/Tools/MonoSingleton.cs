using UnityEngine;

/// <summary>
/// MonoBehaviour with a single instance in the scene, stored in instance.
/// Subclasses initialize in SingletonAwake.
/// </summary>
/// <typeparam name="T">The subclass.</typeparam>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    /// <summary>The instance in the scene.</summary>
    public static T instance { get; private set; }

    // Awake is reserved for the singleton. Subclasses use SingletonAwake.
    protected void Awake()
    {
        if (instance!=null)
        {
            throw new System.Exception(typeof(MonoSingleton<T>) + "already present at scene");
        }

        instance = (T)this;
        SingletonAwake();
    }
    protected abstract void SingletonAwake();

}
