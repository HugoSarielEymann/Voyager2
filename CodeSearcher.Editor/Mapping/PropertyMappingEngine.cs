using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeSearcher.Editor.Mapping
{
    /// <summary>
    /// Représente un mapping entre une ancienne propriété et une nouvelle
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// Nom de la propriété/variable ancienne
        /// </summary>
        public string OldPropertyName { get; set; }

        /// <summary>
        /// Chemin de la nouvelle propriété (peut être imbriquée: "Obj.Prop.SubProp")
        /// </summary>
        public string NewPropertyPath { get; set; }

        /// <summary>
        /// Type ancien (optionnel)
        /// </summary>
        public string? OldType { get; set; }

        /// <summary>
        /// Type nouveau (optionnel)
        /// </summary>
        public string? NewType { get; set; }

        /// <summary>
        /// Si true, la propriété est imbriquée (contient au moins un point)
        /// </summary>
        public bool IsNested { get; set; }

        /// <summary>
        /// Nom de l'objet mappé si imbriqué (ex: "Consignee" si NewPropertyPath = "Consignee.Name")
        /// </summary>
        public string? MappedObjectName { get; set; }

        /// <summary>
        /// Contexte optionnel pour disambiguer (ex: "Employee" vs "Company")
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Description du mapping
        /// </summary>
        public string? Description { get; set; }

        public PropertyMapping()
        {
        }

        public PropertyMapping(string oldName, string newPath)
        {
            OldPropertyName = oldName ?? throw new ArgumentNullException(nameof(oldName));
            NewPropertyPath = newPath ?? throw new ArgumentNullException(nameof(newPath));
            IsNested = newPath.Contains('.');
            
            if (IsNested)
            {
                var parts = newPath.Split('.');
                MappedObjectName = parts[0];
            }
        }

        public override string ToString()
        {
            return $"{OldPropertyName} ? {NewPropertyPath}" + 
                   (!string.IsNullOrEmpty(OldType) ? $" ({OldType} ? {NewType})" : "");
        }
    }

    /// <summary>
    /// Résultat d'une transformation de mapping
    /// </summary>
    public class MappingTransformResult
    {
        public bool Success { get; set; }
        public string? TransformedCode { get; set; }
        public List<string> Changes { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public int ReplacementsCount { get; set; }
    }

    /// <summary>
    /// Moteur de mapping de propriétés pour migrations Legacy
    /// </summary>
    public class PropertyMappingEngine
    {
        private readonly List<PropertyMapping> _mappings = new();
        private bool _caseSensitive = true;
        private bool _wholeWordOnly = true;

        /// <summary>
        /// Crée une nouvelle instance du moteur de mapping
        /// </summary>
        public PropertyMappingEngine()
        {
        }

        /// <summary>
        /// Crée une instance avec sensibilité à la casse configurable
        /// </summary>
        public PropertyMappingEngine(bool caseSensitive = true, bool wholeWordOnly = true)
        {
            _caseSensitive = caseSensitive;
            _wholeWordOnly = wholeWordOnly;
        }

        /// <summary>
        /// Ajoute un mapping simple
        /// </summary>
        public void AddMapping(string oldName, string newPath)
        {
            if (string.IsNullOrWhiteSpace(oldName))
                throw new ArgumentException("Old name cannot be empty", nameof(oldName));
            if (string.IsNullOrWhiteSpace(newPath))
                throw new ArgumentException("New path cannot be empty", nameof(newPath));

            var mapping = new PropertyMapping(oldName, newPath);
            _mappings.Add(mapping);
        }

        /// <summary>
        /// Ajoute un mapping avec types
        /// </summary>
        public void AddMapping(string oldName, string newPath, string oldType, string newType)
        {
            var mapping = new PropertyMapping(oldName, newPath)
            {
                OldType = oldType,
                NewType = newType
            };
            _mappings.Add(mapping);
        }

        /// <summary>
        /// Ajoute un mapping avec contexte (pour désambiguer)
        /// </summary>
        public void AddMappingWithContext(string oldName, string newPath, string context, string? description = null)
        {
            var mapping = new PropertyMapping(oldName, newPath)
            {
                Context = context,
                Description = description
            };
            _mappings.Add(mapping);
        }

        /// <summary>
        /// Ajoute un mapping complet
        /// </summary>
        public void AddMapping(PropertyMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));
            _mappings.Add(mapping);
        }

        /// <summary>
        /// Ajoute plusieurs mappings à la fois
        /// </summary>
        public void AddMappings(params PropertyMapping[] mappings)
        {
            foreach (var mapping in mappings ?? Array.Empty<PropertyMapping>())
            {
                AddMapping(mapping);
            }
        }

        /// <summary>
        /// Trouve un mapping par nom ancien
        /// </summary>
        public PropertyMapping? FindMapping(string oldName)
        {
            return _mappings.FirstOrDefault(m => m.OldPropertyName == oldName);
        }

        /// <summary>
        /// Trouve tous les mappings pour un nom (si contexte)
        /// </summary>
        public IEnumerable<PropertyMapping> FindMappings(string oldName)
        {
            return _mappings.Where(m => m.OldPropertyName == oldName);
        }

        /// <summary>
        /// Récupère tous les mappings
        /// </summary>
        public IReadOnlyList<PropertyMapping> GetAllMappings()
        {
            return _mappings.AsReadOnly();
        }

        /// <summary>
        /// Efface tous les mappings
        /// </summary>
        public void Clear()
        {
            _mappings.Clear();
        }

        /// <summary>
        /// Enlève un mapping spécifique
        /// </summary>
        public bool RemoveMapping(string oldName)
        {
            var mapping = FindMapping(oldName);
            if (mapping != null)
            {
                _mappings.Remove(mapping);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Transforme le code en appliquant tous les mappings
        /// </summary>
        public MappingTransformResult TransformCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new MappingTransformResult 
                { 
                    Success = false, 
                    ErrorMessage = "Code cannot be empty" 
                };

            if (_mappings.Count == 0)
                return new MappingTransformResult 
                { 
                    Success = true, 
                    TransformedCode = code,
                    Changes = new() { "No mappings defined" }
                };

            try
            {
                var result = code;
                var replacementsCount = 0;
                var changes = new List<string>();

                // Appliquer les mappings dans l'ordre
                foreach (var mapping in _mappings)
                {
                    var pattern = BuildPattern(mapping.OldPropertyName);
                    var replacement = mapping.NewPropertyPath;

                    var regex = new Regex(pattern, 
                        _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

                    var matches = regex.Matches(result);
                    if (matches.Count > 0)
                    {
                        result = regex.Replace(result, replacement);
                        replacementsCount += matches.Count;
                        changes.Add($"Mapped '{mapping.OldPropertyName}' ? '{mapping.NewPropertyPath}' ({matches.Count} occurrence(s))");
                    }
                }

                return new MappingTransformResult
                {
                    Success = true,
                    TransformedCode = result,
                    Changes = changes,
                    ReplacementsCount = replacementsCount
                };
            }
            catch (Exception ex)
            {
                return new MappingTransformResult
                {
                    Success = false,
                    ErrorMessage = $"Error during mapping transformation: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Valide que les mappings ont du sens
        /// </summary>
        public List<string> ValidateMappings()
        {
            var issues = new List<string>();

            // Vérifier les doublons
            var duplicates = _mappings
                .GroupBy(m => m.OldPropertyName)
                .Where(g => g.Count() > 1 && g.All(m => m.Context == null))
                .ToList();

            foreach (var dup in duplicates)
            {
                issues.Add($"Duplicate mapping for '{dup.Key}' without context");
            }

            // Vérifier les cycles
            foreach (var mapping in _mappings)
            {
                if (mapping.NewPropertyPath.Contains(mapping.OldPropertyName))
                {
                    issues.Add($"Potential cycle: '{mapping.OldPropertyName}' maps to '{mapping.NewPropertyPath}'");
                }
            }

            // Vérifier les chemins vides
            foreach (var mapping in _mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.OldPropertyName) ||
                    string.IsNullOrWhiteSpace(mapping.NewPropertyPath))
                {
                    issues.Add("Found mapping with empty old or new path");
                }
            }

            return issues;
        }

        /// <summary>
        /// Construit un pattern regex pour la propriété
        /// </summary>
        private string BuildPattern(string propertyName)
        {
            if (_wholeWordOnly)
            {
                return $@"\b{Regex.Escape(propertyName)}\b";
            }
            return Regex.Escape(propertyName);
        }

        /// <summary>
        /// Obtient un rapport sur les mappings
        /// </summary>
        public string GetReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Property Mapping Report");
            report.AppendLine($"======================");
            report.AppendLine($"Total Mappings: {_mappings.Count}");
            report.AppendLine();

            if (_mappings.Count == 0)
            {
                report.AppendLine("No mappings defined.");
                return report.ToString();
            }

            report.AppendLine("Mappings:");
            foreach (var mapping in _mappings.OrderBy(m => m.OldPropertyName))
            {
                report.AppendLine($"  • {mapping}");
                if (!string.IsNullOrEmpty(mapping.Context))
                    report.AppendLine($"    Context: {mapping.Context}");
                if (!string.IsNullOrEmpty(mapping.Description))
                    report.AppendLine($"    Description: {mapping.Description}");
            }

            var validationIssues = ValidateMappings();
            if (validationIssues.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Validation Issues:");
                foreach (var issue in validationIssues)
                {
                    report.AppendLine($"  ??  {issue}");
                }
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Détecteur automatique de mappings (heuristique)
    /// </summary>
    public class MappingDetector
    {
        /// <summary>
        /// Détecte les mappings suggérés en comparant ancien et nouveau code
        /// </summary>
        public List<PropertyMapping> DetectSuggestedMappings(string oldCode, string newCode)
        {
            if (string.IsNullOrWhiteSpace(oldCode) || string.IsNullOrWhiteSpace(newCode))
                return new List<PropertyMapping>();

            var oldProperties = ExtractPropertyNames(oldCode);
            var newProperties = ExtractPropertyPaths(newCode);

            var suggestedMappings = new List<PropertyMapping>();

            foreach (var oldProp in oldProperties)
            {
                // Chercher une correspondance dans le nouveau code
                var bestMatch = FindBestMatch(oldProp, newProperties);

                if (bestMatch != null && bestMatch.Similarity > 0.6)
                {
                    suggestedMappings.Add(new PropertyMapping(oldProp, bestMatch.PropertyPath)
                    {
                        Description = $"Suggested mapping (similarity: {bestMatch.Similarity:P})"
                    });
                }
            }

            return suggestedMappings;
        }

        private List<string> ExtractPropertyNames(string code)
        {
            var regex = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*(?:=|;)");
            var matches = regex.Matches(code);

            return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        private List<string> ExtractPropertyPaths(string code)
        {
            var regex = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_.]*)\b");
            var matches = regex.Matches(code);

            return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        private class MatchResult
        {
            public string PropertyPath { get; set; }
            public double Similarity { get; set; }
        }

        private MatchResult? FindBestMatch(string oldProp, List<string> newProps)
        {
            var bestMatch = newProps
                .Select(newProp => new MatchResult
                {
                    PropertyPath = newProp,
                    Similarity = CalculateSimilarity(oldProp, newProp)
                })
                .OrderByDescending(m => m.Similarity)
                .FirstOrDefault();

            return bestMatch?.Similarity > 0 ? bestMatch : null;
        }

        private double CalculateSimilarity(string str1, string str2)
        {
            // Similarité simple basée sur la sous-chaîne
            var lower1 = str1.ToLower();
            var lower2 = str2.ToLower();

            if (lower1 == lower2) return 1.0;
            if (lower2.Contains(lower1)) return 0.8;
            if (lower1.Contains(lower2)) return 0.8;

            // Levenshtein distance simplifié
            var commonChars = lower1.Intersect(lower2).Count();
            return (double)commonChars / Math.Max(lower1.Length, lower2.Length);
        }
    }
}
