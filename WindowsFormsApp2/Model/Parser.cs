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
            Start, IdRem, FString, FSymbol, CloseQuote, EndFString, Format, OpenArg,
            Scientific, Int, IntRem, Decimal, DecimalRem, Exp, ExpNum, ExpNumRem, End, Error
        }

        private ParserState currentState;
        private List<Lexem> lexems;
        private int position;
        private List<string> stateLog;

        public Parser(List<Lexem> lexems)
        {
            this.lexems = lexems ?? new List<Lexem>();
            this.position = 0;
            this.currentState = ParserState.Start;
            this.stateLog = new List<string>();
        }

        public bool Parse()
        {
            while (position < lexems.Count)
            {
                Lexem currentLexem = lexems[position];
                stateLog.Add($"Состояние автомата: {currentState}, Лексема: {currentLexem.lexemContaintment} (код: {currentLexem.lexemCode}, тип: {currentLexem.lexemType})");

                switch (currentState)
                {
                    case ParserState.Start:
                        if (currentLexem.lexemCode == 1) currentState = ParserState.IdRem;
                        else return ErrorState(currentLexem, "Ожидался идентификатор.");
                        break;

                    case ParserState.IdRem:
                        if (currentLexem.lexemCode == 4) currentState = ParserState.FString;
                        else return ErrorState(currentLexem, "Ожидался символ '='.");
                        break;

                    case ParserState.FString:
                        // Может быть обычная строка (5) или начало f-строки (6)
                        if (currentLexem.lexemCode == 5 || currentLexem.lexemCode == 6)
                        {
                            // Проверяем, есть ли после строки форматный спецификатор
                            if (position + 1 < lexems.Count && lexems[position + 1].lexemCode == 20)
                            {
                                currentState = ParserState.FSymbol;
                            }
                            else
                            {
                                return ErrorState(currentLexem, "Ожидался форматный спецификатор '{:f}' после строки.");
                            }
                        }
                        else return ErrorState(currentLexem, "Ожидалась строка в кавычках.");
                        break;

                    case ParserState.FSymbol:
                        if (currentLexem.lexemCode == 20) // Форматный спецификатор
                        {
                            currentState = ParserState.CloseQuote;
                        }
                        else return ErrorState(currentLexem, "Ожидался форматный спецификатор '{:f}'.");
                        break;

                    case ParserState.CloseQuote:
                        // После спецификатора может быть продолжение строки или закрывающая кавычка
                        if (currentLexem.lexemCode == 5 || currentLexem.lexemCode == 6)
                        {
                            currentState = ParserState.EndFString;
                        }
                        else return ErrorState(currentLexem, "Ожидалась закрывающая кавычка.");
                        break;

                    case ParserState.EndFString:
                        if (currentLexem.lexemCode == 9) currentState = ParserState.Format;
                        else return ErrorState(currentLexem, "Ожидалась точка.");
                        break;

                    case ParserState.Format:
                        if (currentLexem.lexemCode == 2) currentState = ParserState.OpenArg;
                        else return ErrorState(currentLexem, "Ожидано ключевое слово 'format'.");
                        break;

                    case ParserState.OpenArg:
                        if (currentLexem.lexemCode == 16) currentState = ParserState.Scientific;
                        else return ErrorState(currentLexem, "Ожидалась открывающая скобка '('.");
                        break;

                    case ParserState.Scientific:
                        if (currentLexem.lexemCode == 8 || currentLexem.lexemCode == 7 || currentLexem.lexemCode == 12)
                            currentState = ParserState.Int;
                        else return ErrorState(currentLexem, "Ожидался знак '+' или '-' либо число.");
                        break;

                    case ParserState.Int:
                        if (currentLexem.lexemCode == 12) currentState = ParserState.IntRem;
                        else return ErrorState(currentLexem, "Ожидалось число.");
                        break;

                    case ParserState.IntRem:
                        if (currentLexem.lexemCode == 12) { }
                        else if (currentLexem.lexemCode == 13) currentState = ParserState.Decimal;
                        else if (currentLexem.lexemContaintment.ToLower() == "e") currentState = ParserState.Exp;
                        else return ErrorState(currentLexem, "Ожидалось число, 'e' или точка '.'.");
                        break;

                    case ParserState.Decimal:
                        if (currentLexem.lexemCode == 12) currentState = ParserState.DecimalRem;
                        else return ErrorState(currentLexem, "Ожидалось число после точки.");
                        break;

                    case ParserState.DecimalRem:
                        if (currentLexem.lexemCode == 12) { }
                        else if (currentLexem.lexemContaintment.ToLower() == "e") currentState = ParserState.Exp;
                        else return ErrorState(currentLexem, "Ожидалось число или 'e'.");
                        break;

                    case ParserState.Exp:
                        if (currentLexem.lexemCode == 8 || currentLexem.lexemCode == 7)
                            currentState = ParserState.ExpNum;
                        else return ErrorState(currentLexem, "Ожидался знак '+' или '-'.");
                        break;

                    case ParserState.ExpNum:
                        if (currentLexem.lexemCode == 12) currentState = ParserState.ExpNumRem;
                        else return ErrorState(currentLexem, "Ожидалось число в экспоненте.");
                        break;

                    case ParserState.ExpNumRem:
                        if (currentLexem.lexemCode == 12) { }
                        else if (currentLexem.lexemCode == 17) currentState = ParserState.End;
                        else return ErrorState(currentLexem, "Ожидалось число или закрывающая скобка ')'.");
                        break;

                    case ParserState.End:
                        if (currentLexem.lexemCode == 18) return SuccessState();
                        else return ErrorState(currentLexem, "Ожидался символ ';'.");
                }

                position++;
            }

            // Проверка финального состояния
            if (currentState == ParserState.End || currentState == ParserState.ExpNumRem)
                return SuccessState();

            return ErrorState(null, "Неожиданный конец ввода.");
        }

        private bool ErrorState(Lexem lexem, string message)
        {
            string lexemInfo = lexem != null
                ? $"Лексема: '{lexem.lexemContaintment}' (код: {lexem.lexemCode}, тип: {lexem.lexemType})"
                : "конец ввода";
            stateLog.Add($"ОШИБКА: {message} ({lexemInfo})");
            return false;
        }

        private bool SuccessState()
        {
            stateLog.Add("Разбор успешно завершён!");
            return true;
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