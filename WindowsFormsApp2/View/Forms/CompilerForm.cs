using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class CompilerForm : Form
    {
        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private bool isUndoRedoOperation = false;
        private string previousText = "";

        private string currentFilePath = string.Empty;
        private bool isTextModified = false;
        private string lastSavedText = "";

        private LexicalAnalyzer _analyzer = new LexicalAnalyzer();
        private List<SearchMatch> _lastSearchResults = new List<SearchMatch>();
        private ComboBox cmbSearchType;
        private Button btnSearch;
        private Label lblCount;
        private DataGridView dgvResults;
        private Label lblDescription;

        public CompilerForm()
        {
            InitializeComponent();
            InitializeSearchInterface();
            InitializeEditMenu();
            InitializeFileMenu();
            InitializeRunButton();

            UpdateWindowTitle();
            UpdateMenuState();

            this.Icon = Properties.Resources.cpu_icon_212120;
        }

        private void InitializeSearchInterface()
        {
            Panel searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 8, 10, 8)
            };

            Label lblSearchType = new Label
            {
                Text = "Тип поиска:",
                Location = new Point(10, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };

            cmbSearchType = new ComboBox
            {
                Location = new Point(85, 12),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };

            cmbSearchType.Items.AddRange(new string[]
            {
                "ОГРН юридического лица",
                "Имя пользователя",
                "Комплексные числа"
            });
            cmbSearchType.SelectedIndex = 0;
            cmbSearchType.SelectedIndexChanged += CmbSearchType_SelectedIndexChanged;

            lblDescription = new Label
            {
                Location = new Point(85, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            UpdateDescription();

            btnSearch = new Button
            {
                Text = "Найти",
                Location = new Point(300, 10),
                Width = 100,
                Height = 35,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.LightBlue
            };
            btnSearch.Click += BtnSearch_Click;

            lblCount = new Label
            {
                Text = "Найдено: 0",
                Location = new Point(410, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            searchPanel.Controls.AddRange(new Control[] { lblSearchType, cmbSearchType, lblDescription, btnSearch, lblCount });
            this.Controls.Add(searchPanel);

            dgvResults = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 220,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                Font = new Font("Consolas", 10)
            };

            dgvResults.Columns.Add("Substring", "Найденная подстрока");
            dgvResults.Columns.Add("Line", "Строка");
            dgvResults.Columns.Add("Position", "Позиция");
            dgvResults.Columns.Add("Length", "Длина");

            dgvResults.Columns["Substring"].Width = 400;
            dgvResults.Columns["Line"].Width = 80;
            dgvResults.Columns["Position"].Width = 80;
            dgvResults.Columns["Length"].Width = 70;

            dgvResults.SelectionChanged += DgvResults_SelectionChanged;

            this.Controls.Add(dgvResults);

            if (splitContainer1 != null)
            {
                splitContainer1.Top = 80;
                splitContainer1.Height = this.ClientSize.Height - 300;
            }
        }

        private void CmbSearchType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDescription();
            dgvResults.Rows.Clear();
            _lastSearchResults.Clear();
            lblCount.Text = "Найдено: 0";
        }

        private void UpdateDescription()
        {
            var pattern = GetSelectedSearchPattern();
            lblDescription.Text = LexicalAnalyzer.GetPatternDescription(pattern);
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            string text = textBoxEditor.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Введите текст для поиска.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var searchType = GetSelectedSearchPattern();

                _lastSearchResults = _analyzer.FindMatches(text, searchType);

                dgvResults.Rows.Clear();

                foreach (var match in _lastSearchResults)
                {
                    dgvResults.Rows.Add(match.Value, match.Line, match.Column, match.Length);
                }

                lblCount.Text = $"Найдено: {_lastSearchResults.Count}";

                DisplaySearchResults(searchType);

                if (_lastSearchResults.Count == 0)
                {
                    MessageBox.Show("Совпадений не найдено.", "Результат поиска",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplaySearchResults(LexicalAnalyzer.SearchPattern searchType)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"РЕЗУЛЬТАТЫ ПОИСКА: {LexicalAnalyzer.GetPatternName(searchType)} - {DateTime.Now:HH:mm:ss}");
            sb.AppendLine($"Формат: {LexicalAnalyzer.GetPatternDescription(searchType)}");
            sb.AppendLine("==========================================================");
            sb.AppendLine();
            sb.AppendLine($"{"ПОДСТРОКА",-35} {"СТРОКА",-8} {"ПОЗИЦИЯ",-8} {"ДЛИНА",-6}");
            sb.AppendLine(new string('-', 70));

            foreach (var match in _lastSearchResults)
            {
                string substring = match.Value.Length > 33 ? match.Value.Substring(0, 30) + "..." : match.Value;
                sb.AppendLine($"{substring,-35} {match.Line,-8} {match.Column,-8} {match.Length,-6}");
            }

            sb.AppendLine(new string('=', 70));
            sb.AppendLine($"ВСЕГО НАЙДЕНО: {_lastSearchResults.Count}");

            if (_lastSearchResults.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("🔍 Для подсветки подстроки выберите строку в таблице выше.");
            }

        }

        private LexicalAnalyzer.SearchPattern GetSelectedSearchPattern()
        {
            switch (cmbSearchType.SelectedIndex)
            {
                case 0: return LexicalAnalyzer.SearchPattern.OGRN;
                case 1: return LexicalAnalyzer.SearchPattern.Username;
                case 2: return LexicalAnalyzer.SearchPattern.ComplexNumber;
                default: return LexicalAnalyzer.SearchPattern.OGRN;
            }
        }

        private void DgvResults_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count > 0 && _lastSearchResults.Count > 0)
            {
                int index = dgvResults.SelectedRows[0].Index;
                if (index < _lastSearchResults.Count)
                {
                    HighlightSubstring(_lastSearchResults[index]);
                }
            }
        }

        private void HighlightSubstring(SearchMatch match)
        {
            int currentStart = textBoxEditor.SelectionStart;
            int currentLength = textBoxEditor.SelectionLength;
            Color currentBackColor = textBoxEditor.SelectionBackColor;

            ClearHighlighting();

            if (match.StartIndex >= 0 && match.StartIndex + match.Length <= textBoxEditor.TextLength)
            {
                textBoxEditor.Focus();
                textBoxEditor.Select(match.StartIndex, match.Length);
                textBoxEditor.SelectionBackColor = Color.Yellow;
                textBoxEditor.ScrollToCaret();

                Timer timer = new Timer();
                timer.Interval = 3000;
                timer.Tick += (s, args) =>
                {
                    textBoxEditor.Select(currentStart, currentLength);
                    textBoxEditor.SelectionBackColor = currentBackColor;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void ClearHighlighting()
        {
            int start = textBoxEditor.SelectionStart;
            int length = textBoxEditor.SelectionLength;
            Color backColor = textBoxEditor.SelectionBackColor;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            textBoxEditor.Select(start, length);
            textBoxEditor.SelectionBackColor = backColor;
        }

        private void InitializeRunButton()
        {
            Button btnRun = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Пуск");
            if (btnRun != null)
            {
                btnRun.Click += (s, e) => PerformSearch();
            }

            ToolStripMenuItem runMenuItem = GetMenuItem("runToolStripMenuItem");
            if (runMenuItem != null)
            {
                runMenuItem.Click += (s, e) => PerformSearch();
            }
        }

        private ToolStripMenuItem GetMenuItem(string name)
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is MenuStrip menuStrip)
                {
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                    {
                        var found = FindMenuItem(item, name);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private ToolStripMenuItem FindMenuItem(ToolStripMenuItem item, string name)
        {
            if (item.Name == name) return item;

            foreach (ToolStripMenuItem subItem in item.DropDownItems.OfType<ToolStripMenuItem>())
            {
                var found = FindMenuItem(subItem, name);
                if (found != null) return found;
            }

            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (splitContainer1 != null)
            {
                splitContainer1.Height = this.ClientSize.Height - 300;
            }
        }

        private void InitializeEditMenu()
        {
            отменитьToolStripMenuItem.Click += (s, e) => Undo();
            отменитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;

            повторитьToolStripMenuItem.Click += (s, e) => Redo();
            повторитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;

            вырезатьToolStripMenuItem.Click += (s, e) => Cut();
            вырезатьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;

            копироватьToolStripMenuItem.Click += (s, e) => Copy();
            копироватьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;

            вставитьToolStripMenuItem.Click += (s, e) => Paste();
            вставитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;

            удалитьToolStripMenuItem.Click += (s, e) => Delete();
            удалитьToolStripMenuItem.ShortcutKeys = Keys.Delete;

            выделитьВсеToolStripMenuItem.Click += (s, e) => SelectAll();
            выделитьВсеToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;

            textBoxEditor.TextChanged += TextBoxEditor_TextChanged;
            textBoxEditor.KeyUp += (s, e) => UpdateMenuState();
            textBoxEditor.MouseUp += (s, e) => UpdateMenuState();

            splitContainer1.Panel1.Padding = new Padding(47);
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Orientation = Orientation.Horizontal;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.SplitterDistance = this.Width / 2;

            textBoxEditor.Dock = DockStyle.Fill;
            textBoxEditor.Multiline = true;
            textBoxEditor.ScrollBars = RichTextBoxScrollBars.Both;
            textBoxEditor.Font = new Font("Consolas", 12);

            //textBoxResults.Dock = DockStyle.Fill;
            //textBoxResults.Multiline = true;
            //textBoxResults.ScrollBars = RichTextBoxScrollBars.Both;
            //textBoxResults.ReadOnly = true;
            //textBoxResults.BackColor = Color.FromArgb(240, 240, 240);

            splitContainer1.Panel1.Controls.Add(textBoxEditor);
            //splitContainer1.Panel2.Controls.Add(textBoxResults);
            splitContainer1.Panel1MinSize = 200;
            splitContainer1.Panel2MinSize = 200;
        }

        private void TextBoxEditor_TextChanged(object sender, EventArgs e)
        {
            if (isUndoRedoOperation) return;

            if (previousText != textBoxEditor.Text)
            {
                undoStack.Push(previousText);
                previousText = textBoxEditor.Text;
                redoStack.Clear();
            }

            UpdateMenuState();
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(textBoxEditor.Text);

                isUndoRedoOperation = true;
                textBoxEditor.Text = undoStack.Pop();
                textBoxEditor.SelectionStart = textBoxEditor.TextLength;
                previousText = textBoxEditor.Text;
                isUndoRedoOperation = false;

                UpdateMenuState();
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(textBoxEditor.Text);

                isUndoRedoOperation = true;
                textBoxEditor.Text = redoStack.Pop();
                textBoxEditor.SelectionStart = textBoxEditor.TextLength;
                previousText = textBoxEditor.Text;
                isUndoRedoOperation = false;

                UpdateMenuState();
            }
        }

        private void Cut()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                SaveStateBeforeAction();
                Clipboard.SetText(textBoxEditor.SelectedText);
                int start = textBoxEditor.SelectionStart;
                textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                textBoxEditor.SelectionStart = start;
            }
        }

        private void Copy()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                Clipboard.SetText(textBoxEditor.SelectedText);
            }
        }

        private void Paste()
        {
            if (Clipboard.ContainsText())
            {
                SaveStateBeforeAction();
                string textToPaste = Clipboard.GetText();
                int start = textBoxEditor.SelectionStart;

                if (textBoxEditor.SelectionLength > 0)
                {
                    textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                }

                textBoxEditor.Text = textBoxEditor.Text.Insert(start, textToPaste);
                textBoxEditor.SelectionStart = start + textToPaste.Length;
            }
        }

        private void Delete()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                SaveStateBeforeAction();
                int start = textBoxEditor.SelectionStart;
                textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                textBoxEditor.SelectionStart = start;
            }
        }

        private void SelectAll()
        {
            textBoxEditor.SelectAll();
            textBoxEditor.Focus();
        }

        private void SaveStateBeforeAction()
        {
            if (!isUndoRedoOperation && previousText != textBoxEditor.Text)
            {
                undoStack.Push(previousText);
                redoStack.Clear();
            }
        }

        private void UpdateMenuState()
        {
            отменитьToolStripMenuItem.Enabled = undoStack.Count > 0;
            повторитьToolStripMenuItem.Enabled = redoStack.Count > 0;

            bool hasSelection = !string.IsNullOrEmpty(textBoxEditor.SelectedText);
            вырезатьToolStripMenuItem.Enabled = hasSelection;
            копироватьToolStripMenuItem.Enabled = hasSelection;
            удалитьToolStripMenuItem.Enabled = hasSelection;

            вставитьToolStripMenuItem.Enabled = Clipboard.ContainsText();
            выделитьВсеToolStripMenuItem.Enabled = !string.IsNullOrEmpty(textBoxEditor.Text);
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = Application.StartupPath + "\\Info\\AboutProgram.html";
            System.Diagnostics.Process.Start(htmlFilePath);
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = Application.StartupPath + "\\Info\\UserHelp.html";
            System.Diagnostics.Process.Start(htmlFilePath);
        }

        private void left_Click(object sender, EventArgs e) => Undo();
        private void right_Click(object sender, EventArgs e) => Redo();
        private void copy_Click(object sender, EventArgs e) => Copy();
        private void scissors_Click(object sender, EventArgs e) => Cut();
        private void insert_Click(object sender, EventArgs e) => Paste();

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                Application.Exit();
            }
        }

        private void InitializeFileMenu()
        {
            создатьToolStripMenuItem.Click += (s, e) => CreateNewFile();
            создатьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;

            открытьToolStripMenuItem.Click += (s, e) => OpenFile();
            открытьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;

            сохранитьToolStripMenuItem.Click += (s, e) => SaveFile();
            сохранитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;

            сохранитьКакToolStripMenuItem.Click += (s, e) => SaveFileAs();
            сохранитьКакToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;

            выходToolStripMenuItem.Click += (s, e) => this.Close();

            textBoxEditor.TextChanged += (s, e) => CheckForModifications();
        }

        private void CreateNewFile()
        {
            if (isTextModified)
            {
                DialogResult result = MessageBox.Show(
                    "Сохранить изменения в текущем файле?",
                    "Создание нового файла",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (!SaveFile()) return;
                }
                else if (result == DialogResult.Cancel) return;
            }

            textBoxEditor.Clear();
            currentFilePath = string.Empty;
            isTextModified = false;
            lastSavedText = "";

            undoStack.Clear();
            redoStack.Clear();
            undoStack.Push("");

            UpdateWindowTitle();
        }

        private void OpenFile()
        {
            if (isTextModified)
            {
                DialogResult result = MessageBox.Show(
                    "Сохранить изменения в текущем файле?",
                    "Открытие файла",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (!SaveFile()) return;
                }
                else if (result == DialogResult.Cancel) return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Файлы исходного кода (*.cs;*.cpp;*.java)|*.cs;*.cpp;*.java|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Открыть файл";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileContent = System.IO.File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                        textBoxEditor.Text = fileContent;
                        currentFilePath = openFileDialog.FileName;
                        isTextModified = false;
                        lastSavedText = fileContent;

                        undoStack.Clear();
                        redoStack.Clear();
                        undoStack.Push(fileContent);
                        previousText = fileContent;

                        UpdateWindowTitle();
                        UpdateMenuState();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла:\n{ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                return SaveFileAs();
            }
            else
            {
                return SaveToFile(currentFilePath);
            }
        }

        private bool SaveFileAs()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Сохранить файл как";
                saveFileDialog.FileName = string.IsNullOrEmpty(currentFilePath)
                    ? "Новый документ.txt"
                    : System.IO.Path.GetFileName(currentFilePath);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return SaveToFile(saveFileDialog.FileName);
                }
            }
            return false;
        }

        private bool SaveToFile(string filePath)
        {
            try
            {
                string textToSave = textBoxEditor.Text;
                System.IO.File.WriteAllText(filePath, textToSave, Encoding.UTF8);

                currentFilePath = filePath;
                isTextModified = false;
                lastSavedText = textToSave;

                UpdateWindowTitle();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void CheckForModifications()
        {
            isTextModified = (textBoxEditor.Text != lastSavedText);
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(currentFilePath)
                ? "Безымянный Файл"
                : System.IO.Path.GetFileName(currentFilePath);

            string modifiedIndicator = isTextModified ? " *" : "";
            this.Text = $"{fileName}{modifiedIndicator} - Поиск подстрок";
        }

        private bool CheckUnsavedChanges()
        {
            if (isTextModified)
            {
                string fileName = string.IsNullOrEmpty(currentFilePath)
                    ? "Безымянный Файл"
                    : System.IO.Path.GetFileName(currentFilePath);

                DialogResult result = MessageBox.Show(
                    $"Сохранить изменения в файле \"{fileName}\"?",
                    "Несохраненные изменения",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    return SaveFile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        private void folder_Click(object sender, EventArgs e) => OpenFile();
        private void save_Click(object sender, EventArgs e) => SaveFile();
        private void file_Click(object sender, EventArgs e) => CreateNewFile();
        private void runButton_Click(object sender, EventArgs e) => PerformSearch();
        private void questionButton_Click(object sender, EventArgs e) => вызовСправкиToolStripMenuItem_Click(sender, e);
        private void infoButton_Click(object sender, EventArgs e) => оПрограммеToolStripMenuItem_Click(sender, e);
    }
}