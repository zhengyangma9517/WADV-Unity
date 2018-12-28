using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Core.VisualNovel.Compiler.Editor {
    /// <inheritdoc />
    /// <summary>
    /// 适用于Unity 2017.2+的脚本资源导入器
    /// </summary>
    [ScriptedImporter(1, "vns")]
    public class ScriptImporter : ScriptedImporter {
        
        public override void OnImportAsset(AssetImportContext ctx) {
            var text = new TextAsset(File.ReadAllText(ctx.assetPath, Encoding.UTF8));
            ctx.AddObjectToAsset($"VNScript:{ctx.assetPath}", text, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/VNS Icon.png"));
            ctx.SetMainObject(text);
        }

        /// <summary>
        /// 用于新建VNS文件的Unity资源管理器新建项目选单扩展
        /// </summary>
        [MenuItem("Assets/Create/VisualNovel Script", false, 82)]
        public static void CreateScriptFile() {
            var selectPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (File.Exists(selectPath)) {
                selectPath = Path.GetDirectoryName(selectPath) ?? selectPath;
            }
            ProjectWindowUtil.CreateAssetWithContent(Path.Combine(selectPath, "NewScript.vns"), "// Write your script here\n\n", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/VNS Icon.png"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}