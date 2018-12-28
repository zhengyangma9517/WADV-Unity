using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Core.VisualNovel.Compiler.Editor {
    public class ScriptAssetPostProcessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            // 重命名，按照vns -> bin -> lang的顺序处理
            var movingFiles = new List<(string From, string To)>();
            for (var i = -1; ++i < movedFromAssetPaths.Length;) {
                var from = movedFromAssetPaths[i];
                if (from.EndsWith(".vns") || from.EndsWith(".txt") || from.EndsWith(".bin.vnb")) {
                    movingFiles.Add((from, movedAssets[i]));
                }
            }
            foreach (var (movedFromAsset, movedAsset) in movingFiles.OrderBy(e => e.From.EndsWith(".vns") ? 0 : e.From.EndsWith(".bin.vnb") ? 1 : 2)) {
                var origin = CodeCompiler.CreatePathFromAsset(movedFromAsset);
                if (origin == null) continue;
                var target = CodeCompiler.CreatePathFromAsset(movedAsset);
                if (movedFromAsset.EndsWith(".vns")) {
                    if (target == null) { // vns -> ?
                        CompileOptions.Remove(origin);
                    } else if (movedAsset.EndsWith(".vns")) { // vns -> vns
                        // 移动翻译文件
                        foreach (var language in CodeCompiler.FilterAssetFromId(Directory.GetFiles(origin.Directory), origin.SourceResource).Where(e => !string.IsNullOrEmpty(e.Language))) {
                            var from = CodeCompiler.CreateLanguageAssetPathFromId(origin.SourceResource, language.Language);
                            var to = CodeCompiler.CreateLanguageAssetPathFromId(target.SourceResource, language.Language);
                            File.Move(from, to);
                        }
                        // 移动编译文件
                        var binaryFile = CodeCompiler.CreateBinaryAssetPathFromId(origin.SourceResource);
                        if (File.Exists(binaryFile)) {
                            File.Move(binaryFile, CodeCompiler.CreateBinaryAssetPathFromId(target.SourceResource));
                        }
                        // 应用重命名
                        CompileOptions.Rename(origin, target);
                    } else if (movedAsset.EndsWith(".bin.vnb")) { // vns -> bin
                        CompileOptions.Remove(origin);
                        CompileOptions.UpdateBinaryHash(target);
                    } else { // vns -> lang
                        CompileOptions.Remove(origin);
                        CompileOptions.ApplyLanguage(target);
                    }
                } else if (movedFromAsset.EndsWith(".bin.vnb")) {
                    if (target == null) { // bin -> ?
                        CompileOptions.UpdateBinaryHash(origin);
                    } else if (movedAsset.EndsWith(".vns")) { // bin -> vns
                        CompileOptions.UpdateBinaryHash(origin);
                        CompileOptions.CreateOrUpdateScript(target);
                    } else if (movedAsset.EndsWith(".bin.vnb")) { // bin -> bin
                        CompileOptions.Get(target.SourceResource).BinaryHash = CompileOptions.Get(origin.SourceResource).BinaryHash;
                        CompileOptions.UpdateBinaryHash(origin);
                    } else { // bin -> lang
                        CompileOptions.UpdateBinaryHash(origin);
                        CompileOptions.ApplyLanguage(target);
                    }
                } else if (!string.IsNullOrEmpty(origin.Language)) {
                    if (target == null) { // lang -> ?
                        CompileOptions.RemoveLanguage(origin);
                    } else if (movedAsset.EndsWith(".vns")) { // lang -> vns
                        CompileOptions.RemoveLanguage(origin);
                        CompileOptions.CreateOrUpdateScript(target);
                    } else if (movedAsset.EndsWith(".bin.vnb")) { // lang -> bin
                        CompileOptions.RemoveLanguage(origin);
                        CompileOptions.UpdateBinaryHash(target);
                    } else { // lang -> lang
                        CompileOptions.RemoveLanguage(origin);
                        CompileOptions.ApplyLanguage(target);
                    }
                }
            }
            // 处理新建和重新导入
            foreach (var file in importedAssets.Where(e => e.EndsWith(".vns") || e.EndsWith(".txt") || e.EndsWith(".bin.vnb"))) {
                var target = CodeCompiler.CreatePathFromAsset(file);
                if (target == null) continue;
                if (file.EndsWith(".vns")) {
                    CompileOptions.CreateOrUpdateScript(target);
                } else if (file.EndsWith(".bin.vnb")) {
                    CompileOptions.UpdateBinaryHash(target);
                } else {
                    CompileOptions.ApplyLanguage(target);
                }
            }
            // 处理删除
            foreach (var file in deletedAssets.Where(e => e.EndsWith(".vns") || e.EndsWith(".txt") || e.EndsWith(".bin.vnb"))) {
                var target = CodeCompiler.CreatePathFromAsset(file);
                if (target == null) continue;
                if (file.EndsWith(".vns")) {
                    CompileOptions.Remove(target);
                } else if (file.EndsWith(".bin.vnb")) {
                    CompileOptions.UpdateBinaryHash(target);
                } else {
                    CompileOptions.RemoveLanguage(target);
                }
            }
            AssetDatabase.Refresh();
        }
    }
}