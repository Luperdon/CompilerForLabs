using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WindowsFormsApp2
{
    public class LexicalAnalyzer
    {
        private readonly Dictionary<SearchPattern, Regex> _patterns;

        private readonly UsernameAutomaton _usernameAutomaton;

        public enum SearchPattern
        {
            OGRN,           // ОГРН юридического лица
            Username,       // Имя пользователя
            ComplexNumber   // Комплексные числа
        }

        public LexicalAnalyzer()
        {
            _patterns = new Dictionary<SearchPattern, Regex>
            {
                { SearchPattern.OGRN, new Regex(@"\b[15][0-9]{11}\b", RegexOptions.Compiled) },
                
                { SearchPattern.ComplexNumber, new Regex(@"^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?(?:\s*[+-]\s*[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?i)?$|^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?i$", RegexOptions.Compiled | RegexOptions.IgnoreCase) }
            };

            _usernameAutomaton = new UsernameAutomaton();
        }

        public List<SearchMatch> FindMatches(string text, SearchPattern pattern)
        {
            var matches = new List<SearchMatch>();

            if (string.IsNullOrEmpty(text))
                return matches;

            switch (pattern)
            {
                case SearchPattern.Username:
                    return FindUsernameMatches(text);

                case SearchPattern.OGRN:
                case SearchPattern.ComplexNumber:
                    if (!_patterns.ContainsKey(pattern))
                        return matches;

                    var regex = _patterns[pattern];
                    var regexMatches = regex.Matches(text);

                    foreach (Match match in regexMatches)
                    {
                        var position = GetLineAndColumn(text, match.Index);

                        matches.Add(new SearchMatch
                        {
                            Value = match.Value,
                            Line = position.Line,
                            Column = position.Column,
                            StartIndex = match.Index,
                            Length = match.Length
                        });
                    }
                    return matches;

                default:
                    return matches;
            }
        }

        private List<SearchMatch> FindUsernameMatches(string text)
        {
            var matches = new List<SearchMatch>();
            var automatonMatches = _usernameAutomaton.FindAll(text);

            foreach (var autoMatch in automatonMatches)
            {
                string value = autoMatch.GetValue(text);
                var position = GetLineAndColumn(text, autoMatch.StartIndex);

                if (IsWordBoundary(text, autoMatch.StartIndex - 1) &&
                    IsWordBoundary(text, autoMatch.EndIndex + 1))
                {
                    matches.Add(new SearchMatch
                    {
                        Value = value,
                        Line = position.Line,
                        Column = position.Column,
                        StartIndex = autoMatch.StartIndex,
                        Length = autoMatch.Length
                    });
                }
            }

            return matches;
        }

        private bool IsWordBoundary(string text, int index)
        {
            if (index < 0 || index >= text.Length)
                return true;

            char c = text[index];
            return !(char.IsLetterOrDigit(c));
        }

        public Dictionary<SearchPattern, List<SearchMatch>> FindAllMatches(string text)
        {
            var results = new Dictionary<SearchPattern, List<SearchMatch>>();

            foreach (SearchPattern pattern in Enum.GetValues(typeof(SearchPattern)))
            {
                results[pattern] = FindMatches(text, pattern);
            }

            return results;
        }

        private (int Line, int Column) GetLineAndColumn(string text, int index)
        {
            int line = 1;
            int column = 1;

            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }

        public static string GetPatternName(SearchPattern pattern)
        {
            switch (pattern)
            {
                case SearchPattern.OGRN: return "ОГРН юридического лица";
                case SearchPattern.Username: return "Имя пользователя (автомат)";
                case SearchPattern.ComplexNumber: return "Комплексные числа";
                default: return pattern.ToString();
            }
        }

        public static string GetPatternDescription(SearchPattern pattern)
        {
            switch (pattern)
            {
                case SearchPattern.OGRN:
                    return "Формат: 1 или 5 в начале, затем 11 цифр (всего 13 цифр)";
                case SearchPattern.Username:
                    return "Формат (автомат): буква в начале, затем буквы и цифры, длина от 2 до 30 символов";
                case SearchPattern.ComplexNumber:
                    return "Формат: действительная и мнимая части (например: 3+4i, -2.5i, 1e-3+2.5i)";
                default:
                    return pattern.ToString();
            }
        }
    }

    public class SearchMatch
    {
        public string Value { get; set; }           // Найденная подстрока
        public int Line { get; set; }               // Номер строки
        public int Column { get; set; }             // Номер символа в строке
        public int StartIndex { get; set; }         // Индекс начала в тексте
        public int Length { get; set; }             // Длина подстроки

        public override string ToString()
        {
            return $"{Value} (строка {Line}, позиция {Column}, длина {Length})";
        }
    }

    public class UsernameAutomaton
    {
        private enum State
        {
            Start,      // Начальное состояние
            FirstChar,  // Первый символ (должен быть буквой)
            Body,       // Тело username (буквы и цифры)
            End,        // Конечное состояние (успешное завершение)
            Error       // Ошибочное состояние
        }

        private State _currentState;
        private int _startIndex;
        private int _currentLength;
        private List<UsernameMatch> _matches;

        private const int MinLength = 2;
        private const int MaxLength = 30;

        public List<UsernameMatch> FindAll(string text)
        {
            _matches = new List<UsernameMatch>();

            if (string.IsNullOrEmpty(text))
                return _matches;

            ResetAutomaton();

            for (int i = 0; i <= text.Length; i++)
            {
                char currentChar = i < text.Length ? text[i] : '\0';
                ProcessChar(currentChar, i);
            }

            return _matches;
        }

        private void ResetAutomaton()
        {
            _currentState = State.Start;
            _startIndex = -1;
            _currentLength = 0;
        }

        private void ProcessChar(char c, int position)
        {
            switch (_currentState)
            {
                case State.Start:
                    ProcessInStart(c, position);
                    break;

                case State.FirstChar:
                    ProcessInFirstChar(c, position);
                    break;

                case State.Body:
                    ProcessInBody(c, position);
                    break;

                case State.End:
                    ProcessInEnd(c, position);
                    break;

                case State.Error:
                    ProcessInError(c, position);
                    break;
            }
        }

        private void ProcessInStart(char c, int position)
        {
            if (IsLetter(c))
            {
                _currentState = State.FirstChar;
                _startIndex = position;
                _currentLength = 1;
            }
        }

        private void ProcessInFirstChar(char c, int position)
        {
            if (IsLetterOrDigit(c))
            {
                _currentState = State.Body;
                _currentLength++;

                if (_currentLength == MaxLength)
                {
                    if (position + 1 < position)
                    {
                        // Запоминаем, что достигли максимума
                    }
                }
            }
            else
            {
                _currentState = State.Error;
                ProcessInError(c, position);
            }
        }

        private void ProcessInBody(char c, int position)
        {
            if (IsLetterOrDigit(c))
            {
                _currentLength++;

                if (_currentLength > MaxLength)
                {
                    SaveMatch(position - 1);
                    _currentState = State.Error;
                    ProcessInError(c, position);
                }
            }
            else
            {
                if (_currentLength >= MinLength && _currentLength <= MaxLength)
                {
                    SaveMatch(position - 1);
                    _currentState = State.End;
                    ProcessInEnd(c, position);
                }
                else
                {
                    _currentState = State.Error;
                    ProcessInError(c, position);
                }
            }
        }

        private void ProcessInEnd(char c, int position)
        {
            if (IsLetter(c))
            {
                ResetAutomaton();
                ProcessInStart(c, position);
            }
            else if (IsLetterOrDigit(c))
            {
                ResetAutomaton();
                ProcessInStart(c, position);
            }
            else
            {
                ResetAutomaton();
                if (IsLetter(c))
                {
                    ProcessInStart(c, position);
                }
            }
        }

        private void ProcessInError(char c, int position)
        {
            ResetAutomaton();

            if (IsLetter(c))
            {
                ProcessInStart(c, position);
            }
        }

        private void SaveMatch(int endPosition)
        {
            if (_startIndex >= 0 && _currentLength >= MinLength && _currentLength <= MaxLength)
            {
                _matches.Add(new UsernameMatch
                {
                    StartIndex = _startIndex,
                    Length = _currentLength,
                    EndIndex = endPosition
                });
            }
        }

        private bool IsLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private bool IsLetterOrDigit(char c)
        {
            return IsLetter(c) || (c >= '0' && c <= '9');
        }
    }

    public class UsernameMatch
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public int EndIndex { get; set; }

        public string GetValue(string text)
        {
            if (StartIndex >= 0 && Length > 0 && StartIndex + Length <= text.Length)
            {
                return text.Substring(StartIndex, Length);
            }
            return string.Empty;
        }
    }
}