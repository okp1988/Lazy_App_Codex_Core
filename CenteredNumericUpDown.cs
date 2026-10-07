namespace Lazy_App_Codex_Core
{
    internal sealed class CenteredNumericUpDown : NumericUpDown
    {
        private bool _centeringEditor;

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (_centeringEditor)
            {
                return;
            }

            base.OnLayout(e);
            CenterEditor();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            CenterEditor();
        }

        protected override void OnTextBoxResize(object? source, EventArgs e)
        {
            // UpDownBase otherwise restores the editor's full height and undoes centering.
            if (!_centeringEditor)
            {
                base.OnTextBoxResize(source, e);
            }
        }

        private void CenterEditor()
        {
            if (_centeringEditor || Controls.OfType<TextBox>().FirstOrDefault() is not TextBox editor)
            {
                return;
            }

            int textHeight = Math.Min(editor.Font.Height, ClientSize.Height);
            if (textHeight <= 0)
            {
                return;
            }

            _centeringEditor = true;
            try
            {
                editor.SetBounds(editor.Left, (ClientSize.Height - textHeight) / 2, editor.Width, textHeight);
            }
            finally
            {
                _centeringEditor = false;
            }
        }
    }
}
