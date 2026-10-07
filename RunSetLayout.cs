namespace Lazy_App_Codex_Core
{
    // Shared by the main window and isolated diagnostics so checks use the real composition.
    internal static class RunSetLayout
    {
        internal const int Gap = 12;

        internal static Size ClientSize(bool dual) =>
            new(dual ? RunSetControl.FixedWidth * 2 + Gap + 12 : RunSetControl.FixedWidth + 12,
                RunSetControl.FixedHeight + 8);

        internal static void Configure(TableLayoutPanel layout, RunSetControl first, RunSetControl second)
        {
            layout.ColumnCount = 3;
            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RunSetControl.FixedWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Gap));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RunSetControl.FixedWidth));
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Clear();
            layout.Controls.Add(first, 0, 0);
            layout.Controls.Add(second, 2, 0);
        }

        internal static void SetSecondColumnWidths(TableLayoutPanel layout, bool visible)
        {
            layout.ColumnStyles[1].Width = visible ? Gap : 0F;
            layout.ColumnStyles[2].Width = visible ? RunSetControl.FixedWidth : 0F;
        }
    }
}
