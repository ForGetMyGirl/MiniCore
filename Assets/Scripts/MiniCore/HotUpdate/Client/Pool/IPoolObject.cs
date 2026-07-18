
namespace MiniCore.Model
{

    public interface IPoolObject
    {

        /// <summary>
        /// 当前池对象是否可用
        /// </summary>
        bool IsUseful { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 清空
        /// </summary>
        void Clear();

        /// <summary>
        /// 分组名
        /// </summary>
        string GroupName { get; set; }

    }


}
