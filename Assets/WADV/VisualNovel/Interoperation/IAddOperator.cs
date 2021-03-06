using JetBrains.Annotations;

namespace WADV.VisualNovel.Interoperation {
    /// <summary>
    /// 表示一个加法互操作器
    /// </summary>
    public interface IAddOperator {
        /// <summary>
        /// 与目标值相加
        /// </summary>
        /// <param name="target">目标值</param>
        /// <returns></returns>
        [CanBeNull]
        SerializableValue AddWith([NotNull] SerializableValue target);
        
        /// <summary>
        /// 在特定语言下与目标值相加
        /// </summary>
        /// <param name="target">目标值</param>
        /// <param name="language">目标语言</param>
        /// <returns></returns>
        [CanBeNull]
        SerializableValue AddWith([NotNull] SerializableValue target, string language);
    }
}