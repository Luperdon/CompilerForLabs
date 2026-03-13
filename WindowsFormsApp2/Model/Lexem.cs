using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2.Model
{
    public class Lexem
    {
        private int _lexemCode;

        public string lexemContaintment { get; set; }
        public int lexemStartPosition { get; set; }
        public int lexemEndPosition { get; set; }
        public int lexemLine { get; set; }
        public LexemType lexemType { get; private set; }
        public string lexemName { get; private set; }

        public int lexemCode
        {
            get { return _lexemCode; }
            set
            {
                _lexemCode = value;
                SetLexemTypeAndName(value);
            }
        }

        public enum LexemType
        {
            Identificator,          // code 1
            KeyWord,                // code 2
            CharInString,           // code 3
            AssignmentOperator,     // code 4
            Quote,                  // code 5
            F_stringAssign,         // code 6
            Minus,                  // code 7
            Plus,                   // code 8
            Dot,                    // code 9
            MinusComlexInt,         // code 10
            PlusComplexInt,          // code 11
            Int,                    // code 12
            Double,                 // code 13
            MinusComplexDouble,      // code 14
            PlusComplexDouble,       // code 15
            OpenScobe,              // code 16
            CloseScobe,             // code 17
            Ending,                 // code 18
            Error,                  // code 19
            FormatSpecifier,        // code 20
            OpenBrace,              // code 21
            Colon,                  // code 22
            LetterF,                // code 23
            CloseBrace,             // code 24
            StringChar              // code 25
        };

        public Lexem(int code, string containtment, int startPosition, int endPosition, int line = 1)
        {
            lexemCode = code;
            lexemContaintment = containtment;
            lexemStartPosition = startPosition;
            lexemEndPosition = endPosition;
            lexemLine = line; 
        }

        private void SetLexemTypeAndName(int value)
        {
            switch (value)
            {
                case 1:
                    lexemType = LexemType.Identificator;
                    lexemName = "Идентификатор";
                    break;
                case 2:
                    lexemType = LexemType.KeyWord;
                    lexemName = "Ключевое слово";
                    break;
                case 3:
                    lexemType = LexemType.CharInString;
                    lexemName = "Символ в строке";
                    break;
                case 4:
                    lexemType = LexemType.AssignmentOperator;
                    lexemName = "Оператор присваивания";
                    break;
                case 5:
                    lexemType = LexemType.Quote;
                    lexemName = "Кавычка";
                    break;
                case 6:
                    lexemType = LexemType.F_stringAssign;
                    lexemName = "F-строка";
                    break;
                case 7:
                    lexemType = LexemType.Minus;
                    lexemName = "Минус";
                    break;
                case 8:
                    lexemType = LexemType.Plus;
                    lexemName = "Плюс";
                    break;
                case 9:
                    lexemType = LexemType.Dot;
                    lexemName = "Точка";
                    break;
                case 10:
                    lexemType = LexemType.MinusComlexInt;
                    lexemName = "Отрицательное целое";
                    break;
                case 11:
                    lexemType = LexemType.PlusComplexInt;
                    lexemName = "Положительное целое";
                    break;
                case 12:
                    lexemType = LexemType.Int;
                    lexemName = "Целое число";
                    break;
                case 13:
                    lexemType = LexemType.Double;
                    lexemName = "Вещественное число";
                    break;
                case 14:
                    lexemType = LexemType.MinusComplexDouble;
                    lexemName = "Отрицательное вещественное";
                    break;
                case 15:
                    lexemType = LexemType.PlusComplexDouble;
                    lexemName = "Положительное вещественное";
                    break;
                case 16:
                    lexemType = LexemType.OpenScobe;
                    lexemName = "Открывающая скобка";
                    break;
                case 17:
                    lexemType = LexemType.CloseScobe;
                    lexemName = "Закрывающая скобка";
                    break;
                case 18:
                    lexemType = LexemType.Ending;
                    lexemName = "Точка с запятой";
                    break;
                case 19:
                    lexemType = LexemType.Error;
                    lexemName = "Ошибка";
                    break;
                case 20:
                    lexemType = LexemType.FormatSpecifier;
                    lexemName = "Форматный спецификатор";
                    break;
                case 21:
                    lexemType = LexemType.OpenBrace;
                    lexemName = "Открывающая фигурная скобка";
                    break;
                case 22:
                    lexemType = LexemType.Colon;
                    lexemName = "Двоеточие";
                    break;
                case 23:
                    lexemType = LexemType.LetterF;
                    lexemName = "Буква f";
                    break;
                case 24:
                    lexemType = LexemType.CloseBrace;
                    lexemName = "Закрывающая фигурная скобка";
                    break;
                case 25:
                    lexemType = LexemType.StringChar;
                    lexemName = "Символ строки";
                    break;
                default:
                    lexemType = LexemType.Error;
                    lexemName = "Неизвестный тип";
                    break;
            }
        }

        public string GetFormattedString()
        {
            string typeCode = _lexemCode.ToString().PadRight(6);
            string typeDesc = (lexemName?.Length > 15) ? lexemName.Substring(0, 12) + "..." : (lexemName ?? "Unknown");
            typeDesc = typeDesc.PadRight(18);

            string value = (lexemContaintment?.Length > 15) ? lexemContaintment.Substring(0, 12) + "..." : (lexemContaintment ?? "");
            value = value.PadRight(15);

            string location = $"стр.{lexemLine} ({lexemStartPosition}-{lexemEndPosition})";

            if (lexemType == LexemType.Error)
            {
                return $"ОШИБКА: {lexemContaintment} в {location}";
            }

            return $"{typeCode} {typeDesc} {value} {location}";
        }

        public override string ToString()
        {
            return GetFormattedString();
        }
    }
}