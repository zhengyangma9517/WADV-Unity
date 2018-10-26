﻿namespace Assets.Core.VisualNovel.Script {
    public enum TokenType {
        CommandStart,
        CommandNameStart,
        CommandNameEnd,
        CommandFunctionStart,
        CommandFunctionEnd,
        CommandParam,
        CommandEnd,
        DialogueStart,
        DialogueSpeaker,
        DialogueNameStart,
        DialogueNameEnd,
        DialogueContentStart,
        DialogueContentEnd,
        DialogueEnd,
        VariableStart,
        VariableEnd,
        VariableChildPicker,
        Equals,
        Minus,
        Add,
        Mod,
        Divide,
        Multiple,
        BracketStart,
        BracketEnd,
        True,
        False,
        Nothing,
        String,
        Number
    }
}
