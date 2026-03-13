using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2.Model
{
    public class Lexer
    {
        private string text;
        private int position;
        private int currentLine;
        private List<Lexem> lexemsList;

        public Lexer(string textToScan)
        {
            text = textToScan ?? "";
            position = 0;
            currentLine = 1;
            lexemsList = new List<Lexem>();
        }

        public List<Lexem> Scan()
        {
            while (position < text.Length)
            {
                char currentChar = text[position];

                if (currentChar == '\n')
                {
                    currentLine++;
                    position++;
                    continue;
                }

                if (currentChar == ' ' || currentChar == '\t' || currentChar == '\r')
                {
                    position++;
                    continue;
                }

                if (char.IsLetter(currentChar) || currentChar == '_')
                {
                    ProcessIdentifier();
                }
                else if (char.IsDigit(currentChar) || currentChar == '+' || currentChar == '-')
                {
                    ProcessNumber();
                }
                else
                {
                    if (ProcessSymbol()) break;
                }
            }
            return lexemsList;
        }

        private void ProcessIdentifier()
        {
            int start = position;
            int line = currentLine;

            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
            {
                position++;
            }

            string lexeme = text.Substring(start, position - start);

            if (lexeme == "format")
            {
                lexemsList.Add(new Lexem(2, lexeme, start, position, line));
            }
            else
            {
                lexemsList.Add(new Lexem(1, lexeme, start, position, line));
            }
        }

        private void ProcessNumber()
        {
            int start = position;
            int line = currentLine; 
            bool hasDot = false;
            bool hasExp = false;
            bool valid = true;

            if (text[position] == '+' || text[position] == '-')
            {
                position++;
            }

            while (position < text.Length && char.IsDigit(text[position]))
            {
                position++;
            }

            if (position < text.Length && text[position] == '.')
            {
                hasDot = true;
                position++;

                while (position < text.Length && char.IsDigit(text[position]))
                {
                    position++;
                }
            }

            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                hasExp = true;
                position++;

                if (position < text.Length && (text[position] == '+' || text[position] == '-'))
                {
                    position++;
                }

                if (position < text.Length && char.IsDigit(text[position]))
                {
                    while (position < text.Length && char.IsDigit(text[position]))
                    {
                        position++;
                    }
                }
                else
                {
                    valid = false;
                }
            }

            string lexeme = text.Substring(start, position - start);

            if (!valid)
            {
                lexemsList.Add(new Lexem(19, lexeme, start, position, line));
            }
            else if (hasExp)
            {
                if (lexeme.Contains('-') && lexeme.IndexOf('-') > 0)
                    lexemsList.Add(new Lexem(14, lexeme, start, position, line));
                else if (lexeme.Contains('+') && lexeme.IndexOf('+') > 0)
                    lexemsList.Add(new Lexem(15, lexeme, start, position, line));
                else
                    lexemsList.Add(new Lexem(13, lexeme, start, position, line));
            }
            else if (hasDot)
            {
                lexemsList.Add(new Lexem(13, lexeme, start, position, line));
            }
            else
            {
                lexemsList.Add(new Lexem(12, lexeme, start, position, line));
            }
        }

        private bool ProcessSymbol()
        {
            int start = position;
            int line = currentLine;
            char currentChar = text[position++];
            int code;

            switch (currentChar)
            {
                case '=':
                    code = 4;
                    break;
                case '"':
                    ProcessString();
                    return false;
                case '-':
                    code = 7;
                    break;
                case '+':
                    code = 8;
                    break;
                case '.':
                    code = 9;
                    break;
                case '(':
                    code = 16;
                    break;
                case ')':
                    code = 17;
                    break;
                case ';':
                    code = 18;
                    break;
                default:
                    code = 19;
                    break;
            }

            lexemsList.Add(new Lexem(code, currentChar.ToString(), start, position, line));
            return false;
        }

        private void ProcessString()
        {
            int start = position - 1;
            int line = currentLine;

            lexemsList.Add(new Lexem(5, "\"", start, position, line));

            while (position < text.Length)
            {
                char currentChar = text[position];

                if (currentChar == '\n')
                {
                    currentLine++;
                    lexemsList.Add(new Lexem(3, "\\n", position, position + 1, line));
                    position++;
                    continue;
                }

                if (currentChar == '"')
                {
                    position++;
                    lexemsList.Add(new Lexem(5, "\"", position - 1, position, line));
                    return;
                }

                if (currentChar == '{' && position + 3 < text.Length &&
                    text[position + 1] == ':' &&
                    text[position + 2] == 'f' &&
                    text[position + 3] == '}')
                {
                    lexemsList.Add(new Lexem(21, "{", position, position + 1, line));
                    position++;

                    lexemsList.Add(new Lexem(22, ":", position, position + 1, line));
                    position++;

                    lexemsList.Add(new Lexem(23, "f", position, position + 1, line));
                    position++;

                    lexemsList.Add(new Lexem(24, "}", position, position + 1, line));
                    position++;
                }
                else
                {
                    if (currentChar == '{' || currentChar == ':' || currentChar == 'f' || currentChar == '}')
                    {
                        int code = 25;
                        lexemsList.Add(new Lexem(code, currentChar.ToString(), position, position + 1, line));
                    }
                    else
                    {
                        lexemsList.Add(new Lexem(3, currentChar.ToString(), position, position + 1, line));
                    }
                    position++;
                }
            }

            lexemsList.Add(new Lexem(19, "Незакрытая строка", start, position, line));
        }
    }
}