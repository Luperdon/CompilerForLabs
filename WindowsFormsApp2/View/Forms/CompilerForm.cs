using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WindowsFormsApp2.LexicalAnalyzer;
using System.Resources;
using WindowsFormsApp2.Model;

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

        //private LexicalAnalyzer _analyzer = new LexicalAnalyzer();
        private AnalysisResult _lastAnalysisResult;

        private Lexer _lexer;
        private Parser _parser;
        private List<Lexem> _lastLexems;

        public CompilerForm()
        {
            InitializeComponent();

            previousText = textBoxEditor.Text;
            undoStack.Push(previousText);

            InitializeEditMenu();
            InitializeFileMenu();
            InitializeResultsTextBox();
            InitializeRunButton();

            UpdateWindowTitle();
            UpdateMenuState();

            this.Icon = Properties.Resources.cpu_icon_212120;
        }

        private void DisplayAnalysisResults(AnalysisResult result)
        {
            _lastAnalysisResult = result;

            textBoxResults.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"РЕЗУЛЬТАТЫ ЛЕКСИЧЕСКОГО АНАЛИЗА - {DateTime.Now:HH:mm:ss}");
            sb.AppendLine("==========================================================");
            sb.AppendLine();

            sb.AppendLine($"{"КОД",-6} {"ТИП ЛЕКСЕМЫ",-20} {"ЛЕКСЕМА",-25} {"ПОЗИЦИЯ"}");
            sb.AppendLine(new string('=', 80));

            int currentLine = 0;
            foreach (var token in result.Tokens)
            {
                if (token.Line != currentLine)
                {
                    currentLine = token.Line;
                    sb.AppendLine($"--- Строка {currentLine} ---");
                }

                if (token.IsError)
                {
                    sb.AppendLine($"  {token.GetFormattedString()}");
                }
                else
                {
                    sb.AppendLine($"  {token.GetFormattedString()}");
                }
            }

            sb.AppendLine(new string('=', 80));
            int totalTokens = result.Tokens.Count(t => !t.IsError);
            int totalErrors = result.Tokens.Count(t => t.IsError);

            sb.AppendLine($"ИТОГО:");
            sb.AppendLine($"  ✓ Лексем: {totalTokens}");
            sb.AppendLine($"  ✗ Ошибок: {totalErrors}");
            sb.AppendLine($"  ∑ Всего элементов: {result.Tokens.Count}");

            if (result.HasErrors)
            {
                sb.AppendLine();
                sb.AppendLine("🔍 Дважды щелкните на строке с ошибкой для перехода к ней.");
                sb.AppendLine("📋 Используйте контекстное меню для копирования результатов.");
            }

            textBoxResults.Text = sb.ToString();

            // Подсветка ошибок в редакторе
            HighlightErrors(result);

        }

        private void HighlightErrors(AnalysisResult result)
        {
            int currentSelectionStart = textBoxEditor.SelectionStart;
            int currentSelectionLength = textBoxEditor.SelectionLength;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            // Подсвечиваем ошибки
            foreach (var error in result.Errors)
            {
                int startPos = GetPositionFromLineAndColumn(error.Line, error.StartPosition);
                int length = error.EndPosition - error.StartPosition;

                if (startPos >= 0 && length > 0)
                {
                    textBoxEditor.Select(startPos, length);
                    textBoxEditor.SelectionBackColor = Color.LightCoral;
                }
            }

            textBoxEditor.Select(currentSelectionStart, currentSelectionLength);
            textBoxEditor.SelectionBackColor = Color.White; 
        }

        private void InitializeResultsTextBox()
        {
            textBoxResults.ReadOnly = true;
            textBoxResults.BackColor = Color.FromArgb(240, 240, 240);
            textBoxResults.Font = new Font("Consolas", 10); // Моноширинный шрифт для лучшего форматирования
            textBoxResults.WordWrap = false; // Отключаем перенос для сохранения форматирования таблицы

            // Добавляем обработчик двойного щелчка
            textBoxResults.DoubleClick += TextBoxResults_DoubleClick;

            // Добавляем контекстное меню для результатов
            ContextMenuStrip resultsMenu = new ContextMenuStrip();
            ToolStripMenuItem copyItem = new ToolStripMenuItem("Копировать");
            copyItem.Click += (s, e) => CopyResultsToClipboard();
            resultsMenu.Items.Add(copyItem);

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Очистить");
            clearItem.Click += (s, e) => textBoxResults.Clear();
            resultsMenu.Items.Add(clearItem);

            textBoxResults.ContextMenuStrip = resultsMenu;
        }

        private void TextBoxResults_DoubleClick(object sender, EventArgs e)
        {
            if (_lastAnalysisResult == null) return;

            // Получаем позицию курсора в textBoxResults
            int cursorPos = textBoxResults.SelectionStart;
            string text = textBoxResults.Text;

            // Ищем строку, на которой произошел двойной щелчок
            int lineStart = text.LastIndexOf('\n', cursorPos - 1) + 1;
            int lineEnd = text.IndexOf('\n', cursorPos);
            if (lineEnd == -1) lineEnd = text.Length;

            string line = text.Substring(lineStart, lineEnd - lineStart);

            // Проверяем, содержит ли строка информацию об ошибке
            if (line.Contains("ОШИБКА"))
            {
                // Парсим номер строки и позиции из текста
                // Формат: "ОШИБКА: ... в стр.X (Y-Z)"
                int lineNumStart = line.LastIndexOf("стр.") + 4;
                int lineNumEnd = line.IndexOf(' ', lineNumStart);
                if (int.TryParse(line.Substring(lineNumStart, lineNumEnd - lineNumStart), out int errorLine))
                {
                    int posStart = line.IndexOf('(') + 1;
                    int posEnd = line.IndexOf('-');
                    int posEnd2 = line.IndexOf(')');

                    if (int.TryParse(line.Substring(posStart, posEnd - posStart), out int startPos) &&
                        int.TryParse(line.Substring(posEnd + 1, posEnd2 - posEnd - 1), out int endPos))
                    {
                        // Переходим к ошибке
                        NavigateToErrorPosition(startPos, endPos);
                    }
                }
            }
        }

        private void NavigateToErrorPosition(int startPos, int endPos)
        {
            if (startPos >= 0 && startPos < textBoxEditor.TextLength)
            {
                textBoxEditor.Focus();
                textBoxEditor.Select(startPos, Math.Min(endPos - startPos, textBoxEditor.TextLength - startPos));
                textBoxEditor.ScrollToCaret();

                textBoxEditor.SelectionBackColor = Color.Yellow;

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) =>
                {
                    textBoxEditor.SelectionBackColor = Color.White;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void CopyResultsToClipboard()
        {
            if (!string.IsNullOrEmpty(textBoxResults.Text))
            {
                Clipboard.SetText(textBoxResults.Text);
                MessageBox.Show("Результаты скопированы в буфер обмена",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RunAnalysis()
        {
            try
            {
                // Очистка предыдущих результатов
                textBoxResults.Clear();
                _lastLexems = null;

                // Снимаем подсветку ошибок
                textBoxEditor.SelectAll();
                textBoxEditor.SelectionBackColor = Color.White;
                textBoxEditor.Select(0, 0);

                // Получение текста для анализа
                string code = textBoxEditor.Text;

                if (string.IsNullOrWhiteSpace(code))
                {
                    textBoxResults.Text = "Введите текст для анализа.";
                    return;
                }

                // ШАГ 1: Лексический анализ
                _lexer = new Lexer(code);
                _lastLexems = _lexer.Scan();

                // ШАГ 2: Синтаксический анализ (если нет критических ошибок в лексере)
                bool syntaxValid = false;
                if (_lastLexems.Any(l => l.lexemType == Lexem.LexemType.Error))
                {
                    DisplayLexemsOnly(_lastLexems);
                }
                else
                {
                    _parser = new Parser(_lastLexems);
                    syntaxValid = _parser.Parse();

                    // Отображение результатов
                    DisplayFullResults(_lastLexems, syntaxValid, _parser);
                }

                // Подсветка ошибок
                HighlightErrorsFromLexems(_lastLexems);

                // Показываем всплывающее уведомление
                ShowAnalysisResultMessage(_lastLexems, syntaxValid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayLexemsOnly(List<Lexem> lexems)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"РЕЗУЛЬТАТЫ ЛЕКСИЧЕСКОГО АНАЛИЗА - {DateTime.Now:HH:mm:ss}");
            sb.AppendLine("==========================================================");
            sb.AppendLine();
            sb.AppendLine($"{"КОД",-6} {"ТИП ЛЕКСЕМЫ",-20} {"ЛЕКСЕМА",-25} {"ПОЗИЦИЯ"}");
            sb.AppendLine(new string('=', 80));

            int errorCount = 0;
            int lineNumber = 1;

            foreach (var lexem in lexems)
            {
                if (lexem.lexemType == Lexem.LexemType.Error)
                {
                    errorCount++;
                    sb.AppendLine($"  {lexem.GetFormattedString()}");
                }
                else
                {
                    sb.AppendLine($"  {lexem.GetFormattedString()}");
                }
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"ИТОГО:");
            sb.AppendLine($"  ✓ Лексем: {lexems.Count(l => l.lexemType != Lexem.LexemType.Error)}");
            sb.AppendLine($"  ✗ Ошибок лексического анализа: {errorCount}");

            textBoxResults.Text = sb.ToString();
        }

        private void DisplayFullResults(List<Lexem> lexems, bool syntaxValid, Parser parser)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"РЕЗУЛЬТАТЫ АНАЛИЗА - {DateTime.Now:HH:mm:ss}");
            sb.AppendLine("==========================================================");
            sb.AppendLine();
            sb.AppendLine($"{"СТР",-4} {"КОД",-6} {"ТИП ЛЕКСЕМЫ",-20} {"ЛЕКСЕМА",-25} {"ПОЗИЦИЯ"}");
            sb.AppendLine(new string('-', 80));

            int currentLine = 0;
            foreach (var lexem in lexems)
            {
                if (lexem.lexemLine != currentLine)
                {
                    if (currentLine > 0)
                        sb.AppendLine(new string('-', 40));
                    currentLine = lexem.lexemLine;
                }

                string typeCode = lexem.lexemCode.ToString().PadRight(6);
                string typeDesc = (lexem.lexemName?.Length > 18) ? lexem.lexemName.Substring(0, 15) + "..." : (lexem.lexemName ?? "Unknown");
                typeDesc = typeDesc.PadRight(20);

                string value = (lexem.lexemContaintment?.Length > 23) ? lexem.lexemContaintment.Substring(0, 20) + "..." : (lexem.lexemContaintment ?? "");
                value = value.PadRight(25);

                string location = $"({lexem.lexemStartPosition}-{lexem.lexemEndPosition})";

                string lineNum = lexem.lexemLine.ToString().PadRight(4);

                if (lexem.lexemType == Lexem.LexemType.Error)
                {
                    sb.AppendLine($"{lineNum} ОШИБКА: {lexem.lexemContaintment} в {location}");
                }
                else
                {
                    sb.AppendLine($"{lineNum} {typeCode} {typeDesc} {value} {location}");
                }
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine("СИНТАКСИЧЕСКИЙ АНАЛИЗ:");
            sb.AppendLine($"  Результат: {(syntaxValid ? "УСПЕШНО" : "ОШИБКА")}");

            if (!syntaxValid && parser != null)
            {
                sb.AppendLine();
                parser.PrintLog(sb);
            }

            sb.AppendLine(new string('=', 80));

            int totalLines = lexems.Max(l => l.lexemLine);
            int totalTokens = lexems.Count(l => l.lexemType != Lexem.LexemType.Error);
            int totalErrors = lexems.Count(l => l.lexemType == Lexem.LexemType.Error);

            sb.AppendLine($"ИТОГО:");
            sb.AppendLine($"  📄 Строк: {totalLines}");
            sb.AppendLine($"  ✓ Лексем: {totalTokens}");
            sb.AppendLine($"  ✗ Ошибок: {totalErrors}");
            sb.AppendLine($"  ∑ Всего элементов: {lexems.Count}");

            textBoxResults.Text = sb.ToString();
        }

        private void HighlightErrorsFromLexems(List<Lexem> lexems)
        {
            if (lexems == null) return;

            int currentSelectionStart = textBoxEditor.SelectionStart;
            int currentSelectionLength = textBoxEditor.SelectionLength;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            foreach (var error in lexems.Where(l => l.lexemType == Lexem.LexemType.Error))
            {
                int startPos = error.lexemStartPosition;
                int length = error.lexemEndPosition - error.lexemStartPosition;

                if (startPos >= 0 && length > 0 && startPos < textBoxEditor.TextLength)
                {
                    textBoxEditor.Select(startPos, Math.Min(length, textBoxEditor.TextLength - startPos));
                    textBoxEditor.SelectionBackColor = Color.LightCoral;
                }
            }

            if (currentSelectionStart <= textBoxEditor.TextLength)
            {
                textBoxEditor.Select(currentSelectionStart,
                    Math.Min(currentSelectionLength, textBoxEditor.TextLength - currentSelectionStart));
            }
        }

        private void ShowAnalysisResultMessage(List<Lexem> lexems, bool syntaxValid)
        {
            int lexicalErrors = lexems.Count(l => l.lexemType == Lexem.LexemType.Error);

            if (lexicalErrors > 0)
            {
                MessageBox.Show($"Обнаружены ошибки на этапе лексического анализа.\n" +
                              $"Всего ошибок: {lexicalErrors}\n\n" +
                              $"Дважды щелкните на строке с ошибкой в окне результатов,\n" +
                              $"чтобы перейти к проблемному месту.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (!syntaxValid)
            {
                MessageBox.Show("Лексический анализ выполнен успешно, но обнаружены\n" +
                              "синтаксические ошибки в структуре кода.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Анализ выполнен успешно!\n" +
                              $"Лексем: {lexems.Count}\n" +
                              "Синтаксических ошибок не обнаружено.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private int GetPositionFromLineAndColumn(int line, int column)
        {
            string text = textBoxEditor.Text;
            int currentLine = 1;
            int position = 0;

            while (position < text.Length && currentLine < line)
            {
                if (text[position] == '\n')
                {
                    currentLine++;
                }
                position++;
            }

            if (currentLine == line)
            {
                // column - 1 потому что позиции в тексте начинаются с 0
                return position + column - 1;
            }

            return -1; // Строка не найдена
        }

        private void InitializeRunButton()
        {
            Button btnRun = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Пуск");
            if (btnRun != null)
            {
                btnRun.Click += (s, e) => RunAnalysis();
            }

            // Также добавим обработчик для пункта меню, если он есть
            ToolStripMenuItem runMenuItem = this.GetMenuItem("runToolStripMenuItem");
            if (runMenuItem != null)
            {
                runMenuItem.Click += (s, e) => RunAnalysis();
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

            textBoxResults.Dock = DockStyle.Fill;
            textBoxResults.Multiline = true;
            textBoxResults.ScrollBars = RichTextBoxScrollBars.Both;
            textBoxResults.ReadOnly = true;
            textBoxResults.BackColor = Color.FromArgb(240, 240, 240);

            splitContainer1.Panel1.Controls.Add(textBoxEditor);
            splitContainer1.Panel2.Controls.Add(textBoxResults);

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

        private void left_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void right_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void copy_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void scissors_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void insert_Click(object sender, EventArgs e)
        {
            Paste();
        }

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
                    if (!SaveFile())
                    {
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
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
                    if (!SaveFile())
                    {
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Файлы исходного кода (*.cs;*.cpp;*.java)|*.cs;*.cpp;*.java|Все файлы (*.*)|*.*"; openFileDialog.FilterIndex = 1;
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
                            "Ошибка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
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
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

            this.Text = $"{fileName}{modifiedIndicator} - Компиляторный Редактор";
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

        private void folder_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void save_Click(object sender, EventArgs e)
        {
            SaveFile();
            //SaveFileAs();
        }

        private void file_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            RunAnalysis();
        }

        private void questionButton_Click(object sender, EventArgs e)
        {
            вызовСправкиToolStripMenuItem_Click(sender, e);
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            оПрограммеToolStripMenuItem_Click(sender, e);
        }
    }
}