# Examples d'Utilisation de CodeSearcher.Core

Ce document présente des exemples pratiques montrant comment utiliser CodeSearcher pour divers scénarios d'analyse de code.

## 1. Analyse de Repositories (Data Access)

### Problème
Vous avez une base de code large et voulez valider que tous vos repositories suivent le pattern asynchrone.

### Solution
```csharp
public class RepositoryValidator
{
    public static void ValidateRepositoryPattern(string code)
    {
        var context = CodeContext.FromCode(code);
        
        // Trouver toutes les méthodes publiques des repositories
        var publicMethods = context.FindMethods()
            .IsPublic()
            .ReturningTask()
            .Execute()
            .ToList();

        // Valider qu'il n'y a pas de méthodes synchrones publiques retournant des collections
        var syncMethods = context.FindMethods()
            .IsPublic()
            .ReturningType("List")
            .Execute()
            .ToList();

        if (syncMethods.Any())
        {
            throw new Exception("Found synchronous methods in repository - use async instead!");
        }

        Console.WriteLine($"? Repository follows async pattern ({publicMethods.Count} methods validated)");
    }
}
```

---

## 2. Détection de Code Smell

### Problème
Vous voulez identifier les méthodes avec trop de paramètres ou les null checks manquants.

### Solution
```csharp
public class CodeSmellDetector
{
    public static void DetectProblems(string code)
    {
        var context = CodeContext.FromCode(code);

        // Détecter les méthodes avec trop de paramètres
        var problemMethods = context.FindMethods()
            .Execute()
            .Where(m => m.ParameterList.Parameters.Count > 3)
            .ToList();

        if (problemMethods.Any())
        {
            Console.WriteLine("? Methods with too many parameters (> 3):");
            foreach (var method in problemMethods)
            {
                Console.WriteLine($"  - {method.Identifier.Text} ({method.ParameterList.Parameters.Count} params)");
            }
        }

        // Détecter les méthodes sans null check
        var nullReturns = context.FindReturns()
            .ReturningNull()
            .Execute()
            .ToList();

        Console.WriteLine($"? Found {nullReturns.Count} null return points");
    }
}
```

---

## 3. Analyse de Dépendances de Classes

### Problème
Vous voulez comprendre la structure d'héritage et les dépendances entre classes.

### Solution
```csharp
public class ArchitectureAnalyzer
{
    public static void AnalyzeArchitecture(string code)
    {
        var context = CodeContext.FromCode(code);

        // Trouver toutes les classes abstraites
        var abstractClasses = context.FindClasses()
            .IsAbstract()
            .Execute()
            .ToList();

        Console.WriteLine($"Found {abstractClasses.Count} abstract base classes:");
        foreach (var cls in abstractClasses)
        {
            Console.WriteLine($"  - {cls.Identifier.Text}");
        }

        // Trouver les classes sealed
        var sealedClasses = context.FindClasses()
            .IsSealed()
            .Execute()
            .ToList();

        Console.WriteLine($"\nFound {sealedClasses.Count} sealed classes (no subclasses possible):");
        foreach (var cls in sealedClasses)
        {
            Console.WriteLine($"  - {cls.Identifier.Text}");
        }

        // Trouver toutes les classes public
        var publicClasses = context.FindClasses()
            .IsPublic()
            .Execute()
            .ToList();

        Console.WriteLine($"\n? Total public API surface: {publicClasses.Count} classes");
    }
}
```

---

## 4. Audit de Sécurité - Détection de Patterns Dangereux

### Problème
Vous voulez détecter les appels à des méthodes dangereuses ou les patterns non sécurisés.

### Solution
```csharp
public class SecurityAuditor
{
    public static void AuditCode(string code)
    {
        var context = CodeContext.FromCode(code);

        // Détecter les return null sans validation préalable
        var unsafeNulls = context.FindReturns()
            .ReturningNull()
            .Execute()
            .ToList();

        Console.WriteLine($"? Methods returning null without proper validation: {unsafeNulls.Count}");

        // Trouver les propriétés sans attributs de validation
        var unvalidatedProps = context.FindVariables()
            .IsPublic()
            .WithType("string")
            .Execute()
            .Where(p => !p.ToString().Contains("Required") && !p.ToString().Contains("MinLength"))
            .ToList();

        if (unvalidatedProps.Any())
        {
            Console.WriteLine($"\n? String properties without validation: {unvalidatedProps.Count}");
        }

        Console.WriteLine("\n? Security audit complete");
    }
}
```

---

## 5. Refactoring Assisté - Renommage de Méthodes

### Problème
Vous voulez refactorer votre code en renommant les méthodes, mais d'abord vous devez les identifier.

### Solution
```csharp
public class RefactoringAssistant
{
    public static void FindMethodsToRefactor(string code)
    {
        var context = CodeContext.FromCode(code);

        // Trouver les méthodes avec des noms pauvres (old naming convention)
        var oldStyleMethods = context.FindMethods()
            .WithNameContaining("Get")
            .Execute()
            .ToList();

        Console.WriteLine("Methods to refactor (old Get* pattern):");
        foreach (var method in oldStyleMethods)
        {
            Console.WriteLine($"  - {method.Identifier.Text}");
            Console.WriteLine($"    Return type: {method.ReturnType}");
            Console.WriteLine($"    Visibility: {(method.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword) ? "public" : "private")}");
            Console.WriteLine();
        }

        // Utiliser avec CodeEditor pour appliquer les changements
        var editor = CodeEditor.FromCode(code);
        foreach (var method in oldStyleMethods)
        {
            var oldName = method.Identifier.Text;
            var newName = oldName.Replace("Get", "Fetch");
            editor.RenameMethod(oldName, newName);
        }

        var result = editor.Apply();
        if (result.Success)
        {
            Console.WriteLine($"? Refactoring successful: {result.Changes.Count} changes");
        }
    }
}
```

---

## 6. Validation de Conventions de Nommage

### Problème
Vous voulez vérifier que votre équipe suit les conventions de nommage établies.

### Solution
```csharp
public class NamingConventionValidator
{
    public static void ValidateNamingConventions(string code)
    {
        var context = CodeContext.FromCode(code);

        // Vérifier que les interfaces commencent par 'I'
        var badInterfaces = context.FindClasses()
            .Execute()
            .Where(c => 
                !c.Identifier.Text.StartsWith("I") && 
                c.BaseList?.Types.Any(t => t.ToString().Contains("interface")) == true)
            .ToList();

        if (badInterfaces.Any())
        {
            Console.WriteLine("? Interfaces not following 'I' prefix convention:");
            foreach (var iface in badInterfaces)
            {
                Console.WriteLine($"  - {iface.Identifier.Text} (should be I{iface.Identifier.Text})");
            }
        }

        // Vérifier que les propriétés privées commencent par '_'
        var badPrivateProps = context.FindVariables()
            .IsPrivate()
            .Execute()
            .Where(p => 
            {
                var name = p switch
                {
                    PropertyDeclarationSyntax prop => prop.Identifier.Text,
                    FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
                    VariableDeclaratorSyntax varDec => varDec.Identifier.Text,
                    _ => null
                };
                return name != null && !name.StartsWith("_");
            })
            .ToList();

        if (badPrivateProps.Any())
        {
            Console.WriteLine($"\n? Private properties not following '_' prefix: {badPrivateProps.Count}");
        }

        Console.WriteLine("\n? Naming convention validation complete");
    }
}
```

---

## 7. Analyse de Complexité - Détecter les Méthodes Complexes

### Problème
Identifier les méthodes avec trop de return statements (indicateur de complexité).

### Solution
```csharp
public class ComplexityAnalyzer
{
    public static void AnalyzeMethods(string code)
    {
        var context = CodeContext.FromCode(code);

        var allMethods = context.FindMethods().Execute().ToList();

        var complexMethods = new List<(string name, int returnCount)>();

        foreach (var method in allMethods)
        {
            var returnsInMethod = context.FindReturns()
                .InMethod(method.Identifier.Text)
                .Execute()
                .Count();

            if (returnsInMethod > 3) // Seuil de complexité
            {
                complexMethods.Add((method.Identifier.Text, returnsInMethod));
            }
        }

        if (complexMethods.Any())
        {
            Console.WriteLine("? Complex methods (> 3 return statements):");
            foreach (var (methodName, returnCount) in complexMethods.OrderByDescending(x => x.returnCount))
            {
                Console.WriteLine($"  - {methodName}: {returnCount} return points");
            }
        }

        Console.WriteLine($"\n? Analyzed {allMethods.Count} methods");
        Console.WriteLine($"  Complexity issues: {complexMethods.Count}");
    }
}
```

---

## 8. Documentation Automatique

### Problème
Générer une documentation des API publiques.

### Solution
```csharp
public class APIDocumentationGenerator
{
    public static string GenerateDocumentation(string code)
    {
        var context = CodeContext.FromCode(code);
        var sb = new StringBuilder();

        sb.AppendLine("# API Documentation\n");

        // Documenter les classes publiques
        var publicClasses = context.FindClasses()
            .IsPublic()
            .Execute()
            .ToList();

        sb.AppendLine($"## Public Classes ({publicClasses.Count})\n");

        foreach (var cls in publicClasses)
        {
            sb.AppendLine($"### {cls.Identifier.Text}");
            
            // Trouver les méthodes publiques de cette classe
            var classMethods = context.FindMethods()
                .IsPublic()
                .Execute()
                .Where(m => m.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.Text == cls.Identifier.Text))
                .ToList();

            if (classMethods.Any())
            {
                sb.AppendLine("#### Methods\n");
                foreach (var method in classMethods)
                {
                    var returnType = method.ReturnType.ToString();
                    var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier.Text}"));
                    sb.AppendLine($"- `{returnType} {method.Identifier.Text}({parameters})`");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

---

## 9. Détection de Code Dupliqué

### Problème
Identifier les méthodes similaires qui pourraient être consolidées.

### Solution
```csharp
public class DuplicateCodeDetector
{
    public static void FindDuplicates(string code)
    {
        var context = CodeContext.FromCode(code);

        var allMethods = context.FindMethods().Execute().ToList();

        // Grouper les méthodes par type de retour
        var groupedByReturnType = allMethods
            .GroupBy(m => m.ReturnType.ToString())
            .Where(g => g.Count() > 1)
            .ToList();

        Console.WriteLine("Potential duplicate methods (same return type):");
        foreach (var group in groupedByReturnType)
        {
            Console.WriteLine($"\nReturn type: {group.Key}");
            foreach (var method in group)
            {
                var returnCount = context.FindReturns()
                    .InMethod(method.Identifier.Text)
                    .Execute()
                    .Count();
                
                Console.WriteLine($"  - {method.Identifier.Text} ({returnCount} returns)");
            }
        }

        Console.WriteLine("\n? Duplicate analysis complete");
    }
}
```

---

## 10. Tests de Conformité aux Patterns

### Problème
Valider que votre code suit des patterns architecturaux établis.

### Solution
```csharp
public class PatternValidator
{
    public static void ValidateRepositoryPattern(string code)
    {
        var context = CodeContext.FromCode(code);

        // Valider le pattern Repository
        // Règles:
        // 1. Les méthodes doivent être async (Task<T>)
        // 2. Les méthodes doivent être public
        // 3. Les méthodes doivent avoir un nom significatif (Get, Find, Query, etc.)

        var violations = new List<string>();

        var publicMethods = context.FindMethods()
            .IsPublic()
            .Execute()
            .ToList();

        foreach (var method in publicMethods)
        {
            var isAsync = method.Modifiers.Any(m => m.Kind() == SyntaxKind.AsyncKeyword);
            var returnsTask = method.ReturnType.ToString().Contains("Task");
            var hasGoodName = 
                method.Identifier.Text.StartsWith("Get") ||
                method.Identifier.Text.StartsWith("Find") ||
                method.Identifier.Text.StartsWith("Query") ||
                method.Identifier.Text.StartsWith("Search");

            if (!isAsync || !returnsTask)
            {
                violations.Add($"Method '{method.Identifier.Text}' is not async/Task-based");
            }

            if (!hasGoodName)
            {
                violations.Add($"Method '{method.Identifier.Text}' has unclear naming");
            }
        }

        if (violations.Any())
        {
            Console.WriteLine("? Repository Pattern Violations:");
            foreach (var violation in violations)
            {
                Console.WriteLine($"  - {violation}");
            }
        }
        else
        {
            Console.WriteLine("? Code follows Repository Pattern perfectly!");
        }
    }
}
```

---

## Résumé des Capacités Démontrées

| Cas d'Usage | API Utilisée | Complexité |
|------------|-------------|-----------|
| Validation de Patterns | `FindMethods()` + `ReturningTask()` | ??? |
| Détection de Code Smell | `FindMethods().WithExpression()` | ??? |
| Analyse d'Architecture | `FindClasses()` + `.IsAbstract()` | ??? |
| Audit de Sécurité | `FindReturns().ReturningNull()` | ??? |
| Refactoring Assisté | `FindMethods()` + `CodeEditor` | ??? |
| Validation de Conventions | `FindVariables()` + Prédicats | ??? |
| Analyse de Complexité | `FindReturns().InMethod()` | ??? |
| Documentation Automatique | Combinaison de queries | ??? |
| Détection de Doublons | `GroupBy()` sur les résultats | ??? |
| Validation de Conformité | Combinaison d'assertions | ??? |

---

## Conclusion

CodeSearcher.Core offre une fondation flexible et puissante pour automatiser l'analyse de code C#. Les exemples ci-dessus ne sont que quelques scénarios possibles - l'API fluide permet des combinaisons infinies de requêtes pour adapter à vos besoins spécifiques.
