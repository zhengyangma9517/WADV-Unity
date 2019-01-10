using JetBrains.Annotations;

namespace WADV.VisualNovel.Interoperation {
    /// <summary>
    /// 表示一个取子元素互操作器
    /// </summary>
    public interface IPickChildOperator {
        /// <summary>
        /// 取出指定子元素
        /// </summary>
        /// <param name="name">子元素标记</param>
        /// <returns></returns>
        [CanBeNull]
        SerializableValue PickChild([NotNull] SerializableValue name);
    }
}