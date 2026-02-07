namespace ObjectIR.Core.Compilers;

using System;
using System.Collections.Generic;

public class PascalParser
{
    private readonly List<PascalToken> _tokens;
    private int _cur = 0;

    public PascalParser(List<PascalToken> tokens) => _tokens = tokens;

    public PascalProgram ParseProgram()
    {
        var vars = new List<PascalVarDecl>();

        if (Match(PascalTokenType.Var))
        {
            // read declarations until begin
            while (!Check(PascalTokenType.Begin) && !IsAtEnd())
            {
                vars.Add(ParseVarDecl());
            }
        }

        Consume(PascalTokenType.Begin, "Expected 'begin'");
        var body = ParseBlock();
        // accept optional '.' after end
        if (Match(PascalTokenType.Dot)) { }

        return new PascalProgram(vars, body);
    }

    private PascalVarDecl ParseVarDecl()
    {
        var names = new List<string>();
        names.Add(Consume(PascalTokenType.Identifier, "Expected identifier").Text);
        while (Match(PascalTokenType.Comma))
        {
            names.Add(Consume(PascalTokenType.Identifier, "Expected identifier").Text);
        }
        Consume(PascalTokenType.Colon, "Expected ':'");
        var typeName = Consume(PascalTokenType.Identifier, "Expected type name").Text;
        Consume(PascalTokenType.Semicolon, "Expected ';'");
        return new PascalVarDecl(names, typeName);
    }

    private PascalBlock ParseBlock()
    {
        var stmts = new List<PascalStatement>();
        while (!Check(PascalTokenType.End) && !IsAtEnd())
        {
            stmts.Add(ParseStatement());
        }
        Consume(PascalTokenType.End, "Expected 'end'");
        // optional semicolon
        Match(PascalTokenType.Semicolon);
        return new PascalBlock(stmts);
    }

    private PascalStatement ParseStatement()
    {
        if (Check(PascalTokenType.Identifier))
        {
            // could be assignment or procedure call
            var id = Consume(PascalTokenType.Identifier, "Expected identifier").Text;
            if (Match(PascalTokenType.Assign))
            {
                var expr = ParseExpression();
                Match(PascalTokenType.Semicolon);
                return new PascalAssignment(id, expr);
            }
            else if (Match(PascalTokenType.LeftParen) || Check(PascalTokenType.Semicolon) || Check(PascalTokenType.Comma))
            {
                // procedure call
                List<PascalExpression> args = new();
                if (Previous().Type == PascalTokenType.LeftParen) // we consumed leftparen
                {
                    if (!Check(PascalTokenType.RightParen))
                    {
                        do { args.Add(ParseExpression()); } while (Match(PascalTokenType.Comma));
                    }
                    Consume(PascalTokenType.RightParen, "Expected ')'");
                }
                Match(PascalTokenType.Semicolon);
                return new PascalProcedureCall(id, args);
            }
        }

        if (Match(PascalTokenType.For))
        {
            var variable = Consume(PascalTokenType.Identifier, "Expected loop variable").Text;
            Consume(PascalTokenType.Assign, "Expected ':='");
            var from = ParseExpression();
            bool isDownTo = false;
            if (Match(PascalTokenType.To)) { isDownTo = false; }
            else if (Match(PascalTokenType.Downto)) { isDownTo = true; }
            else throw new ParseException("Expected 'to' or 'downto'");
            var to = ParseExpression();
            Consume(PascalTokenType.Do, "Expected 'do'");
            if (Match(PascalTokenType.Begin))
            {
                var body = ParseBlock();
                return new PascalForStatement(variable, from, to, isDownTo, body);
            }
            else
            {
                var stmt = ParseStatement();
                return new PascalForStatement(variable, from, to, isDownTo, new PascalBlock(new []{ stmt }));
            }
        }

        // procedure call without parens (e.g., WriteLn 'x', i; ) is uncommon in modern Pascal; we'll require parens
        throw new ParseException($"Unsupported statement at token {Peek().Text}");
    }

    private PascalExpression ParseExpression()
    {
        if (Match(PascalTokenType.Number)) return new PascalNumberLiteral(int.Parse(Previous().Text));
        if (Match(PascalTokenType.String)) return new PascalStringLiteral(Previous().Text);
        if (Match(PascalTokenType.Identifier)) return new PascalIdentifier(Previous().Text);
        throw new ParseException($"Unexpected expression token: {Peek().Text}");
    }

    private bool Match(PascalTokenType t)
    {
        if (Check(t)) { Advance(); return true; }
        return false;
    }

    private PascalToken Consume(PascalTokenType t, string msg)
    {
        if (Check(t)) return Advance();
        throw new ParseException(msg + " at " + Peek().Text);
    }

    private bool Check(PascalTokenType t)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == t;
    }

    private PascalToken Advance() { if (!IsAtEnd()) _cur++; return Previous(); }
    private bool IsAtEnd() => Peek().Type == PascalTokenType.EOF;
    private PascalToken Peek() => _tokens[_cur];
    private PascalToken Previous() => _tokens[_cur - 1];
}
