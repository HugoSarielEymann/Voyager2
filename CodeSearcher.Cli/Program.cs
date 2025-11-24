using CodeSearcher.Cli;
using CodeSearcher.Core.Abstractions;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeSearcher.Cli
{
    /// <summary>
    /// Programme CLI interactif pour les transformations de code
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("??????????????????????????????????????????");
            Console.WriteLine("?  CodeSearcher Transformation Engine    ?");
            Console.WriteLine("?  Version 1.0                           ?");
            Console.WriteLine("??????????????????????????????????????????\n");

            if (args.Length > 0)
            {
                HandleConfigFile(args[0]);
            }
            else
            {
                InteractiveMode();
            }
        }

        static void InteractiveMode()
        {
            Console.WriteLine("Mode: Interactive");
            Console.WriteLine("Commands:");
            Console.WriteLine("  config <path>    - Load configuration from JSON file");
            Console.WriteLine("  create           - Create new configuration interactively");
            Console.WriteLine("  help             - Show help");
            Console.WriteLine("  exit             - Exit program\n");

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(input))
                    continue;

                var parts = input.Split(' ');
                var command = parts[0].ToLower();

                switch (command)
                {
                    case "config":
                        if (parts.Length > 1)
                            HandleConfigFile(parts[1]);
                        else
                            Console.WriteLine("Usage: config <path-to-config.json>");
                        break;

                    case "create":
                        CreateConfigInteractive();
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "exit":
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        break;
                }
            }
        }

        static void HandleConfigFile(string configPath)
        {
            try
            {
                Console.WriteLine($"\n?? Loading configuration from: {configPath}");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"? File not found: {configPath}");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var config = JsonSerializer.Deserialize<TransformationConfig>(json, options);

                if (config == null)
                {
                    Console.WriteLine("? Failed to deserialize configuration");
                    return;
                }

                Console.WriteLine($"? Configuration loaded: {config.Name}");
                Console.WriteLine($"  Description: {config.Description}");
                Console.WriteLine($"  Target Project: {config.TargetProject}");
                Console.WriteLine($"  Files: {config.FilePatterns.Count}");
                Console.WriteLine($"  Transformations: {config.Transformations.Count}");
                Console.Write("\n? Execute transformation? (y/n): ");

                if (Console.ReadLine()?.ToLower() == "y")
                {
                    ExecuteTransformation(config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }

        static void ExecuteTransformation(TransformationConfig config)
        {
            Console.WriteLine("\n??  Executing transformations...\n");

            var logger = new ConsoleLogger(isDebug: true);
            var engine = new TransformationEngine(config, logger);
            var result = engine.Execute();

            Console.WriteLine("\n?? Results:");
            Console.WriteLine($"  Success: {result.Success}");
            Console.WriteLine($"  Total Files: {result.TotalFiles}");
            Console.WriteLine($"  Processed: {result.ProcessedFiles.Count}");
            Console.WriteLine($"  Successful: {result.SuccessfulTransformations}");
            Console.WriteLine($"  Failed: {result.FailedTransformations}");

            if (result.ProcessedFiles.Count > 0)
            {
                Console.WriteLine("\n? Processed Files:");
                foreach (var file in result.ProcessedFiles)
                {
                    Console.WriteLine($"    - {file}");
                }
            }

            if (result.Errors.Count > 0)
            {
                Console.WriteLine("\n??  Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"    - {error}");
                }
            }

            Console.WriteLine($"\n?? Output directory: {Path.GetFullPath(config.OutputDirectory)}");
        }

        static void CreateConfigInteractive()
        {
            Console.WriteLine("\n?? Creating new configuration...\n");

            var config = new TransformationConfig();

            Console.Write("Configuration name: ");
            config.Name = Console.ReadLine() ?? "";

            Console.Write("Description: ");
            config.Description = Console.ReadLine() ?? "";

            Console.Write("Target project (relative path): ");
            config.TargetProject = Console.ReadLine() ?? "";

            Console.Write("File patterns (comma-separated, e.g. '*.cs,**/*.cs'): ");
            var patterns = Console.ReadLine() ?? "";
            config.FilePatterns = patterns.Split(',').Select(p => p.Trim()).ToList();

            Console.Write("Output directory: ");
            config.OutputDirectory = Console.ReadLine() ?? "./output";

            Console.Write("Create backups? (y/n): ");
            config.CreateBackup = Console.ReadLine()?.ToLower() == "y";

            // Ajouter une transformation d'exemple
            Console.WriteLine("\nAdd transformation? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                AddTransformationInteractive(config);
            }

            // Sauvegarder la configuration
            Console.Write("\nSave configuration to file? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                SaveConfiguration(config);
            }
        }

        static void AddTransformationInteractive(TransformationConfig config)
        {
            Console.WriteLine("\nTransformation types: rename, wrap, replace, wrapReturnsinTask");
            Console.Write("Transformation type: ");
            var type = Console.ReadLine() ?? "";

            Console.Write("Transformation name: ");
            var name = Console.ReadLine() ?? "";

            Console.Write("Target (method/class name): ");
            var target = Console.ReadLine() ?? "";

            var transformation = new TransformationRule
            {
                Name = name,
                Type = type,
                Target = target,
                Description = ""
            };

            // Ajouter les paramètres selon le type
            switch (type.ToLower())
            {
                case "rename":
                    Console.Write("New name: ");
                    transformation.Parameters["newName"] = Console.ReadLine() ?? "";
                    Console.Write("Type (method/class/variable/property): ");
                    transformation.Parameters["type"] = Console.ReadLine() ?? "method";
                    break;

                case "wrap":
                    Console.Write("Wrap type (trycatch/logging/validation): ");
                    transformation.Parameters["wrapType"] = Console.ReadLine() ?? "trycatch";
                    Console.Write("Handler code: ");
                    transformation.Parameters["handler"] = Console.ReadLine() ?? "";
                    break;

                case "replace":
                    Console.Write("Old code: ");
                    transformation.Parameters["oldCode"] = Console.ReadLine() ?? "";
                    Console.Write("New code: ");
                    transformation.Parameters["newCode"] = Console.ReadLine() ?? "";
                    break;

                case "wrapreturnstintask":
                    Console.Write("Style (TaskFromResult/AwaitTaskFromResult/Auto): ");
                    transformation.Parameters["style"] = Console.ReadLine() ?? "TaskFromResult";
                    break;
            }

            config.Transformations.Add(transformation);
            Console.WriteLine("? Transformation added");
        }

        static void SaveConfiguration(TransformationConfig config)
        {
            var filename = $"{config.Name.Replace(" ", "_").ToLower()}_config.json";
            Console.Write($"Filename (default: {filename}): ");
            var userFilename = Console.ReadLine();
            if (!string.IsNullOrEmpty(userFilename))
                filename = userFilename;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(filename, json);

            Console.WriteLine($"? Configuration saved to: {filename}");
        }

        static void ShowHelp()
        {
            Console.WriteLine("\n?? CodeSearcher CLI Help\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run <config-file.json>   - Execute transformation from config");
            Console.WriteLine("  dotnet run                       - Start interactive mode\n");

            Console.WriteLine("Configuration File Format:");
            Console.WriteLine(@"
{
  ""name"": ""My Transformation"",
  ""description"": ""Convert sync methods to async"",
  ""targetProject"": ""MyProject"",
  ""filePatterns"": [""*.cs"", ""**/*.cs""],
  ""outputDirectory"": ""./output"",
  ""createBackup"": true,
  ""selections"": [],
  ""transformations"": [
    {
      ""name"": ""Rename GetUser to FetchUser"",
      ""type"": ""rename"",
      ""target"": ""GetUser"",
      ""parameters"": {
        ""newName"": ""FetchUser"",
        ""type"": ""method""
      }
    }
  ]
}");

            Console.WriteLine("\nTransformation Types:");
            Console.WriteLine("  rename              - Rename methods, classes, variables, properties");
            Console.WriteLine("  wrap                - Add try-catch, logging, validation wrapper");
            Console.WriteLine("  replace             - Replace code snippets");
            Console.WriteLine("  wrapReturnsinTask   - Convert sync methods to async Task<T>");
            Console.WriteLine("  custom              - Custom transformation logic\n");
        }
    }
}
