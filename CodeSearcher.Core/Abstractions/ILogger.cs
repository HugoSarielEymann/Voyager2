using System;
using System.Collections.Generic;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface pour les services de logging
    /// Permet l'injection de dépendance de différentes stratégies de logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log une sélection de code
        /// </summary>
        void LogSelection(string description, string code);

        /// <summary>
        /// Log une transformation
        /// </summary>
        void LogTransformation(string description, string oldCode, string newCode);

        /// <summary>
        /// Log une erreur
        /// </summary>
        void LogError(string message, Exception ex = null);

        /// <summary>
        /// Log une information
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Log un debug
        /// </summary>
        void LogDebug(string message);
    }

    /// <summary>
    /// Logger par défaut (console)
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly bool _isDebug;

        public ConsoleLogger(bool isDebug = false)
        {
            _isDebug = isDebug;
        }

        public void LogSelection(string description, string code)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[SELECTION] {description}");
            if (_isDebug)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  Code: {code.Replace("\n", "\n  ")}");
            }
            Console.ResetColor();
        }

        public void LogTransformation(string description, string oldCode, string newCode)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[TRANSFORMATION] {description}");
            if (_isDebug)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  Old: {oldCode.Replace("\n", "\n  ")}");
                Console.WriteLine($"  New: {newCode.Replace("\n", "\n  ")}");
            }
            Console.ResetColor();
        }

        public void LogError(string message, Exception ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            if (ex != null && _isDebug)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
            }
            Console.ResetColor();
        }

        public void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        public void LogDebug(string message)
        {
            if (_isDebug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[DEBUG] {message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Logger null (pas de logging)
    /// </summary>
    public class NullLogger : ILogger
    {
        public void LogSelection(string description, string code) { }
        public void LogTransformation(string description, string oldCode, string newCode) { }
        public void LogError(string message, Exception ex = null) { }
        public void LogInfo(string message) { }
        public void LogDebug(string message) { }
    }

    /// <summary>
    /// Logger en mémoire pour les tests
    /// </summary>
    public class MemoryLogger : ILogger
    {
        private readonly List<string> _logs = new();

        public IReadOnlyList<string> Logs => _logs.AsReadOnly();

        public void LogSelection(string description, string code) => _logs.Add($"SELECTION: {description}");
        public void LogTransformation(string description, string oldCode, string newCode) => _logs.Add($"TRANSFORMATION: {description}");
        public void LogError(string message, Exception ex = null) => _logs.Add($"ERROR: {message}");
        public void LogInfo(string message) => _logs.Add($"INFO: {message}");
        public void LogDebug(string message) => _logs.Add($"DEBUG: {message}");

        public void Clear() => _logs.Clear();
    }
}
