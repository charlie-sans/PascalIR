namespace ObjectIR.Core.Compilers;

using ObjectIR.Core.Builder;
using ObjectIR.Core.IR;
using ObjectIR.Core.Serialization;
using System.Collections.Generic;

public class PascalCompiler
{
    public Module CompileSource(string source)
    {
        var lexer = new PascalLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new PascalParser(tokens);
        var program = parser.ParseProgram();

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
