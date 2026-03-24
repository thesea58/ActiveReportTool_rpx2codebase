using RpxCodeGenerator.Generators;
using RpxCodeGenerator.Parsers;

// Configuration
const string rpxDirectory = "./rpx_folder";
const string outputDirectory = "./output";

// Optional input argument: absolute path of an RPX file or a folder containing RPX files.
// If omitted, all RPX files in the hardcoded rpxDirectory will be processed.
// Examples:
//   dotnet run -- /absolute/path/to/KP031110.rpx
//   dotnet run -- /absolute/path/to/rpx_folder
string? inputArg = args.Length > 0 ? args[0] : null;

// Create output directory if not exists
Directory.CreateDirectory(outputDirectory);

Console.WriteLine("╔════════════════════════════════════════════╗");
Console.WriteLine("║   RPX to C# Code Generator Tool           ║");
Console.WriteLine("║   Convert RPX files to initialization code║");
Console.WriteLine("╚════════════════════════════════════════════╝");
Console.WriteLine();

try
{
    var parser = new RpxParser();
    var codeGenerator = new CodeGenerator();

    // Get all RPX files in the directory
    if (!Directory.Exists(rpxDirectory))
    {
        Console.WriteLine($"❌ Error: Directory not found: {rpxDirectory}");
        Console.WriteLine($"   Current directory: {Directory.GetCurrentDirectory()}");
        return;
    }

    var rpxFiles = Directory.GetFiles(rpxDirectory, "*.rpx")
        .OrderBy(f => Path.GetFileName(f))
        .ToList();

    if (rpxFiles.Count == 0)
    {
        Console.WriteLine("❌ No RPX files found in: " + rpxDirectory);
        return;
    }

    Console.WriteLine($"📁 Found {rpxFiles.Count} RPX files");
    Console.WriteLine();

    List<string> filesToProcess;
    if (!string.IsNullOrWhiteSpace(inputArg))
    {
        var resolvedPath = Path.GetFullPath(inputArg);

        if (Directory.Exists(resolvedPath))
        {
            // Argument is a folder — process all RPX files inside it
            filesToProcess = Directory.GetFiles(resolvedPath, "*.rpx")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            if (filesToProcess.Count == 0)
            {
                Console.WriteLine($"❌ No RPX files found in folder: {resolvedPath}");
                return;
            }

            Console.WriteLine($"📂 Folder mode: {resolvedPath} ({filesToProcess.Count} files)");
            Console.WriteLine();
        }
        else if (File.Exists(resolvedPath))
        {
            // Argument is a single file
            filesToProcess = [resolvedPath];
            Console.WriteLine($"🎯 Single-file mode: {Path.GetFileName(resolvedPath)}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"❌ Path not found: {resolvedPath}");
            return;
        }
    }
    else
    {
        // Default: all RPX files in the hardcoded directory
        filesToProcess = rpxFiles;
        Console.WriteLine($"📁 Processing all {filesToProcess.Count} files in default directory.");
        Console.WriteLine();
    }
    var totalSections = 0;
    var totalControls = 0;

    foreach (var rpxFile in filesToProcess)
    {
        var fileName = Path.GetFileName(rpxFile);
        Console.WriteLine($"📄 Processing: {fileName}");

        try
        {
            // Parse RPX file
            var rpxDoc = parser.ParseFile(rpxFile);
            totalSections += rpxDoc.Sections.Count;
            totalControls += rpxDoc.Sections.Sum(s => s.Controls.Count);

            // Generate C# code
            var initCode = codeGenerator.Generate(rpxDoc);
            var summary = codeGenerator.GenerateReportSummary(rpxDoc);
            var typedCode = codeGenerator.GenerateTypedControlsExtraction(rpxDoc);

            // Save generated code
            var baseFileName = Path.GetFileNameWithoutExtension(rpxFile);
            var initCodePath = Path.Combine(outputDirectory, $"{baseFileName}_Initialize.cs");
            var typedCodePath = Path.Combine(outputDirectory, $"{baseFileName}_Controls.cs");
            var summaryPath = Path.Combine(outputDirectory, $"{baseFileName}_Summary.txt");

            File.WriteAllText(initCodePath, initCode);
            File.WriteAllText(typedCodePath, typedCode);
            File.WriteAllText(summaryPath, summary);

            Console.WriteLine($"  ✓ Sections: {rpxDoc.Sections.Count}");
            Console.WriteLine($"  ✓ Total Controls: {rpxDoc.Sections.Sum(s => s.Controls.Count)}");
            Console.WriteLine($"  ✓ Generated: {baseFileName}_Initialize.cs");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            Console.WriteLine();
        }
    }

    // Summary
    Console.WriteLine("═" + new string('═', 42) + "═");
    Console.WriteLine("📊 Processing Summary:");
    Console.WriteLine($"   Files processed: {filesToProcess.Count}");
    Console.WriteLine($"   Total sections: {totalSections}");
    Console.WriteLine($"   Total controls: {totalControls}");
    Console.WriteLine($"   Output location: {Path.GetFullPath(outputDirectory)}");
    Console.WriteLine();
    Console.WriteLine("✅ Code generation completed successfully!");
    Console.WriteLine();

    // Display sample code from first file
    if (filesToProcess.Count > 0)
    {
        var firstFile = filesToProcess[0];
        var rpxDoc = parser.ParseFile(firstFile);
        var sampleCode = codeGenerator.Generate(rpxDoc);

        Console.WriteLine("📝 Sample generated code (first 30 lines):");
        Console.WriteLine("─" + new string('─', 42) + "─");
        var lines = sampleCode.Split('\n').Take(30);
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("─" + new string('─', 42) + "─");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Fatal error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
