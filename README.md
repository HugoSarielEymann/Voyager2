# CodeSearcher

**Bibliothèque .NET pour rechercher et modifier du code C# par programmation**

CodeSearcher permet d'analyser, chercher et transformer du code C# en utilisant une API fluide basée sur Roslyn.

---

## ?? Ce que ça apporte

- **Recherche de code** : Trouvez méthodes, classes, variables et retours avec des filtres puissants
- **Modification de code** : Renommez, wrappez, transformez du code de manière fluide
- **Analyse conditionnelle** : Identifiez les chemins d'exécution et conditions
- **API fluide** : Syntaxe naturelle et chainable
- **CLI interactif** : Outil en ligne de commande pour transformations batch

---

## ?? Projets

| Projet | Description |
|--------|-------------|
| `CodeSearcher.Core` | Moteur de recherche et analyse de code |
| `CodeSearcher.Editor` | Opérations de modification de code |
| `CodeSearcher.Cli` | Interface en ligne de commande |
| `CodeSearcher.Tests` | Tests unitaires et d'intégration |

---

## ?? Installation

```bash
dotnet add package CodeSearcher.Core
dotnet add package CodeSearcher.Editor
```

---

## ?? Utilisation

### 1?? **Recherche de code**

```csharp
using CodeSearcher.Core;

// Charger du code
var context = CodeContext.FromCode(sourceCode);
// ou
var context = CodeContext.FromFile("MyClass.cs");

// Chercher des méthodes
var asyncMethods = context.FindMethods()
    .IsAsync()
    .ReturningTask()
    .IsPublic()
    .Execute();

// Chercher des classes
var controllers = context.FindClasses()
    .WithNameContaining("Controller")
    .IsPublic()
    .Implements("IController")
    .Execute();

// Chercher des variables
var variables = context.FindVariables()
    .WithName("userId")
    .OfType("int")
    .Execute();

// Chercher des returns
var returns = context.FindReturns()
    .WithValue("null")
    .InMethod("GetUser")
    .Execute();
```

### 2?? **Modification de code**

```csharp
using CodeSearcher.Editor;

// Créer un éditeur
var editor = CodeEditor.FromCode(sourceCode);

// Renommer
editor.RenameMethod("OldName", "NewName")
      .RenameClass("User", "Customer")
      .RenameVariable("temp", "result");

// Wrapper avec try-catch
editor.WrapWithTryCatch("ProcessData");

// Wrapper avec logging
editor.WrapWithLogging("SaveUser", "Console.WriteLine(\"Saving...\");");

// Transformer en async/Task
editor.WrapReturnsInTask("GetData");

// Remplacer du code
editor.Replace("var x = 0;", "var x = 10;");

// Appliquer les modifications
var result = editor.Apply();
if (result.Success)
{
    Console.WriteLine(result.ModifiedCode);
    editor.SaveToFile("output.cs");
}
```

### 3?? **Analyse conditionnelle**

```csharp
// Trouver les conditions menant à une instruction
var paths = context.FindConditionsLeadingTo(statement);

foreach (var path in paths)
{
    Console.WriteLine($"Condition: {path.Condition}");
    Console.WriteLine($"Type: {path.Type}"); // If, While, Switch, etc.
    Console.WriteLine($"Branch: {path.BranchTaken}");
}

// Vérifier l'atteignabilité
bool isReachable = context.IsStatementReachable(statement);
bool isAlwaysExecuted = context.IsStatementUnconditionallyReachable(statement);
```

### 4?? **CLI - Transformations en batch**

```bash
cd CodeSearcher.Cli
dotnet run config.json
```

Fichier de configuration :
```json
{
  "name": "Convertir en Async",
  "filePatterns": ["*.cs"],
  "transformations": [
    {
      "type": "wrapReturnsinTask",
      "target": "GetUser",
      "parameters": { "style": "TaskFromResult" }
    }
  ]
}
```

Voir `CodeSearcher.Cli/README.md` pour plus de détails.

---

## ?? Filtres disponibles

### Méthodes
- `WithName(string)` / `WithNameContaining(string)`
- `IsAsync()` / `IsPublic()` / `IsPrivate()` / `IsProtected()`
- `ReturningTask()` / `ReturningTask<T>()` / `ReturningType(string)`
- `HasParameterCount(int)` / `HasParameter(predicate)`
- `WithAttribute(string)`

### Classes
- `WithName(string)` / `WithNameContaining(string)`
- `IsPublic()` / `IsAbstract()` / `IsSealed()`
- `InNamespace(string)`
- `Inherits(string)` / `Implements(string)`
- `WithAttribute(string)`
- `WithMemberCount(int, predicate)`

### Variables
- `WithName(string)` / `OfType(string)`
- `InMethod(string)` / `InClass(string)`
- `IsConst()` / `WithInitializer(predicate)`

### Returns
- `WithValue(string)` / `WithType(string)`
- `InMethod(string)` / `InClass(string)`

---

## ?? Exemples pratiques

**Trouver toutes les méthodes async publiques retournant Task<User>**
```csharp
var methods = context.FindMethods()
    .IsAsync()
    .IsPublic()
    .ReturningTask<User>()
    .Execute();
```

**Ajouter try-catch à toutes les méthodes sans gestion d'erreurs**
```csharp
var editor = CodeEditor.FromFile("Service.cs");
foreach (var method in unsafeMethods)
{
    editor.WrapWithTryCatch(method.Identifier.Text);
}
editor.Apply();
```

**Renommer une classe et toutes ses références**
```csharp
editor.RenameClass("OldUser", "Customer")
      .RenameProperty("UserName", "CustomerName")
      .Apply();
```

**Convertir des méthodes sync en async (batch)**
```csharp
var editor = CodeEditor.FromFile("Repository.cs");
editor.WrapReturnsInTask("GetById")
      .WrapReturnsInTask("GetAll")
      .WrapReturnsInTask("Save");
var result = editor.Apply();
```

---

## ?? Tests

```bash
dotnet test
```

Les tests incluent :
- Tests unitaires pour chaque query
- Tests d'intégration pour les transformations
- Tests de scénarios réels

---

## ??? Architecture

- **Core** : Roslyn-based parsing et querying
- **Editor** : Strategy pattern pour les transformations
- **Logging** : Interface `ILogger` injectable pour debugging
- **Async** : Support complet async/await et `Task<T>`
- **CLI** : Configuration JSON pour automatisation

### Pattern Strategy
Les transformations utilisent le pattern Strategy :
- `RenameStrategy` : Renommage d'éléments
- `WrapperStrategy` : Wrapper try-catch, logging, etc.
- `ReturnTypeWrapperStrategy` : Conversion sync ? async

---

## ?? Cas d'usage

1. **Migration de code legacy** : Renommer en masse des APIs obsolètes
2. **Ajout de logging** : Wrapper automatiquement des méthodes critiques
3. **Conversion async** : Migrer du code sync vers async/await
4. **Analyse de code** : Identifier des patterns ou anti-patterns
5. **Refactoring** : Renommer uniformément des classes/méthodes
6. **Gestion d'erreurs** : Ajouter try-catch aux méthodes à risque

---

## ?? Ressources

- **Core API** : Pour recherche et analyse programmatique
- **Editor API** : Pour transformations programmatiques
- **CLI** : Pour automatisation et batch processing
  - Voir `CodeSearcher.Cli/README.md` pour le guide complet
  - Exemples dans `CodeSearcher.Cli/Examples/`

---

## ?? License

MIT

---

## ?? Contribution

Les contributions sont bienvenues ! Ouvrez une issue ou soumettez une PR.

---

**Happy Coding! ??**
# CodeSearcher

**Bibliothèque .NET pour rechercher et modifier du code C# par programmation**

CodeSearcher permet d'analyser, chercher et transformer du code C# en utilisant une API fluide basée sur Roslyn.

---

## ?? Ce que ça apporte

- **Recherche de code** : Trouvez méthodes, classes, variables et retours avec des filtres puissants
- **Modification de code** : Renommez, wrappez, transformez du code de manière fluide
- **Analyse conditionnelle** : Identifiez les chemins d'exécution et conditions
- **API fluide** : Syntaxe naturelle et chainable
- **CLI interactif** : Outil en ligne de commande pour transformations batch

---

## ?? Projets

| Projet | Description |
|--------|-------------|
| `CodeSearcher.Core` | Moteur de recherche et analyse de code |
| `CodeSearcher.Editor` | Opérations de modification de code |
| `CodeSearcher.Cli` | Interface en ligne de commande |
| `CodeSearcher.Tests` | Tests unitaires et d'intégration |

---

## ?? Installation

```bash
dotnet add package CodeSearcher.Core
dotnet add package CodeSearcher.Editor
```

---

## ?? Utilisation

### 1?? **Recherche de code**

```csharp
using CodeSearcher.Core;

// Charger du code
var context = CodeContext.FromCode(sourceCode);
// ou
var context = CodeContext.FromFile("MyClass.cs");

// Chercher des méthodes
var asyncMethods = context.FindMethods()
    .IsAsync()
    .ReturningTask()
    .IsPublic()
    .Execute();

// Chercher des classes
var controllers = context.FindClasses()
    .WithNameContaining("Controller")
    .IsPublic()
    .Implements("IController")
    .Execute();

// Chercher des variables
var variables = context.FindVariables()
    .WithName("userId")
    .OfType("int")
    .Execute();

// Chercher des returns
var returns = context.FindReturns()
    .WithValue("null")
    .InMethod("GetUser")
    .Execute();
```

### 2?? **Modification de code**

```csharp
using CodeSearcher.Editor;

// Créer un éditeur
var editor = CodeEditor.FromCode(sourceCode);

// Renommer
editor.RenameMethod("OldName", "NewName")
      .RenameClass("User", "Customer")
      .RenameVariable("temp", "result");

// Wrapper avec try-catch
editor.WrapWithTryCatch("ProcessData");

// Wrapper avec logging
editor.WrapWithLogging("SaveUser", "Console.WriteLine(\"Saving...\");");

// Transformer en async/Task
editor.WrapReturnsInTask("GetData");

// Remplacer du code
editor.Replace("var x = 0;", "var x = 10;");

// Appliquer les modifications
var result = editor.Apply();
if (result.Success)
{
    Console.WriteLine(result.ModifiedCode);
    editor.SaveToFile("output.cs");
}
```

### 3?? **Analyse conditionnelle**

```csharp
// Trouver les conditions menant à une instruction
var paths = context.FindConditionsLeadingTo(statement);

foreach (var path in paths)
{
    Console.WriteLine($"Condition: {path.Condition}");
    Console.WriteLine($"Type: {path.Type}"); // If, While, Switch, etc.
    Console.WriteLine($"Branch: {path.BranchTaken}");
}

// Vérifier l'atteignabilité
bool isReachable = context.IsStatementReachable(statement);
bool isAlwaysExecuted = context.IsStatementUnconditionallyReachable(statement);
```

### 4?? **CLI - Transformations en batch**

```bash
cd CodeSearcher.Cli
dotnet run config.json
```

Fichier de configuration :
```json
{
  "name": "Convertir en Async",
  "filePatterns": ["*.cs"],
  "transformations": [
    {
      "type": "wrapReturnsinTask",
      "target": "GetUser",
      "parameters": { "style": "TaskFromResult" }
    }
  ]
}
```

Voir `CodeSearcher.Cli/README.md` pour plus de détails.

---

## ?? Filtres disponibles

### Méthodes
- `WithName(string)` / `WithNameContaining(string)`
- `IsAsync()` / `IsPublic()` / `IsPrivate()` / `IsProtected()`
- `ReturningTask()` / `ReturningTask<T>()` / `ReturningType(string)`
- `HasParameterCount(int)` / `HasParameter(predicate)`
- `WithAttribute(string)`

### Classes
- `WithName(string)` / `WithNameContaining(string)`
- `IsPublic()` / `IsAbstract()` / `IsSealed()`
- `InNamespace(string)`
- `Inherits(string)` / `Implements(string)`
- `WithAttribute(string)`
- `WithMemberCount(int, predicate)`

### Variables
- `WithName(string)` / `OfType(string)`
- `InMethod(string)` / `InClass(string)`
- `IsConst()` / `WithInitializer(predicate)`

### Returns
- `WithValue(string)` / `WithType(string)`
- `InMethod(string)` / `InClass(string)`

---

## ?? Exemples pratiques

**Trouver toutes les méthodes async publiques retournant Task<User>**
```csharp
var methods = context.FindMethods()
    .IsAsync()
    .IsPublic()
    .ReturningTask<User>()
    .Execute();
```

**Ajouter try-catch à toutes les méthodes sans gestion d'erreurs**
```csharp
var editor = CodeEditor.FromFile("Service.cs");
foreach (var method in unsafeMethods)
{
    editor.WrapWithTryCatch(method.Identifier.Text);
}
editor.Apply();
```

**Renommer une classe et toutes ses références**
```csharp
editor.RenameClass("OldUser", "Customer")
      .RenameProperty("UserName", "CustomerName")
      .Apply();
```

**Convertir des méthodes sync en async (batch)**
```csharp
var editor = CodeEditor.FromFile("Repository.cs");
editor.WrapReturnsInTask("GetById")
      .WrapReturnsInTask("GetAll")
      .WrapReturnsInTask("Save");
var result = editor.Apply();
```

---

## ?? Tests

```bash
dotnet test
```

Les tests incluent :
- Tests unitaires pour chaque query
- Tests d'intégration pour les transformations
- Tests de scénarios réels

---

## ??? Architecture

- **Core** : Roslyn-based parsing et querying
- **Editor** : Strategy pattern pour les transformations
- **Logging** : Interface `ILogger` injectable pour debugging
- **Async** : Support complet async/await et `Task<T>`
- **CLI** : Configuration JSON pour automatisation

### Pattern Strategy
Les transformations utilisent le pattern Strategy :
- `RenameStrategy` : Renommage d'éléments
- `WrapperStrategy` : Wrapper try-catch, logging, etc.
- `ReturnTypeWrapperStrategy` : Conversion sync ? async

---

## ?? Cas d'usage

1. **Migration de code legacy** : Renommer en masse des APIs obsolètes
2. **Ajout de logging** : Wrapper automatiquement des méthodes critiques
3. **Conversion async** : Migrer du code sync vers async/await
4. **Analyse de code** : Identifier des patterns ou anti-patterns
5. **Refactoring** : Renommer uniformément des classes/méthodes
6. **Gestion d'erreurs** : Ajouter try-catch aux méthodes à risque

---

## ?? Ressources

- **Core API** : Pour recherche et analyse programmatique
- **Editor API** : Pour transformations programmatiques
- **CLI** : Pour automatisation et batch processing
  - Voir `CodeSearcher.Cli/README.md` pour le guide complet
  - Exemples dans `CodeSearcher.Cli/Examples/`

---

## ?? License

MIT

---

## ?? Contribution

Les contributions sont bienvenues ! Ouvrez une issue ou soumettez une PR.

---

**Happy Coding! ??**
