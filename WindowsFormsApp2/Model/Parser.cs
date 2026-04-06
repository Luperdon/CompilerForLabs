using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2.Model
{
    public class Parser
    {
        private enum ParserState
        {
            Start, IdRem, AfterEqual, OpenQuote, OpenBrace, Colon, LetterF,
            CloseBrace, CloseQuote, AfterDot, Format, OpenArg, InNumber,
            AfterNumber, CloseArg, End, Error
        }

        public class SyntaxError
        {
            public string Fragment { get; set; }
            public int Line { get; set; }
            public int Position { get; set; }
            public string Description { get; set; }
        }

        private ParserState currentState;
        private List<Lexem> lexems;
        private int position;
        private List<string> stateLog;
        private List<SyntaxError> errors;
        private bool hasNumberInCurrentState;
        private int currentLine;
        private int lastErrorPositionInLine;
        private int consecutiveErrors;

        public Parser(List<Lexem> lexems)
        {
            this.lexems = lexems ?? new List<Lexem>();
            this.position = 0;
            this.currentState = ParserState.Start;
            this.stateLog = new List<string>();
            this.errors = new List<SyntaxError>();
            this.hasNumberInCurrentState = false;
            this.currentLine = 1;
            this.lastErrorPositionInLine = -1;
            this.consecutiveErrors = 0;
        }

        /// <summary>
        /// Основной метод синтаксического анализа
        /// </summary>
        public bool Parse()
        {
            errors.Clear();
            currentState = ParserState.Start;
            position = 0;
            currentLine = 1;
            lastErrorPositionInLine = -1;
            consecutiveErrors = 0;

            while (position < lexems.Count)
            {
                Lexem currentLexem = lexems[position];

                if (currentLexem.lexemLine > currentLine)
                {
                    currentLine = currentLexem.lexemLine;
                    lastErrorPositionInLine = -1;
                    consecutiveErrors = 0;
                    currentState = ParserState.Start;
                    stateLog.Add($"--- Новая строка {currentLine} ---");
                }

                // Проверяем, соответствует ли текущая лексема ожидаемой
                if (IsExpectedLexem(currentState, currentLexem.lexemCode))
                {
                    ProcessLexem(currentLexem);
                    position++;
                    consecutiveErrors = 0;
                }
                else
                {
                    bool isCascadingError = IsCascadingError(currentLexem);

                    if (!isCascadingError)
                    {
                        string description = GetErrorDescription(currentState, currentLexem);
                        AddError(currentLexem, description, currentLexem.lexemLine, currentLexem.lexemStartPosition);
                        stateLog.Add($"ОШИБКА: {description} (Лексема: '{currentLexem.lexemContaintment}')");

                        lastErrorPositionInLine = currentLexem.lexemStartPosition;
                        consecutiveErrors++;

                        // Восстановление после ошибки в зависимости от состояния
                        currentState = RecoverState(currentState, currentLexem);
                    }
                    else
                    {
                        stateLog.Add($"Пропуск каскадной ошибки: '{currentLexem.lexemContaintment}'");
                    }

                    position++;
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Восстановление состояния после ошибки
        /// </summary>
        private ParserState RecoverState(ParserState state, Lexem lexem)
        {
            // Если ошибка в состоянии CloseQuote (ожидалась точка)
            if (state == ParserState.CloseQuote)
            {
                // Проверяем, является ли текущая лексема началом вызова format
                if (lexem.lexemCode == 2) // format
                {
                    return ParserState.AfterDot;
                }
                // Иначе переходим в состояние Format (ожидаем format)
                return ParserState.Format;
            }

            // Если ошибка в состоянии AfterDot (ожидался format)
            if (state == ParserState.AfterDot)
            {
                // Если текущая лексема - открывающая скобка, возможно пропущен format
                if (lexem.lexemCode == 16) // (
                {
                    return ParserState.OpenArg;
                }
                return ParserState.Format;
            }

            // Если ошибка в состоянии Format (ожидалась открывающая скобка)
            if (state == ParserState.Format)
            {
                // Если текущая лексема - число, возможно пропущена скобка
                if (lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                    lexem.lexemCode == 14 || lexem.lexemCode == 15)
                {
                    return ParserState.OpenArg;
                }
                return ParserState.OpenArg;
            }

            // Если слишком много ошибок подряд, сбрасываем состояние
            if (consecutiveErrors >= 3)
            {
                stateLog.Add($"Слишком много ошибок подряд. Сброс состояния.");
                return ParserState.Start;
            }

            // В остальных случаях оставляем текущее состояние
            return state;
        }

        /// <summary>
        /// Проверка, является ли ошибка каскадной
        /// </summary>
        private bool IsCascadingError(Lexem lexem)
        {
            // Если ошибка в той же строке и позиция очень близка к предыдущей ошибке
            if (lexem.lexemLine == currentLine &&
                lastErrorPositionInLine != -1 &&
                Math.Abs(lexem.lexemStartPosition - lastErrorPositionInLine) <= 3)
            {
                return true;
            }

            // Если это та же лексема, что и предыдущая ошибка
            if (consecutiveErrors > 1 && lexem.lexemCode == 19)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка, ожидается ли данная лексема в текущем состоянии
        /// </summary>
        private bool IsExpectedLexem(ParserState state, int lexemCode)
        {
            switch (state)
            {
                case ParserState.Start:
                    return lexemCode == 1;
                case ParserState.IdRem:
                    return lexemCode == 4;
                case ParserState.AfterEqual:
                    return lexemCode == 5;
                case ParserState.OpenQuote:
                    return lexemCode == 21;
                case ParserState.OpenBrace:
                    return lexemCode == 22;
                case ParserState.Colon:
                    return lexemCode == 23;
                case ParserState.LetterF:
                    return lexemCode == 24;
                case ParserState.CloseBrace:
                    return lexemCode == 5;
                case ParserState.CloseQuote:
                    return lexemCode == 9;
                case ParserState.AfterDot:
                    return lexemCode == 2;
                case ParserState.Format:
                    return lexemCode == 16;
                case ParserState.OpenArg:
                    return lexemCode == 7 || lexemCode == 8 || lexemCode == 12 ||
                           lexemCode == 13 || lexemCode == 14 || lexemCode == 15;
                case ParserState.InNumber:
                    return lexemCode == 12 || lexemCode == 13 || lexemCode == 14 || lexemCode == 15;
                case ParserState.AfterNumber:
                    return lexemCode == 17;
                case ParserState.CloseArg:
                    return lexemCode == 18;
                case ParserState.End:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Обработка корректной лексемы
        /// </summary>
        private void ProcessLexem(Lexem lexem)
        {
            stateLog.Add($"Состояние: {currentState}, Лексема: '{lexem.lexemContaintment}' (код: {lexem.lexemCode})");

            switch (currentState)
            {
                case ParserState.Start:
                    if (lexem.lexemCode == 1) currentState = ParserState.IdRem;
                    break;
                case ParserState.IdRem:
                    if (lexem.lexemCode == 4) currentState = ParserState.AfterEqual;
                    break;
                case ParserState.AfterEqual:
                    if (lexem.lexemCode == 5) currentState = ParserState.OpenQuote;
                    break;
                case ParserState.OpenQuote:
                    if (lexem.lexemCode == 21) currentState = ParserState.OpenBrace;
                    break;
                case ParserState.OpenBrace:
                    if (lexem.lexemCode == 22) currentState = ParserState.Colon;
                    break;
                case ParserState.Colon:
                    if (lexem.lexemCode == 23) currentState = ParserState.LetterF;
                    break;
                case ParserState.LetterF:
                    if (lexem.lexemCode == 24) currentState = ParserState.CloseBrace;
                    break;
                case ParserState.CloseBrace:
                    if (lexem.lexemCode == 5) currentState = ParserState.CloseQuote;
                    break;
                case ParserState.CloseQuote:
                    if (lexem.lexemCode == 9) currentState = ParserState.AfterDot;
                    break;
                case ParserState.AfterDot:
                    if (lexem.lexemCode == 2) currentState = ParserState.Format;
                    break;
                case ParserState.Format:
                    if (lexem.lexemCode == 16) currentState = ParserState.OpenArg;
                    break;
                case ParserState.OpenArg:
                    if (lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                        lexem.lexemCode == 14 || lexem.lexemCode == 15)
                    {
                        hasNumberInCurrentState = true;
                        currentState = ParserState.AfterNumber;
                    }
                    else if (lexem.lexemCode == 7 || lexem.lexemCode == 8)
                    {
                        currentState = ParserState.InNumber;
                    }
                    break;
                case ParserState.InNumber:
                    if (lexem.lexemCode == 12 || lexem.lexemCode == 13 ||
                        lexem.lexemCode == 14 || lexem.lexemCode == 15)
                    {
                        currentState = ParserState.AfterNumber;
                    }
                    break;
                case ParserState.AfterNumber:
                    if (lexem.lexemCode == 17) currentState = ParserState.CloseArg;
                    break;
                case ParserState.CloseArg:
                    if (lexem.lexemCode == 18) currentState = ParserState.End;
                    break;
            }
        }

        /// <summary>
        /// Получение описания ошибки
        /// </summary>
        private string GetErrorDescription(ParserState state, Lexem lexem)
        {
            if (lexem.lexemCode == 19)
            {
                return $"Недопустимый символ '{lexem.lexemContaintment}'";
            }

            switch (state)
            {
                case ParserState.Start:
                    return $"Ожидался идентификатор, получен '{lexem.lexemContaintment}'";
                case ParserState.IdRem:
                    return $"Ожидался '=', получен '{lexem.lexemContaintment}'";
                case ParserState.AfterEqual:
                    return $"Ожидалась открывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
                case ParserState.OpenQuote:
                    return $"Ожидалась открывающая фигурная скобка '{{', получен '{lexem.lexemContaintment}'";
                case ParserState.OpenBrace:
                    return $"Ожидалось двоеточие ':', получен '{lexem.lexemContaintment}'";
                case ParserState.Colon:
                    return $"Ожидалась буква 'f', получен '{lexem.lexemContaintment}'";
                case ParserState.LetterF:
                    return $"Ожидалась закрывающая фигурная скобка '}}', получен '{lexem.lexemContaintment}'";
                case ParserState.CloseBrace:
                    return $"Ожидалась закрывающая кавычка '\"', получен '{lexem.lexemContaintment}'";
                case ParserState.CloseQuote:
                    return $"Ожидалась точка '.', получен '{lexem.lexemContaintment}'";
                case ParserState.AfterDot:
                    return $"Ожидалось ключевое слово 'format', получено '{lexem.lexemContaintment}'";
                case ParserState.Format:
                    return $"Ожидалась открывающая скобка '(', получен '{lexem.lexemContaintment}'";
                case ParserState.OpenArg:
                    return $"Ожидалось число или знак числа, получен '{lexem.lexemContaintment}'";
                case ParserState.AfterNumber:
                    return $"Ожидалась закрывающая скобка ')', получен '{lexem.lexemContaintment}'";
                case ParserState.CloseArg:
                    return $"Ожидалась точка с запятой ';', получен '{lexem.lexemContaintment}'";
                default:
                    return $"Ожидалась корректная лексема, получен '{lexem.lexemContaintment}'";
            }
        }

        private void AddError(Lexem lexem, string description, int line, int position)
        {
            if (errors.Any(e => e.Line == line && e.Position == position))
                return;

            errors.Add(new SyntaxError
            {
                Fragment = lexem?.lexemContaintment ?? "конец ввода",
                Line = line,
                Position = position,
                Description = description
            });
        }

        public List<SyntaxError> GetErrors()
        {
            return errors;
        }

        public void PrintLog(StringBuilder sb)
        {
            sb.AppendLine("  Лог состояний конечного автомата:");
            foreach (var entry in stateLog)
            {
                sb.AppendLine($"    {entry}");
            }
        }

        public List<string> GetStateLog()
        {
            return stateLog;
        }
    }
}