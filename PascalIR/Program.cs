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

	// Expand include directives recursively. Syntax: //[include "relative/path.ext"]
	string ExpandIncludes(string content, string baseDir, HashSet<string> seen)
	{
		var sb = new System.Text.StringBuilder();
		using (var sr = new StringReader(content))
		{
			string? line;
			while ((line = sr.ReadLine()) != null)
			{
				var trimmed = line.TrimStart();
				if (trimmed.StartsWith("//[include", StringComparison.OrdinalIgnoreCase))
				{
					var firstQuote = trimmed.IndexOf('"');
					var lastQuote = trimmed.LastIndexOf('"');
					if (firstQuote >= 0 && lastQuote > firstQuote)
					{
						var incPath = trimmed.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
						var resolved = Path.GetFullPath(Path.Combine(baseDir, incPath));
						if (seen.Contains(resolved))
						{
							Console.WriteLine($"Skipping already-included file: {resolved}");
							continue;
						}
						if (File.Exists(resolved))
						{
							seen.Add(resolved);
							var incContent = File.ReadAllText(resolved);
							var incBase = Path.GetDirectoryName(resolved) ?? baseDir;
							sb.AppendLine(ExpandIncludes(incContent, incBase, seen));
							continue;
						}
						else
						{
							Console.WriteLine($"Include not found: {resolved}");
						}
					}
				}

				sb.AppendLine(line);
			}
		}
		return sb.ToString();
	}

	var srcDir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";
	var expanded = ExpandIncludes(source, srcDir, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
	var compiler = new PascalLanguageCompiler();
	var module = compiler.CompileSource(expanded);

	var irCode = module.Serialize().DumpToIRCode();
	var outPath = Path.ChangeExtension(inputPath, ".oir");
	File.WriteAllText(outPath, irCode);
	Console.WriteLine($"Wrote IR code: {outPath}");
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}

