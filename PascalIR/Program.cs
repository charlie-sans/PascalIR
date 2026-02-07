using System;
using System.IO;
using ObjectIR.Core.Compilers;
using ObjectIR.Core.Serialization;
using static ObjectIR.Core.Serialization.ModuleSerializationExtensions;

if (args.Length == 0)
{
	Console.WriteLine("Usage: pair <source.pas>");
	return;
}

var inputPath = args[0];
if (!File.Exists(inputPath))
{
	Console.WriteLine($"Source file not found: {inputPath}");
	return;
}

try
{
	var source = File.ReadAllText(inputPath);
	var compiler = new PascalLanguageCompiler();
	var module = compiler.CompileSource(source);

	var irCode = module.Serialize().DumpToIRCode();
	var outPath = Path.ChangeExtension(inputPath, ".oir");
	File.WriteAllText(outPath, irCode);
	Console.WriteLine($"Wrote IR code: {outPath}");
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}

