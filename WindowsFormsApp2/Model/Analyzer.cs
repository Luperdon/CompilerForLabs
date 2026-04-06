using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WindowsFormsApp2
{
    /// <summary>
    /// Класс для поиска подстрок по заданным форматам
    /// </summary>
    public class LexicalAnalyzer
    {
        // Регулярные выражения для различных типов подстрок
        private readonly Dictionary<SearchPattern, Regex> _patterns;

        // Автомат для поиска username
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
                // ОГРН: 2 цифры (первая 1, вторая 5), затем 11 цифр
                { SearchPattern.OGRN, new Regex(@"\b[15][0-9]{11}\b", RegexOptions.Compiled) },
                
                // Комплексные числа: различные форматы (действительная и мнимая части)
                { SearchPattern.ComplexNumber, new Regex(@"^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?(?:\s*[+-]\s*[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?i)?$|^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?i$", RegexOptions.Compiled | RegexOptions.IgnoreCase) }
            };

            // Инициализация автомата для username
            _usernameAutomaton = new UsernameAutomaton();
        }

        /// <summary>
        /// Поиск подстрок в тексте по заданному шаблону
        /// </summary>
        /// <param name="text">Исходный текст</param>
        /// <param name="pattern">Тип искомых подстрок</param>
        /// <returns>Список найденных подстрок с информацией о позициях</returns>
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

        /// <summary>
        /// Поиск username с использованием конечного автомата
        /// </summary>
        private List<SearchMatch> FindUsernameMatches(string text)
        {
            var matches = new List<SearchMatch>();
            var automatonMatches = _usernameAutomaton.FindAll(text);

            foreach (var autoMatch in automatonMatches)
            {
                string value = autoMatch.GetValue(text);
                var position = GetLineAndColumn(text, autoMatch.StartIndex);

                // Проверка границ слова (username должен быть отдельным словом)
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

        /// <summary>
        /// Проверка границы слова
        /// </summary>
        private bool IsWordBoundary(string text, int index)
        {
            if (index < 0 || index >= text.Length)
                return true;

            char c = text[index];
            return !(char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// Поиск всех типов подстрок (для отладки)
        /// </summary>
        public Dictionary<SearchPattern, List<SearchMatch>> FindAllMatches(string text)
        {
            var results = new Dictionary<SearchPattern, List<SearchMatch>>();

            foreach (SearchPattern pattern in Enum.GetValues(typeof(SearchPattern)))
            {
                results[pattern] = FindMatches(text, pattern);
            }

            return results;
        }

        /// <summary>
        /// Получить позицию (строка, колонка) по индексу в тексте
        /// </summary>
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

        /// <summary>
        /// Получить название шаблона поиска
        /// </summary>
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

        /// <summary>
        /// Получить описание формата
        /// </summary>
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

    /// <summary>
    /// Класс для хранения результатов поиска
    /// </summary>
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
        // Состояния автомата
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

        // Параметры username
        private const int MinLength = 2;
        private const int MaxLength = 30;

        /// <summary>
        /// Поиск всех username в тексте
        /// </summary>
        /// <param name="text">Исходный текст</param>
        /// <returns>Список найденных username с позициями</returns>
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

        /// <summary>
        /// Сброс автомата в начальное состояние
        /// </summary>
        private void ResetAutomaton()
        {
            _currentState = State.Start;
            _startIndex = -1;
            _currentLength = 0;
        }

        /// <summary>
        /// Обработка символа автоматом
        /// </summary>
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

        /// <summary>
        /// Обработка в начальном состоянии
        /// </summary>
        private void ProcessInStart(char c, int position)
        {
            if (IsLetter(c))
            {
                // Начало username - первый символ буква
                _currentState = State.FirstChar;
                _startIndex = position;
                _currentLength = 1;
            }
            // Иначе остаемся в Start
        }

        /// <summary>
        /// Обработка в состоянии первого символа
        /// </summary>
        private void ProcessInFirstChar(char c, int position)
        {
            if (IsLetterOrDigit(c))
            {
                // Продолжаем username
                _currentState = State.Body;
                _currentLength++;

                // Проверяем, не достигли ли максимальной длины
                if (_currentLength == MaxLength)
                {
                    // Если следующий символ не буква/цифра, то это валидный username
                    if (position + 1 < position) // Проверка будет в следующем символе
                    {
                        // Запоминаем, что достигли максимума
                    }
                }
            }
            else
            {
                // Username из одного символа - не подходит (мин. длина 2)
                // Завершаем с ошибкой
                _currentState = State.Error;
                ProcessInError(c, position);
            }
        }

        /// <summary>
        /// Обработка в состоянии тела username
        /// </summary>
        private void ProcessInBody(char c, int position)
        {
            if (IsLetterOrDigit(c))
            {
                // Продолжаем username
                _currentLength++;

                // Проверка на превышение максимальной длины
                if (_currentLength > MaxLength)
                {
                    // Достигнут максимум - сохраняем предыдущий валидный username
                    SaveMatch(position - 1);
                    _currentState = State.Error;
                    ProcessInError(c, position);
                }
            }
            else
            {
                // Конец username - проверяем минимальную длину
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

        /// <summary>
        /// Обработка в конечном состоянии
        /// </summary>
        private void ProcessInEnd(char c, int position)
        {
            if (IsLetter(c))
            {
                // Начинаем новый username
                ResetAutomaton();
                ProcessInStart(c, position);
            }
            else if (IsLetterOrDigit(c))
            {
                // Не может быть, т.к. после валидного username должен быть разделитель
                ResetAutomaton();
                ProcessInStart(c, position);
            }
            else
            {
                ResetAutomaton();
                // Проверяем, не начинается ли новый username с текущего символа
                if (IsLetter(c))
                {
                    ProcessInStart(c, position);
                }
            }
        }

        /// <summary>
        /// Обработка в ошибочном состоянии
        /// </summary>
        private void ProcessInError(char c, int position)
        {
            // Сбрасываем автомат и ищем новое начало
            ResetAutomaton();

            // Проверяем, может ли текущий символ начать новый username
            if (IsLetter(c))
            {
                ProcessInStart(c, position);
            }
        }

        /// <summary>
        /// Сохранение найденного username
        /// </summary>
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

        /// <summary>
        /// Проверка, является ли символ буквой (латиница)
        /// </summary>
        private bool IsLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Проверка, является ли символ буквой или цифрой
        /// </summary>
        private bool IsLetterOrDigit(char c)
        {
            return IsLetter(c) || (c >= '0' && c <= '9');
        }
    }

    /// <summary>
    /// Результат поиска username
    /// </summary>
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