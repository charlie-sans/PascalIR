namespace ObjectIR.Core.Compilers;

using System;
using System.Collections.Generic;

public class PascalParser
{
    private readonly List<PascalToken> _tokens;
    private int _cur = 0;
    public List<string> Errors { get; } = new();

    public PascalParser(List<PascalToken> tokens) => _tokens = tokens;

    public PascalProgram ParseProgram()
    {
        var vars = new List<PascalVarDecl>();

        // optional program header: program Name;
        if (Match(PascalTokenType.Program))
        {
            Consume(PascalTokenType.Identifier, "Expected program name");
            Match(PascalTokenType.Semicolon);
        }

        // optional uses clause: uses Unit1, Unit2;
        if (Match(PascalTokenType.Uses))
        {
            // consume identifiers separated by commas until semicolon
            do { Consume(PascalTokenType.Identifier, "Expected unit name"); } while (Match(PascalTokenType.Comma));
            Match(PascalTokenType.Semicolon);
        }

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
        if (Match(PascalTokenType.If))
        {
            var condition = ParseExpression();
            Consume(PascalTokenType.Then, "Expected 'then'");
            PascalBlock thenBlock;
            if (Match(PascalTokenType.Begin)) thenBlock = ParseBlock();
            else
            {
                var s = ParseStatement();
                thenBlock = new PascalBlock(new[] { s });
            }

            PascalBlock? elseBlock = null;
            if (Match(PascalTokenType.Else))
            {
                if (Match(PascalTokenType.Begin)) elseBlock = ParseBlock();
                else { var s2 = ParseStatement(); elseBlock = new PascalBlock(new[] { s2 }); }
            }

            return new PascalIfStatement(condition, thenBlock, elseBlock);
        }

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
            else
            {
                Errors.Add($"Expected 'to' or 'downto' at {Peek().Text}");
            }
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

        if (Match(PascalTokenType.While))
        {
            var cond = ParseExpression();
            Consume(PascalTokenType.Do, "Expected 'do'");
            if (Match(PascalTokenType.Begin))
            {
                var body = ParseBlock();
                return new PascalWhileStatement(cond, body);
            }
            else
            {
                var stmt = ParseStatement();
                return new PascalWhileStatement(cond, new PascalBlock(new []{ stmt }));
            }
        }

        // procedure call without parens (e.g., WriteLn 'x', i; ) is uncommon in modern Pascal; we'll require parens
        Errors.Add($"Unsupported statement at token {Peek().Text}");
        // simple sync: advance until semicolon or end of block
        while (!Check(PascalTokenType.Semicolon) && !Check(PascalTokenType.End) && !IsAtEnd()) Advance();
        Match(PascalTokenType.Semicolon);
        return new PascalProcedureCall("_skip", new List<PascalExpression>());
    }

    private PascalExpression ParseExpression()
    {
        return ParseOr();
    }

    // or -> and ( 'or' and )*
    private PascalExpression ParseOr()
    {
        var left = ParseAnd();
        while (Match(PascalTokenType.Or))
        {
            var right = ParseAnd();
            left = new PascalBinaryExpression(left, "or", right);
        }
        return left;
    }

    // and -> not ( 'and' not )*
    private PascalExpression ParseAnd()
    {
        var left = ParseNot();
        while (Match(PascalTokenType.And))
        {
            var right = ParseNot();
            left = new PascalBinaryExpression(left, "and", right);
        }
        return left;
    }

    // not -> 'not' not | comparison
    private PascalExpression ParseNot()
    {
        if (Match(PascalTokenType.Not))
        {
            var operand = ParseNot();
            return new PascalUnaryExpression("not", operand);
        }
        if (Match(PascalTokenType.Minus))
        {
            var operand = ParseNot();
            return new PascalUnaryExpression("-", operand);
        }
        return ParseComparison();
    }

    // comparison -> additive (compOp additive)?
    private PascalExpression ParseComparison()
    {
        var left = ParseAdditive();
        while (Check(PascalTokenType.Equals) || Check(PascalTokenType.NotEqual) || Check(PascalTokenType.Less) || Check(PascalTokenType.Greater) || Check(PascalTokenType.LessOrEqual) || Check(PascalTokenType.GreaterOrEqual))
        {
            var tok = Advance();
            var op = tok.Text;
            var right = ParseAdditive();
            left = new PascalBinaryExpression(left, op, right);
        }
        return left;
    }

    // additive -> multiplicative ( ('+'|'-') multiplicative )*
    private PascalExpression ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (Check(PascalTokenType.Plus) || Check(PascalTokenType.Minus))
        {
            var tok = Advance();
            var op = tok.Text;
            var right = ParseMultiplicative();
            left = new PascalBinaryExpression(left, op, right);
        }
        return left;
    }

    // multiplicative -> primary ( ('*'|'/') primary )*
    private PascalExpression ParseMultiplicative()
    {
        var left = ParsePrimary();
        while (Check(PascalTokenType.Star) || Check(PascalTokenType.Slash))
        {
            var tok = Advance();
            var op = tok.Text;
            var right = ParsePrimary();
            left = new PascalBinaryExpression(left, op, right);
        }
        return left;
    }

    private PascalExpression ParsePrimary()
    {
        if (Match(PascalTokenType.Number)) return new PascalNumberLiteral(int.Parse(Previous().Text));
        if (Match(PascalTokenType.String)) return new PascalStringLiteral(Previous().Text);
        if (Match(PascalTokenType.True)) return new PascalNumberLiteral(1);
        if (Match(PascalTokenType.False)) return new PascalNumberLiteral(0);
        if (Match(PascalTokenType.Identifier)) return new PascalIdentifier(Previous().Text);
        if (Match(PascalTokenType.LeftParen))
        {
            var expr = ParseExpression();
            Consume(PascalTokenType.RightParen, "Expected ')'");
            return expr;
        }
        Errors.Add($"Unexpected expression token: {Peek().Text}");
        return new PascalNumberLiteral(0);
    }

    private bool Match(PascalTokenType t)
    {
        if (Check(t)) { Advance(); return true; }
        return false;
    }

    private PascalToken Consume(PascalTokenType t, string msg)
    {
        if (Check(t)) return Advance();
        Errors.Add(msg + " at " + Peek().Text);
        // try to recover by advancing until we find the expected token or a synchronization point
        while (!Check(t) && !IsAtEnd() && !Check(PascalTokenType.Semicolon) && !Check(PascalTokenType.End) && !Check(PascalTokenType.Begin)) Advance();
        if (Check(t)) return Advance();
        // fabricate a token of the expected type at current position
        var p = Peek();
        return new PascalToken(t, string.Empty, p.Line, p.Column);
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
