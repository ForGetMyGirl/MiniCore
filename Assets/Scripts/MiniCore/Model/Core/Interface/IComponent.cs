
namespace MiniCore.Model
{
    public interface IComponent
    {
        T AddComponent<T>() where T : class, IComponent;

        T AddComponent<T>(object[] args) where T : class, IComponent;

        T GetComponent<T>() where T : class, IComponent;

        void AddComponent(IComponent component);

        void RemoveComponent<T>() where T : class, IComponent;
        void RemoveComponent(IComponent component);

        void Awake();

        /// <summary>
        /// 是否激活：设计原则为任意的Awake后激活
        /// </summary>
        bool IsActive { get; set; }

        void Awake(object[] args);

        void Update();


        //void Dispose();
    }
}