using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeSearcher.Cli
{
    /// <summary>
    /// Configuration de transformation pour le CLI
    /// </summary>
    public class TransformationConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("targetProject")]
        public string TargetProject { get; set; }

        [JsonPropertyName("filePatterns")]
        public List<string> FilePatterns { get; set; } = new();

        [JsonPropertyName("selections")]
        public List<SelectionRule> Selections { get; set; } = new();

        [JsonPropertyName("transformations")]
        public List<TransformationRule> Transformations { get; set; } = new();

        [JsonPropertyName("outputDirectory")]
        public string OutputDirectory { get; set; } = "./output";

        [JsonPropertyName("createBackup")]
        public bool CreateBackup { get; set; } = true;
    }

    /// <summary>
    /// Règle de sélection de code
    /// </summary>
    public class SelectionRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }  // "methods", "classes", "returns", "variables", "custom"

        [JsonPropertyName("filters")]
        public Dictionary<string, object> Filters { get; set; } = new();

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Règle de transformation de code
    /// </summary>
    public class TransformationRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }  // "rename", "wrap", "replace", "wrapReturnsInTask"

        [JsonPropertyName("target")]
        public string Target { get; set; }  // nom de la méthode/classe/etc.

        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new();

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("applyToAll")]
        public bool ApplyToAll { get; set; }  // Appliquer à tous les résultats de sélection
    }

    /// <summary>
    /// Résultat d'exécution
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> ProcessedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalFiles { get; set; }
        public int SuccessfulTransformations { get; set; }
        public int FailedTransformations { get; set; }
    }

    /// <summary>
    /// Configuration par fichier
    /// </summary>
    public class FileTransformationResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string OriginalCode { get; set; }
        public string ModifiedCode { get; set; }
        public List<string> Changes { get; set; } = new();
        public string ErrorMessage { get; set; }
    }
}
