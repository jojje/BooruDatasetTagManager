﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Translator;
using static BooruDatasetTagManager.DatasetManager;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BooruDatasetTagManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Themer.Darkify(this.Controls);
            tagsBuffer = new List<string>();
            gridViewTags.CellValueChanged += DataGridView1_CellValueChanged;
            gridViewTags.RowsAdded += DataGridView1_RowsAdded;
            gridViewTags.RowsRemoved += DataGridView1_RowsRemoved;
            previewPicBox = new PictureBox();
            previewPicBox.Name = "previewPicBox";
            allTagsFilter = new Form_filter();
            switchLanguage();
            Program.KeyBinder.BindKeyEvents(this);  // allows shortcuts to be triggered on this form
        }

        private void DataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            SetChangedStatus(true);
        }

        private void DataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            SetChangedStatus(true);
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            SetChangedStatus(true);
        }
        private Form_filter allTagsFilter;
        List<string> tagsBuffer;

        private bool isAllTags = true;
        private bool isTranslate = false;
        private bool isFiltered = false;
        private bool showCount = false;

        private Form_preview fPreview;
        private bool isShowPreview = false;
        private PictureBox previewPicBox;
        private int previewRowIndex = -1;
        private FilterType filterAnd = FilterType.Or;
        private int lastGridViewTagsHash = -1;
        private bool isLoading = false;
        private List<string> selectedFiles = new List<string>();

        private bool isCtrlOrShiftPressed = false;
        private bool needReloadTags = false;


        Dictionary<string, string> Trans = new Dictionary<string, string>();

        private void Form1_Load(object sender, EventArgs e)
        {
            Text += " " + Application.ProductVersion;
            gridViewDS.RowTemplate.Height = Program.Settings.PreviewSize + 10;
            gridViewAllTags.RowTemplate.Height = Program.Settings.GridViewRowHeight;
            gridViewTags.RowTemplate.Height = Program.Settings.GridViewRowHeight;
            gridViewTags.DefaultCellStyle.Font = Program.Settings.GridViewFont.GetFont();
            gridViewAllTags.DefaultCellStyle.Font = Program.Settings.GridViewFont.GetFont();
            gridViewDS.DefaultCellStyle.Font = Program.Settings.GridViewFont.GetFont();
            splitContainer2.SplitterDistance = Width / 3;
            promptFixedLengthComboBox.SelectedIndex = 0;
        }

        private void SetChangedStatus(bool changed)
        {
            BtnTagApply.Enabled = changed;
            BtnTagReset.Enabled = changed;
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.DataManager != null)
            {
                saveAllChangesToolStripMenuItem_Click(sender, e);
            }
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() != DialogResult.OK)
                return;
            isLoading = true;
            Program.DataManager = new DatasetManager();
            if (!Program.DataManager.LoadFromFolder(openFolderDialog.Folder, Program.Settings.FixTagsOnLoad))
            {
                SetStatus(I18n.GetText("TipFolderWrong"));
                return;
            }

            gridViewDS.DataSource = Program.DataManager.GetDataSource();
            Program.DataManager.UpdateData();
            BindTagList();
            ApplyDataSetGridStyle();
            isLoading = false;
            gridViewDS.AutoResizeColumns();
        }

        private async Task FillTranslation(DataGridView grid)
        {
            LockEdit(true);
            SetStatus(I18n.GetText("StatusTranslating"));
            try
            {
                HttpClient client = new HttpClient();
                for (int i = 0; i < grid.RowCount; i++)
                {
                    SetStatus($"{I18n.GetText("SettingTabTranslations")} {i}/{grid.RowCount}");
                    grid["Translation", i].ReadOnly = true;
                    grid["Translation", i].Value = await Program.TransManager.TranslateAsync(grid[0, i].Value as string);
                }
            }
            catch (Exception ex)
            {

            }
            SetStatus(I18n.GetText("StatusTranslationComplete"));
            LockEdit(false);
        }

        private void LockEdit(bool locked)
        {
            toolStrip2.Enabled = !locked;
            toolStrip1.Enabled = !locked;
            gridViewTags.Enabled = !locked;
            if (gridViewTags.SelectedRows.Count == 1)
                gridViewTags.AllowDrop = !locked;
            gridViewAllTags.Enabled = !locked;
            gridViewDS.Enabled = !locked;
        }

        private void ShowPreview(string img)
        {
            if (fPreview == null || fPreview.IsDisposed)
                fPreview = new Form_preview();
            fPreview.Show(img);
        }

        private void HidePreview()
        {
            fPreview?.Hide();
        }

        private async void LoadSelectedImageToGrid()
        {
            if (gridViewDS.SelectedRows.Count == 0)
                return;
            if (gridViewDS.SelectedRows.Count == 1)
            {
                gridViewTags.AllowDrop = true;
                gridViewTags.Rows.Clear();
                ChageImageColumn(false);
                List<string> tags = Program.DataManager.DataSet[(string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value].Tags;
                gridViewTags.Tag = (string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value;
                //gridViewTags.Columns["ImageTags"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (gridViewTags.Columns.Contains("Translation"))
                {
                    gridViewTags.Columns["Translation"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    gridViewTags.Columns["Translation"].ReadOnly = true;
                }
                foreach (var item in tags)
                    gridViewTags.Rows.Add(item);
                if (isShowPreview)
                {
                    ShowPreview((string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value);
                }
            }
            else
            {
                if (isShowPreview)
                {
                    HidePreview();
                }
                gridViewTags.AllowDrop = false;
                gridViewTags.Rows.Clear();
                ChageImageColumn(true);
                //gridViewTags.Columns["ImageTags"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                if (gridViewTags.Columns.Contains("Translation"))
                {
                    gridViewTags.Columns["Translation"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    gridViewTags.Columns["Translation"].ReadOnly = true;
                }
                gridViewTags.Tag = "0";
                Dictionary<string, List<DataItem>> table = new Dictionary<string, List<DataItem>>();
                List<DataItem> selectedTagsList = new List<DataItem>();
                for (int i = 0; i < gridViewDS.SelectedRows.Count; i++)
                {
                    selectedTagsList.Add(Program.DataManager.DataSet[(string)gridViewDS.SelectedRows[i].Cells["ImageFilePath"].Value]);
                }

                int maxCount = selectedTagsList.Max(a => a.Tags.Count);

                for (int i = 0; i < maxCount; i++)
                {
                    for (int j = 0; j < selectedTagsList.Count; j++)
                    {
                        var curTags = selectedTagsList[j];
                        if (i < curTags.Tags.Count)
                        {
                            if (table.ContainsKey(curTags.Tags[i]))
                            {
                                table[curTags.Tags[i]].Add(curTags);
                            }
                            else
                            {
                                table.Add(curTags.Tags[i], new List<DataItem>() { curTags });
                            }
                        }
                    }
                }
                foreach (var item in table)
                {
                    item.Value.Sort((x, y) => x.Name.CompareTo(y.Name));
                    DataGridViewRow[] rows = new DataGridViewRow[item.Value.Count];
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        DataGridViewRow row = new DataGridViewRow();
                        row.CreateCells(gridViewTags);
                        row.Tag = item.Key;//tag
                        row.Cells["ImageTags".IdxFromName(gridViewTags)].Value = i == 0 ? item.Key : "";//tag
                        row.Cells["ImageTags".IdxFromName(gridViewTags)].Tag = item.Value[i];//tagItem
                        row.Cells["Image".IdxFromName(gridViewTags)].Value = item.Value[i].ImageFilePath;//ImgName
                        row.Cells["Image".IdxFromName(gridViewTags)].Tag = item.Key;//tag
                        row.Cells["Name".IdxFromName(gridViewTags)].Value = item.Value[i].Name;//ImgName
                        row.Cells["Name".IdxFromName(gridViewTags)].Tag = item.Key;//tag
                        rows[i] = row;
                    }
                    gridViewTags.Rows.AddRange(rows);
                }
            }

            if (Program.Settings.AutoSort)
            {
                SortPrompt();
            }

            gridViewDS.Focus();
            if (isTranslate)
                await FillTranslation(gridViewTags);
            if (showCount)
                UpdateTagCount();
            SetChangedStatus(false);
        }

        /// <summary>
        /// Add or remove Image column
        /// </summary>
        /// <param name="add"> true to add, false to remove</param>
        private void ChageImageColumn(bool add)
        {
            if (gridViewTags.Columns.Contains("Image"))
            {
                if (!add)
                {
                    gridViewTags.Columns.Remove("Image");
                    gridViewTags.Columns.Remove("Name");
                }
            }
            else
            {
                if (add)
                {
                    gridViewTags.Columns.Add("Image", "Image");
                    gridViewTags.Columns["Image"].Visible = false;
                    gridViewTags.Columns.Add("Name", "Name");
                    gridViewTags.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    gridViewTags.Columns["ImageTags"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
        }

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;
        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {

                    // Proceed with the drag and drop, passing in the list item.                    
                    DragDropEffects dropEffect = gridViewTags.DoDragDrop(
                    gridViewTags.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
                }
            }
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            rowIndexFromMouseDown = gridViewTags.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred. 
                // The DragSize indicates the size that the mouse can move 
                // before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                    dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            Point clientPoint = gridViewTags.PointToClient(new Point(e.X, e.Y));

            // Get the row index of the item the mouse is below. 
            rowIndexOfItemUnderMouseToDrop =
                gridViewTags.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            // If the drag operation was a move then remove and insert the row.
            if (e.Effect == DragDropEffects.Move)
            {
                if (rowIndexFromMouseDown != rowIndexOfItemUnderMouseToDrop)
                {
                    DataGridViewRow rowToMove = e.Data.GetData(
                        typeof(DataGridViewRow)) as DataGridViewRow;
                    gridViewTags.Rows.RemoveAt(rowIndexFromMouseDown);
                    int toDrop = rowIndexOfItemUnderMouseToDrop;
                    if (toDrop < 0 || toDrop > gridViewTags.Rows.Count)
                    {
                        toDrop = gridViewTags.Rows.Count;
                    }

                    gridViewTags.Rows.Insert(toDrop, rowToMove);
                    gridViewTags.ClearSelection();
                    gridViewTags[0, toDrop].Selected = true;
                }
            }
        }

        private void BtnAddTag_Clicked(object sender, EventArgs e)
        {
            AddNewRow();
        }

        internal void AddNewRow()
        {
            if (gridViewDS.SelectedRows.Count > 1)
            {
                //MessageBox.Show("Adding tags does not support multiple selection. Choose one image.");
                //return;
                using (Form_addTag addTag = new Form_addTag())
                {
                    addTag.comboBox1.Enabled = false;
                    if (addTag.ShowDialog() == DialogResult.OK)
                    {
                        AddTagMultiselectedMode(addTag.tagTextBox.Text);
                    }
                    addTag.Close();
                }
            }
            else
            {
                int newRowOffset = 0;
                if (gridViewTags.SelectedCells.Count == 0 || gridViewTags.RowCount == 0)
                    gridViewTags.Rows.Add();
                else
                {
                    gridViewTags.Rows.Insert(gridViewTags.SelectedCells[0].RowIndex + 1);
                    newRowOffset++;
                }
                // enter edit mode automatically when adding a new tag
                var newRow = gridViewTags.Rows[gridViewTags.SelectedCells[0].RowIndex + newRowOffset];
                gridViewTags.CurrentCell = newRow.Cells[0];
                gridViewTags.BeginEdit(true);
            }
        }

        private void BtnTagDelete_Click(object sender, EventArgs e)
        {
            DeleteTag();
        }

        internal void DeleteTag()
        {
            if (gridViewTags.SelectedCells.Count == 0)
                return;
            gridViewTags.Rows.RemoveAt(gridViewTags.SelectedCells[0].RowIndex);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (gridViewTags.SelectedCells.Count == 0 || gridViewTags.SelectedCells[0].RowIndex == 0)
                return;
            int curIndex = gridViewTags.SelectedCells[0].RowIndex;
            string upperValue = (string)gridViewTags["ImageTags", curIndex - 1].Value;
            if (isTranslate)
            {
                string upperValueTrans = (string)gridViewTags["Translation", curIndex - 1].Value;
                gridViewTags["Translation", curIndex - 1].Value = gridViewTags[1, curIndex].Value;
                gridViewTags["Translation", curIndex].Value = upperValueTrans;
            }
            if (showCount)
                UpdateTagCount();
            gridViewTags["ImageTags", curIndex - 1].Value = gridViewTags["ImageTags", curIndex].Value;
            gridViewTags["ImageTags", curIndex].Value = upperValue;
            gridViewTags.ClearSelection();
            gridViewTags["ImageTags", curIndex - 1].Selected = true;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            if (gridViewTags.SelectedCells.Count == 0 || gridViewTags.SelectedCells[0].RowIndex == gridViewTags.RowCount - 1)
                return;
            int curIndex = gridViewTags.SelectedCells[0].RowIndex;
            string lowerValue = (string)gridViewTags["ImageTags", curIndex + 1].Value;

            if (isTranslate)
            {
                string lowerValueTrans = (string)gridViewTags["Translation", curIndex + 1].Value;
                gridViewTags["Translation", curIndex + 1].Value = gridViewTags[1, curIndex].Value;
                gridViewTags["Translation", curIndex].Value = lowerValueTrans;
            }
            if (showCount)
                UpdateTagCount();

            gridViewTags["ImageTags", curIndex + 1].Value = gridViewTags[0, curIndex].Value;
            gridViewTags["ImageTags", curIndex].Value = lowerValue;
            gridViewTags.ClearSelection();
            gridViewTags["ImageTags", curIndex + 1].Selected = true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ApplyTagsChanges();
        }

        internal void ApplyTagsChanges()
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            if ((string)gridViewTags.Tag != "0")
            {
                List<string> nTags = new List<string>();
                for (int i = 0; i < gridViewTags.RowCount; i++)
                {
                    nTags.Add((string)gridViewTags["ImageTags", i].Value);
                }
                Program.DataManager.DataSet[(string)gridViewTags.Tag].Tags = nTags;
            }
            else
            {
                Dictionary<string, List<string>> nTagsList = new Dictionary<string, List<string>>();
                for (int i = 0; i < gridViewTags.RowCount; i++)
                {
                    string tag = (string)gridViewTags["Image", i].Tag;
                    string img = (string)gridViewTags["Image", i].Value;
                    if (string.IsNullOrEmpty(img))
                        throw new Exception("Image file name is empty!");
                    if (string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty((string)gridViewTags["ImageTags", i].Value))
                        throw new NotImplementedException();
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;
                    if (nTagsList.ContainsKey(img))
                        nTagsList[img].Add(tag);
                    else
                        nTagsList.Add(img, new List<string>() { tag });
                }
                foreach (var item in nTagsList)
                {
                    Program.DataManager.DataSet[item.Key].Tags = item.Value;
                }
            }
            Program.DataManager.UpdateData();
            BindTagList();
            SetChangedStatus(false);
            lastGridViewTagsHash = GetgridViewTagsHash();
            SetStatus("Saved");
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            isAllTags = !isAllTags;
            if (isAllTags)
                LabelAllTags.Text = I18n.GetText("UILabelAllTags");
            else
                LabelAllTags.Text = I18n.GetText("UILabelCommonTags");
            BindTagList();
        }

        private void BindTagList()
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            if (isAllTags)
            {
                BingSourceToDGV(gridViewAllTags, Program.DataManager.AllTags);
            }
            else
            {
                BingSourceToDGV(gridViewAllTags, Program.DataManager.CommonTags);
            }
            gridViewAllTags.Columns["Tag"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private async void BingSourceToDGV(DataGridView dgv, List<TagValue> source)
        {
            var scroll = dgv.FirstDisplayedScrollingRowIndex;
            var all = GetSelectedTags();
            dgv.Rows.Clear();
            if (dgv.Columns.Count == 0)
                dgv.Columns.Add("Tag", "Tag");
            foreach (var item in source)
            {
                int row = dgv.Rows.Add(item.Tag);
                if (row == 0)
                    dgv.Rows[row].Selected = false;
                if (all.Contains(item.Tag))
                    dgv.Rows[row].Selected = true;
            }

            if (scroll >= dgv.RowCount)
            {
                scroll = dgv.Rows.Count - 1;
            }
            if (scroll != -1)
            {
                dgv.FirstDisplayedScrollingRowIndex = scroll;
            }
            if (isTranslate)
            {
                await FillTranslation(dgv);
            }

            if (showCount)
                UpdateTagCount();
        }

        private void BtnAddTagForAll_Click(object sender, EventArgs e)
        {
            AddTagToAll(false);
        }

        private async void AddTagToAll(bool filtered)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            Form_addTag addTag = new Form_addTag();
            int index = gridViewAllTags.RowCount;
            if (gridViewAllTags.SelectedCells.Count > 0)
            {
                index = gridViewAllTags.SelectedCells[0].RowIndex;
                addTag.tagTextBox.Text = (string)gridViewAllTags.Rows[index].Cells[0].Value;
                addTag.tagTextBox.SelectAll();
            }
            if (addTag.ShowDialog() == DialogResult.OK)
            {
                int customIndex = (int)addTag.numericUpDown1.Value;

                DatasetManager.AddingType addType = (DatasetManager.AddingType)Enum.Parse(typeof(DatasetManager.AddingType), (string)addTag.comboBox1.SelectedItem);
                Program.DataManager.AddTagToAll(addTag.tagTextBox.Text, addType, customIndex, filtered);
                Program.DataManager.UpdateData();
                int valIndex = IndexOfValueInGrig(gridViewTags, "ImageTags", addTag.tagTextBox.Text);
                if (gridViewDS.SelectedRows.Count == 1)
                {
                    if (valIndex != -1)
                    {
                        gridViewTags.Rows.RemoveAt(valIndex);
                    }
                    int insertIndex = 0;
                    switch (addType)
                    {
                        case DatasetManager.AddingType.Top:
                            {
                                insertIndex = 0;
                                break;
                            }
                        case DatasetManager.AddingType.Center:
                            {
                                insertIndex = gridViewTags.RowCount / 2;
                                break;
                            }
                        case DatasetManager.AddingType.Down:
                            {
                                insertIndex = gridViewTags.RowCount;
                                break;
                            }
                        case DatasetManager.AddingType.Custom:
                            {
                                if (customIndex >= gridViewTags.RowCount)
                                {
                                    insertIndex = gridViewTags.RowCount;
                                }
                                else if (customIndex < 0)
                                {
                                    insertIndex = 0;
                                }
                                else
                                    insertIndex = customIndex;
                                break;
                            }
                    }
                    gridViewTags.Rows.Insert(insertIndex, addTag.tagTextBox.Text);
                    string transString = null;
                    if (isTranslate)
                    {
                        transString = await Program.TransManager.TranslateAsync(addTag.tagTextBox.Text);
                        gridViewTags.Rows[insertIndex].Cells[1].Value = transString;
                    }

                    if (showCount)
                        UpdateTagCount();

                    var allIndex = IndexOfValueInGrig(gridViewAllTags, "Tag", addTag.tagTextBox.Text);
                    if (allIndex == -1)
                    {
                        gridViewAllTags.Rows.Insert(index, 1);
                        gridViewAllTags.Rows[index].Cells[0].Value = addTag.tagTextBox.Text;
                        if (isTranslate)
                        {
                            gridViewAllTags.Rows[index].Cells[1].Value = transString;
                        }

                        if (showCount)
                            UpdateTagCount();
                    }
                }
                else
                {
                    AddTagMultiselectedMode(addTag.tagTextBox.Text);
                }
                //BindTagList();
            }
            addTag.Close();
        }

        private int IndexOfValueInGrig(DataGridView gridView, string colName, string value)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                if (gridView[colName, i].Value != DBNull.Value)
                {
                    if ((string)gridView[colName, i].Value == value)
                        return i;
                }
                else if (value == null)
                    return i;
            }
            return -1;
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (gridViewDS.SelectedRows.Count != 1)
            {
                MessageBox.Show("Replace does not support multiple selection. Choose one image.");
                return;
            }

            if (gridViewAllTags.SelectedCells.Count == 0)
                return;
            Form_replaceAll replaceAll = new Form_replaceAll();
            replaceAll.comboBox1.DataSource = Program.DataManager.AllTags;
            replaceAll.comboBox1.DisplayMember = "Tag";
            replaceAll.comboBox1.SelectedIndex = gridViewAllTags.SelectedCells[0].RowIndex;
            replaceAll.comboBox2.Items.AddRange(Program.DataManager.AllTags.Select(a => a.Tag).ToArray());
            if (replaceAll.ShowDialog() == DialogResult.OK)
            {
                Program.DataManager.ReplaceTagInAll(((TagValue)replaceAll.comboBox1.SelectedItem).Tag, (string)replaceAll.comboBox2.Text, true);
                Program.DataManager.UpdateData();
                int indexToReplace = -1;
                int indexToDelete = -1;
                for (int i = 0; i < gridViewTags.RowCount; i++)
                {
                    string srcText = (string)gridViewTags[0, i].Value;
                    if (srcText == (string)replaceAll.comboBox2.Text)
                        indexToDelete = i;
                    else if (srcText == ((TagValue)replaceAll.comboBox1.SelectedItem).Tag)
                        indexToReplace = i;
                }
                if (indexToReplace != -1)
                {
                    gridViewTags[0, indexToReplace].Value = (string)replaceAll.comboBox2.Text;
                    if (indexToDelete != -1)
                        gridViewTags.Rows.RemoveAt(indexToDelete);
                }
            }
            replaceAll.Close();
            BindTagList();
        }

        private void saveAllChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            ApplyTagsChanges();
            Program.DataManager.SaveAll(Program.Settings.FixTagsOnSave);
            Program.DataManager.UpdateDatasetHash();
            SetStatus(I18n.GetText("StatusSaved"));
        }

        private void showPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            isShowPreview = !isShowPreview;
            showPreviewToolStripMenuItem.Checked = isShowPreview;
            if (isShowPreview)
            {
                if (gridViewDS.SelectedRows.Count == 1)
                {
                    ShowPreview((string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value);
                }
                else
                {
                    HidePreview();
                }
            }
            else
            {
                HidePreview();
            }
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (gridViewDS.SelectedRows.Count == 1)
            {
                tagsBuffer.Clear();
                for (int i = 0; i < gridViewTags.RowCount; i++)
                {
                    tagsBuffer.Add((string)gridViewTags["ImageTags", i].Value);
                }
                SetStatus(I18n.GetText("StatusCopied"));
            }
            else if (gridViewDS.SelectedRows.Count > 1)
            {
                MessageBox.Show(I18n.GetText("TipMultiImageCopy"));
            }
            else
            {
                MessageBox.Show(I18n.GetText("TipSelectImage"));
            }
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private async void BtnPasteTag_Click(object sender, EventArgs e)
        {
            if (gridViewDS.SelectedRows.Count == 1)
            {
                gridViewTags.Rows.Clear();
                for (int i = 0; i < tagsBuffer.Count; i++)
                {
                    gridViewTags.Rows.Add(tagsBuffer[i]);
                }
                if (isTranslate)
                    await FillTranslation(gridViewTags);
                if (showCount)
                    UpdateTagCount();
                SetStatus(I18n.GetText("StatusPasted"));
            }
            else if (gridViewDS.SelectedRows.Count > 1)
            {
                MessageBox.Show(I18n.GetText("TipMultiImagePaste"));
            }
            else
            {
                MessageBox.Show(I18n.GetText("TipSelectImage"));
            }
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            LoadSelectedImageToGrid();
            lastGridViewTagsHash = GetgridViewTagsHash();
        }

        private void BtnDeleteTagForAll_Click(object sender, EventArgs e)
        {
            RemoveTagFromAll(false);
        }

        private void RemoveTagFromAll(bool filtered)
        {

            List<KeyValuePair<int, string>> tagsToDel = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < gridViewAllTags.SelectedCells.Count; i++)
            {
                var row = gridViewAllTags.SelectedCells[i].RowIndex;
                tagsToDel.Add(new KeyValuePair<int, string>(row, (string)gridViewAllTags.Rows[row].Cells[0].Value));
            }

            tagsToDel.Sort((a, b) => b.Key.CompareTo(a.Key));

            foreach (var item in tagsToDel)
            {
                Program.DataManager.DeleteTagFromAll(item.Value, filtered);
                RemoveTagFromImageTags(item.Value);
                if (!Program.DataManager.AllTags.Exists(a => a.Tag == item.Value))
                    gridViewAllTags.Rows.RemoveAt(item.Key);
            }
            Program.DataManager.UpdateData();
        }

        private async void translateTagsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isTranslate = !isTranslate;
            MenuItemTranslateTags.Checked = isTranslate;
            if (isTranslate)
            {
                gridViewAllTags.Columns.Insert(1, new DataGridViewTextBoxColumn()
                {
                    Name = "Translation",
                    HeaderText = "Translation",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });
                gridViewTags.Columns.Insert(1, new DataGridViewTextBoxColumn()
                {
                    Name = "Translation",
                    HeaderText = "Translation",
                    ReadOnly = true,
                    AutoSizeMode = gridViewTags.Columns.Contains("Image") ? DataGridViewAutoSizeColumnMode.AllCellsExceptHeader : DataGridViewAutoSizeColumnMode.Fill
                });
                await FillTranslation(gridViewAllTags);
                await FillTranslation(gridViewTags);
            }
            else
            {
                gridViewAllTags.Columns.Remove("Translation");
                gridViewTags.Columns.Remove("Translation");
            }
        }

        //private int findIndex = -1;
        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            SetFilter();
        }

        private HashSet<string> GetSelectedTags()
        {
            HashSet<string> findTags = new HashSet<string>();
            for (int i = 0; i < gridViewAllTags.SelectedCells.Count; i++)
            {
                int row = gridViewAllTags.SelectedCells[i].RowIndex;
                string value = (string)gridViewAllTags.Rows[row].Cells[0].Value;
                if (!findTags.Contains(value))
                    findTags.Add(value);
            }
            return findTags;
        }

        private void SaveSelectedInViewDs()
        {
            selectedFiles.Clear();
            for (int i = 0; i < gridViewDS.SelectedRows.Count; i++)
            {
                selectedFiles.Add((string)gridViewDS.SelectedRows[i].Cells["ImageFilePath"].Value);
            }
        }

        private void LoadSelectedInViewDs()
        {
            gridViewDS.ClearSelection();
            bool foundSelected = false;
            int firstDisplayed = 0;
            for (int i = 0; i < gridViewDS.RowCount; i++)
            {
                if (selectedFiles.Contains((string)gridViewDS["ImageFilePath", i].Value))
                {
                    if (firstDisplayed == 0)
                        firstDisplayed = i;
                    gridViewDS.Rows[i].Selected = true;
                    foundSelected = true;
                }
            }
            if (!foundSelected && gridViewDS.RowCount > 0)
            {
                gridViewDS.Rows[0].Selected = true;
            }
            // Will throw an exception by itself if there is nothing found due to being set to -1 internally when the list is loaded in empty. Lazy bypass of 
            try
            {
                gridViewDS.FirstDisplayedScrollingRowIndex = firstDisplayed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void SetFilter()
        {
            isLoading = true;
            if (gridViewAllTags.SelectedCells.Count > 0)
            {
                SaveSelectedInViewDs();
                if (isFiltered)
                {
                    ResetFilter();
                }

                gridViewDS.DataSource = Program.DataManager.GetDataSource(DatasetManager.OrderType.Name, filterAnd, GetSelectedTags());
                if (gridViewDS.RowCount == 0)
                    gridViewTags.Rows.Clear();
                isFiltered = true;
                LoadSelectedInViewDs();
                BtnImageExitFilter.Enabled = true;
            }
            isLoading = false;
        }

        private void ResetFilter()
        {
            isLoading = true;
            if (isFiltered)
            {
                SaveSelectedInViewDs();
                gridViewDS.DataSource = Program.DataManager.GetDataSource();
                isFiltered = false;
                BtnImageExitFilter.Enabled = false;
                LoadSelectedInViewDs();
            }
            isLoading = false;
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            ResetFilter();
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                BtnTagDelete.PerformClick();
            }
            else if (e.KeyCode == Keys.Insert)
            {
                BtnTagAdd.PerformClick();
            }
        }

        private void loadLossFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            Program.DataManager.LoadLossFromFile(openFileDialog.FileName);
            gridViewDS.DataSource = Program.DataManager.GetDataSource();
        }

        private async void toolStripButton15_Click(object sender, EventArgs e)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            if (Clipboard.ContainsText())
            {
                gridViewTags.Rows.Clear();
                string text = Clipboard.GetText();
                string[] lines = text.Split(new string[] { Program.Settings.SeparatorOnLoad }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                    gridViewTags.Rows.Add(lines[i].ToLower().Trim());

                if (isTranslate)
                    await FillTranslation(gridViewTags);

                if (showCount)
                    UpdateTagCount();
            }
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < gridViewTags.RowCount; i++)
                lines.Add((string)gridViewTags["ImageTags", i].Value);
            Form_Edit fPrint = new Form_Edit();
            fPrint.textBox1.Text = string.Join(Program.Settings.SeparatorOnSave, lines.Distinct().Where(a => !String.IsNullOrWhiteSpace(a)));
            fPrint.Show();
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (gridViewTags.CurrentCell.ColumnIndex == 0)
            {
                AutoCompleteTextBox autoText = e.Control as AutoCompleteTextBox;
                if (autoText != null)
                {
                    //autoText.SetParent(gridViewTags);
                    if (Program.Settings.AutocompleteMode != AutocompleteMode.Disable && autoText.Values == null)
                    {
                        autoText.SetAutocompleteMode(Program.Settings.AutocompleteMode, Program.Settings.AutocompleteSort);
                        autoText.Values = Program.TagsList.Tags;
                    }
                    //autoText.Location = new Point(10, 10);
                    //autoText.Size = new Size(25, 75);
                    //autoText.AutoCompleteMode = AutoCompleteMode.Suggest;
                    //autoText.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    //autoText.AutoCompleteCustomSource = Program.TagsList.Tags;
                }
            }
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            if (gridViewDS.SelectedRows.Count != 1)
            {
                MessageBox.Show("Select one image!");
                return;
            }
            List<string> tags = new List<string>();
            for (int i = 0; i < gridViewTags.RowCount; i++)
            {
                tags.Add((string)gridViewTags["ImageTags", i].Value);
            }
            switch (MessageBox.Show("Set tag list to empty images only?\nYes - only empty, No - to all images, Cancel - do nothing.", "Tag setting option", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
            {
                case DialogResult.Yes:
                    Program.DataManager.SetTagListToAll(tags, true);
                    break;
                case DialogResult.No:
                    Program.DataManager.SetTagListToAll(tags, false);
                    break;
                case DialogResult.Cancel:
                    return;
            }
            Program.DataManager.UpdateData();
            BindTagList();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.DataManager != null && Program.DataManager.IsDataSetChanged())
            {
                DialogResult result = MessageBox.Show("The dataset has been changed,\ndo you want to save the changes?", "Saving changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Program.DataManager.SaveAll(Program.Settings.FixTagsOnSave);
                }
                else if (result == DialogResult.Cancel)
                    e.Cancel = true;
            }
        }

        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1)
                return;
            AddSelectedAllTagsToImageTags();
        }

        private void dataGridView3_DataSourceChanged(object sender, EventArgs e)
        {

        }

        private void ApplyDataSetGridStyle()
        {
            for (int i = 0; i < gridViewDS.ColumnCount; i++)
            {
                if (gridViewDS.Columns[i].ValueType == typeof(Image))
                {
                    ((DataGridViewImageColumn)gridViewDS.Columns[i]).ImageLayout = DataGridViewImageCellLayout.NotSet;
                    gridViewDS.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                if (gridViewDS.Columns[i].Name == "Loss" || gridViewDS.Columns[i].Name == "LastLoss")
                {
                    gridViewDS.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                    gridViewDS.Columns[i].Visible = Program.DataManager.IsLossLoaded;
                }

            }
        }

        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            if (isCtrlOrShiftPressed)
            {
                needReloadTags = true;
                return;
            }
            needReloadTags = false;
            if (isLoading)
            {
                LoadSelectedImageToGrid();
            }
            else
            {
                if (lastGridViewTagsHash != -1)
                {
                    if (lastGridViewTagsHash != GetgridViewTagsHash())
                    {
                        if (Program.Settings.AskSaveChanges)
                        {
                            if (MessageBox.Show("The list of tags has been changed. Save changes?", "Saving changes",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                ApplyTagsChanges();
                            }
                        }
                        else
                            ApplyTagsChanges();
                    }
                }
                LoadSelectedImageToGrid();
            }
            lastGridViewTagsHash = GetgridViewTagsHash();
        }


        private int GetgridViewTagsHash()
        {
            List<string> tags = new List<string>();
            for (int i = 0; i < gridViewTags.RowCount; i++)
            {
                tags.Add((string)gridViewTags["ImageTags", i].Value);
            }
            return string.Join("|", tags).GetHashCode();
        }

        private void dataGridViewTags_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1)
            {
                if (gridViewTags.Columns.Contains("Image"))
                {
                    if (e.RowIndex != previewRowIndex)
                    {
                        //var dataItem = Program.DataManager.DataSet[(string)gridViewTags["Image", e.RowIndex].Value];
                        var dataItem = (DataItem)gridViewTags["ImageTags", e.RowIndex].Tag;
                        previewPicBox.Size = new Size(Program.Settings.PreviewSize, Program.Settings.PreviewSize);
                        previewPicBox.Image = dataItem.Img;
                        previewPicBox.SizeMode = PictureBoxSizeMode.AutoSize;
                        previewPicBox.Location = new Point(splitContainer1.Panel2.Location.X, PointToClient(Cursor.Position).Y);

                        if (!this.Controls.ContainsKey("previewPicBox"))
                        {
                            this.Controls.Add(previewPicBox);
                        }
                        previewPicBox.BringToFront();
                        previewRowIndex = e.RowIndex;
                    }
                }
                else
                {
                    if (this.Controls.ContainsKey("previewPicBox"))
                    {
                        this.Controls.RemoveByKey("previewPicBox");
                        previewRowIndex = -1;
                    }
                }
            }
        }

        private void dataGridViewTags_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (this.Controls.ContainsKey("previewPicBox"))
            {
                this.Controls.RemoveByKey("previewPicBox");
                previewRowIndex = -1;
            }
        }

        private void toolStripButton18_Click(object sender, EventArgs e)
        {
            switch (filterAnd)
            {
                case FilterType.Not:
                    filterAnd = FilterType.Or;
                    BtnTagMultiModeSwitch.Image = Properties.Resources.ORIcon;
                    break;
                case FilterType.Or:
                    filterAnd = FilterType.Xor;
                    BtnTagMultiModeSwitch.Image = Properties.Resources.XORIcon;
                    break;
                case FilterType.Xor:
                    filterAnd = FilterType.And;
                    BtnTagMultiModeSwitch.Image = Properties.Resources.ANDIcon;
                    break;
                case FilterType.And:
                    filterAnd = FilterType.Not;
                    BtnTagMultiModeSwitch.Image = Properties.Resources.NOTIcon;
                    break;
                default:
                    throw new ArgumentException($"Invalid filter type: {filterAnd}");
            }
            SetFilter();
        }

        private void gridViewTags_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (gridViewTags.Columns["ImageTags"].Index == e.ColumnIndex && e.RowIndex != -1)
            {
                string editedValue = (string)gridViewTags[e.ColumnIndex, e.RowIndex].Value;
                if (gridViewDS.SelectedRows.Count == 1)
                {
                    for (int i = 0; i < gridViewTags.RowCount; i++)
                    {
                        if (i != e.RowIndex && (string)gridViewTags[e.ColumnIndex, i].Value == editedValue)
                        {
                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                gridViewTags.Rows.RemoveAt(e.RowIndex);
                            }));

                        }
                    }
                }
                else if (gridViewDS.SelectedRows.Count > 1)
                {
                    if (string.IsNullOrEmpty((string)gridViewTags["Image", e.RowIndex].Value))
                    {
                        MessageBox.Show("Image name must be filled!");
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            gridViewTags.Rows.RemoveAt(e.RowIndex);
                        }));
                    }
                    else
                    {
                        gridViewTags["Image", e.RowIndex].Tag = gridViewTags["ImageTags", e.RowIndex].Value;
                        gridViewTags["Name", e.RowIndex].Tag = gridViewTags["ImageTags", e.RowIndex].Value;
                    }
                }
            }
        }

        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            AddSelectedAllTagsToImageTags();
        }

        private void AddTagSingleSelectedMode(string tag)
        {
            if (gridViewDS.SelectedRows.Count != 1)
            {
                SetStatus("The number of selected images is not equal to 1");
                return;
            }

            for (int i = 0; i < gridViewTags.RowCount; i++)
            {
                if ((string)gridViewTags["ImageTags", i].Value == tag)
                {
                    return;
                }
            }
            gridViewTags.Rows.Add(tag);
        }

        private void AddTagMultiselectedMode(string tag)
        {
            if (gridViewDS.SelectedRows.Count < 2)
            {
                SetStatus("The number of selected images must be greater than 1");
                return;
            }
            //List<string> selectedImages = new List<string>();
            //for (int i = 0; i < gridViewDS.SelectedRows.Count; i++)
            //{
            //    selectedImages.Add((string)gridViewDS.SelectedRows[i].Cells["Name"].Value);
            //}
            List<DataItem> selectedImages = new List<DataItem>();
            for (int i = 0; i < gridViewDS.SelectedRows.Count; i++)
            {
                selectedImages.Add(Program.DataManager.DataSet[(string)gridViewDS.SelectedRows[i].Cells["ImageFilePath"].Value]);
            }

            selectedImages.Sort((a, b) => FileNamesComparer.StrCmpLogicalW(a.Name, b.Name));

            List<KeyValuePair<int, DataItem>> alreadyContainsImages = new List<KeyValuePair<int, DataItem>>();


            for (int i = 0; i < gridViewTags.RowCount; i++)
            {
                if ((string)gridViewTags.Rows[i].Tag == tag)
                {
                    alreadyContainsImages.Add(new KeyValuePair<int, DataItem>(i, (DataItem)gridViewTags["ImageTags", i].Tag));
                }
            }
            if (alreadyContainsImages.Count > 0)
            {
                foreach (var item in alreadyContainsImages)
                {
                    selectedImages.Remove(item.Value);
                }
                int insertIndex = alreadyContainsImages.Max(a => a.Key) + 1;
                List<DataGridViewRow> rowsCopy = gridViewTags.Rows.OfType<DataGridViewRow>().ToList();
                for (int i = 0; i < selectedImages.Count; i++)
                {
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(gridViewTags);
                    row.Tag = tag;
                    row.Cells["ImageTags".IdxFromName(gridViewTags)].Value = "";
                    row.Cells["ImageTags".IdxFromName(gridViewTags)].Tag = selectedImages[i];
                    row.Cells["Image".IdxFromName(gridViewTags)].Value = selectedImages[i].ImageFilePath;
                    row.Cells["Image".IdxFromName(gridViewTags)].Tag = tag;
                    row.Cells["Name".IdxFromName(gridViewTags)].Value = selectedImages[i].Name;
                    row.Cells["Name".IdxFromName(gridViewTags)].Tag = tag;
                    rowsCopy.Insert(insertIndex, row);
                    insertIndex++;
                }
                gridViewTags.Rows.Clear();
                gridViewTags.Rows.AddRange(rowsCopy.ToArray());
            }
            else
            {
                for (int i = 0; i < selectedImages.Count; i++)
                {
                    int rowIndex = gridViewTags.Rows.Add();
                    DataGridViewRow row = gridViewTags.Rows[rowIndex];
                    row.Tag = tag;
                    row.Cells["ImageTags"].Value = i == 0 ? tag : "";
                    row.Cells["ImageTags"].Tag = selectedImages[i];
                    row.Cells["Image"].Value = selectedImages[i].ImageFilePath;
                    row.Cells["Image"].Tag = tag;
                    row.Cells["Name"].Value = selectedImages[i].Name;
                    row.Cells["Name"].Tag = tag;
                }
            }
        }

        private void RemoveTagFromImageTags(string tag)
        {
            if (gridViewDS.SelectedRows.Count == 0)
            {
                SetStatus("The number of selected images must be greater than 0");
                return;
            }

            for (int i = gridViewTags.RowCount - 1; i >= 0; i--)
            {
                if (gridViewDS.SelectedRows.Count == 1)
                {
                    if ((string)gridViewTags["ImageTags", i].Value == tag)
                    {
                        gridViewTags.Rows.RemoveAt(i);
                    }
                }
                else
                {
                    if ((string)gridViewTags.Rows[i].Tag == tag)
                    {
                        gridViewTags.Rows.RemoveAt(i);
                    }
                }


            }
        }

        private List<string> GetSelectedTagsInAllTags()
        {
            List<string> selectedTags = new List<string>();
            for (int i = 0; i < gridViewAllTags.SelectedCells.Count; i++)
            {
                var row = gridViewAllTags.SelectedCells[i].RowIndex;
                var tag = (string)gridViewAllTags.Rows[row].Cells[0].Value;
                if (!selectedTags.Contains(tag))
                    selectedTags.Add(tag);
            }
            return selectedTags;
        }

        private async void AddSelectedAllTagsToImageTags()
        {
            if (gridViewAllTags.SelectedCells.Count == 0 || gridViewDS.SelectedRows.Count == 0)
            {
                SetStatus("Images or tags not selected!");
                return;
            }
            foreach (var item in GetSelectedTagsInAllTags())
            {
                if (gridViewDS.SelectedRows.Count == 1)
                    AddTagSingleSelectedMode(item);
                else
                    AddTagMultiselectedMode(item);
            }
            if (isTranslate)
                await FillTranslation(gridViewTags);

            if (showCount)
                UpdateTagCount();
        }

        private void RemoveSelectedAllTagsToImageTags()
        {
            if (gridViewAllTags.SelectedCells.Count == 0 || gridViewDS.SelectedRows.Count == 0)
            {
                SetStatus("Images or tags not selected!");
                return;
            }
            foreach (var item in GetSelectedTagsInAllTags())
            {
                RemoveTagFromImageTags(item);
            }
        }

        private void toolStripButton20_Click(object sender, EventArgs e)
        {
            RemoveSelectedAllTagsToImageTags();
        }

        private void toolStripButton21_Click(object sender, EventArgs e)
        {
            AddTagToAll(true);
        }

        private void toolStripButton22_Click(object sender, EventArgs e)
        {
            RemoveTagFromAll(true);
        }

        private void gridViewDS_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.ColumnIndex != -1)
            {
                if (Enum.IsDefined(typeof(DatasetManager.OrderType), gridViewDS.Columns[e.ColumnIndex].Name))
                {
                    isLoading = true;
                    gridViewDS.DataSource = Program.DataManager.GetDataSourceWithLastFilter((DatasetManager.OrderType)Enum.Parse(typeof(DatasetManager.OrderType), gridViewDS.Columns[e.ColumnIndex].Name));
                    isLoading = false;
                }
            }
        }

        private void toolStripButton23_Click(object sender, EventArgs e)
        {
            string searchedTag;
            if (gridViewDS.SelectedRows.Count == 1)
            {
                searchedTag = (string)gridViewTags["ImageTags", gridViewTags.CurrentCell.RowIndex].Value;
            }
            else if (gridViewDS.SelectedRows.Count > 1)
            {
                searchedTag = (string)gridViewTags.Rows[gridViewTags.CurrentCell.RowIndex].Tag;
            }
            else
                return;
            for (int i = 0; i < gridViewAllTags.RowCount; i++)
            {
                if (((string)gridViewAllTags[0, i].Value) == searchedTag)
                {
                    gridViewAllTags.ClearSelection();
                    gridViewAllTags.Rows[i].Selected = true;
                    if (i < gridViewAllTags.FirstDisplayedScrollingRowIndex || i > gridViewAllTags.FirstDisplayedScrollingRowIndex + gridViewAllTags.DisplayedRowCount(false))
                    {
                        gridViewAllTags.FirstDisplayedScrollingRowIndex = i;
                    }
                }
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_settings settings = new Form_settings();
            if (settings.ShowDialog() == DialogResult.OK)
            {
                SetStatus("Settings have been saved");
            }
            settings.Close();
            switchLanguage();
        }

        private void gridViewTags_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void gridViewDS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteImage();
            }
            if (e.Control || e.Shift)
            {
                isCtrlOrShiftPressed = true;
            }
        }

        private void DeleteImage()
        {
            if (gridViewDS.SelectedRows.Count < 1)
                return;
            if (MessageBox.Show(I18n.GetText("TipDeleteFile"), I18n.GetText("LabelDeleteFile"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                gridViewTags.Rows.Clear();
                ApplyTagsChanges();

                var scroll = gridViewDS.FirstDisplayedScrollingRowIndex;
                var select = gridViewDS.SelectedRows[0].Index;
                var selects = new List<DataItem>();
                var list = gridViewDS.DataSource as List<DataItem>;
                foreach (DataGridViewRow item in gridViewDS.SelectedRows)
                {
                    selects.Add(list[item.Index]);
                    var file = (string)item.Cells["ImageFilePath"].Value;
                    var tagFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".txt");
                    try
                    {
                        File.Delete(file);
                        File.Delete(tagFile);
                        Program.DataManager.Remove(file);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                Program.DataManager.UpdateData();
                BindTagList();
                //gridViewDS.DataSource = Program.DataManager.GetDataSource();
                foreach (var item in selects)
                {
                    list.Remove(item);
                }
                gridViewDS.DataSource = null;
                gridViewDS.DataSource = list;
                if (gridViewDS.RowCount > 0)
                {
                    gridViewDS.FirstDisplayedScrollingRowIndex = scroll;
                    if (select >= gridViewDS.RowCount)
                    {
                        select = gridViewDS.RowCount - 1;
                    }
                    gridViewDS.ClearSelection();
                    gridViewDS.Rows[select].Selected = true;
                }
            }
        }

        private void gridViewDS_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && gridViewDS.SelectedRows.Count > 0)
            {
                var file = (string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value;
                ShowPreview(file);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (gridViewDS.SelectedRows.Count > 0)
            {
                var file = (string)gridViewDS.SelectedRows[0].Cells["ImageFilePath"].Value;
                ExplorerFile(file);
            }
        }

        private void gridViewDS_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 && e.RowIndex < gridViewDS.Rows.Count && e.Button == MouseButtons.Right)
            {
                gridViewDS.ClearSelection();
                gridViewDS.Rows[e.RowIndex].Selected = true;
                contextMenuStrip1.Show(MousePosition);
            }
        }

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

        public static void ExplorerFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                IntPtr pidlList = ILCreateFromPathW(filePath);
                if (pidlList != IntPtr.Zero)
                {
                    try
                    {
                        SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0);
                    }
                    catch { }
                    finally
                    {
                        ILFree(pidlList);
                    }
                }
                return;
            }

            if (Directory.Exists(filePath))
            {
                Process.Start(@"explorer.exe", "/select,\"" + filePath + "\"");
                return;
            }
            var dir = Path.GetDirectoryName(filePath);
            if (Directory.Exists(dir))
            {
                Process.Start(@"explorer.exe", "\"" + dir + "\"");
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            DeleteImage();
        }

        private void gridView_Enter(object sender, EventArgs e)
        {
            if (sender is DataGridView grid)
                grid.BorderStyle = BorderStyle.FixedSingle;
        }

        private void gridView_Leave(object sender, EventArgs e)
        {
            if (sender is DataGridView grid)
                grid.BorderStyle = BorderStyle.Fixed3D;
        }

        private void ShowAllTagsFilter(bool show)
        {
            if (!show)
                textBox1.TextChanged -= TextBox1_TextChanged;
            textBox1.Clear();
            textBox1.Visible = show;
            button1.Visible = show;
            if (show)
            {
                textBox1.Focus();
                textBox1.TextChanged += TextBox1_TextChanged;
            }
            else
            {
                gridViewAllTags.Focus();
            }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                isLoading = true;
                int index = Program.DataManager.AllTags.FindIndex(a => a.Tag.StartsWith(textBox1.Text));
                if (index != -1)
                {
                    //gridViewAllTags.ClearSelection();
                    //gridViewAllTags.Rows[index].Selected = true;
                    gridViewAllTags.CurrentCell = gridViewAllTags.Rows[index].Cells[0];
                    if (index < gridViewAllTags.FirstDisplayedScrollingRowIndex || index > gridViewAllTags.FirstDisplayedScrollingRowIndex + gridViewAllTags.DisplayedRowCount(false))
                    {
                        gridViewAllTags.FirstDisplayedScrollingRowIndex = index;
                    }
                }
                else
                {
                    textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
                    textBox1.SelectionStart = textBox1.TextLength;
                }
                isLoading = false;
            }
        }

        private void gridViewAllTags_KeyPress(object sender, KeyPressEventArgs e)
        {
            ShowAllTagsFilter(true);
            textBox1.Text = e.KeyChar.ToString();
            textBox1.SelectionStart = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowAllTagsFilter(false);
        }

        private void gridViewAllTags_SelectionChanged(object sender, EventArgs e)
        {
            if (!isLoading)
                ShowAllTagsFilter(false);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            int pos = -1;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                ShowAllTagsFilter(false);
                if (e.KeyCode == Keys.Down)
                    pos = 1;
                int index = gridViewAllTags.CurrentCell.RowIndex;
                gridViewAllTags.CurrentCell = gridViewAllTags.Rows[index + pos].Cells[0];
            }
        }

        private void gridViewDS_KeyUp(object sender, KeyEventArgs e)
        {
            if (isCtrlOrShiftPressed && !e.Control && !e.Shift)
            {
                isCtrlOrShiftPressed = false;
                if (needReloadTags)
                {
                    dataGridView3_SelectionChanged(sender, EventArgs.Empty);
                }
            }
        }

        private void toolStripButton24_Click(object sender, EventArgs e)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            if (allTagsFilter == null || allTagsFilter.IsDisposed)
            {
                allTagsFilter = new Form_filter();
            }
            if (allTagsFilter.ShowDialog() != DialogResult.OK)
                return;
            if (isAllTags)
            {
                BingSourceToDGV(gridViewAllTags, Program.DataManager.GetFilteredAllTags(allTagsFilter.textBox1.Text));
            }
            //string filterText = 

        }

        private void toolStripButton25_Click(object sender, EventArgs e)
        {
            BindTagList();
        }

        private void promptSortBtn_Click(object sender, EventArgs e)
        {
            SortPrompt();
        }

        private void SortPrompt()
        {
            var fixedLengthIndex = promptFixedLengthComboBox.SelectedIndex;
            if (fixedLengthIndex == -1) return;
            var fixLength = fixedLengthIndex;
            if (fixLength >= 0)
            {
                if (Program.DataManager == null)
                {
                    return;
                }
                var newRows = new List<DataGridViewRow>();
                for (var i = 0; i < fixedLengthIndex; ++i)
                {
                    newRows.Add(gridViewTags.Rows[i]);
                }

                var toSortRows = new List<DataGridViewRow>();
                var sortLength = gridViewTags.Rows.Count - fixedLengthIndex;
                if (sortLength <= 0) return;

                for (var i = fixedLengthIndex; i < gridViewTags.Rows.Count; ++i)
                {
                    toSortRows.Add(gridViewTags.Rows[i]);
                }

                DataGridViewRowComparer rowComparer = new DataGridViewRowComparer();
                toSortRows.Sort(rowComparer);
                for (var i = 0; i < sortLength; ++i)
                {
                    newRows.Add(toSortRows[i]);
                }

                // copy
                gridViewTags.Rows.Clear();
                foreach (DataGridViewRow newRow in newRows)
                {
                    gridViewTags.Rows.Add(newRow);
                }
            }
        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            settingsToolStripMenuItem_Click(sender, e);
        }

        public void switchLanguage()
        {
            I18n.Initialize(Program.Settings.Language);
            fileToolStripMenuItem.Text = I18n.GetText("MenuLabelFile");
            MenuSetting.Text = I18n.GetText("MenuLabelSettings");
            viewToolStripMenuItem.Text = I18n.GetText("MenuLabelView");
            LabelDataSet.Text = I18n.GetText("UILabelDataSet");
            LabelAllTags.Text = I18n.GetText("UILabelAllTags");
            LabelImageTags.Text = I18n.GetText("UILabelImageTags");
            promptFixTipLabel.Text = I18n.GetText("UILabelFixPromptLength");
            openFolderToolStripMenuItem.Text = I18n.GetText("MenuItemLoadFolder");
            saveAllChangesToolStripMenuItem.Text = I18n.GetText("MenuItemSaveChanges");
            loadLossFromFileToolStripMenuItem.Text = I18n.GetText("MenuItemLoadLoss");
            showPreviewToolStripMenuItem.Text = I18n.GetText("MenuItemShowPreview");
            MenuItemTranslateTags.Text = I18n.GetText("MenuItemTranslateTags");

            BtnTagAddToAll.Text = I18n.GetText("BtnTagAddToAll");
            BtnTagAdd.Text = I18n.GetText("BtnTagAdd");
            BtnTagReset.Text = I18n.GetText("BtnTagReset");
            BtnTagApply.Text = I18n.GetText("BtnTagApply");
            BtnTagDelete.Text = I18n.GetText("BtnTagDelete");
            BtnTagCopy.Text = I18n.GetText("BtnTagCopy");
            BtnTagPaste.Text = I18n.GetText("BtnTagPaste");
            BtnTagSetToAll.Text = I18n.GetText("BtnTagSetToAll");
            BtnTagPasteFromClipBoard.Text = I18n.GetText("BtnTagPasteFromClipBoard");
            BtnTagShow.Text = I18n.GetText("BtnTagShow");
            BtnTagUp.Text = I18n.GetText("BtnTagUp");
            BtnTagDown.Text = I18n.GetText("BtnTagDown");
            BtnTagFindInAll.Text = I18n.GetText("BtnTagFindInAll");

            BtnTagSwitch.Text = I18n.GetText("BtnTagSwitch");
            BtnTagAddToAll.Text = I18n.GetText("BtnTagAddToAll");
            BtnTagDeleteForAll.Text = I18n.GetText("BtnTagDeleteForAll");
            BtnTagReplace.Text = I18n.GetText("BtnTagReplace");
            BtnTagAddToSelected.Text = I18n.GetText("BtnTagAddToSelected");
            BtnTagDeleteForSelected.Text = I18n.GetText("BtnTagDeleteForSelected");
            BtnTagAddToFiltered.Text = I18n.GetText("BtnTagAddToFiltered");
            BtnTagDeleteForFiltered.Text = I18n.GetText("BtnTagDeleteForFiltered");
            BtnTagMultiModeSwitch.Text = I18n.GetText("BtnTagMultiModeSwitch");
            BtnImageFilter.Text = I18n.GetText("BtnImageFilter");
            BtnImageExitFilter.Text = I18n.GetText("BtnImageExitFilter");
            BtnTagFilter.Text = I18n.GetText("BtnTagFilter");
            BtnTagExitFilter.Text = I18n.GetText("BtnTagExitFilter");
            MenuShowTagCount.Text = I18n.GetText("MenuShowCount");

            switch (Program.Settings.Language)
            {
                case "en-US":
                    LanguageENBtn.Checked = true;
                    LanguageCNBtn.Checked = false;
                    break;
                case "zh-CN":
                    LanguageENBtn.Checked = false;
                    LanguageCNBtn.Checked = true;
                    break;
                default:
                    break;
            }
        }

        private void LanguageENBtn_Click(object sender, EventArgs e)
        {
            if (LanguageENBtn.Checked) { return; }
            Program.Settings.Language = "en-US";
            Program.Settings.SaveSettings();
            switchLanguage();
            LanguageENBtn.Checked = true;
            LanguageCNBtn.Checked = false;
        }

        private void LanguageCNBtn_Click(object sender, EventArgs e)
        {
            if (LanguageCNBtn.Checked) { return; }
            Program.Settings.Language = "zh-CN";
            Program.Settings.SaveSettings();
            switchLanguage();
            LanguageCNBtn.Checked = true;
            LanguageENBtn.Checked = false;
        }

        private void MenuShowTagCount_Click(object sender, EventArgs e)
        {
            if (Program.DataManager == null)
            {
                MessageBox.Show(I18n.GetText("TipDatasetNoLoad"));
                return;
            }
            showCount = !showCount;
            MenuShowTagCount.Checked = showCount;
            var Header = "Count";
            if (showCount)
            {
                gridViewAllTags.Columns.Insert(1, new DataGridViewTextBoxColumn()
                {
                    Name = Header,
                    HeaderText = Header,
                    ReadOnly = true,
                    Width = 80,
                });

                // add count
                LockEdit(true);
                for (int i = 0; i < gridViewAllTags.RowCount; i++)
                {
                    gridViewAllTags[Header, i].ReadOnly = true;
                    gridViewAllTags[Header, i].Value = 0;
                }
                UpdateTagCount();
                LockEdit(false);
            }
            else
            {
                gridViewAllTags.Columns.Remove(Header);
            }
        }

        public void UpdateTagCount()
        {
            var dataset = Program.DataManager.DataSet;
            var Header = "Count";
            int tmpCount;
            for (int i = 0; i < gridViewAllTags.RowCount; i++)
            {
                tmpCount = 0;
                foreach (var item in dataset)
                {
                    for (int j = 0; j < item.Value.Tags.Count; ++j)
                    {
                        if (item.Value.Tags[j] == gridViewAllTags["Tag", i].Value.ToString())
                        {
                            tmpCount++;
                            break;
                        }
                    }
                }
                gridViewAllTags[Header, i].Value = tmpCount;
            }
        }

        //private void CreateDataGridViewTags()
        //{
        //    DataGridView gridViewTags = new DataGridView();
        //    DataGridViewTextBoxColumn tbc = new DataGridViewTextBoxColumn();
        //    tbc.Name = "ImageTags";
        //    tbc.HeaderText = "Tags";
        //    tbc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        //    tbc.Resizable = DataGridViewTriState.False;
        //    tbc.MinimumWidth = 9;
        //    tbc.SortMode = DataGridViewColumnSortMode.Automatic;
        //    gridViewTags.Columns.Add(tbc);
        //    gridViewTags.BorderStyle = BorderStyle.Fixed3D;
        //    gridViewTags.ColumnHeadersVisible = false;
        //    gridViewTags.RowHeadersVisible = false;

        //    DataGridViewCellStyle defCellStyle = new DataGridViewCellStyle();
        //    defCellStyle.Font = new Font("Tahoma", 14);
        //    defCellStyle.WrapMode = DataGridViewTriState.False;
        //    defCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        //    gridViewTags.DefaultCellStyle = defCellStyle;
        //    DataGridViewRow dgvr = new DataGridViewRow();
        //    dgvr.Height = 29;
        //    dgvr.DefaultCellStyle = new DataGridViewCellStyle();
        //    gridViewTags.RowTemplate = dgvr;
        //    gridViewTags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        //    //gridViewTags.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        //    gridViewTags.Dock = DockStyle.Fill;
        //    gridViewTags.Location = new Point(0, 30);
        //    gridViewTags.Margin = new Padding(4, 3, 4, 3);
        //    gridViewTags.RowHeadersWidth = 72;
        //    gridViewTags.Size = new Size(369, 647);
        //    gridViewTags.AllowDrop = true;
        //    gridViewTags.AllowUserToAddRows = false;
        //    gridViewTags.AllowUserToResizeColumns = false;
        //    gridViewTags.AllowUserToResizeRows = false;
        //    gridViewTags.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        //    gridViewTags.MultiSelect = false;
        //    gridViewTags.TabIndex = 2;
        //    gridViewTags.CellEndEdit += gridViewTags_CellEndEdit;
        //    gridViewTags.EditingControlShowing += dataGridView1_EditingControlShowing;
        //    gridViewTags.KeyDown += dataGridView1_KeyDown;
        //    gridViewTags.CellMouseEnter += dataGridViewTags_CellMouseEnter;
        //    gridViewTags.CellMouseLeave += dataGridViewTags_CellMouseLeave;
        //    gridViewTags.MouseMove += dataGridView1_MouseMove;
        //    gridViewTags.MouseDown += dataGridView1_MouseDown;
        //    gridViewTags.DragDrop += dataGridView1_DragDrop;
        //    gridViewTags.DragOver += dataGridView1_DragOver;
        //    gridViewTags.Enter += gridView_Enter;
        //    gridViewTags.Leave += gridView_Leave;
        //}
    }
    class DataGridViewRowComparer : IComparer<DataGridViewRow>
    {
        public int Compare(DataGridViewRow x, DataGridViewRow y)
        {
            if (x == null || y == null)
                return 0;

            return string.Compare(
                x.Cells[0].Value?.ToString(),
                y.Cells[0].Value?.ToString(),
                StringComparison.Ordinal);
        }
    }
}
