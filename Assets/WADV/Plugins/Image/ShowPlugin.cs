using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using WADV.Extensions;
using WADV.MessageSystem;
using WADV.Plugins.Image.Effects;
using WADV.Plugins.Image.Utilities;
using WADV.Plugins.Unity;
using WADV.Reflection;
using WADV.Thread;
using WADV.VisualNovel.Interoperation;
using WADV.VisualNovel.Runtime.Utilities;

namespace WADV.Plugins.Image {
    [StaticRegistrationInfo("Show")]
    [UsedImplicitly]
    public partial class ShowPlugin : IVisualNovelPlugin {
        private Dictionary<string, ImageDisplayInformation> _images = new Dictionary<string, ImageDisplayInformation>();
        private TransformValue _defaultTransform = new TransformValue();
        private int _defaultLayer;
        private MainThreadPlaceholder _placeholder;

        public ShowPlugin() {
            _defaultTransform.Set(TransformValue.PropertyName.PositionX, 0);
            _defaultTransform.Set(TransformValue.PropertyName.PositionY, 0);
            _defaultTransform.Set(TransformValue.PropertyName.PositionZ, 0);
        }

        public async Task<SerializableValue> Execute(PluginExecuteContext context) {
            var (mode, layer, effect, images) = AnalyseParameters(context);
            if (!images.Any()) return new NullValue();
            CreatePlaceholder();
            InitializeImage(images, layer);
            if (effect == null) {
                await PlaceNewImages(images);
                CompletePlaceholder();
                return new NullValue();
            }
            if (mode == ImageBindMode.None) {
                // TODO: Images separate display
                CompletePlaceholder();
                return new NullValue();
            }
            var names = images.Select(e => e.Name).ToArray();
            var overlayCanvas = await BindImages(FindPreBindImages(names));
            var targetCanvas = await BindImages(images);
            RectInt displayArea;
            (overlayCanvas, targetCanvas, displayArea) = CutDisplayArea(new RectInt(0, 0, targetCanvas.width, targetCanvas.height), overlayCanvas, targetCanvas, mode);
            var overlayName = await PlaceOverlayCanvas(overlayCanvas, displayArea.position, layer);
            await RemoveHiddenSeparateImages(names);
            await PlayOverlayEffect(overlayName, targetCanvas, effect.Effect as SingleGraphicEffect);
            await PlaceNewImages(images);
            await RemoveOverlayImage(overlayName);
            for (var i = -1; ++i < images.Length;) {
                _images.Add(images[i].Name, images[i]);
            }
            CompletePlaceholder();
            return new NullValue();
        }

        public void OnRegister() { }

        public void OnUnregister(bool isReplace) { }

        private void CreatePlaceholder() {
            if (_placeholder != null) {
                CompletePlaceholder();
            }
            _placeholder = Dispatcher.CreatePlaceholder();
        }

        private void CompletePlaceholder() {
            _placeholder.Complete();
            _placeholder = null;
        }

        private (ImageBindMode Mode, int Layer, EffectValue Effect, ImageDisplayInformation[] Images) AnalyseParameters(PluginExecuteContext context) {
            EffectValue effect = null;
            var bind = ImageBindMode.None;
            int? layer = null;
            var images = new List<ImageDisplayInformation>();
            ImageValue currentImage = null;
            string currentName = null;
            TransformValue currentTransform = null;
            void AddImage() {
                // ReSharper disable once AccessToModifiedClosure
                var existed = images.FindIndex(e => e.Content.EqualsWith(currentImage, context.Language));
                if (existed > -1) {
                    images.RemoveAt(existed);
                }
                if (string.IsNullOrEmpty(currentName)) throw new MissingMemberException($"Unable to create show command: missing image name for {currentImage.ConvertToString(context.Language)}");
                images.Add(new ImageDisplayInformation(currentName, currentImage, currentTransform == null ? null : (TransformValue) _defaultTransform.AddWith(currentTransform)));
                currentName = null;
                currentImage = null;
                currentTransform = null;
            }
            foreach (var (key, value) in context.Parameters) {
                switch (key) {
                    case EffectValue effectValue:
                        effect = effectValue;
                        break;
                    case ImageValue imageValue:
                        if (currentImage == null) {
                            currentImage = imageValue;
                            continue;
                        }
                        AddImage();
                        currentImage = imageValue;
                        currentTransform = new TransformValue();
                        break;
                    case IStringConverter stringConverter:
                        var name = stringConverter.ConvertToString(context.Language);
                        switch (name) {
                            case "Layer":
                                layer = IntegerValue.TryParse(value);
                                break;
                            case "Name":
                                currentName = StringValue.TryParse(value);
                                break;
                            case "Effect":
                                effect = value is EffectValue effectValue ? effectValue : throw new ArgumentException($"Unable to create show command: effect {value} is not EffectValue");
                                break;
                            case "Transform":
                                currentTransform.AddWith(value);
                                break;
                            case "Bind":
                                if (value is NullValue) {
                                    bind = ImageBindMode.Canvas;
                                } else {
                                    switch (StringValue.TryParse(value)) {
                                        case "Canvas":
                                        case "Maximal":
                                        case "Max":
                                            bind = ImageBindMode.Canvas;
                                            break;
                                        case "Minimal":
                                        case "Min":
                                            bind = ImageBindMode.Minimal;
                                            break;
                                    }
                                }
                                break;
                            case "DefaultTransform":
                                _defaultTransform = value is TransformValue transformValue ? transformValue : throw new ArgumentException($"Unable to set show command default transform: target {value} is not TransformValue");
                                break;
                            case "DefaultLayer":
                                _defaultLayer = IntegerValue.TryParse(value);
                                break;
                            default:
                                TransformPlugin.AnalyzePropertyTo(name, value, currentTransform, context.Language);
                                break;
                        }
                        break;
                }
            }
            if (currentImage != null) {
                AddImage();
            }
            layer = layer ?? _defaultLayer;
            var list = images.ToArray();
            for (var i = -1; ++i < list.Length;) {
                list[i].layer = layer.Value;
                list[i].status = ImageStatus.PrepareToShow;
            }
            return (bind, layer.Value, effect, list);
        }

        private static async Task<Texture2D> BindImages(ImageDisplayInformation[] images) {
            if (images.Length == 0) return null;
            images = (await MessageService.ProcessAsync<ImageDisplayInformation[]>(
                Message<ImageDisplayInformation[]>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.UpdateInformation, images))).Content;
            var canvasSize = (await MessageService.ProcessAsync<Vector2Int>(Message.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.GetCanvasSize))).Content;
            if (canvasSize.x == 0 && canvasSize.y == 0) throw new NotSupportedException("Unable to create show command: image canvas size must not be 0");
            var shader = SystemInfo.supportsComputeShaders
                ? (await MessageService.ProcessAsync(Message.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.GetBindShader)) as Message<ComputeShader>)?.Content
                : null;
            var canvas = new Texture2DCombiner(canvasSize.x, canvasSize.y, shader);
            for (var i = -1; ++i < images.Length;) {
                await images[i].Content.ReadTexture();
                if (images[i].Content.texture == null) continue;
                if (images[i].status == ImageStatus.OnScreen) {
                    if (i == 0) continue;
                    canvas.Clear(new RectInt(0, 0, images[i].Content.texture.width, images[i].Content.texture.height), images[i].displayMatrix);
                } else {
                    var pivot = new Vector2(images[i].Transform?.Get(TransformValue.PropertyName.PivotX) ?? 0.0F, images[i].Transform?.Get(TransformValue.PropertyName.PivotY) ?? 0.0F);
                    canvas.DrawTexture(images[i].Content.texture, images[i].displayMatrix, images[i].Content.Color.value, pivot);
                }
            }
            return canvas.Combine();
        }
        
        private ImageDisplayInformation[] FindPreBindImages(string[] names) {
            var minLayer = _images.Where(e => names.Contains(e.Key)).Select(e => e.Value.layer).Min();
            var targets = _images.Where(e => e.Value.layer >= minLayer).Select(e => e.Value).ToArray();
            for (var i = -1; ++i < targets.Length;) {
                targets[i].status = names.Contains(targets[i].Name) ? ImageStatus.PrepareToHide : ImageStatus.OnScreen;
            }
            Array.Sort(targets, (x, y) => x.layer - y.layer);
            return targets;
        }
        
        private static void InitializeImage(ImageDisplayInformation[] images, int layer) {
            for (var i = -1; ++i < images.Length;) {
                images[i].status = ImageStatus.PrepareToShow;
                images[i].layer = layer;
            }
        }

        private static (Texture2D Overlay, Texture2D Target, RectInt Area) CutDisplayArea(RectInt displayArea, Texture2D overlay, Texture2D target, ImageBindMode mode) {
            if (mode != ImageBindMode.Minimal) return (overlay, target, displayArea);
            RectInt actualArea;
            if (overlay == null) {
                actualArea = target.GetVisibleContentArea();
                return actualArea.Equals(displayArea) ? (overlay, target, actualArea) : (overlay, target.Cut(displayArea), actualArea);
            }
            actualArea = overlay.GetVisibleContentArea().MergeWith(target.GetVisibleContentArea());
            return actualArea.Equals(displayArea) ? (overlay, target, actualArea) : (overlay.Cut(actualArea), target.Cut(actualArea), actualArea);
        }

        private static async Task<string> PlaceOverlayCanvas(Texture2D canvas, Vector2Int position, int layer) {
            var name = $"OVERLAY{{{Guid.NewGuid().ToString().ToUpper()}}}";
            var transform = new TransformValue();
            transform.Set(TransformValue.PropertyName.PositionX, position.x);
            transform.Set(TransformValue.PropertyName.PositionY, position.y);
            var content = new ImageMessageIntegration.ShowImageContent {
                Images = new[] {new ImageDisplayInformation(name, new ImageValue {texture = canvas}, transform) {layer = layer, status = ImageStatus.PrepareToShow}}
            };
            await MessageService.ProcessAsync(Message<ImageMessageIntegration.ShowImageContent>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.ShowImage, content));
            return name;
        }
        
        private async Task RemoveHiddenSeparateImages(string[] names) {
            var content = new ImageMessageIntegration.HideImageContent {Names = names};
            await MessageService.ProcessAsync(Message<ImageMessageIntegration.HideImageContent>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.HideImage, content));
            _images.RemoveAll(names);
        }
        
        private static async Task PlayOverlayEffect(string name, Texture2D target, SingleGraphicEffect effect) {
            var content = new ImageMessageIntegration.ShowImageContent {
                Effect = effect,
                Images = new[] {new ImageDisplayInformation(name, new ImageValue {texture = target}, null)}
            };
            await MessageService.ProcessAsync(Message<ImageMessageIntegration.ShowImageContent>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.ShowImage, content));
        }

        private static async Task PlaceNewImages(ImageDisplayInformation[] images) {
            var content = new ImageMessageIntegration.ShowImageContent {Images = images};
            await MessageService.ProcessAsync(Message<ImageMessageIntegration.ShowImageContent>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.ShowImage, content));
            for (var i = -1; ++i < images.Length;) {
                images[i].status = ImageStatus.OnScreen;
            }
        }

        private static async Task RemoveOverlayImage(string overlayName) {
            var content = new ImageMessageIntegration.HideImageContent {Names = new[] {overlayName}};
            await MessageService.ProcessAsync(Message<ImageMessageIntegration.HideImageContent>.Create(ImageMessageIntegration.Mask, ImageMessageIntegration.HideImage, content));
        }
    }
}