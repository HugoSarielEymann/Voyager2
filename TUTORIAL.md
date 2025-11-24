# ?? TUTORIEL - CodeSearcher - Apprendre à Utiliser la Fluent Selection et Transformation

## ?? Objectif

Ce tutoriel vous apprendra à:
1. **Chercher du code** avec CodeSearcher.Core (Fluent Selection)
2. **Transformer du code** avec CodeSearcher.Editor (Fluent Transformation)
3. **Combiner les deux** pour un refactoring automatisé complet

---

## ?? Table des Matières

1. [Niveau 1: Démarrage Rapide (5 min)](#niveau-1)
2. [Niveau 2: Recherche Approfondie (15 min)](#niveau-2)
3. [Niveau 3: Transformation Approfondie (15 min)](#niveau-3)
4. [Niveau 4: Cas Réels et Avancés (20 min)](#niveau-4)

---

## Niveau 1: Démarrage Rapide {#niveau-1}

### Installation

```csharp
// Importer les namespaces
using CodeSearcher.Core;
using CodeSearcher.Editor;
```

### Concept Simple: Chercher et Transformer

```csharp
// 1. CHERCHER
var code = @"
public class UserService
{
    public void GetUser(int id) { }
    public void GetData(string key) { }
}
";

var context = CodeContext.FromCode(code);
var methods = context.FindMethods()
    .WithNameContaining("Get")
    .Execute()
    .ToList();

Console.WriteLine($"Trouvé {methods.Count} méthodes");
// Output: Trouvé 2 méthodes
```

### Transformer Quoi?

```csharp
// 2. TRANSFORMER
var editor = CodeEditor.FromCode(code);
var result = editor
    .RenameMethod("GetUser", "FetchUser")
    .Apply();

Console.WriteLine(result.ModifiedCode);
// Le code contient maintenant "FetchUser"
```

### Combiner: Chercher + Transformer

```csharp
// 3. CHERCHER ET TRANSFORMER ENSEMBLE
var context = CodeContext.FromCode(code);

// Étape 1: Chercher les méthodes
var methods = context.FindMethods()
    .WithNameContaining("Get")
    .Execute()
    .ToList();

// Étape 2: Préparer le refactoring
var editor = CodeEditor.FromCode(code);

// Étape 3: Transformer chaque méthode trouvée
foreach (var method in methods)
{
    var newName = "Fetch" + method.Identifier.Text.Substring(3);
    editor.RenameMethod(method.Identifier.Text, newName);
}

// Étape 4: Appliquer et vérifier
var result = editor.Apply();
Console.WriteLine(result.Success ? "? Succès" : "? Erreur");
```

---

## Niveau 2: Recherche Approfondie {#niveau-2}

### Concept Clé: Fluent Selection

La "Fluent Selection" signifie **chaîner des filtres** pour affiner la recherche:

```
.FindMethods()      ? Commencer par chercher des méthodes
  .IsPublic()       ? Ajouter un filtre: doit être public
  .IsAsync()        ? Ajouter un filtre: doit être async
  .WithName("Get*") ? Ajouter un filtre: le nom doit contenir "Get"
  .Execute()        ? Exécuter la requête
```

### Les Différents Types de Recherche

#### 1. Chercher des Méthodes

```csharp
var context = CodeContext.FromCode(code);

// Par nom exact
var methods1 = context.FindMethods()
    .WithName("Execute")
    .Execute()
    .ToList();

// Par nom partiel
var methods2 = context.FindMethods()
    .WithNameContaining("Get")
    .Execute()
    .ToList();

// Toutes les méthodes publiques
var methods3 = context.FindMethods()
    .IsPublic()
    .Execute()
    .ToList();

// Toutes les méthodes asynchrones
var methods4 = context.FindMethods()
    .IsAsync()
    .Execute()
    .ToList();

// Méthodes retournant un type spécifique
var methods5 = context.FindMethods()
    .ReturningType("Task")
    .Execute()
    .ToList();
```

#### 2. Chercher des Classes

```csharp
// Par nom exact
var classes1 = context.FindClasses()
    .WithName("UserService")
    .Execute()
    .ToList();

// Par namespace
var classes2 = context.FindClasses()
    .InNamespace("MyApp.Services")
    .Execute()
    .ToList();

// Toutes les classes abstraites
var classes3 = context.FindClasses()
    .IsAbstract()
    .Execute()
    .ToList();

// Toutes les classes avec un attribut
var classes4 = context.FindClasses()
    .WithAttribute("Serializable")
    .Execute()
    .ToList();
```

#### 3. Chercher des Variables

```csharp
// Par nom
var vars1 = context.FindVariables()
    .WithName("userId")
    .Execute()
    .ToList();

// Par type
var vars2 = context.FindVariables()
    .WithType("string")
    .Execute()
    .ToList();

// Propriétés publiques
var vars3 = context.FindVariables()
    .IsPublic()
    .Execute()
    .ToList();

// Propriétés read-only
var vars4 = context.FindVariables()
    .IsReadOnly()
    .Execute()
    .ToList();
```

#### 4. Chercher des Return Statements

```csharp
// Tous les returns d'une méthode
var returns1 = context.FindReturns()
    .InMethod("Process")
    .Execute()
    .ToList();

// Les returns qui retournent null
var returns2 = context.FindReturns()
    .ReturningNull()
    .Execute()
    .ToList();

// Les returns d'un type spécifique
var returns3 = context.FindReturns()
    .ReturningType("User")
    .Execute()
    .ToList();
```

### Chaîner les Filtres (Fluent)

```csharp
// Combiner plusieurs filtres
var methods = context.FindMethods()
    .IsPublic()          // Filtre 1: doit être public
    .IsAsync()           // Filtre 2: doit être async
    .ReturningTask()     // Filtre 3: doit retourner Task
    .WithNameContaining("User")  // Filtre 4: nom contient "User"
    .Execute()
    .ToList();

// Le résultat: toutes les méthodes publiques, asynchrones,
// retournant Task et dont le nom contient "User"
```

### Utiliser les Résultats

```csharp
var methods = context.FindMethods()
    .IsPublic()
    .Execute()
    .ToList();

// Les résultats sont des syntaxes Roslyn
foreach (var method in methods)
{
    // Accéder aux propriétés
    Console.WriteLine($"Nom: {method.Identifier.Text}");
    Console.WriteLine($"Type retour: {method.ReturnType}");
    Console.WriteLine($"Modifiers: {string.Join(",", method.Modifiers)}");
    Console.WriteLine($"Paramètres: {method.ParameterList.Parameters.Count}");
}
```

---

## Niveau 3: Transformation Approfondie {#niveau-3}

### Concept Clé: Fluent Transformation

La "Fluent Transformation" signifie **chaîner des opérations de modification**:

```
CodeEditor.FromCode(code)  ? Créer un éditeur
  .RenameMethod(...)       ? Ajouter une opération: renommer
  .WrapWithTryCatch(...)   ? Ajouter une opération: wrapper
  .Replace(...)            ? Ajouter une opération: remplacer
  .Apply()                 ? Exécuter toutes les opérations
```

### Les Différents Types de Transformation

#### 1. Renommer des Méthodes

```csharp
var code = @"
public class Service
{
    public void GetUser() { }
}
";

var editor = CodeEditor.FromCode(code);

// Renommer une méthode
var result = editor
    .RenameMethod("GetUser", "FetchUser")
    .Apply();

Console.WriteLine(result.ModifiedCode);
// Le code contient maintenant "FetchUser"
```

#### 2. Renommer des Classes

```csharp
var code = @"
public class User
{
    public User() { }
}
";

var editor = CodeEditor.FromCode(code);

// Renommer une classe (renomme aussi les instantiations)
var result = editor
    .RenameClass("User", "Person")
    .Apply();

// Résultat: "class Person" et "new Person()"
```

#### 3. Renommer des Variables

```csharp
var code = @"
public void Execute()
{
    int temp = 5;
    Console.WriteLine(temp);
}
";

var editor = CodeEditor.FromCode(code);

// Renommer une variable
var result = editor
    .RenameVariable("temp", "count")
    .Apply();

// Résultat: "int count = 5;" et "Console.WriteLine(count);"
```

#### 4. Renommer des Propriétés

```csharp
var code = @"
public class Product
{
    public string Name { get; set; }
}

public class Shop
{
    var p = new Product();
    p.Name = "Test";
}
";

var editor = CodeEditor.FromCode(code);

// Renommer une propriété
var result = editor
    .RenameProperty("Name", "Title")
    .Apply();

// Résultat: "public string Title" et "p.Title = "Test""
```

#### 5. Ajouter un Try-Catch

```csharp
var code = @"
public void ProcessData(string data)
{
    int value = int.Parse(data);
}
";

var editor = CodeEditor.FromCode(code);

// Wrapper avec try-catch
var result = editor
    .WrapWithTryCatch("ProcessData", "Console.WriteLine(\"Erreur\");")
    .Apply();

// Résultat: la méthode est enveloppée dans try-catch
```

#### 6. Ajouter du Logging

```csharp
var code = @"
public void Execute()
{
    DoWork();
}
";

var editor = CodeEditor.FromCode(code);

// Wrapper avec logging
var result = editor
    .WrapWithLogging("Execute", "Console.WriteLine(\"Starting...\");")
    .Apply();

// Résultat: "Console.WriteLine(...)" est ajouté au début
```

#### 7. Ajouter de la Validation

```csharp
var code = @"
public void Process(string name)
{
    Console.WriteLine(name);
}
";

var editor = CodeEditor.FromCode(code);

// Wrapper avec validation
var result = editor
    .WrapWithValidation("Process", 
        "if (string.IsNullOrEmpty(name)) throw new ArgumentException();")
    .Apply();

// Résultat: validation ajoutée au début de la méthode
```

#### 8. Remplacer du Code

```csharp
var code = @"
public int Convert(string value)
{
    return int.Parse(value);
}
";

var editor = CodeEditor.FromCode(code);

// Remplacer un snippet
var result = editor
    .Replace("int.Parse(value)", "Convert.ToInt32(value)")
    .Apply();

// Résultat: int.Parse ? Convert.ToInt32
```

### Chaîner les Transformations

```csharp
var editor = CodeEditor.FromCode(code);

var result = editor
    .RenameMethod("GetData", "FetchData")      // 1ère opération
    .RenameVariable("temp", "cache")           // 2ème opération
    .Replace("null", "new Empty()")            // 3ème opération
    .Apply();                                   // Exécuter tout

// Toutes les opérations sont appliquées dans l'ordre
```

### Vérifier le Résultat

```csharp
var result = editor.Apply();

if (result.Success)
{
    Console.WriteLine("? Succès");
    Console.WriteLine(result.ModifiedCode);
    
    // Afficher les changements
    foreach (var change in result.Changes)
    {
        Console.WriteLine($"• {change}");
    }
}
else
{
    Console.WriteLine($"? Erreur: {result.ErrorMessage}");
}
```

---

## Niveau 4: Cas Réels et Avancés {#niveau-4}

### Cas 1: Refactoring Legacy API

**Problème**: Vous avez du code legacy avec des noms de méthode mal choisis (Get* partout).

```csharp
var code = @"
public class UserRepository
{
    public User GetById(int id) { return null; }
    public User GetByEmail(string email) { return null; }
    public List<User> GetAll() { return null; }
}
";

// ÉTAPE 1: Chercher toutes les méthodes Get*
var context = CodeContext.FromCode(code);
var methods = context.FindMethods()
    .WithNameContaining("Get")
    .IsPublic()
    .Execute()
    .ToList();

// ÉTAPE 2: Préparer le refactoring
var editor = CodeEditor.FromCode(code);

// ÉTAPE 3: Renommer chaque méthode
foreach (var method in methods)
{
    var oldName = method.Identifier.Text;  // "GetById", "GetByEmail", etc.
    var newName = "Fetch" + oldName.Substring(3);  // "FetchById", "FetchByEmail", etc.
    editor.RenameMethod(oldName, newName);
}

// ÉTAPE 4: Appliquer
var result = editor.Apply();
Console.WriteLine(result.ModifiedCode);

// Résultat:
// GetById ? FetchById
// GetByEmail ? FetchByEmail
// GetAll ? FetchAll
```

### Cas 2: Améliorer la Gestion des Erreurs

**Problème**: Les méthodes de data access n'ont pas de gestion d'erreurs.

```csharp
var code = @"
public class Database
{
    public User QueryUser(int id) { return null; }
    public List<User> QueryAll() { return null; }
    public void Save(User user) { }
}
";

// ÉTAPE 1: Chercher les méthodes de requête
var context = CodeContext.FromCode(code);
var queryMethods = context.FindMethods()
    .WithNameContaining("Query")
    .IsPublic()
    .Execute()
    .ToList();

// ÉTAPE 2: Ajouter try-catch à chacune
var editor = CodeEditor.FromCode(code);
foreach (var method in queryMethods)
{
    editor.WrapWithTryCatch(
        method.Identifier.Text,
        "Console.WriteLine(\"Database error: \" + ex.Message); return null;"
    );
}

// ÉTAPE 3: Appliquer
var result = editor.Apply();
```

### Cas 3: Migrer un Pattern de Code

**Problème**: Remplacer int.Parse par int.TryParse partout.

```csharp
var code = @"
public class Parser
{
    public int Parse1(string value) { return int.Parse(value); }
    public int Parse2(string value) { return int.Parse(value); }
    public int Parse3(string value) { return int.Parse(value); }
}
";

var editor = CodeEditor.FromCode(code);

// Remplacer chaque occurrence
var result = editor
    .Replace(
        "int.Parse(value)",
        "int.TryParse(value, out int result) ? result : 0"
    )
    .Apply();

Console.WriteLine(result.ModifiedCode);
// Toutes les occurrences de int.Parse sont remplacées
```

### Cas 4: Refactoring Complet Multi-Étapes

**Problème**: Refactoriser complètement une classe.

```csharp
var code = @"
public class UserRep
{
    public User GetUser(int id) { return null; }
    public User GetById(int id) { return null; }
    
    public void SaveUser(User user) { }
    
    public List<User> GetAll() { return null; }
}

public class User
{
    public string UserName { get; set; }
}
";

var context = CodeContext.FromCode(code);

// ÉTAPE 1: Chercher et renommer la classe
var userClass = context.FindClasses()
    .WithName("UserRep")
    .Execute()
    .FirstOrDefault();

// ÉTAPE 2: Chercher les méthodes Get*
var getMethods = context.FindMethods()
    .WithNameContaining("Get")
    .Execute()
    .ToList();

// ÉTAPE 3: Chercher les propriétés mal nommées
var userNameProp = context.FindVariables()
    .WithName("UserName")
    .Execute()
    .FirstOrDefault();

// ÉTAPE 4: Préparer le refactoring
var editor = CodeEditor.FromCode(code);

// ÉTAPE 5: Appliquer tous les changements
var result = editor
    // Renommer la classe
    .RenameClass("UserRep", "UserRepository")
    
    // Renommer les méthodes Get* en Fetch*
    .RenameMethod("GetUser", "FetchUser")
    .RenameMethod("GetById", "FetchById")
    .RenameMethod("GetAll", "FetchAll")
    
    // Renommer Save*
    .RenameMethod("SaveUser", "PersistUser")
    
    // Renommer les propriétés
    .RenameProperty("UserName", "FullName")
    
    .Apply();

Console.WriteLine(result.ModifiedCode);
```

### Cas 5: Combiner avec Logging pour Déboguer

```csharp
var code = @"public class Service { public void Execute() { } }";

// Créer un logger
var logger = new ConsoleLogger(isDebug: true);

// ÉTAPE 1: Chercher avec logging
var context = CodeContext.FromCode(code, logger);
var methods = context.FindMethods()
    .IsPublic()
    .Execute()
    .ToList();

// ÉTAPE 2: Transformer avec logs
var editor = CodeEditor.FromCode(code);
foreach (var method in methods)
{
    Console.WriteLine($"Transformation: {method.Identifier.Text}");
    editor.RenameMethod(method.Identifier.Text, "Handle" + method.Identifier.Text);
}

var result = editor.Apply();
Console.WriteLine($"Résultat: {result.Success}");
```

---

## ?? Bonnes Pratiques

### 1. Toujours Vérifier le Résultat

```csharp
var result = editor.Apply();

if (!result.Success)
{
    Console.WriteLine($"Erreur: {result.ErrorMessage}");
    return;
}
```

### 2. Utiliser Reset/Clear pour Tester

```csharp
var editor = CodeEditor.FromCode(code);

// Ajouter des opérations
editor.RenameMethod("A", "B");

// Tester d'abord
var result = editor.Apply();
if (result.Success)
{
    // Garder les changements
}
else
{
    // Annuler
    editor.Clear();
}
```

### 3. Chaîner les Opérations pour la Lisibilité

```csharp
// ? Mauvais
var editor = CodeEditor.FromCode(code);
editor.RenameMethod("A", "B");
editor.RenameClass("X", "Y");
editor.Replace("old", "new");
var result = editor.Apply();

// ? Bon
var result = CodeEditor.FromCode(code)
    .RenameMethod("A", "B")
    .RenameClass("X", "Y")
    .Replace("old", "new")
    .Apply();
```

### 4. Valider Avant de Transformer

```csharp
var context = CodeContext.FromCode(code);

// Vérifier que la méthode existe
var method = context.FindMethods()
    .WithName("MyMethod")
    .Execute()
    .FirstOrDefault();

if (method == null)
{
    Console.WriteLine("Méthode non trouvée");
    return;
}

// Maintenant transformer en toute sécurité
var editor = CodeEditor.FromCode(code);
var result = editor
    .RenameMethod("MyMethod", "NewMethod")
    .Apply();
```

---

## ?? Feuille de Triche Rapide

### Recherche (Core)

```csharp
// Méthodes
context.FindMethods().WithName("X").IsPublic().IsAsync().Execute()

// Classes
context.FindClasses().WithName("X").InNamespace("Y").IsAbstract().Execute()

// Variables
context.FindVariables().WithName("X").WithType("string").IsPublic().Execute()

// Returns
context.FindReturns().InMethod("X").ReturningNull().Execute()
```

### Transformation (Editor)

```csharp
editor.RenameMethod("old", "new")
editor.RenameClass("old", "new")
editor.RenameVariable("old", "new")
editor.RenameProperty("old", "new")
editor.WrapWithTryCatch("method", "handler")
editor.WrapWithLogging("method", "log")
editor.WrapWithValidation("method", "validation")
editor.Replace("old", "new")
editor.Apply()
```

---

## ?? Résumé

| Concept | Signification | Exemple |
|---------|--------------|---------|
| **Fluent Selection** | Chaîner des filtres pour chercher | `.WithName().IsPublic().IsAsync().Execute()` |
| **Fluent Transformation** | Chaîner des opérations pour transformer | `.RenameMethod().WrapWithTryCatch().Apply()` |
| **Core** | Pour chercher du code | `CodeContext.FromCode(code).FindMethods()` |
| **Editor** | Pour transformer du code | `CodeEditor.FromCode(code).RenameMethod()` |
| **Intégration** | Chercher + Transformer ensemble | Trouvez les éléments, puis transformez-les |

---

## ?? Prochaines Étapes

1. **Testez les exemples** du tutoriel dans votre code
2. **Explorez** les tests pour voir plus d'exemples: `CodeEditorTransformationTests.cs`
3. **Créez** votre propre refactoring sur un vrai projet
4. **Documentez** vos patterns découverts

---

**Besoin d'aide?**
- Documentation technique: `NOUVELLES_FONCTIONNALITES_V2.md`
- Exemples avancés: `EXAMPLES_CodeSearcher_Usage.md`
- Tests comme référence: `CodeEditorTransformationTests.cs`

---

**Version**: 2.0  
**Status**: ? Production-Ready  
**Recommandation**: Commencez par le Niveau 1, progressez à votre rythme ??
