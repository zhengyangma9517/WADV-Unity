using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace WADV.Plugins.Image.Effects {
    /// <inheritdoc cref="ShaderLoader" />
    /// <summary>
    /// 基于Shader的无限循环UI元素效果
    /// </summary>
    public abstract class StaticShaderGraphicEffect : StaticGraphicEffect {
        private readonly string _shaderName;
        protected Shader EffectShader;
        
        protected StaticShaderGraphicEffect(string shaderName) {
            _shaderName = shaderName;
        }

        public override async Task Initialize() {
            EffectShader = await ShaderLoader.Load(_shaderName);
        }
        
        /// <summary>
        /// 初始化材质
        /// </summary>
        /// <returns></returns>
        protected abstract Material CreateMaterial();
        
        public override async Task StartEffect(Graphic target) {
            var material = CreateMaterial();
            target.material = material;
            await PlayStartEffect(material);
        }

        public override async Task EndEffect(Graphic target) {
            await PlayEndEffect(target.material);
            target.material = null;
        }

        protected abstract Task PlayStartEffect(Material material);

        protected abstract Task PlayEndEffect(Material material);
    }
}