using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public static class Themer
{
	public static void Darkify(Control.ControlCollection container)
	{
        // styles for dark mode
        var BG = Color.FromArgb(32, 32, 32);
        var FG = Color.White;
        var panelBG = Color.FromArgb(28, 28, 28);
        var buttonBG = Color.FromArgb(44, 44, 44);

        var gridSelBG = Color.FromArgb(12, 12, 12);
        var gridGrid = Color.FromArgb(80, 80, 80);

        var buttonBorderSize = 0;
        var buttonFlatStyle = FlatStyle.Flat;
        var textboxBorderStyle = BorderStyle.None;

        foreach (Control c in container) {
            // properties taken from the form designer
            if (c is Panel || c is SplitContainer)
            {
                c.BackColor = panelBG;
                c.ForeColor = FG;
            }
            else if (c is Button b)
            {
                c.BackColor = buttonBG;
                c.ForeColor = FG;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.BorderColor = gridGrid;
                b.FlatAppearance.MouseOverBackColor = gridGrid;
                b.FlatAppearance.MouseDownBackColor = Color.Green;

            }
            else if (c is ComboBox box)
            {
                c.BackColor = BG;
                c.ForeColor = FG;
                box.DropDownStyle = ComboBoxStyle.DropDown;
                box.FlatStyle = FlatStyle.Flat;
            }
            else if (c is TextBox)
            {
                c.BackColor = BG;
                c.ForeColor = FG;
            }
            else if (c is DataGridView view)
            {
                c.BackColor = BG;
                c.ForeColor = FG;
                view.DefaultCellStyle.BackColor = BG;
                view.DefaultCellStyle.SelectionBackColor = gridSelBG;
                view.BackgroundColor = BG;
                view.GridColor = gridGrid;
                view.ColumnHeadersDefaultCellStyle.BackColor = BG;
                view.ColumnHeadersDefaultCellStyle.ForeColor = FG;
                view.EnableHeadersVisualStyles = false;
            }
            else if (c is MenuStrip strip)
            {
                foreach (ToolStripItem item in strip.Items)
                {
                    item.BackColor = BG;
                    item.ForeColor = FG;

                    if (item is ToolStripDropDownItem dropdownItem)
                    {
                        foreach (ToolStripItem subItem in dropdownItem.DropDownItems)
                        {
                            subItem.BackColor = BG;
                            subItem.ForeColor = FG;
                        }
                    }
                }
                c.BackColor = BG;
            }
            else if (c is ContextMenuStrip menu)
            {
                foreach (ToolStripItem item in menu.Items)
                {
                    item.BackColor = BG;
                    item.ForeColor = FG;
                }
            }
            else
            {
                c.BackColor = BG;
                c.ForeColor = FG;
            }
            Darkify(c.Controls);
        }
    }
}
