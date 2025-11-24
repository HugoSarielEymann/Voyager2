# ?? FEUILLE DE TRICHE - CodeSearcher

## ?? Les 2 Concepts Clés

### 1?? Fluent Selection (Recherche)
Chercher du code avec des filtres chaînés

### 2?? Fluent Transformation (Transformation)
Modifier du code avec des opérations chaînées

---

## ?? RECHERCHE - Fluent Selection

### Setup
```csharp
using CodeSearcher.Core;

var context = CodeContext.FromCode(code);
```

### Chercher des Méthodes
```csharp
context.FindMethods()
    .WithName("Execute")              // Nom exact
    .WithNameContaining("Get")        // Nom partiel
    .IsPublic()                       // Modificateur
    .IsPrivate()                      // Modificateur
    .IsAsync()                        // Async?
    .ReturningType("Task")            // Type retour
    .ReturningTask()                  // Retourne Task
    .HasParameterCount(2)             // Nombre de params
    .WithAttribute("Obsolete")        // Attribut
    .Execute()                        // Exécuter!
    .ToList()
```

### Chercher des Classes
```csharp
context.FindClasses()
    .WithName("UserService")          // Nom exact
    .WithNameContaining("User")       // Nom partiel
    .IsPublic()                       // Modificateur
    .IsAbstract()                     // Modificateur
    .IsSealed()                       // Modificateur
    .InNamespace("MyApp.Services")    // Namespace
    .WithAttribute("Serializable")    // Attribut
    .Inherits("BaseClass")            // Héritage
    .Implements("IService")           // Interface
    .Execute()
    .ToList()
```

### Chercher des Variables/Propriétés
```csharp
context.FindVariables()
    .WithName("userId")               // Nom exact
    .WithNameContaining("user")       // Nom partiel
    .WithType("string")               // Type
    .IsPublic()                       // Modificateur
    .IsPrivate()                      // Modificateur
    .IsReadOnly()                     // Read-only?
    .WithAttribute("Required")        // Attribut
    .WithInitializer()                // A une valeur par défaut?
    .Execute()
    .ToList()
```

### Chercher des Return Statements
```csharp
context.FindReturns()
    .InMethod("Process")              // Dans quelle méthode?
    .ReturningNull()                  // Retourne null?
    .ReturningType("User")            // Type retourné
    .WithExpression(expr =>           // Condition perso
        expr.ToString().Contains("data")
    )
    .Execute()
    .ToList()
```

### Chercher Avec Predicate Personnalisé
```csharp
context.FindByPredicate(node =>
    node is MethodDeclarationSyntax m &&
    m.Identifier.Text.StartsWith("Get") &&
    m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword))
)
.ToList()
```

---

## ?? TRANSFORMATION - Fluent Transformation

### Setup
```csharp
using CodeSearcher.Editor;

var editor = CodeEditor.FromCode(code);
```

### Renommer
```csharp
editor
    .RenameMethod("oldName", "newName")
    .RenameClass("OldClass", "NewClass")
    .RenameVariable("oldVar", "newVar")
    .RenameProperty("OldProp", "NewProp")
    .Apply()
```

### Wrapper (Envelopper)
```csharp
editor
    .WrapWithTryCatch("methodName", "throw;")
    .WrapWithLogging("methodName", "Console.WriteLine(\"Start\");")
    .WrapWithValidation("methodName", "if (x == null) throw;")
    .Apply()
```

### Remplacer
```csharp
editor
    .Replace("int.Parse(", "Convert.ToInt32(")
    .Replace("null", "Empty")
    .Apply()
```

### Exécuter et Vérifier
```csharp
var result = editor.Apply();

if (result.Success)
{
    Console.WriteLine(result.ModifiedCode);
    foreach (var change in result.Changes)
    {
        Console.WriteLine($"? {change}");
    }
}
else
{
    Console.WriteLine($"? {result.ErrorMessage}");
}
```

### Gestion d'État
```csharp
editor.Clear()              // Efface les opérations
editor.Reset()              // Restaure le code original
editor.GetCode()            // Récupère le code actuel
editor.GetChangeLog()       // Voir tous les changements
editor.SaveToFile("out.cs") // Sauvegarder dans un fichier
```

---

## ?? INTÉGRATION - Chercher + Transformer

### Pattern Complet
```csharp
// 1. Créer le contexte
var context = CodeContext.FromCode(code);

// 2. Chercher
var items = context.FindMethods()
    .WithNameContaining("Get")
    .IsPublic()
    .Execute()
    .ToList();

// 3. Créer l'éditeur
var editor = CodeEditor.FromCode(code);

// 4. Transformer chaque item
foreach (var item in items)
{
    var oldName = item.Identifier.Text;
    var newName = "Fetch" + oldName.Substring(3);
    editor.RenameMethod(oldName, newName);
}

// 5. Appliquer
var result = editor.Apply();
```

---

## ?? TABLEAU DE RÉFÉRENCE RAPIDE

| Besoin | Code |
|--------|------|
| Trouver des **méthodes publiques** | `FindMethods().IsPublic().Execute()` |
| Trouver des **Get*** | `FindMethods().WithNameContaining("Get").Execute()` |
| Trouver des **async Task** | `FindMethods().IsAsync().ReturningTask().Execute()` |
| Trouver les **classes abstraites** | `FindClasses().IsAbstract().Execute()` |
| Trouver les **variables string** | `FindVariables().WithType("string").Execute()` |
| Trouver les **null returns** | `FindReturns().ReturningNull().Execute()` |
| **Renommer méthode** | `RenameMethod("old", "new")` |
| **Renommer classe** | `RenameClass("Old", "New")` |
| **Ajouter try-catch** | `WrapWithTryCatch("method", "handler")` |
| **Remplacer code** | `Replace("oldPattern", "newPattern")` |
| **Appliquer tout** | `Apply()` |

---

## ?? PATTERNS COURANTS

### Refactorer Get* ? Fetch*
```csharp
var methods = context.FindMethods()
    .WithNameContaining("Get").Execute();

var editor = CodeEditor.FromCode(code);
foreach (var m in methods)
{
    editor.RenameMethod(m.Identifier.Text, 
        "Fetch" + m.Identifier.Text.Substring(3));
}
editor.Apply();
```

### Ajouter Try-Catch aux Méthodes de Data
```csharp
var dataMethods = context.FindMethods()
    .WithNameContaining("Query").Execute();

var editor = CodeEditor.FromCode(code);
foreach (var m in dataMethods)
{
    editor.WrapWithTryCatch(m.Identifier.Text, "return null;");
}
editor.Apply();
```

### Remplacer un Pattern Partout
```csharp
var editor = CodeEditor.FromCode(code);
editor.Replace("int.Parse(", "Convert.ToInt32(")
      .Replace("DateTime.Now", "DateTime.UtcNow")
      .Apply();
```

### Valider Avant de Transformer
```csharp
var method = context.FindMethods()
    .WithName("Critical").Execute().FirstOrDefault();

if (method != null)
{
    var editor = CodeEditor.FromCode(code);
    editor.RenameMethod("Critical", "ValidatedCritical")
          .WrapWithValidation("ValidatedCritical", "/* check */"

)
          .Apply();
}
```

---

## ?? PIÈGES À ÉVITER

### ? Mauvais: Pas de vérification
```csharp
editor.RenameMethod("method", "newName").Apply();
// Et si "method" n'existe pas?
```

### ? Bon: Vérifier d'abord
```csharp
var method = context.FindMethods()
    .WithName("method").Execute().FirstOrDefault();

if (method != null)
{
    editor.RenameMethod("method", "newName").Apply();
}
```

### ? Mauvais: Pas d'état initial
```csharp
var editor = CodeEditor.FromCode(code);
editor.RenameMethod("A", "B");
// Problème? Pas de possibilité d'annuler
```

### ? Bon: Sauvegarder l'original
```csharp
var originalCode = code;
var editor = CodeEditor.FromCode(code);
var result = editor.RenameMethod("A", "B").Apply();

if (!result.Success)
{
    var editor2 = CodeEditor.FromCode(originalCode);
    // Recommencer
}
```

---

## ?? LOGGING & DEBUG

### Avec Logging Console
```csharp
var logger = new ConsoleLogger(isDebug: true);
var context = CodeContext.FromCode(code, logger);

var methods = context.FindMethods()
    .IsPublic()
    .Execute();
// Tous les filtres sont loggés!
```

### Avec Logging Mémoire (Tests)
```csharp
var logger = new MemoryLogger();
var context = CodeContext.FromCode(code, logger);

context.FindMethods().Execute();

Assert.NotEmpty(logger.Logs);
foreach (var log in logger.Logs)
{
    Console.WriteLine(log);
}
```

---

## ?? SAUVEGARDE & PERSISTANCE

```csharp
var result = editor.Apply();

// Sauvegarder dans un fichier
if (result.Success)
{
    editor.SaveToFile("output.cs");
}

// Ou obtenir le code
var modifiedCode = result.ModifiedCode;
File.WriteAllText("output.cs", modifiedCode);
```

---

## ?? AIDE-MÉMOIRE

| Opération | Méthode |
|-----------|---------|
| **Créer contexte** | `CodeContext.FromCode(code)` |
| **Créer éditeur** | `CodeEditor.FromCode(code)` |
| **Chercher** | `FindMethods()`, `FindClasses()`, etc. |
| **Filtrer** | `.WithName()`, `.IsPublic()`, etc. |
| **Exécuter requête** | `.Execute()` |
| **Transformer** | `.RenameMethod()`, `.Replace()`, etc. |
| **Appliquer changements** | `.Apply()` |
| **Vérifier succès** | `result.Success` |
| **Obtenir résultat** | `result.ModifiedCode` |
| **Voir logs** | `result.Changes` |

---

## ?? RÉFÉRENCES RAPIDES

- **Tutoriel complet**: `TUTORIAL.md`
- **Documentation API**: `NOUVELLES_FONCTIONNALITES_V2.md`
- **Exemples avancés**: `EXAMPLES_CodeSearcher_Usage.md`
- **Tests comme référence**: `CodeEditorTransformationTests.cs`

---

**Besoin de plus de détails?** ? Consultez le **TUTORIAL.md** correspondant

**Version**: 2.0  
**À jour**: Oui ?
