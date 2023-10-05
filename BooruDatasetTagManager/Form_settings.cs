﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BooruDatasetTagManager
{
    public partial class Form_settings : Form
    {
        public Form_settings()
        {
            InitializeComponent();
        }
        private FontSettings gridFontSettings = null;
        private FontSettings autocompleteFontSettings = null;
        private void Form_settings_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = Program.Settings.AvaibleLanguages;
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Code";
            comboBox1.SelectedValue = Program.Settings.TranslationLanguage;
            comboBox2.Items.AddRange(Enum.GetNames(typeof(TranslationService)));
            comboBox2.SelectedItem = Program.Settings.TransService.ToString();
            checkBox1.Checked = Program.Settings.OnlyManualTransInAutocomplete;
            comboAutocompMode.Items.AddRange(Enum.GetNames(typeof(AutocompleteMode)));
            comboAutocompMode.SelectedItem = Program.Settings.AutocompleteMode.ToString();
            comboAutocompSort.Items.AddRange(Enum.GetNames(typeof(AutocompleteSort)));
            comboAutocompSort.SelectedItem = Program.Settings.AutocompleteSort.ToString();
            CheckFixLoad.Checked = Program.Settings.FixTagsOnLoad;
            CheckFixSave.Checked = Program.Settings.FixTagsOnSave;
            textBox1.Text = Program.Settings.SeparatorOnLoad;
            textBox2.Text = Program.Settings.SeparatorOnSave;
            numericUpDown1.Value = Program.Settings.PreviewSize;
            numericUpDown2.Value = Program.Settings.ShowAutocompleteAfterCharCount;
            CheckAskChange.Checked = Program.Settings.AskSaveChanges;
            AutoSortCheckBox.Checked = Program.Settings.AutoSort;
            //UI
            numericUpDown3.Value = Program.Settings.GridViewRowHeight;
            label11.Text = Program.Settings.GridViewFont.ToString();
            gridFontSettings = Program.Settings.GridViewFont;
            label14.Text = Program.Settings.AutocompleteFont.ToString();
            autocompleteFontSettings = Program.Settings.AutocompleteFont;
            LanguageComboBox.Text = Program.Settings.Language;
            //--
            SwitchLanguage();

            renderShortcutsView();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            MessageBox.Show(I18n.GetText("TipSaveSettings"));
            Program.Settings.PreviewSize = (int)numericUpDown1.Value;
            Program.Settings.ShowAutocompleteAfterCharCount = (int)numericUpDown2.Value;
            Program.Settings.TranslationLanguage = (string)comboBox1.SelectedValue;
            Program.Settings.TransService = (TranslationService)Enum.Parse(typeof(TranslationService), comboBox2.SelectedItem.ToString(), true);
            Program.Settings.OnlyManualTransInAutocomplete = checkBox1.Checked;
            switch (Program.Settings.Language)
            {
                default:
                case "en-US":
                    Program.Settings.AutocompleteMode = (AutocompleteMode)Enum.Parse(typeof(AutocompleteMode), comboAutocompMode.SelectedItem.ToString(), true);
                    Program.Settings.AutocompleteSort = (AutocompleteSort)Enum.Parse(typeof(AutocompleteSort), comboAutocompSort.SelectedItem.ToString(), true);
                    break;
                case "zh-CN":
                    Program.Settings.AutocompleteMode = (AutocompleteMode)Enum.Parse(typeof(AutocompleteMode_ZH_CN), comboAutocompMode.SelectedItem.ToString(), true);
                    Program.Settings.AutocompleteSort = (AutocompleteSort)Enum.Parse(typeof(AutocompleteSort_ZH_CN), comboAutocompSort.SelectedItem.ToString(), true);
                    break;
            }
            Program.Settings.FixTagsOnLoad = CheckFixLoad.Checked;
            Program.Settings.FixTagsOnSave = CheckFixSave.Checked;
            Program.Settings.SeparatorOnLoad = textBox1.Text;
            Program.Settings.SeparatorOnSave = textBox2.Text;
            Program.Settings.AskSaveChanges = CheckAskChange.Checked;
            Program.Settings.AutoSort = AutoSortCheckBox.Checked;
            //UI
            Program.Settings.GridViewRowHeight = (int)numericUpDown3.Value;
            Program.Settings.GridViewFont = gridFontSettings;
            Program.Settings.AutocompleteFont = autocompleteFontSettings;
            Program.Settings.Language = (string)LanguageComboBox.SelectedItem;
            Program.Settings.SaveSettings();
            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void BtnGridviewFontChange_Click(object sender, EventArgs e)
        {
            FontDialog dialog = new FontDialog();
            dialog.Font = gridFontSettings.GetFont();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                gridFontSettings = FontSettings.Create(dialog.Font);
                label11.Text = gridFontSettings.ToString();
            }
            dialog.Dispose();
        }

        private void BtnAutocompFontChange_Click(object sender, EventArgs e)
        {
            FontDialog dialog = new FontDialog();
            dialog.Font = autocompleteFontSettings.GetFont();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                autocompleteFontSettings = FontSettings.Create(dialog.Font);
                label14.Text = autocompleteFontSettings.ToString();
            }
            dialog.Dispose();
        }

        private void SwitchLanguage()
        {
            this.Text = I18n.GetText("MenuLabelSettings");
            SettingFrame.Controls[0].Text = I18n.GetText("SettingTabGeneral");
            SettingFrame.Controls[1].Text = I18n.GetText("SettingTabUI");
            SettingFrame.Controls[2].Text = I18n.GetText("SettingTabTranslations");
            LabelPreviewImageSize.Text = I18n.GetText("SettingPreviewImageSize");
            LabelAutocompMode.Text = I18n.GetText("SettingAutocompMode");
            LabelAutocompFont.Text = I18n.GetText("SettingAutocompFont");
            LabelAutocompSort.Text = I18n.GetText("SettingAutocompSort");
            LabelAutocompAfter.Text = I18n.GetText("SettingAutocompPrefix");
            LabelChars.Text = I18n.GetText("SettingAutocompChars");
            LabelLanguage.Text = I18n.GetText("SettingUILanguage");
            LabelSeparatorLoad.Text = I18n.GetText("SettingSeperatorLoad");
            LabelSeparatorSave.Text = I18n.GetText("SettingSeperatorSave");
            LabelTagFont.Text = I18n.GetText("SettingUITagFont");
            LabelTagHeight.Text = I18n.GetText("SettingUIRowHeight");
            CheckAskChange.Text = I18n.GetText("SettingPromptToSave");
            CheckFixSave.Text = I18n.GetText("SettingFixTagLoad");
            CheckFixLoad.Text = I18n.GetText("SettingFixTagSave");
            AutoSortCheckBox.Text = I18n.GetText("SettingAutoSortCheck");
            BtnSave.Text = I18n.GetText("SettingBtnSave");
            BtnCancel.Text = I18n.GetText("SettingBtnCancel");
            BtnGridviewFontChange.Text = I18n.GetText("SettingBtnChange");
            BtnAutocompFontChange.Text = I18n.GetText("SettingBtnChange");

            comboAutocompMode.Items.Clear();
            comboAutocompSort.Items.Clear();
            var defaultAutocompMode = Enum.GetNames(typeof(AutocompleteMode));
            var defaultAutocompSort = Enum.GetNames(typeof(AutocompleteSort));
            switch (Program.Settings.Language)
            {
                default:
                case "en-US":
                    comboAutocompMode.Items.AddRange(defaultAutocompMode);
                    comboAutocompSort.Items.AddRange(defaultAutocompSort);
                    comboAutocompMode.SelectedItem = Enum.GetName(typeof(AutocompleteMode), Enum.ToObject(typeof(AutocompleteMode), Program.Settings.AutocompleteMode));
                    comboAutocompSort.SelectedItem = Enum.GetName(typeof(AutocompleteSort), Enum.ToObject(typeof(AutocompleteSort), Program.Settings.AutocompleteSort));
                    break;
                case "zh-CN":
                    defaultAutocompMode = Enum.GetNames(typeof(AutocompleteMode_ZH_CN));
                    defaultAutocompSort = Enum.GetNames(typeof(AutocompleteSort_ZH_CN));
                    comboAutocompMode.Items.AddRange(defaultAutocompMode);
                    comboAutocompSort.Items.AddRange(defaultAutocompSort);
                    comboAutocompMode.SelectedItem = Enum.GetName(typeof(AutocompleteMode_ZH_CN), Enum.ToObject(typeof(AutocompleteMode_ZH_CN), Program.Settings.AutocompleteMode));
                    comboAutocompSort.SelectedItem = Enum.GetName(typeof(AutocompleteSort_ZH_CN), Enum.ToObject(typeof(AutocompleteSort_ZH_CN), Program.Settings.AutocompleteSort));
                    break;
            }
        }

        private void renderShortcutsView()
        {
            // initialize layout panel
            var tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Padding = new Padding(0, 20, 0, 0);  // add some vertical top padding to create some "air"
            tableLayoutPanel.ColumnStyles.Clear();
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            tabPageShortcuts.Controls.Add(tableLayoutPanel);

            // dynamically create a list of all the shortcuts, with user configurability for each.
            var it = Program.Shortcuts.KeyBindings.GetEnumerator();
            int row = 0;
            while (it.MoveNext()) {
                var keyCode = it.Current.Value;
                var commandName = it.Current.Key;
                var commandDesc = Program.Shortcuts.FindCommandByName(commandName).Description;

                var commandNameLabel = new Label { Text = commandName };
                var toolTip = new System.Windows.Forms.ToolTip();
                toolTip.SetToolTip(commandNameLabel, commandDesc);

                var keyCodeEdit = new System.Windows.Forms.TextBox { Text = KeyCodeConverter.StringEncode(keyCode), Width = 200 };
                keyCodeEdit.KeyDown += (sender, e) =>
                {
                    // whisp the event under the rug, as to trigger as few unintended side-effects as possible (in case the app has some other key-listener code).
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    // Don't allow binding pure meta-keys. Will definitely interfere with other code, plus looks confusing in the settings UI (eg Ctrl+Control !?!)
                    if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ControlKey) return;

                    Keys newKeyCode = e.KeyCode;
                    if (e.Control) newKeyCode |= Keys.Control;
                    if (e.Alt) newKeyCode |= Keys.Alt;
                    if (e.Shift) newKeyCode |= Keys.Shift;

                    Program.Settings.KeyBindings.UpdateShortcut(commandName, newKeyCode);
                    keyCodeEdit.Text = KeyCodeConverter.StringEncode(newKeyCode);
                };

                tableLayoutPanel.Controls.Add(commandNameLabel, 0, row);
                tableLayoutPanel.Controls.Add(keyCodeEdit, 1, row);
                row++;
            }
        }
    }
}
