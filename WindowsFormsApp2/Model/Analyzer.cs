using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class LexicalAnalyzer
    {
        private readonly HashSet<char> _letters = new HashSet<char>();
        private readonly HashSet<char> _digits = new HashSet<char>();
        private readonly HashSet<char> _operators = new HashSet<char>();
        private readonly HashSet<char> _separators = new HashSet<char>();
        private readonly HashSet<string> _keywords = new HashSet<string>();

        private enum State
        {
            Start,          // Начальное состояние
            InIdentifier,    // В идентификаторе
            InInteger,       // В целом числе
            InFloat,         // В вещественном числе
            InOperator,      // В операторе
            InString,        // В строковом литерале
            InLineComment,   // В однострочном комментарии
            InBlockComment,  // В многострочном комментарии
            InError,         // В ошибочном состоянии
            Done             // Конец обработки
        }

        public LexicalAnalyzer()
        {
            InitializeSymbolSets();
        }

       
        private void InitializeSymbolSets()
        {
            for (char c = 'a'; c <= 'z'; c++) _letters.Add(c);
            for (char c = 'A'; c <= 'Z'; c++) _letters.Add(c);
            for (char c = 'а'; c <= 'я'; c++) _letters.Add(c);
            for (char c = 'А'; c <= 'Я'; c++) _letters.Add(c);
            _letters.Add('_');

            for (char c = '0'; c <= '9'; c++) _digits.Add(c);

            string operators = "+-*/%=!&|<>";
            foreach (char c in operators) _operators.Add(c);

            string separators = "(){}[];,.:";
            foreach (char c in separators) _separators.Add(c);

            string[] keywords = {
                "int", "float", "double", "char", "string", "bool",
                "if", "else", "for", "while", "do", "switch", "case",
                "break", "continue", "return", "void", "class", "struct",
                "public", "private", "protected", "static", "const",
                "true", "false", "null", "this", "base", "new"
            };
            foreach (string kw in keywords) _keywords.Add(kw);
        }

       
        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrEmpty(text))
                return tokens;

            State state = State.Start;
            var currentToken = new StringBuilder();
            int line = 1;
            int pos = 0;
            int tokenStartLine = 1;
            int tokenStartPos = 0;
            int length = text.Length;

            while (pos < length)
            {
                char c = text[pos];
                char nextChar = pos < length - 1 ? text[pos + 1] : '\0';

                switch (state)
                {
                    case State.Start:
                        if (char.IsWhiteSpace(c))
                        {
                            if (c == '\n') line++;
                            pos++;
                            continue;
                        }

                        tokenStartLine = line;
                        tokenStartPos = pos + 1;
                        currentToken.Clear();

                        if (_letters.Contains(c))
                        {
                            state = State.InIdentifier;
                            currentToken.Append(c);
                        }
                        else if (_digits.Contains(c))
                        {
                            state = State.InInteger;
                            currentToken.Append(c);
                        }
                        else if (_operators.Contains(c))
                        {
                            if ((c == '=' && nextChar == '=') ||
                                (c == '!' && nextChar == '=') ||
                                (c == '<' && nextChar == '=') ||
                                (c == '>' && nextChar == '=') ||
                                (c == '&' && nextChar == '&') ||
                                (c == '|' && nextChar == '|'))
                            {
                                currentToken.Append(c);
                                currentToken.Append(nextChar);
                                tokens.Add(CreateToken(TokenType.Operator, currentToken.ToString(),
                                    tokenStartLine, tokenStartPos, pos + 2));
                                pos += 2;
                                state = State.Start;
                                continue;
                            }
                            else
                            {
                                state = State.InOperator;
                                currentToken.Append(c);
                            }
                        }
                        else if (_separators.Contains(c))
                        {
                            currentToken.Append(c);
                            tokens.Add(CreateToken(TokenType.Separator, c.ToString(),
                                tokenStartLine, tokenStartPos, pos + 1));
                            pos++;
                            continue;
                        }
                        else if (c == '"')
                        {
                            state = State.InString;
                            currentToken.Append(c);
                        }
                        else if (c == '/')
                        {
                            if (nextChar == '/')
                            {
                                state = State.InLineComment;
                                currentToken.Append(c);
                                currentToken.Append(nextChar);
                                pos += 2;
                                continue;
                            }
                            else if (nextChar == '*')
                            {
                                state = State.InBlockComment;
                                currentToken.Append(c);
                                currentToken.Append(nextChar);
                                pos += 2;
                                continue;
                            }
                            else
                            {
                                state = State.InOperator;
                                currentToken.Append(c);
                            }
                        }
                        else
                        {
                            tokens.Add(CreateErrorToken($"Недопустимый символ '{c}'",
                                line, pos + 1, pos + 1));
                        }
                        pos++;
                        break;

                    case State.InIdentifier:
                        if (_letters.Contains(c) || _digits.Contains(c))
                        {
                            currentToken.Append(c);
                            pos++;
                        }
                        else
                        {
                            string tokenValue = currentToken.ToString();
                            TokenType type = _keywords.Contains(tokenValue)
                                ? TokenType.Keyword
                                : TokenType.Identifier;

                            tokens.Add(CreateToken(type, tokenValue,
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        break;

                    case State.InInteger:
                        if (_digits.Contains(c))
                        {
                            currentToken.Append(c);
                            pos++;
                        }
                        else if (c == '.')
                        {
                            state = State.InFloat;
                            currentToken.Append(c);
                            pos++;
                        }
                        else
                        {
                            tokens.Add(CreateToken(TokenType.Integer, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        break;

                    case State.InFloat:
                        if (_digits.Contains(c))
                        {
                            currentToken.Append(c);
                            pos++;
                        }
                        else
                        {
                            tokens.Add(CreateToken(TokenType.Float, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        break;

                    case State.InOperator:
                        if (_operators.Contains(c))
                        {
                            currentToken.Append(c);
                            pos++;
                        }
                        else
                        {
                            tokens.Add(CreateToken(TokenType.Operator, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        break;

                    case State.InString:
                        currentToken.Append(c);

                        if (c == '"' && currentToken.Length > 1) 
                        {
                            tokens.Add(CreateToken(TokenType.StringLiteral, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos + 1));
                            state = State.Start;
                        }
                        else if (c == '\n') 
                        {
                            tokens.Add(CreateErrorToken("Незакрытая строка",
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        pos++;
                        break;

                    case State.InLineComment:
                        if (c == '\n')
                        {
                            tokens.Add(CreateToken(TokenType.Comment, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos));
                            state = State.Start;
                        }
                        else
                        {
                            currentToken.Append(c);
                            pos++;
                        }
                        break;

                    case State.InBlockComment:
                        currentToken.Append(c);

                        if (c == '*' && nextChar == '/')
                        {
                            currentToken.Append(nextChar);
                            tokens.Add(CreateToken(TokenType.Comment, currentToken.ToString(),
                                tokenStartLine, tokenStartPos, pos + 2));
                            pos += 2;
                            state = State.Start;
                        }
                        else
                        {
                            if (c == '\n') line++;
                            pos++;
                        }
                        break;

                    case State.InError:
                        // Восстанавливаемся после ошибки
                        if (char.IsWhiteSpace(c) || _separators.Contains(c))
                        {
                            state = State.Start;
                        }
                        else
                        {
                            pos++;
                        }
                        break;
                }
            }

            // Обработка незавершенных токенов в конце файла
            if (state != State.Start)
            {
                switch (state)
                {
                    case State.InIdentifier:
                        tokens.Add(CreateToken(TokenType.Identifier, currentToken.ToString(),
                            tokenStartLine, tokenStartPos, length));
                        break;
                    case State.InInteger:
                        tokens.Add(CreateToken(TokenType.Integer, currentToken.ToString(),
                            tokenStartLine, tokenStartPos, length));
                        break;
                    case State.InFloat:
                        tokens.Add(CreateToken(TokenType.Float, currentToken.ToString(),
                            tokenStartLine, tokenStartPos, length));
                        break;
                    case State.InOperator:
                        tokens.Add(CreateToken(TokenType.Operator, currentToken.ToString(),
                            tokenStartLine, tokenStartPos, length));
                        break;
                    case State.InString:
                    case State.InBlockComment:
                        tokens.Add(CreateErrorToken("Незавершенная конструкция",
                            tokenStartLine, tokenStartPos, length));
                        break;
                }
            }

            return tokens;
        }

        public class AnalysisResult
        {
            public List<Token> Tokens { get; set; } = new List<Token>();
            public bool HasErrors => Tokens.Any(t => t.IsError);
            public List<Token> Errors => Tokens.Where(t => t.IsError).ToList();
        }

        /// <summary>
        /// Создание токена
        /// </summary>
        private Token CreateToken(TokenType type, string value, int line, int start, int end)
        {
            return new Token
            {
                Type = type,
                Value = value,
                Line = line,
                StartPosition = start,
                EndPosition = end,
                IsError = false
            };
        }

        /// <summary>
        /// Создание токена ошибки
        /// </summary>
        private Token CreateErrorToken(string message, int line, int start, int end)
        {
            return new Token
            {
                Type = TokenType.Error,
                Value = message,
                Line = line,
                StartPosition = start,
                EndPosition = end,
                IsError = true,
                ErrorMessage = message
            };
        }
    }

    public enum TokenType
    {
        [Description("Ключевое слово")]
        Keyword = 1,

        [Description("Идентификатор")]
        Identifier = 2,

        [Description("Целое число")]
        Integer = 3,

        [Description("Вещественное число")]
        Float = 4,

        [Description("Оператор")]
        Operator = 5,

        [Description("Разделитель")]
        Separator = 6,

        [Description("Строковый литерал")]
        StringLiteral = 7,

        [Description("Комментарий")]
        Comment = 8,

        [Description("Ошибка")]
        Error = 99
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public string GetFormattedString()
        {
            string typeCode = ((int)Type).ToString().PadRight(8);
            string typeDesc = Type.GetDescription().PadRight(20);
            string value = (Value.Length > 30) ? Value.Substring(0, 27) + "..." : Value;
            value = value.PadRight(25);
            string location = $"стр.{Line} ({StartPosition}-{EndPosition})";

            if (IsError)
            {
                return $"ОШИБКА: {ErrorMessage} в {location}";
            }

            return $"{typeCode} {typeDesc} {value} {location}";
        }

        public override string ToString()
        {
            return GetFormattedString();
        }
    }


}
