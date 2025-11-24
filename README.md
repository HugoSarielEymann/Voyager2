# CodeSearcher - C# Code Analysis & Modification Library

Une bibliothèque complète pour **requêter, analyser et modifier du code C#** en utilisant une API fluente basée sur Roslyn/CodeAnalysis.

## ?? Architecture

Le projet est divisé en **deux phases** :

### **Phase 1 : CodeSearcher.Core** - Requêtes de Code
Permet de **rechercher et sélectionner du code C#** avec une syntaxe LINQ-like fluente.

### **Phase 2 : CodeSearcher.Editor** - Modification de Code  
Permet de **modifier, renommer et transformer du code C#** de manière programmatique.

## ?? Installation

```bash
cd CodeSearcher
dotnet build
dotnet test
```

## ?? Tests

- **Phase 1** : 51 tests ?
- **Phase 2** : 10 tests ?
- **Total** : 61 tests passants

```bash
dotnet test CodeSearcher.sln --verbosity minimal
```

---

## Phase 1 : CodeSearcher.Core - Requêtes

### Utilisation Basique

```csharp
// Charger du code depuis une chaîne
var context = CodeContext.FromCode(codeString);

// Ou depuis un fichier
var context = CodeContext.FromFile("MyClass.cs");
```

### Requêtes Simples

```csharp
// Trouver une méthode spécifique
var method = context.FindMethods()
    .WithName("ProcessData")
    .FirstOrDefault();

// Trouver toutes les classes publiques
var classes = context.FindClasses()
    .IsPublic()
    .Execute();

// Trouver les variables de type string
var strings = context.FindVariables()
    .WithType("string")
    .Execute();

// Trouver les return statements
var returns = context.FindReturns()
    .InMethod("GetData")
    .Execute();
```

### Requêtes Chaînées Complexes

```csharp
// Toutes les méthodes publiques, async, retournant Task
var asyncPublicMethods = context.FindMethods()
    .IsPublic()
    .IsAsync()
    .ReturningTask()
    .Execute();

// Classes publiques implémentant IDisposable
var disposableClas = context.FindClasses()
    .IsPublic()
    .Implements("IDisposable")
    .Execute();

// Variables readonly de type string
var readOnlyStrings = context.FindVariables()
    .IsReadOnly()
    .WithType("string")
    .Execute();
```

### API MethodQuery

```csharp
.WithName(name)                 // Nom exact
.WithNameContaining(partial)    // Nom contenant
.ReturningTask()                // Retournant Task<T>
.ReturningTask<T>()             // Task<T> spécifique
.ReturningType(typeName)        // Type de retour
.IsAsync()                      // Méthodes async
.IsPublic/IsPrivate/IsProtected() // Visibilité
.HasParameterCount(n)           // Nombre de paramètres
.HasParameter(predicate)        // Filtre personnalisé
.WithAttribute(name)            // Avec attribut
```

### API ClassQuery

```csharp
.WithName(name)
.WithNameContaining(partial)
.InNamespace(namespace)
.WithAttribute(name)
.IsAbstract()
.IsSealed()
.IsPublic()
.Inherits(baseName)
.Implements(interfaceName)
.WithMemberCount(count, predicate)
```

### API VariableQuery

```csharp
.WithName(name)
.WithNameContaining(partial)
.WithType(typeName)
.WithAttribute(name)
.IsPublic/IsPrivate/IsProtected()
.IsReadOnly()
.WithInitializer()
```

### API ReturnQuery

```csharp
.InMethod(methodName)
.ReturningType(typeName)
.ReturningNull()
.WithExpression(predicate)
```

---

## Phase 2 : CodeSearcher.Editor - Modification

### Utilisation Basique

```csharp
// Créer un éditeur
var editor = CodeEditor.FromCode(codeString);

// Ou depuis un fichier
var editor = CodeEditor.FromFile("MyClass.cs");
```

### Renommage

```csharp
// Renommer une méthode
var result = editor
    .RenameMethod("GetUser", "FetchUser")
    .Apply();

// Renommer une classe
editor.RenameClass("User", "Person");

// Renommer une propriété
editor.RenameProperty("Name", "FullName");

// Renommer une variable
editor.RenameVariable("temp", "temporary");
```

### Wrapping de Code

```csharp
// Ajouter un try-catch
editor.WrapWithTryCatch("ProcessData", "throw;");

// Ajouter du logging
editor.WrapWithLogging("GetUser", "Logger.Info(\"Getting user\");");

// Ajouter de la validation
editor.WrapWithValidation("ProcessUser", "if (user == null) return;");
```

### Remplacement

```csharp
// Remplacer un snippet de code
editor.Replace(
    "new User { Name = \"Default\" }",
    "User.CreateDefault()"
);
```

### Opérations Chaînées

```csharp
var result = editor
    .RenameMethod("OldMethod", "NewMethod")
    .RenameClass("OldClass", "NewClass")
    .Replace("oldPattern", "newPattern")
    .WrapWithLogging("MyMethod", "Console.WriteLine(\"Called\");")
    .Apply();

if (result.Success)
{
    // Sauvegarder le code modifié
    editor.SaveToFile("Modified.cs");
    
    // Ou obtenir le code directement
    var modifiedCode = editor.GetCode();
    
    // Consulter le journal des modifications
    foreach (var change in result.Changes)
    {
        Console.WriteLine(change);
    }
}
else
{
    Console.WriteLine($"Erreur: {result.ErrorMessage}");
}
```

### Gestion du Code

```csharp
// Obtenir le code actuel
string current = editor.GetCode();

// Obtenir le code original
string original = editor.GetOriginalCode();

// Réinitialiser au code original
editor.Reset();

// Effacer les opérations (garde le code actuel)
editor.Clear();

// Sauvegarder dans un fichier
editor.SaveToFile("output.cs");

// Consulter l'historique des modifications
var log = editor.GetChangeLog();
```

---

## ?? Scénario Complet : Refactoring Legacy

```csharp
// Charger du code legacy
var context = CodeContext.FromFile("LegacyService.cs");

// 1. Analyser le code
var asyncMethods = context.FindMethods()
    .IsPublic()
    .IsAsync()
    .Execute()
    .ToList();

var taskMethods = context.FindMethods()
    .ReturningTask()
    .Execute()
    .ToList();

Console.WriteLine($"Found {asyncMethods.Count} async methods");
Console.WriteLine($"Found {taskMethods.Count} methods returning Task");

// 2. Modifier le code
var editor = CodeEditor.FromFile("LegacyService.cs");

editor
    .RenameMethod("GetData", "FetchDataAsync")
    .RenameClass("Service", "ModernService")
    .WrapWithLogging("ProcessData", "Logger.Debug(\"Processing\");")
    .Replace("var x =", "int x =")
    .Apply();

// 3. Sauvegarder
editor.SaveToFile("ModernService.cs");

// 4. Vérifier les modifications
var changeLog = editor.GetChangeLog();
foreach (var change in changeLog)
{
    Console.WriteLine($"? {change}");
}
```

---

## ?? Cas d'Usage

### Migrations Automatiques
- Renommer des méthodes/classes en masse
- Mettre à jour les appels de méthodes
- Refactoring de code legacy

### Analyse de Code
- Trouver toutes les méthodes async non attendues
- Localiser les usages d'une API dépréciée
- Identifier les classes sans interface publique

### Génération & Transformation
- Ajouter du logging automatique
- Envelopper le code avec validation
- Générer des wrappers

### Audits
- Compter les méthodes publiques
- Analyser les dépendances
- Vérifier les conventions de nommage

---

## ?? Tests

Tous les tests utilisent des **fixtures de code réel** pour validar chaque fonctionnalité :

```bash
# Tests Phase 1 (Requêtes)
dotnet test CodeSearcher.Tests --filter "MethodQueryTests or ClassQueryTests or VariableQueryTests or ReturnQueryTests"

# Tests Phase 2 (Modification)
dotnet test CodeSearcher.Tests --filter "CodeEditorTests"

# Tests d'intégration
dotnet test CodeSearcher.Tests --filter "Integration"
```

---

## ?? Licence

MIT - Libre d'utilisation pour tout projet

## ?? Contribution

Les contributions sont bienvenues ! N'hésitez pas à :
- Signaler des bugs
- Proposer de nouvelles fonctionnalités
- Améliorer la documentation
- Ajouter des tests

---

## ?? Roadmap

- [ ] **Phase 3** : CodeSearcher.Analyzer - Analyse statique avancée
- [ ] **Phase 4** : CodeSearcher.Generator - Génération de code
- [ ] CLI `codesearcher` pour utilisation en ligne de commande
- [ ] Support des projets multi-fichiers
- [ ] Caching pour améliorer les performances

---

**Version** : 1.0.0  
**Status** : ? Production Ready  
**Tests** : 61/61 passants
