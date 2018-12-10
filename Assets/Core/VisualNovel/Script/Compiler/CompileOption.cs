using System;
using System.Collections.Generic;

namespace Core.VisualNovel.Script.Compiler {
    /// <summary>
    /// 表示一个编译选项
    /// </summary>
    [Serializable]
    public class CompileOption {
        /// <summary>
        /// 是否删除所有找不到对应原始字符串的翻译（如果一个翻译对应多个原始字符串，只有找不到ID的会被去掉）
        /// </summary>
        public bool RemoveUselessTranslations { get; set; }

        /// <summary>
        /// 编译时除了默认语言外需要额外生成或更新的翻译文件
        /// </summary>
        public List<string> ExtraTranslationLanguages { get; } = new List<string>();
    }
}