namespace ObjectIR.Core.Compilers;

using System;
using System.Collections.Generic;

public enum PascalTokenType
{
    Identifier,
    Number,
    String,
    Var,
    Begin,
    End,
    For,
    To,
    Downto,
    Do,
    Semicolon,
    Colon,
    Comma,
    Assign, // :=
    Dot,
    LeftParen,
    RightParen,
    EOF
}

public record PascalToken(PascalTokenType Type, string Text, int Line, int Column);

public class PascalLexer
{
    private readonly string _src;
    private int _pos = 0;
    private int _line = 1;
    private int _col = 1;

    public PascalLexer(string src) => _src = src ?? string.Empty;

    public List<PascalToken> Tokenize()
    {
        var tokens = new List<PascalToken>();

        while (_pos < _src.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _src.Length) break;

            var ch = _src[_pos];
            if (char.IsLetter(ch))
            {
                tokens.Add(ReadIdentifierOrKeyword());
            }
            else if (char.IsDigit(ch))
            {
                tokens.Add(ReadNumber());
            }
            else if (ch == '\'')
            {
                tokens.Add(ReadString());
            }
            else
            {
                switch (ch)
                {
                    case ';': tokens.Add(MakeToken(PascalTokenType.Semicolon, ";")); Advance(); break;
                    case ':':
                        if (Peek() == '=') { tokens.Add(MakeToken(PascalTokenType.Assign, ":=")); Advance(); Advance(); }
                        else { tokens.Add(MakeToken(PascalTokenType.Colon, ":")); Advance(); }
                        break;
                    case ',': tokens.Add(MakeToken(PascalTokenType.Comma, ",")); Advance(); break;
                    case '.': tokens.Add(MakeToken(PascalTokenType.Dot, ".")); Advance(); break;
                    case '(' : tokens.Add(MakeToken(PascalTokenType.LeftParen, "(")); Advance(); break;
                    case ')' : tokens.Add(MakeToken(PascalTokenType.RightParen, ")")); Advance(); break;
                    default:
                        throw new ParseException($"Unexpected character '{ch}' at {_line}:{_col}");
                }
            }
        }

        tokens.Add(new PascalToken(PascalTokenType.EOF, string.Empty, _line, _col));
        return tokens;
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _src.Length)
        {
            var ch = _src[_pos];
            if (char.IsWhiteSpace(ch))
            {
                if (ch == '\n') { _line++; _col = 1; } else { _col++; }
                _pos++;
            }
            else if (ch == '{')
            {
                // skip until }
                _pos++;
                while (_pos < _src.Length && _src[_pos] != '}')
                {
                    if (_src[_pos] == '\n') { _line++; _col = 1; }
                    _pos++;
                }
                if (_pos < _src.Length) { _pos++; _col++; }
            }
            else if (ch == '/' && Peek() == '/')
            {
                // line comment
                while (_pos < _src.Length && _src[_pos] != '\n') _pos++;
            }
            else
            {
                break;
            }
        }
    }

    private PascalToken ReadIdentifierOrKeyword()
    {
        int start = _pos;
        int colStart = _col;
        while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_'))
        {
            _pos++; _col++;
        }

        var text = _src[start.._pos];
        var lower = text.ToLowerInvariant();
        var type = lower switch
        {
            "var" => PascalTokenType.Var,
            "begin" => PascalTokenType.Begin,
            "end" => PascalTokenType.End,
            "for" => PascalTokenType.For,
            "to" => PascalTokenType.To,
            "downto" => PascalTokenType.Downto,
            "do" => PascalTokenType.Do,
            _ => PascalTokenType.Identifier
        };

        return new PascalToken(type, text, _line, colStart);
    }

    private PascalToken ReadNumber()
    {
        int start = _pos; int colStart = _col;
        while (_pos < _src.Length && char.IsDigit(_src[_pos])) { _pos++; _col++; }
        var text = _src[start.._pos];
        return new PascalToken(PascalTokenType.Number, text, _line, colStart);
    }

    private PascalToken ReadString()
    {
        int colStart = _col;
        // opening '
        Advance();
        int start = _pos;
        while (_pos < _src.Length && _src[_pos] != '\'') { _pos++; _col++; }
        if (_pos >= _src.Length) throw new ParseException("Unterminated string");
        var text = _src[start.._pos];
        Advance(); // skip closing '
        return new PascalToken(PascalTokenType.String, text, _line, colStart);
    }

    private char Peek() => (_pos + 1) < _src.Length ? _src[_pos + 1] : '\0';
    private void Advance() { _pos++; _col++; }
    private PascalToken MakeToken(PascalTokenType t, string txt) => new PascalToken(t, txt, _line, _col);
}

public class ParseException : Exception { public ParseException(string m): base(m){} }
