using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal class Tokenizer
{
    internal static readonly char EndOfFileChar = '\0';

    internal string Input = String.Empty;

    internal Cursor Cursor = new();
    internal Cursor PreviousCursor = new();

    internal Tokenizer()
    {
    }

    internal void Reset(string input, int startingLine = 0)
    {
        Input = input;

        Cursor.Position = 0;
        Cursor.Line = startingLine;
    }

    internal char GetChar()
    {
        PreviousCursor = Cursor;

        if (IsEOF())
        {
            Cursor.Position++;
            return EndOfFileChar;
        }

        char c = Input[Cursor.Position];

        if (c == '\n')
            Cursor.Line++;

        Cursor.Position++;
        return c;
    }

    internal void UngetChar()
    {
        Cursor = PreviousCursor;
    }

    internal char Peek()
    {
        return !IsEOF() ? Input[Cursor.Position] : EndOfFileChar;
    }

    internal char GetLeadingChar()
    {
        char c;

        for (c = GetChar(); c != Tokenizer.EndOfFileChar; c = GetChar())
        {
            if (c == '\n')
                continue;

            if (char.IsWhiteSpace(c) || char.IsControl(c))
                continue;

            char next = Peek();
            if (c == '/' && next == '/')
            {
                int indentationLastLine = 0;
                while (!IsEOF() && c == '/' && next == '/')
                {
                    string line = string.Empty;
                    for (c = GetChar();
                      c != EndOfFileChar && c != '\n';
                      c = GetChar())
                    {
                        line += c;
                    }

                    int? lastSlashIndex = line.Select((x, i) => new { Val = x, Idx = (int?)i })
                        .Where(x => "/".IndexOf(x.Val) == -1)
                        .Select(x => x.Idx)
                        .FirstOrDefault();

                    if (lastSlashIndex == null)
                        line = "";
                    else
                        line = line.Substring((int)lastSlashIndex);

                    int? firstCharIndex = line.Select((x, i) => new { Val = x, Idx = (int?)i })
                        .Where(x => " \t".IndexOf(x.Val) == -1)
                        .Select(x => x.Idx)
                        .FirstOrDefault();

                    if (firstCharIndex == null)
                        line = "";
                    else
                        line = line.Substring((int)firstCharIndex);

                    if (firstCharIndex > indentationLastLine && line.Length > 0)
                    {

                    }
                    else
                    {

                    }

                    while (!IsEOF() && char.IsWhiteSpace(c = GetChar())) ;

                    if (!IsEOF())
                        next = Peek();
                }

                if (!IsEOF())
                    UngetChar();

                continue;
            }

            if (c == '/' && next == '*')
            {
                for (c = GetChar(), next = Peek();
                    c != EndOfFileChar && (c != '*' || next != '/');
                    c = GetChar(), next = Peek())
                {

                }

                if (c != Tokenizer.EndOfFileChar)
                    GetChar();

                while (!IsEOF() && char.IsWhiteSpace(c = GetChar())) ;
                if (!IsEOF())
                    UngetChar();

                continue;
            }

            break;
        }

        return c;
    }

    internal static bool Pair(char a0, char a1, char b0, char b1)
    {
        return a0 == a1 && b0 == b1;
    }

    internal bool GetToken(ref Token token)
    {
        char c = GetLeadingChar();
        char p = Peek();

        if (c == EndOfFileChar)
        {
            UngetChar();
            return false;
        }

        token.StartPosition = PreviousCursor.Position;
        token.StartLine = PreviousCursor.Line;

        if (char.IsLetter(c) || c == '_')
        {
            do
            {
                token.Identifier += c;
                c = GetChar();
            } while (char.IsLetterOrDigit(c) || c == '_');

            UngetChar();

            return true;
        }
        else
        {
            token.Identifier += c;

            char d = GetChar();
            if (Pair(c, '<', d, '<') ||
               Pair(c, '-', d, '>') ||
               //(!seperateBraces && Pair(c, '>', d, '>')) ||
               (!false && Pair(c, '>', d, '>')) ||
               Pair(c, '!', d, '=') ||
               Pair(c, '<', d, '=') ||
               Pair(c, '>', d, '=') ||
               Pair(c, '+', d, '+') ||
               Pair(c, '-', d, '-') ||
               Pair(c, '+', d, '=') ||
               Pair(c, '-', d, '=') ||
               Pair(c, '*', d, '=') ||
               Pair(c, '/', d, '=') ||
               Pair(c, '^', d, '=') ||
               Pair(c, '|', d, '=') ||
               Pair(c, '&', d, '=') ||
               Pair(c, '~', d, '=') ||
               Pair(c, '%', d, '=') ||
               Pair(c, '&', d, '&') ||
               Pair(c, '|', d, '|') ||
               Pair(c, '=', d, '=') ||
               Pair(c, ':', d, ':')
              )
            {
                token.Identifier += d;
            }
            else
                UngetChar();

            return true;
        }
    }

    internal bool IsEOF()
    {
        return Cursor.Position >= Input.Length;
    }

    internal void UngetToken(ref Token token)
    {
        Cursor.Position = token.StartPosition;
        Cursor.Line = token.StartLine;
    }
}
