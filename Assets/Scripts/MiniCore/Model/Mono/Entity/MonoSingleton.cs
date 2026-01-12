using UnityEngine;
namespace MiniCore.Model
{

    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {

        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        //保证脚本先Awake再进行Init();
                        new GameObject($"{typeof(T)}Singleton").AddComponent<T>();
                    }
                    else
                    {
                        instance.Init();
                    }
                }
                return instance;
            }
        }

        protected virtual void Init()
        {
            //DontDestroyOnLoad(gameObject);
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                Init();
            }
        }



    }

}
