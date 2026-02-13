namespace ObjectIR.Core.Compilers;

using System;
using System.Collections.Generic;

public abstract class PascalNode { }

public class PascalProgram : PascalNode
{
    public List<PascalVarDecl> Vars { get; } = new();
    public PascalBlock Body { get; set; }

    public PascalProgram(List<PascalVarDecl> vars, PascalBlock body)
    {
        Vars.AddRange(vars);
        Body = body;
    }
}

public class PascalVarDecl : PascalNode
{
    public List<string> Names { get; } = new();
    public string TypeName { get; }

    public PascalVarDecl(IEnumerable<string> names, string typeName)
    {
        Names.AddRange(names);
        TypeName = typeName;
    }
}
public class PascalBlock : PascalNode
{
    public List<PascalStatement> Statements { get; } = new();

    public PascalBlock(IEnumerable<PascalStatement> statements)
    {
        Statements.AddRange(statements);
    }
}

public abstract class PascalStatement : PascalNode { }

public class PascalAssignment : PascalStatement
{
    public string Target { get; }
    public PascalExpression Value { get; }

    public PascalAssignment(string target, PascalExpression value)
    {
        Target = target;
        Value = value;
    }
}

public class PascalForStatement : PascalStatement
{
    public string Variable { get; }
    public PascalExpression From { get; }
    public PascalExpression To { get; }
    public bool IsDownTo { get; }
    public PascalBlock Body { get; }

    public PascalForStatement(string variable, PascalExpression from, PascalExpression to, bool isDownTo, PascalBlock body)
    {
        Variable = variable;
        From = from;
        To = to;
        IsDownTo = isDownTo;
        Body = body;
    }
}

public class PascalProcedureCall : PascalStatement
{
    public string Name { get; }
    public List<PascalExpression> Arguments { get; }

    public PascalProcedureCall(string name, IEnumerable<PascalExpression> args)
    {
        Name = name;
        Arguments = new List<PascalExpression>(args);
    }
}

public class PascalIfStatement : PascalStatement
{
    public PascalExpression Condition { get; }
    public PascalBlock ThenBlock { get; }
    public PascalBlock? ElseBlock { get; }

    public PascalIfStatement(PascalExpression condition, PascalBlock thenBlock, PascalBlock? elseBlock)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
    }
}

public class PascalWhileStatement : PascalStatement
{
    public PascalExpression Condition { get; }
    public PascalBlock Body { get; }

    public PascalWhileStatement(PascalExpression condition, PascalBlock body)
    {
        Condition = condition;
        Body = body;
    }
}

public class PascalBinaryExpression : PascalExpression
{
    public PascalExpression Left { get; }
    public string Operator { get; }
    public PascalExpression Right { get; }

    public PascalBinaryExpression(PascalExpression left, string op, PascalExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

public class PascalUnaryExpression : PascalExpression
{
    public string Operator { get; }
    public PascalExpression Operand { get; }

    public PascalUnaryExpression(string op, PascalExpression operand)
    {
        Operator = op;
        Operand = operand;
    }
}

public abstract class PascalExpression : PascalNode { }

public class PascalNumberLiteral : PascalExpression
{
    public int Value { get; }
    public PascalNumberLiteral(int v) => Value = v;
}

public class PascalStringLiteral : PascalExpression
{
    public string Value { get; }
    public PascalStringLiteral(string v) => Value = v;
}

public class PascalIdentifier : PascalExpression
{
    public string Name { get; }
    public PascalIdentifier(string n) => Name = n;
}
