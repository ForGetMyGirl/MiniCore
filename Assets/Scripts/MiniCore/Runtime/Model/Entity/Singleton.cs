namespace MiniCore.Model
{
    public class Singleton<T> where T : Singleton<T>, new()
    {

        protected Singleton()
        {
            Init();
        }

        private static readonly object lockObj = new object();
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                            instance = new T();
                    }
                }
                return instance;
            }
        }

        protected virtual void Init() { }

    }

}
