using System;
using Sandbox.ModAPI;

namespace RichHudFramework.UI
{
    public class TextInput
    {
        private readonly Action<char> OnAppendAction;
        private readonly Action OnBackspaceAction;
        public Func<char, bool> IsCharAllowedFunc;

        public TextInput(Action<char> OnAppendAction, Action OnBackspaceAction,
            Func<char, bool> IsCharAllowedFunc = null)
        {
            this.OnAppendAction = OnAppendAction;
            this.OnBackspaceAction = OnBackspaceAction;
            this.IsCharAllowedFunc = IsCharAllowedFunc;
        }

        private void Backspace()
        {
            OnBackspaceAction?.Invoke();
        }

        public void HandleInput()
        {
            var input = MyAPIGateway.Input.TextInput;

            if (SharedBinds.Back.IsPressedAndHeld || SharedBinds.Back.IsNewPressed)
                Backspace();

            for (var n = 0; n < input.Count; n++)
                if (input[n] != '\b' && (IsCharAllowedFunc == null || IsCharAllowedFunc(input[n])))
                    OnAppendAction?.Invoke(input[n]);
        }
    }
}