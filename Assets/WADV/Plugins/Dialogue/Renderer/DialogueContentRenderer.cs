using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor.U2D;
using UnityEngine;
using WADV.Extensions;
using WADV.MessageSystem;
using WADV.Plugins.Dialogue.Items;
using WADV.Plugins.Dialogue.TextGenerator;
using WADV.Thread;

namespace WADV.Plugins.Dialogue.Renderer {
    /// <inheritdoc cref="IMessenger" />
    /// <summary>
    /// 基础对话框内容组件
    /// </summary>
    public abstract class DialogueContentRenderer : MonoBehaviour, IMessenger {
        /// <inheritdoc />
        public int Mask { get; } = DialoguePlugin.MessageMask | CoreConstant.Mask;
        public bool IsStandaloneMessage { get; } = true;

        /// <summary>
        /// 获取渲染状态
        /// </summary>
        public RenderState State { get; private set; } = RenderState.Idle;

        /// <summary>
        /// 文本生成器
        /// </summary>
        public DialogueTextGeneratorType textGenerator = DialogueTextGeneratorType.Simple;
        
        /// <summary>
        /// 每两次生成的帧间隔
        /// </summary>
        [Range(0.0F, 0.5F)]
        public float timeSpan;
        
        /// <summary>
        /// 获取当前显示的文本（用于多段生成时缓存之前文本段的内容）
        /// </summary>
        protected abstract string CurrentText { get; }

        private DialogueTextGenerator _generator;
        [CanBeNull] private MainThreadPlaceholder _currentPlaceholder;
        [CanBeNull] private DialogueDescription _currentDialogue;
        
        private void OnEnable() {
            MessageService.Receivers.CreateChild(this);
        }

        private void OnDisable() {
            MessageService.Receivers.RemoveChild(this);
        }

        /// <summary>
        /// 清空文本
        /// </summary>
        protected abstract void ClearText();

        /// <summary>
        /// 准备文本段样式
        /// </summary>
        /// <param name="currentText">目标文本段</param>
        protected abstract void PrepareStyle(TextDialogueItem currentText);

        /// <summary>
        /// 显示文本
        /// </summary>
        /// <param name="previousPart">之前所有文本段已实际显示（或格式化）的文本</param>
        /// <param name="text">当前段已经生成的文本</param>
        /// <returns></returns>
        protected abstract void ShowText([CanBeNull] string previousPart, StringBuilder text);

        public void ResetGenerator(DialogueTextGeneratorType type) {
            textGenerator = type;
            _generator = DialogueTextGenerator.Create(textGenerator);
        }

        /// <inheritdoc />
        public async Task<Message> Receive(Message message) {
            if (message.Tag == DialoguePlugin.NewDialogueMessageTag && message is Message<DialogueDescription> dialogueMessage) {
                _currentPlaceholder = message.CreatePlaceholder();
                _currentDialogue = dialogueMessage.Content;
                await ProcessText(dialogueMessage.Content.Context.Runtime.ActiveLanguage);
                _currentDialogue = null;
                if (_currentPlaceholder == null) return message;
                _currentPlaceholder.Complete();
                _currentPlaceholder = null;
            } else if (_currentDialogue != null && message.Tag == CoreConstant.LanguageChange && message is Message<string> languageMessage) {
                if (_currentPlaceholder != null) {
                    await _currentPlaceholder;
                }
                _currentPlaceholder = message.CreatePlaceholder();
                await ProcessText(languageMessage.Content);
                if (_currentPlaceholder == null) return message;
                _currentPlaceholder.Complete();
                _currentPlaceholder = null;
            }
            return message;
        }

        private async Task ProcessText(string language) {
            if (_currentDialogue == null) return;
            var (content, noWait, noClear) = DialoguePlugin.ProcessDialogueContent(_currentDialogue.Context.Runtime, _currentDialogue.RawContent, language);
            var history = noClear ? CurrentText : null;
            if (_generator == null) {
                ResetGenerator(textGenerator);
            }
            if (content.Any()) {
                foreach (var item in content) {
                    switch (item) {
                        case ClearDialogueItem _:
                            ClearText();
                            history = null;
                            break;
                        case PauseDialogueItem pauseDialogueItem:
                            State = RenderState.Waiting;
                            if (pauseDialogueItem.Time.HasValue) {
                                await Dispatcher.WaitForSeconds(pauseDialogueItem.Time.Value);
                            } else {
                                await MessageService.WaitUntil(DialoguePlugin.MessageMask, DialoguePlugin.FinishContentWaiting);
                            }
                            State = RenderState.Idle;
                            break;
                        case TextDialogueItem textDialogueItem:
                            State = RenderState.Rendering;
                            PrepareStyle(textDialogueItem);
                            _generator.Text = textDialogueItem.Text;
                            _generator.Reset();
                            while (_generator.MoveNext()) {
                                ShowText(history, _generator.Current);
                                if (timeSpan <= 0.0F) continue;
                                var time = 0.0F;
                                while (time < timeSpan) {
                                    time += Time.deltaTime;
                                    await Dispatcher.NextUpdate();
                                }
                            }
                            State = RenderState.Idle;
                            history = CurrentText;
                            break;
                    }
                }
            } else {
                ShowText(history, new StringBuilder());
            }
            if (!noWait) {
                await MessageService.WaitUntil(DialoguePlugin.MessageMask, DialoguePlugin.FinishContentWaiting);
            }
        }
    }
}