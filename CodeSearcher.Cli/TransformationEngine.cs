using CodeSearcher.Core;
using CodeSearcher.Core.Abstractions;
using CodeSearcher.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeSearcher.Cli
{
    /// <summary>
    /// Moteur d'exécution des transformations
    /// </summary>
    public class TransformationEngine
    {
        private readonly TransformationConfig _config;
        private readonly ILogger _logger;

        public TransformationEngine(TransformationConfig config, ILogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? new NullLogger();
        }

        /// <summary>
        /// Exécute les transformations selon la configuration
        /// </summary>
        public ExecutionResult Execute()
        {
            var result = new ExecutionResult
            {
                Success = true,
                Message = "Transformation started"
            };

            try
            {
                // Étape 1: Créer le répertoire de sortie
                CreateOutputDirectory();

                // Étape 2: Trouver les fichiers à traiter
                var files = FindTargetFiles();
                result.TotalFiles = files.Count;

                if (files.Count == 0)
                {
                    result.Success = false;
                    result.Message = "No files found matching the criteria";
                    return result;
                }

                _logger.LogInfo($"Found {files.Count} file(s) to process");

                // Étape 3: Traiter chaque fichier
                foreach (var filePath in files)
                {
                    ProcessFile(filePath, result);
                }

                result.Message = $"Transformation completed. {result.SuccessfulTransformations} successful, {result.FailedTransformations} failed";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Transformation failed: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError("Transformation failed", ex);
                return result;
            }
        }

        private void CreateOutputDirectory()
        {
            var outputPath = Path.IsPathRooted(_config.OutputDirectory)
                ? _config.OutputDirectory
                : Path.Combine(Directory.GetCurrentDirectory(), _config.OutputDirectory);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                _logger.LogInfo($"Created output directory: {outputPath}");
            }
        }

        private List<string> FindTargetFiles()
        {
            var files = new List<string>();
            var projectPath = FindProjectPath();

            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException($"Project not found: {projectPath}");
            }

            foreach (var pattern in _config.FilePatterns)
            {
                var matchedFiles = Directory.GetFiles(projectPath, pattern, SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                files.AddRange(matchedFiles);
            }

            return files.Distinct().ToList();
        }

        private string FindProjectPath()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var solutionDir = FindSolutionDirectory(currentDir);

            var projectPath = Path.Combine(solutionDir, _config.TargetProject);
            return projectPath;
        }

        private string FindSolutionDirectory(string startDir)
        {
            var current = new DirectoryInfo(startDir);

            while (current != null)
            {
                var slnFiles = current.GetFiles("*.sln");
                if (slnFiles.Length > 0)
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return startDir;
        }

        private void ProcessFile(string filePath, ExecutionResult result)
        {
            try
            {
                _logger.LogInfo($"Processing: {filePath}");

                var originalCode = File.ReadAllText(filePath);
                var context = CodeContext.FromCode(originalCode, _logger);
                var editor = CodeEditor.FromCode(originalCode);

                // Appliquer les sélections et transformations
                ApplyTransformations(context, editor, filePath);

                var editResult = editor.Apply();

                if (editResult.Success)
                {
                    SaveModifiedFile(filePath, editResult.ModifiedCode);
                    result.ProcessedFiles.Add(filePath);
                    result.SuccessfulTransformations++;
                    _logger.LogInfo($"? Transformed: {filePath}");
                }
                else
                {
                    result.FailedTransformations++;
                    result.Errors.Add($"{filePath}: {editResult.ErrorMessage}");
                    _logger.LogError($"? Failed to transform: {filePath}");
                }
            }
            catch (Exception ex)
            {
                result.FailedTransformations++;
                result.Errors.Add($"{filePath}: {ex.Message}");
                _logger.LogError($"Error processing {filePath}", ex);
            }
        }

        private void ApplyTransformations(CodeContext context, CodeEditor editor, string filePath)
        {
            foreach (var transformation in _config.Transformations)
            {
                try
                {
                    switch (transformation.Type.ToLower())
                    {
                        case "rename":
                            ApplyRename(editor, transformation);
                            break;

                        case "wrap":
                            ApplyWrap(editor, transformation);
                            break;

                        case "replace":
                            ApplyReplace(editor, transformation);
                            break;

                        case "wrapreturnstintask":
                            ApplyWrapReturnsInTask(editor, transformation);
                            break;

                        case "custom":
                            ApplyCustomTransformation(context, editor, transformation);
                            break;

                        default:
                            _logger.LogError($"Unknown transformation type: {transformation.Type}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error applying transformation '{transformation.Name}'", ex);
                }
            }
        }

        private void ApplyRename(CodeEditor editor, TransformationRule rule)
        {
            var target = rule.Target;
            var newName = rule.Parameters.ContainsKey("newName") ? rule.Parameters["newName"] : "";

            if (rule.Parameters.ContainsKey("type"))
            {
                var type = rule.Parameters["type"].ToLower();
                switch (type)
                {
                    case "method":
                        editor.RenameMethod(target, newName);
                        break;
                    case "class":
                        editor.RenameClass(target, newName);
                        break;
                    case "variable":
                        editor.RenameVariable(target, newName);
                        break;
                    case "property":
                        editor.RenameProperty(target, newName);
                        break;
                }
            }
        }

        private void ApplyWrap(CodeEditor editor, TransformationRule rule)
        {
            var methodName = rule.Target;
            var wrapType = rule.Parameters.ContainsKey("wrapType") ? rule.Parameters["wrapType"] : "trycatch";
            var handlerCode = rule.Parameters.ContainsKey("handler") ? rule.Parameters["handler"] : "";

            switch (wrapType.ToLower())
            {
                case "trycatch":
                    editor.WrapWithTryCatch(methodName, handlerCode);
                    break;
                case "logging":
                    editor.WrapWithLogging(methodName, handlerCode);
                    break;
                case "validation":
                    editor.WrapWithValidation(methodName, handlerCode);
                    break;
            }
        }

        private void ApplyReplace(CodeEditor editor, TransformationRule rule)
        {
            var oldCode = rule.Parameters.ContainsKey("oldCode") ? rule.Parameters["oldCode"] : "";
            var newCode = rule.Parameters.ContainsKey("newCode") ? rule.Parameters["newCode"] : "";

            if (!string.IsNullOrEmpty(oldCode) && !string.IsNullOrEmpty(newCode))
            {
                editor.Replace(oldCode, newCode);
            }
        }

        private void ApplyWrapReturnsInTask(CodeEditor editor, TransformationRule rule)
        {
            var methodName = rule.Target;
            
            // Simple call without style parameter for now
            editor.WrapReturnsInTask(methodName);
        }

        private void ApplyCustomTransformation(CodeContext context, CodeEditor editor, TransformationRule rule)
        {
            _logger.LogInfo($"Custom transformation: {rule.Name}");
            // Permet aux développeurs d'implémenter des transformations custom
        }

        private void SaveModifiedFile(string filePath, string modifiedCode)
        {
            // Créer un backup si configuré
            if (_config.CreateBackup)
            {
                var backupPath = Path.Combine(_config.OutputDirectory, "backups", Path.GetFileName(filePath) + ".bak");
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                File.Copy(filePath, backupPath, true);
            }

            // Sauvegarder le fichier modifié
            var outputPath = Path.Combine(_config.OutputDirectory, Path.GetFileName(filePath));
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, modifiedCode);
        }
    }
}
