using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T instance { get; private set; }
    //no se debe crear un awake nuevo, en caso de querer inicializar usar el metodo ArtificialAwake
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
