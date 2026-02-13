namespace ObjectIR.Core.Compilers;

using ObjectIR.Core.Builder;
using ObjectIR.Core.IR;
using ObjectIR.Core.Serialization;
using System;
using System.Collections.Generic;

public class PascalCompiler
{
    public Module CompileSource(string source)
    {
        var lexer = new PascalLexer(source ?? string.Empty);
        var tokens = lexer.Tokenize();
        if (lexer.Errors.Count > 0)
        {
            throw new Exception(string.Join("\n", lexer.Errors));
        }
        var parser = new PascalParser(tokens);
        var program = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            throw new Exception(string.Join("\n", parser.Errors));
        }

        var builder = new IRBuilder("PascalProgram");
        var classB = builder.Class("Program");

        var main = classB.Method("Main", TypeReference.Void)
            .Access(AccessModifier.Public)
            .Static();

        // define locals for declared vars
        foreach (var v in program.Vars)
        {
            foreach (var name in v.Names)
            {
                main.Local(name, TypeReference.Int32);
            }
        }

        var ib = main.Body();

        // compile statements
        foreach (var s in program.Body.Statements)
            CompileStatement(s, ib);

        ib.Ret();
        main.EndMethod();
        classB.EndClass();
        return builder.Build();
    }

    private void CompileStatement(PascalStatement stmt, InstructionBuilder ib)
    {
        switch (stmt)
        {
            case PascalAssignment a:
                CompileExpression(a.Value, ib);
                ib.Stloc(a.Target);
                break;
            case PascalProcedureCall p:
                CompileProcedureCall(p, ib);
                break;
            case PascalIfStatement i:
                    // if we have a simple binary equality condition, evaluate it to a boolean on the stack
                    if (i.Condition is PascalBinaryExpression be && be.Operator == "=")
                    {
                        // compile the binary expression which emits the comparison result (Ceq) onto the stack
                        CompileExpression(be, ib);
                        ib.If(Condition.Stack(), thenBuilder =>
                        {
                            foreach (var s in i.ThenBlock.Statements)
                                CompileStatement(s, thenBuilder);
                        }, elseBuilder =>
                        {
                            if (i.ElseBlock != null)
                            {
                                foreach (var s in i.ElseBlock.Statements)
                                    CompileStatement(s, elseBuilder);
                            }
                        });
                    }
                else
                {
                    // fallback: evaluate expr to boolean on stack and use Stack condition
                    CompileExpression(i.Condition, ib);
                    ib.If(Condition.Stack(), thenBuilder =>
                    {
                        foreach (var s in i.ThenBlock.Statements)
                            CompileStatement(s, thenBuilder);
                    }, elseBuilder =>
                    {
                        if (i.ElseBlock != null)
                        {
                            foreach (var s in i.ElseBlock.Statements)
                                CompileStatement(s, elseBuilder);
                        }
                    });
                }
                break;
            case PascalWhileStatement w:
                // emit condition evaluation (will be used by the While condition)
                CompileExpression(w.Condition, ib);
                ib.While(Condition.Stack(), loop =>
                {
                    foreach (var s in w.Body.Statements)
                        CompileStatement(s, loop);
                });
                break;
            case PascalForStatement f:
                CompileForUnrolled(f, ib);
                break;
        }
    }

    private void CompileExpression(PascalExpression expr, InstructionBuilder ib)
    {
        switch (expr)
        {
            case PascalNumberLiteral n:
                ib.LdcI4(n.Value);
                break;
            case PascalStringLiteral s:
                ib.Ldstr(s.Value);
                break;
            case PascalIdentifier id:
                ib.Ldloc(id.Name);
                break;
            case PascalBinaryExpression be:
                var op = be.Operator.ToLowerInvariant();
                if (op == "and")
                {
                    // short-circuit AND: evaluate left, if true evaluate right, else push 0
                    CompileExpression(be.Left, ib);
                    ib.If(Condition.Stack(), thenBuilder =>
                    {
                        CompileExpression(be.Right, thenBuilder);
                    }, elseBuilder =>
                    {
                        elseBuilder.LdcI4(0);
                    });
                }
                else if (op == "or")
                {
                    // short-circuit OR: evaluate left, if true push 1, else evaluate right
                    CompileExpression(be.Left, ib);
                    ib.If(Condition.Stack(), thenBuilder =>
                    {
                        thenBuilder.LdcI4(1);
                    }, elseBuilder =>
                    {
                        CompileExpression(be.Right, elseBuilder);
                    });
                }
                else
                {
                    // arithmetic or relational/comparison
                    if (op == "+" || op == "-" || op == "*" || op == "/")
                    {
                        CompileExpression(be.Left, ib);
                        CompileExpression(be.Right, ib);
                        switch (op)
                        {
                            case "+": ib.Add(); break;
                            case "-": ib.Sub(); break;
                            case "*": ib.Mul(); break;
                            case "/": ib.Div(); break;
                        }
                    }
                    else
                    {
                        // relational/comparison
                        CompileExpression(be.Left, ib);
                        CompileExpression(be.Right, ib);
                        switch (op)
                        {
                            case "=": ib.Ceq(); break;
                            case "<>":
                            case "!=":
                                ib.Ceq(); ib.LdcI4(0); ib.Ceq();
                                break;
                            case "<": ib.Clt(); break;
                            case ">": ib.Cgt(); break;
                            case "<=": ib.Cgt(); ib.LdcI4(0); ib.Ceq(); break;
                            case ">=": ib.Clt(); ib.LdcI4(0); ib.Ceq(); break;
                            default: throw new Exception($"Unsupported binary operator {be.Operator}");
                        }
                    }
                }
                break;
            case PascalUnaryExpression ue:
                var uop = ue.Operator.ToLowerInvariant();
                if (uop == "not")
                {
                    CompileExpression(ue.Operand, ib);
                    ib.LdcI4(0);
                    ib.Ceq();
                }
                else if (uop == "-")
                {
                    // unary minus: emit 0 <operand> sub -> 0 - operand
                    ib.LdcI4(0);
                    CompileExpression(ue.Operand, ib);
                    ib.Sub();
                }
                else
                {
                    throw new Exception($"Unsupported unary operator {ue.Operator}");
                }
                break;
        }
    }

    private void CompileProcedureCall(PascalProcedureCall call, InstructionBuilder ib)
    {
        var name = call.Name.ToLowerInvariant();
        if (name == "writeln" || name == "writeln" )
        {
            var paramTypes = new List<TypeReference>();
            foreach (var arg in call.Arguments)
            {
                if (arg is PascalNumberLiteral) paramTypes.Add(TypeReference.Int32);
                else paramTypes.Add(TypeReference.String);
            }

            var methodRef = new MethodReference(TypeReference.FromName("System"), "Console.WriteLine", TypeReference.Void, paramTypes);

            // push args
            foreach (var arg in call.Arguments)
            {
                CompileExpression(arg, ib);
            }

            ib.Call(methodRef);
        }
        else
        {
            // unknown procedure: ignore
        }
    }

    private void CompileForUnrolled(PascalForStatement f, InstructionBuilder ib)
    {
        // Use the high-level While/Condition APIs to emit a proper loop.
        // Emit initializer, then a pair of loads (var, bound) followed by a While with a Binary condition.
        if (f.From is PascalNumberLiteral from && f.To is PascalNumberLiteral to)
        {
            // init
            ib.LdcI4(from.Value);
            ib.Stloc(f.Variable);

            if (!f.IsDownTo)
            {
                // emit condition loads: load var, load bound
                ib.Ldloc(f.Variable);
                ib.LdcI4(to.Value);

                ib.While(Condition.Binary(ComparisonOp.LessOrEqual), loop =>
                {
                    // body
                    foreach (var s in f.Body.Statements)
                        CompileStatement(s, loop);

                    // increment
                    loop.Ldloc(f.Variable);
                    loop.LdcI4(1);
                    loop.Add();
                    loop.Stloc(f.Variable);
                });
            }
            else
            {
                // down-to: condition var >= bound
                ib.Ldloc(f.Variable);
                ib.LdcI4(to.Value);

                ib.While(Condition.Binary(ComparisonOp.GreaterOrEqual), loop =>
                {
                    foreach (var s in f.Body.Statements)
                        CompileStatement(s, loop);

                    // decrement
                    loop.Ldloc(f.Variable);
                    loop.LdcI4(1);
                    loop.Sub();
                    loop.Stloc(f.Variable);
                });
            }
        }
        else
        {
            // non-constant bounds not supported in this minimal compiler
        }
    }
}

public class PascalLanguageCompiler
{
    public Module CompileSource(string source)
    {
        var c = new PascalCompiler();
        return c.CompileSource(source);
    }

    public string CompileSourceToJson(string source)
    {
        var m = CompileSource(source);
        return m.DumpJson();
    }

    public string CompileSourceToText(string source)
    {
        var m = CompileSource(source);
        return m.DumpText();
    }
}
