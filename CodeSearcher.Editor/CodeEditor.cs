using CodeSearcher.Core;
using CodeSearcher.Editor.Abstractions;
using CodeSearcher.Editor.Operations;
using CodeSearcher.Editor.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Editor
{
    /// <summary>
    /// Éditeur de code C# avec support des opérations fluentes
    /// </summary>
    public class CodeEditor : ICodeModifier
    {
        private readonly string _originalCode;
        private string _currentCode;
        private readonly List<IEditOperation> _operations;
        private readonly List<string> _changeLog;

        public CodeEditor(string code)
        {
            _originalCode = code ?? throw new ArgumentNullException(nameof(code));
            _currentCode = code;
            _operations = new List<IEditOperation>();
            _changeLog = new List<string>();
        }

        /// <summary>
        /// Crée un éditeur à partir d'une chaîne de code
        /// </summary>
        public static CodeEditor FromCode(string code) => new(code);

        /// <summary>
        /// Crée un éditeur à partir d'un fichier
        /// </summary>
        public static CodeEditor FromFile(string filePath)
        {
            var code = System.IO.File.ReadAllText(filePath);
            return new(code);
        }

        /// <summary>
        /// Renomme une méthode
        /// </summary>
        public CodeEditor RenameMethod(string oldName, string newName)
        {
            AddOperation(new RenameOperation(oldName, newName, "method"));
            return this;
        }

        /// <summary>
        /// Renomme une classe
        /// </summary>
        public CodeEditor RenameClass(string oldName, string newName)
        {
            AddOperation(new RenameOperation(oldName, newName, "class"));
            return this;
        }

        /// <summary>
        /// Renomme une variable
        /// </summary>
        public CodeEditor RenameVariable(string oldName, string newName)
        {
            AddOperation(new RenameOperation(oldName, newName, "variable"));
            return this;
        }

        /// <summary>
        /// Renomme une propriété
        /// </summary>
        public CodeEditor RenameProperty(string oldName, string newName)
        {
            AddOperation(new RenameOperation(oldName, newName, "property"));
            return this;
        }

        /// <summary>
        /// Wraps une méthode avec un try-catch
        /// </summary>
        public CodeEditor WrapWithTryCatch(string methodName, string? exceptionHandling = null)
        {
            AddOperation(new WrapOperation(methodName, "trycatch", exceptionHandling ?? "throw;"));
            return this;
        }

        /// <summary>
        /// Wraps une méthode avec du logging
        /// </summary>
        public CodeEditor WrapWithLogging(string methodName, string loggingCode)
        {
            if (string.IsNullOrWhiteSpace(loggingCode))
                throw new ArgumentException("Logging code cannot be empty", nameof(loggingCode));

            AddOperation(new WrapOperation(methodName, "logging", loggingCode));
            return this;
        }

        /// <summary>
        /// Wraps une méthode avec de la validation
        /// </summary>
        public CodeEditor WrapWithValidation(string methodName, string validationCode)
        {
            if (string.IsNullOrWhiteSpace(validationCode))
                throw new ArgumentException("Validation code cannot be empty", nameof(validationCode));

            AddOperation(new WrapOperation(methodName, "validation", validationCode));
            return this;
        }

        /// <summary>
        /// Remplace du code
        /// </summary>
        public CodeEditor Replace(string oldCode, string newCode)
        {
            if (string.IsNullOrWhiteSpace(oldCode))
                throw new ArgumentException("Old code cannot be empty", nameof(oldCode));

            if (newCode == null)
                throw new ArgumentNullException(nameof(newCode));

            AddOperation(new ReplaceOperation(oldCode, newCode));
            return this;
        }

        /// <summary>
        /// Enveloppe les returns d'une méthode dans Task<ReturnType>
        /// et rend la méthode async
        /// </summary>
        public CodeEditor WrapReturnsInTask(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

            var strategy = new ReturnTypeWrapperStrategy(_currentCode);
            var result = strategy.WrapReturnsInTask(methodName);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }

            _currentCode = result.ModifiedCode;
            _changeLog.AddRange(result.Changes);
            return this;
        }

        /// <summary>
        /// Enveloppe les returns d'une méthode dans Task avec un style spécifique
        /// </summary>
        public CodeEditor WrapReturnsInTask(string methodName, ReturnWrapStyle style)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

            var strategy = new ReturnTypeWrapperStrategy(_currentCode);
            var result = strategy.WrapReturnsInTask(methodName, style);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }

            _currentCode = result.ModifiedCode;
            _changeLog.AddRange(result.Changes);
            return this;
        }

        /// <summary>
        /// Ajoute une opération personnalisée
        /// </summary>
        public ICodeModifier AddOperation(IEditOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// Exécute toutes les opérations
        /// </summary>
        public EditResult Apply(string? code = null)
        {
            var codeToEdit = code ?? _currentCode;
            _currentCode = codeToEdit;

            if (!_operations.Any())
            {
                return new EditResult
                {
                    Success = true,
                    ModifiedCode = _currentCode,
                    Changes = new() { "No operations to apply" }
                };
            }

            foreach (var operation in _operations)
            {
                var result = operation.Execute(_currentCode);
                
                if (!result.Success)
                {
                    return new EditResult
                    {
                        Success = false,
                        ErrorMessage = $"Operation '{operation.Description}' failed: {result.ErrorMessage}",
                        ModifiedCode = _currentCode
                    };
                }

                _currentCode = result.ModifiedCode;
                _changeLog.AddRange(result.Changes);
            }

            return new EditResult
            {
                Success = true,
                ModifiedCode = _currentCode,
                Changes = _changeLog.ToList()
            };
        }

        /// <summary>
        /// Réinitialise les opérations
        /// </summary>
        public ICodeModifier Clear()
        {
            _operations.Clear();
            _changeLog.Clear();
            _currentCode = _originalCode;
            return this;
        }

        /// <summary>
        /// Sauvegarde le code modifié dans un fichier
        /// </summary>
        public void SaveToFile(string filePath, string? code = null)
        {
            var contentToSave = code ?? _currentCode;
            System.IO.File.WriteAllText(filePath, contentToSave);
        }

        /// <summary>
        /// Retourne le code actuel
        /// </summary>
        public string GetCode() => _currentCode;

        /// <summary>
        /// Retourne le journal des modifications
        /// </summary>
        public List<string> GetChangeLog() => new(_changeLog);

        /// <summary>
        /// Retourne le code original
        /// </summary>
        public string GetOriginalCode() => _originalCode;

        /// <summary>
        /// Réinitialise le code au code original
        /// </summary>
        public void Reset()
        {
            _currentCode = _originalCode;
            _operations.Clear();
            _changeLog.Clear();
        }
    }
}
