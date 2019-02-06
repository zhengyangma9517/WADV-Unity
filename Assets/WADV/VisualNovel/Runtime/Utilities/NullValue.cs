using System;
using WADV.VisualNovel.Interoperation;
using WADV.VisualNovel.Translation;

namespace WADV.VisualNovel.Runtime.Utilities {
    /// <inheritdoc cref="SerializableValue" />
    /// <summary>
    /// <para>表示一个空内存值</para>
    /// <list type="bullet">
    ///     <listheader><description>互操作支持</description></listheader>
    ///     <item><description>布尔转换器</description></item>
    ///     <item><description>浮点转换器</description></item>
    ///     <item><description>整数转换器</description></item>
    ///     <item><description>字符串转换器</description></item>
    ///     <item><description>加法互操作器</description></item>
    ///     <item><description>减法互操作器</description></item>
    ///     <item><description>乘法互操作器</description></item>
    ///     <item><description>除法互操作器</description></item>
    ///     <item><description>真值比较互操作器</description></item>
    /// </list>
    /// </summary>
    [Serializable]
    public class NullValue : SerializableValue, IBooleanConverter, IFloatConverter, IIntegerConverter, IStringConverter, IAddOperator, ISubtractOperator, IMultiplyOperator, IDivideOperator,
                             IEqualOperator {
        public override SerializableValue Duplicate() {
            return new NullValue();
        }

        public bool ConvertToBoolean(string language = TranslationManager.DefaultLanguage) {
            return false;
        }

        public float ConvertToFloat(string language = TranslationManager.DefaultLanguage) {
            return 0.0F;
        }

        public int ConvertToInteger(string language = TranslationManager.DefaultLanguage) {
            return 0;
        }
        
        public string ConvertToString(string language = TranslationManager.DefaultLanguage) {
            return "";
        }

        public override string ToString() {
            return ConvertToString();
        }

        public bool EqualsWith(SerializableValue target, string language = TranslationManager.DefaultLanguage) {
            return target is NullValue;
        }

        public SerializableValue AddWith(SerializableValue target, string language = TranslationManager.DefaultLanguage) {
            return target.Duplicate();
        }

        public SerializableValue SubtractWith(SerializableValue target, string language = TranslationManager.DefaultLanguage) {
            return target is NullValue ? new NullValue() : throw new NotSupportedException("Unable to subtract null with any other value except null");
        }

        public SerializableValue MultiplyWith(SerializableValue target, string language = TranslationManager.DefaultLanguage) {
            return new NullValue();
        }

        public SerializableValue DivideWith(SerializableValue target, string language = TranslationManager.DefaultLanguage) {
            return target is NullValue ? new NullValue() : throw new NotSupportedException("Unable to divide null with any other value except null");
        }
    }
}