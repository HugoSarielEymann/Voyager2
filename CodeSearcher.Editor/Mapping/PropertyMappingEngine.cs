using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeSearcher.Editor.Mapping
{
    /// <summary>
    /// Représente les informations complètes d'une propriété source avec chemin de types
    /// </summary>
    public class SourcePropertyInfo
    {
        /// <summary>
        /// Nom de la propriété source
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Type de la propriété source (ex: "string", "int", "Address")
        /// </summary>
        public string? PropertyType { get; set; }

        /// <summary>
        /// Nom du parent (classe ou objet contenant) si applicable
        /// </summary>
        public string? ParentName { get; set; }

        /// <summary>
        /// Type du parent si applicable (ex: "Order", "Sender")
        /// </summary>
        public string? ParentType { get; set; }

        /// <summary>
        /// Chemin complet des types depuis la racine (ex: "Order.Sender.Address")
        /// Utilisé pour désambiguïser les propriétés avec le même nom dans différents contextes
        /// </summary>
        public string? TypePath { get; set; }

        /// <summary>
        /// Chemin complet (ex: "Parent.Property")
        /// </summary>
        public string FullPath => string.IsNullOrEmpty(ParentName) 
            ? PropertyName 
            : $"{ParentName}.{PropertyName}";

        /// <summary>
        /// Clé unique pour identifier cette propriété dans un contexte de types
        /// Format: "TypePath.PropertyName:PropertyType" ou "ParentType.PropertyName:PropertyType"
        /// </summary>
        public string TypeQualifiedKey
        {
            get
            {
                var path = !string.IsNullOrEmpty(TypePath) ? TypePath : ParentType;
                var key = !string.IsNullOrEmpty(path) ? $"{path}.{PropertyName}" : PropertyName;
                return !string.IsNullOrEmpty(PropertyType) ? $"{key}:{PropertyType}" : key;
            }
        }

        public override string ToString() => 
            $"{FullPath}" + (!string.IsNullOrEmpty(PropertyType) ? $" ({PropertyType})" : "");
    }

    /// <summary>
    /// Représente les informations complètes d'une propriété cible avec chemin de types
    /// </summary>
    public class TargetPropertyInfo
    {
        /// <summary>
        /// Nom de la propriété cible
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Type de la propriété cible
        /// </summary>
        public string? PropertyType { get; set; }

        /// <summary>
        /// Nom du parent cible (classe ou objet contenant) si applicable
        /// </summary>
        public string? ParentName { get; set; }

        /// <summary>
        /// Type du parent cible si applicable
        /// </summary>
        public string? ParentType { get; set; }

        /// <summary>
        /// Chemin complet des types cible (ex: "Order.Sender.Address")
        /// </summary>
        public string? TypePath { get; set; }

        /// <summary>
        /// Chemin complet (ex: "NewParent.NewProperty")
        /// </summary>
        public string FullPath => string.IsNullOrEmpty(ParentName) 
            ? PropertyName 
            : $"{ParentName}.{PropertyName}";

        public override string ToString() => 
            $"{FullPath}" + (!string.IsNullOrEmpty(PropertyType) ? $" ({PropertyType})" : "");
    }

    /// <summary>
    /// Règle de matching pour déterminer quand appliquer un mapping
    /// </summary>
    public class MappingMatchRule
    {
        /// <summary>
        /// Matcher par nom de propriété (obligatoire)
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Matcher par type de propriété (optionnel)
        /// </summary>
        public string? PropertyType { get; set; }

        /// <summary>
        /// Matcher par type du parent direct (optionnel)
        /// </summary>
        public string? ParentType { get; set; }

        /// <summary>
        /// Matcher par chemin de types complet (le plus précis)
        /// Ex: "Order.Sender" pour matcher Sender.Name dans le contexte Order.Sender
        /// </summary>
        public string? TypePath { get; set; }

        /// <summary>
        /// Priorité du matching (plus élevé = plus prioritaire)
        /// Calculé automatiquement basé sur la spécificité
        /// </summary>
        public int Priority => CalculatePriority();

        private int CalculatePriority()
        {
            int priority = 0;
            if (!string.IsNullOrEmpty(TypePath)) priority += 100;      // Chemin complet = plus spécifique
            if (!string.IsNullOrEmpty(ParentType)) priority += 50;     // Type parent = assez spécifique
            if (!string.IsNullOrEmpty(PropertyType)) priority += 25;   // Type propriété = précision supplémentaire
            if (!string.IsNullOrEmpty(PropertyName)) priority += 10;   // Nom = base
            return priority;
        }

        /// <summary>
        /// Vérifie si cette règle correspond aux informations de propriété données
        /// </summary>
        public bool Matches(string propertyName, string? propertyType, string? parentType, string? typePath)
        {
            // Le nom doit correspondre
            if (!string.Equals(PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                return false;

            // Vérifier le chemin de types (le plus spécifique)
            if (!string.IsNullOrEmpty(TypePath))
            {
                if (string.IsNullOrEmpty(typePath))
                    return false;
                if (!typePath.EndsWith(TypePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(typePath, TypePath, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Vérifier le type parent
            if (!string.IsNullOrEmpty(ParentType))
            {
                if (string.IsNullOrEmpty(parentType))
                    return false;
                if (!string.Equals(parentType, ParentType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Vérifier le type de propriété
            if (!string.IsNullOrEmpty(PropertyType))
            {
                if (string.IsNullOrEmpty(propertyType))
                    return false;
                if (!string.Equals(propertyType, PropertyType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Représente un mapping entre une ancienne propriété et une nouvelle avec informations complètes
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// Informations complètes de la propriété source
        /// </summary>
        public SourcePropertyInfo Source { get; set; } = new();

        /// <summary>
        /// Informations complètes de la propriété cible
        /// </summary>
        public TargetPropertyInfo Target { get; set; } = new();

        /// <summary>
        /// Règle de matching pour cette propriété (optionnel, calculé depuis Source si absent)
        /// </summary>
        public MappingMatchRule? MatchRule { get; set; }

        /// <summary>
        /// Obtient la règle de matching (crée depuis Source si non définie)
        /// </summary>
        public MappingMatchRule GetMatchRule()
        {
            return MatchRule ?? new MappingMatchRule
            {
                PropertyName = Source.PropertyName,
                PropertyType = Source.PropertyType,
                ParentType = Source.ParentType,
                TypePath = Source.TypePath
            };
        }

        /// <summary>
        /// Nom de la propriété/variable ancienne (raccourci pour compatibilité)
        /// </summary>
        public string OldPropertyName 
        { 
            get => Source.PropertyName;
            set => Source.PropertyName = value;
        }

        /// <summary>
        /// Chemin de la nouvelle propriété (peut être imbriquée: "Obj.Prop.SubProp")
        /// </summary>
        public string NewPropertyPath 
        { 
            get => Target.FullPath;
            set => ParseTargetPath(value);
        }

        /// <summary>
        /// Type ancien (optionnel)
        /// </summary>
        public string? OldType 
        { 
            get => Source.PropertyType;
            set => Source.PropertyType = value;
        }

        /// <summary>
        /// Type nouveau (optionnel)
        /// </summary>
        public string? NewType 
        { 
            get => Target.PropertyType;
            set => Target.PropertyType = value;
        }

        /// <summary>
        /// Si true, la propriété est imbriquée (contient au moins un point)
        /// </summary>
        public bool IsNested => Target.FullPath.Contains('.');

        /// <summary>
        /// Nom de l'objet mappé si imbriqué (ex: "Consignee" si NewPropertyPath = "Consignee.Name")
        /// </summary>
        public string? MappedObjectName => Target.ParentName;

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
            Source.PropertyName = oldName ?? throw new ArgumentNullException(nameof(oldName));
            ParseTargetPath(newPath ?? throw new ArgumentNullException(nameof(newPath)));
        }

        /// <summary>
        /// Crée un mapping complet avec toutes les informations source et cible
        /// </summary>
        public PropertyMapping(SourcePropertyInfo source, TargetPropertyInfo target)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        private void ParseTargetPath(string path)
        {
            if (path.Contains('.'))
            {
                var parts = path.Split('.');
                Target.ParentName = string.Join(".", parts.Take(parts.Length - 1));
                Target.PropertyName = parts.Last();
            }
            else
            {
                Target.PropertyName = path;
                Target.ParentName = null;
            }
        }

        public override string ToString()
        {
            var sourceStr = Source.ToString();
            var targetStr = Target.ToString();
            return $"{sourceStr} ? {targetStr}";
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
    /// Moteur de mapping de propriétés pour migrations Legacy utilisant Roslyn
    /// Supporte le matching par chemin de types complet pour désambiguïsation
    /// </summary>
    public class PropertyMappingEngine
    {
        private readonly List<PropertyMapping> _mappings = new();
        private bool _caseSensitive = true;
        private bool _wholeWordOnly = true;
        private bool _useSemanticAnalysis = false;

        /// <summary>
        /// Crée une nouvelle instance du moteur de mapping
        /// </summary>
        public PropertyMappingEngine()
        {
        }

        /// <summary>
        /// Crée une instance avec configuration
        /// </summary>
        public PropertyMappingEngine(bool caseSensitive = true, bool wholeWordOnly = true, bool useSemanticAnalysis = false)
        {
            _caseSensitive = caseSensitive;
            _wholeWordOnly = wholeWordOnly;
            _useSemanticAnalysis = useSemanticAnalysis;
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
        /// Ajoute un mapping complet avec informations source et cible
        /// </summary>
        public void AddMapping(SourcePropertyInfo source, TargetPropertyInfo target, string? context = null, string? description = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var mapping = new PropertyMapping(source, target)
            {
                Context = context,
                Description = description
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
        /// Ajoute un mapping avec contexte parent complet (types inclus)
        /// </summary>
        public void AddMappingWithParentContext(
            string oldPropertyName, string oldPropertyType,
            string? oldParentName, string? oldParentType,
            string newPropertyName, string newPropertyType,
            string? newParentName = null, string? newParentType = null,
            string? description = null)
        {
            var source = new SourcePropertyInfo
            {
                PropertyName = oldPropertyName,
                PropertyType = oldPropertyType,
                ParentName = oldParentName,
                ParentType = oldParentType
            };

            var target = new TargetPropertyInfo
            {
                PropertyName = newPropertyName,
                PropertyType = newPropertyType,
                ParentName = newParentName,
                ParentType = newParentType
            };

            var mapping = new PropertyMapping(source, target)
            {
                Description = description
            };
            _mappings.Add(mapping);
        }

        /// <summary>
        /// Ajoute un mapping avec chemin de types complet pour désambiguïsation maximale
        /// C'est LA méthode recommandée pour les cas complexes
        /// </summary>
        /// <param name="oldPropertyName">Nom de la propriété source (ex: "Name")</param>
        /// <param name="oldPropertyType">Type de la propriété source (ex: "string")</param>
        /// <param name="oldTypePath">Chemin de types source (ex: "Order.Sender" pour Order.Sender.Name)</param>
        /// <param name="newPropertyPath">Chemin complet de la cible (ex: "Sender.Address.Name")</param>
        /// <param name="newPropertyType">Type de la propriété cible (ex: "string")</param>
        /// <param name="description">Description optionnelle</param>
        public void AddMappingWithTypePath(
            string oldPropertyName,
            string oldPropertyType,
            string oldTypePath,
            string newPropertyPath,
            string newPropertyType,
            string? description = null)
        {
            var source = new SourcePropertyInfo
            {
                PropertyName = oldPropertyName,
                PropertyType = oldPropertyType,
                TypePath = oldTypePath
            };

            // Parser le chemin cible
            var targetParts = newPropertyPath.Split('.');
            var target = new TargetPropertyInfo
            {
                PropertyName = targetParts.Last(),
                PropertyType = newPropertyType,
                ParentName = targetParts.Length > 1 
                    ? string.Join(".", targetParts.Take(targetParts.Length - 1)) 
                    : null
            };

            var mapping = new PropertyMapping(source, target)
            {
                Description = description,
                MatchRule = new MappingMatchRule
                {
                    PropertyName = oldPropertyName,
                    PropertyType = oldPropertyType,
                    TypePath = oldTypePath
                }
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
        /// Trouve un mapping par nom et contexte parent
        /// </summary>
        public PropertyMapping? FindMapping(string oldName, string? parentName, string? parentType = null)
        {
            return _mappings.FirstOrDefault(m => 
                m.OldPropertyName == oldName &&
                (parentName == null || m.Source.ParentName == parentName) &&
                (parentType == null || m.Source.ParentType == parentType));
        }

        /// <summary>
        /// Trouve le meilleur mapping correspondant en utilisant les règles de priorité
        /// </summary>
        public PropertyMapping? FindBestMapping(string propertyName, string? propertyType, string? parentType, string? typePath)
        {
            var matchingMappings = _mappings
                .Where(m => m.GetMatchRule().Matches(propertyName, propertyType, parentType, typePath))
                .OrderByDescending(m => m.GetMatchRule().Priority)
                .ToList();

            return matchingMappings.FirstOrDefault();
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
        /// Transforme le code en appliquant tous les mappings (utilise Roslyn si configuré)
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
                if (_useSemanticAnalysis)
                {
                    return TransformCodeWithRoslyn(code);
                }
                else
                {
                    return TransformCodeWithRegex(code);
                }
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
        /// Transforme le code en utilisant l'analyse sémantique Roslyn
        /// </summary>
        private MappingTransformResult TransformCodeWithRoslyn(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var changes = new List<string>();
            var replacementsCount = 0;

            var rewriter = new PropertyMappingRewriter(_mappings, _caseSensitive);
            var newRoot = rewriter.Visit(root);

            replacementsCount = rewriter.ReplacementsCount;
            changes.AddRange(rewriter.Changes);

            return new MappingTransformResult
            {
                Success = true,
                TransformedCode = newRoot.ToFullString(),
                Changes = changes,
                ReplacementsCount = replacementsCount
            };
        }

        /// <summary>
        /// Transforme le code en utilisant les expressions régulières
        /// </summary>
        private MappingTransformResult TransformCodeWithRegex(string code)
        {
            var result = code;
            var replacementsCount = 0;
            var changes = new List<string>();

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

        /// <summary>
        /// Valide que les mappings ont du sens
        /// </summary>
        public List<string> ValidateMappings()
        {
            var issues = new List<string>();

            // Vérifier les doublons (même clé de matching)
            var duplicates = _mappings
                .GroupBy(m => new { m.OldPropertyName, m.Source.ParentName, m.Source.TypePath })
                .Where(g => g.Count() > 1 && g.All(m => m.Context == null))
                .ToList();

            foreach (var dup in duplicates)
            {
                var context = dup.Key.TypePath ?? dup.Key.ParentName;
                issues.Add($"Duplicate mapping for '{dup.Key.OldPropertyName}'" + 
                    (context != null ? $" in '{context}'" : "") +
                    " without context");
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

            // Vérifier la cohérence des types
            foreach (var mapping in _mappings)
            {
                if (!string.IsNullOrEmpty(mapping.OldType) && !string.IsNullOrEmpty(mapping.NewType))
                {
                    if (!AreTypesCompatible(mapping.OldType, mapping.NewType))
                    {
                        issues.Add($"Type mismatch: '{mapping.OldPropertyName}' ({mapping.OldType}) ? ({mapping.NewType})");
                    }
                }
            }

            return issues;
        }

        private bool AreTypesCompatible(string oldType, string newType)
        {
            // Types identiques
            if (oldType == newType) return true;

            // Types numériques compatibles
            var numericTypes = new HashSet<string> { "int", "long", "short", "byte", "float", "double", "decimal" };
            if (numericTypes.Contains(oldType) && numericTypes.Contains(newType)) return true;

            // Nullable compatible
            if (newType == oldType + "?" || oldType == newType + "?") return true;

            return false;
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
            report.AppendLine($"Semantic Analysis: {(_useSemanticAnalysis ? "Enabled" : "Disabled")}");
            report.AppendLine();

            if (_mappings.Count == 0)
            {
                report.AppendLine("No mappings defined.");
                return report.ToString();
            }

            report.AppendLine("Mappings:");
            foreach (var mapping in _mappings)
            {
                report.AppendLine($"  • {mapping}");
                
                if (!string.IsNullOrEmpty(mapping.Source.TypePath))
                    report.AppendLine($"    Type Path: {mapping.Source.TypePath}");
                else if (mapping.Source.ParentName != null)
                    report.AppendLine($"    Source Parent: {mapping.Source.ParentName}" + 
                        (!string.IsNullOrEmpty(mapping.Source.ParentType) ? $" ({mapping.Source.ParentType})" : ""));
                
                if (mapping.Target.ParentName != null)
                    report.AppendLine($"    Target Parent: {mapping.Target.ParentName}" + 
                        (!string.IsNullOrEmpty(mapping.Target.ParentType) ? $" ({mapping.Target.ParentType})" : ""));
                
                if (!string.IsNullOrEmpty(mapping.Context))
                    report.AppendLine($"    Context: {mapping.Context}");
                if (!string.IsNullOrEmpty(mapping.Description))
                    report.AppendLine($"    Description: {mapping.Description}");
                
                var rule = mapping.GetMatchRule();
                report.AppendLine($"    Match Priority: {rule.Priority}");
            }

            var validationIssues = ValidateMappings();
            if (validationIssues.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Validation Issues:");
                foreach (var issue in validationIssues)
                {
                    report.AppendLine($"  ? {issue}");
                }
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Rewriter Roslyn pour appliquer les mappings de propriétés avec support TypePath
    /// </summary>
    internal class PropertyMappingRewriter : CSharpSyntaxRewriter
    {
        private readonly List<PropertyMapping> _mappings;
        private readonly bool _caseSensitive;
        public int ReplacementsCount { get; private set; }
        public List<string> Changes { get; } = new();

        public PropertyMappingRewriter(List<PropertyMapping> mappings, bool caseSensitive)
        {
            _mappings = mappings;
            _caseSensitive = caseSensitive;
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var memberName = node.Name.Identifier.Text;
            var (parentName, typePath) = GetExpressionContext(node.Expression);

            // Chercher le meilleur mapping correspondant
            var mapping = FindBestMatchingMapping(memberName, null, parentName, typePath);
            if (mapping != null)
            {
                ReplacementsCount++;
                Changes.Add($"Mapped '{memberName}' ? '{mapping.NewPropertyPath}' (context: {typePath ?? parentName ?? "none"})");

                // Construire la nouvelle expression
                var newExpression = BuildNewMemberAccess(node.Expression, mapping.NewPropertyPath);
                return newExpression;
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Ne pas traiter si c'est déjà dans un MemberAccessExpression
            if (node.Parent is MemberAccessExpressionSyntax)
                return base.VisitIdentifierName(node);

            var name = node.Identifier.Text;
            var mapping = FindBestMatchingMapping(name, null, null, null);
            
            if (mapping != null)
            {
                ReplacementsCount++;
                Changes.Add($"Mapped identifier '{name}' ? '{mapping.NewPropertyPath}'");

                if (mapping.IsNested)
                {
                    return SyntaxFactory.ParseExpression(mapping.NewPropertyPath);
                }
                else
                {
                    return SyntaxFactory.IdentifierName(mapping.Target.PropertyName);
                }
            }

            return base.VisitIdentifierName(node);
        }

        /// <summary>
        /// Extrait le contexte (nom parent et chemin de types) d'une expression
        /// </summary>
        private (string? parentName, string? typePath) GetExpressionContext(ExpressionSyntax expression)
        {
            var parts = new List<string>();
            var current = expression;

            while (current != null)
            {
                switch (current)
                {
                    case IdentifierNameSyntax id:
                        parts.Insert(0, id.Identifier.Text);
                        current = null;
                        break;
                    case MemberAccessExpressionSyntax mae:
                        parts.Insert(0, mae.Name.Identifier.Text);
                        current = mae.Expression;
                        break;
                    case ThisExpressionSyntax:
                        parts.Insert(0, "this");
                        current = null;
                        break;
                    default:
                        current = null;
                        break;
                }
            }

            if (parts.Count == 0)
                return (null, null);

            var parentName = parts.Last();
            var typePath = string.Join(".", parts);

            return (parentName, typePath);
        }

        private PropertyMapping? FindBestMatchingMapping(string propertyName, string? propertyType, string? parentName, string? typePath)
        {
            var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Trier par priorité de matching
            var candidates = _mappings
                .Select(m => new
                {
                    Mapping = m,
                    Rule = m.GetMatchRule(),
                    Matches = m.GetMatchRule().Matches(propertyName, propertyType, parentName, typePath)
                })
                .Where(x => x.Matches || string.Equals(x.Mapping.OldPropertyName, propertyName, comparison))
                .OrderByDescending(x => x.Matches ? x.Rule.Priority : 0)
                .ThenByDescending(x => x.Mapping.Source.TypePath != null ? 1 : 0)
                .ThenByDescending(x => x.Mapping.Source.ParentName != null ? 1 : 0)
                .ToList();

            return candidates.FirstOrDefault()?.Mapping;
        }

        private ExpressionSyntax BuildNewMemberAccess(ExpressionSyntax baseExpression, string newPath)
        {
            var parts = newPath.Split('.');
            ExpressionSyntax current = baseExpression;

            foreach (var part in parts)
            {
                current = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    current,
                    SyntaxFactory.IdentifierName(part));
            }

            return current;
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

            var oldProperties = ExtractPropertyInfos(oldCode);
            var newProperties = ExtractPropertyInfos(newCode);

            var suggestedMappings = new List<PropertyMapping>();

            foreach (var oldProp in oldProperties)
            {
                var bestMatch = FindBestMatch(oldProp, newProperties);

                if (bestMatch != null && bestMatch.Similarity > 0.6)
                {
                    suggestedMappings.Add(new PropertyMapping(
                        new SourcePropertyInfo
                        {
                            PropertyName = oldProp.PropertyName,
                            PropertyType = oldProp.PropertyType,
                            ParentName = oldProp.ParentName,
                            ParentType = oldProp.ParentType,
                            TypePath = oldProp.TypePath
                        },
                        new TargetPropertyInfo
                        {
                            PropertyName = bestMatch.Property.PropertyName,
                            PropertyType = bestMatch.Property.PropertyType,
                            ParentName = bestMatch.Property.ParentName,
                            ParentType = bestMatch.Property.ParentType,
                            TypePath = bestMatch.Property.TypePath
                        })
                    {
                        Description = $"Suggested mapping (similarity: {bestMatch.Similarity:P})"
                    });
                }
            }

            return suggestedMappings;
        }

        /// <summary>
        /// Extrait les informations de propriétés en utilisant Roslyn
        /// </summary>
        private List<SourcePropertyInfo> ExtractPropertyInfos(string code)
        {
            var properties = new List<SourcePropertyInfo>();

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                // Extraire les propriétés
                foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var parentClass = prop.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    properties.Add(new SourcePropertyInfo
                    {
                        PropertyName = prop.Identifier.Text,
                        PropertyType = prop.Type.ToString(),
                        ParentName = parentClass?.Identifier.Text,
                        ParentType = "class",
                        TypePath = parentClass?.Identifier.Text
                    });
                }

                // Extraire les champs
                foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    var parentClass = field.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    foreach (var variable in field.Declaration.Variables)
                    {
                        properties.Add(new SourcePropertyInfo
                        {
                            PropertyName = variable.Identifier.Text,
                            PropertyType = field.Declaration.Type.ToString(),
                            ParentName = parentClass?.Identifier.Text,
                            ParentType = "class",
                            TypePath = parentClass?.Identifier.Text
                        });
                    }
                }
            }
            catch
            {
                // Fallback to regex extraction if parsing fails
                var regex = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*(?:=|;)");
                var matches = regex.Matches(code);
                foreach (Match match in matches)
                {
                    properties.Add(new SourcePropertyInfo
                    {
                        PropertyName = match.Groups[1].Value
                    });
                }
            }

            return properties;
        }

        private class MatchResult
        {
            public SourcePropertyInfo Property { get; set; } = new();
            public double Similarity { get; set; }
        }

        private MatchResult? FindBestMatch(SourcePropertyInfo oldProp, List<SourcePropertyInfo> newProps)
        {
            var bestMatch = newProps
                .Select(newProp => new MatchResult
                {
                    Property = newProp,
                    Similarity = CalculateSimilarity(oldProp, newProp)
                })
                .OrderByDescending(m => m.Similarity)
                .FirstOrDefault();

            return bestMatch?.Similarity > 0 ? bestMatch : null;
        }

        private double CalculateSimilarity(SourcePropertyInfo prop1, SourcePropertyInfo prop2)
        {
            double score = 0;
            double weight = 0;

            // Comparaison du nom (poids le plus élevé)
            var nameScore = CalculateStringSimilarity(prop1.PropertyName, prop2.PropertyName);
            score += nameScore * 3;
            weight += 3;

            // Comparaison du type
            if (!string.IsNullOrEmpty(prop1.PropertyType) && !string.IsNullOrEmpty(prop2.PropertyType))
            {
                var typeScore = prop1.PropertyType == prop2.PropertyType ? 1.0 : 0.5;
                score += typeScore;
                weight += 1;
            }

            // Comparaison du parent
            if (!string.IsNullOrEmpty(prop1.ParentName) && !string.IsNullOrEmpty(prop2.ParentName))
            {
                var parentScore = CalculateStringSimilarity(prop1.ParentName, prop2.ParentName);
                score += parentScore;
                weight += 1;
            }

            return weight > 0 ? score / weight : 0;
        }

        private double CalculateStringSimilarity(string str1, string str2)
        {
            var lower1 = str1.ToLower();
            var lower2 = str2.ToLower();

            if (lower1 == lower2) return 1.0;
            if (lower2.Contains(lower1) || lower1.Contains(lower2)) return 0.8;

            var commonChars = lower1.Intersect(lower2).Count();
            return (double)commonChars / Math.Max(lower1.Length, lower2.Length);
        }
    }
}
