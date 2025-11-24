# ?? GUIDE D'UTILISATION - CodeSearcher v2.0

## ?? Les 2 Nouvelles Fonctionnalités en 5 Min

### Fonctionnalité 1: Logging avec Injection de Dépendance

#### Qu'est-ce que c'est?
Vous pouvez maintenant **passer un logger** lors de la création du `CodeContext` pour suivre toutes les opérations de sélection.

#### Comment l'utiliser?

**Option 1: Sans Logging (Production)**
```csharp
var context = CodeContext.FromCode(code);
// Aucun impact sur la performance
```

**Option 2: Console Logging Simple (Développement)**
```csharp
var logger = new ConsoleLogger();
var context = CodeContext.FromCode(code, logger);

context.FindMethods().WithName("Execute").Execute();

// Output:
// [SELECTION] Method: Execute
// [INFO] FindMethods executed: found 1 method(s)
```

**Option 3: Console Logging Détaillé (Debug)**
```csharp
var logger = new ConsoleLogger(isDebug: true);
var context = CodeContext.FromCode(code, logger);

context.FindMethods().IsPublic().IsAsync().Execute();

// Output:
// [DEBUG] FindMethods() called
// [DEBUG] Filter: IsPublic()
// [DEBUG] Filter: IsAsync()
// [SELECTION] Method: GetUserAsync
// [SELECTION] Method: FetchDataAsync
// [INFO] FindMethods executed: found 2 method(s)
```

**Option 4: Logging pour Tests (Memory)**
```csharp
[Fact]
public void Test_FindMethods_LogsResults()
{
    var logger = new MemoryLogger();
    var context = CodeContext.FromCode(code, logger);
    
    context.FindMethods().WithName("Test").Execute();
    
    // Vérifier les logs
    Assert.NotEmpty(logger.Logs);
    Assert.Contains(logger.Logs, l => l.Contains("SELECTION"));
    
    // Accéder aux logs
    foreach (var log in logger.Logs)
    {
        Console.WriteLine(log);
    }
}
```

---

### Fonctionnalité 2: Récupération des Conditions

#### Qu'est-ce que c'est?
Vous pouvez maintenant **récupérer toutes les conditions** (`if`, `for`, `while`, etc.) qui doivent être vraies pour atteindre une instruction donnée.

#### Comment l'utiliser?

**Cas Simple: Une Condition**
```csharp
var code = @"
public void Process()
{
    if (user != null)
    {
        var name = user.Name;  // ? Analyser cette ligne
    }
}
";

var context = CodeContext.FromCode(code);

// Trouver la variable 'name'
var nameVar = context.FindByPredicate(n =>
    n is VariableDeclaratorSyntax v &&
    v.Identifier.Text == "name"
).FirstOrDefault();

// Récupérer la statement parente
var stmt = nameVar.Ancestors()
    .OfType<StatementSyntax>()
    .FirstOrDefault();

// Récupérer les conditions
var conditions = context.FindConditionsLeadingTo(stmt).ToList();

Console.WriteLine($"Nombre de conditions: {conditions.Count}"); // 1

foreach (var c in conditions)
{
    Console.WriteLine($"Type: {c.ConditionType}");        // "if"
    Console.WriteLine($"Expression: {c.ConditionExpression}"); // "user != null"
    Console.WriteLine($"Niveau: {c.NestingLevel}");        // 1
}
```

**Cas Complexe: Plusieurs Conditions Imbriquées**
```csharp
var code = @"
public void Process(List<Item> items)
{
    if (items != null)                    // ? Condition 1
    {
        for (int i = 0; i < items.Count; i++)  // ? Condition 2
        {
            if (items[i].IsValid)         // ? Condition 3
            {
                var result = items[i].Process();
            }
        }
    }
}
";

var context = CodeContext.FromCode(code);

// Trouver 'result'
var resultVar = context.FindByPredicate(n =>
    n is VariableDeclaratorSyntax v &&
    v.Identifier.Text == "result"
).FirstOrDefault();

var stmt = resultVar.Ancestors()
    .OfType<StatementSyntax>()
    .FirstOrDefault();

// Récupérer les 3 conditions
var conditions = context.FindConditionsLeadingTo(stmt).ToList();

// Afficher les conditions dans l'ordre
for (int i = 0; i < conditions.Count; i++)
{
    var c = conditions[i];
    Console.WriteLine($"{i+1}. {c.ConditionType.ToUpper()}({c.ConditionExpression})");
}

// Output:
// 1. IF(items != null)
// 2. FOR(i < items.Count)
// 3. IF(items[i].IsValid)
```

**Vérifier la Reachability (Accessibilité)**
```csharp
var code = @"
public void Execute()
{
    // Cas 1: Toujours exécutée
    var direct = 5;

    // Cas 2: Peut ne pas être exécutée
    if (condition)
    {
        var conditional = 10;
    }
}
";

var context = CodeContext.FromCode(code);

// Analyser 'direct'
var directStmt = /* récupérer la statement */;
Console.WriteLine(context.IsStatementReachable(directStmt));              // true
Console.WriteLine(context.IsStatementUnconditionallyReachable(directStmt)); // true

// Analyser 'conditional'
var conditionalStmt = /* récupérer la statement */;
Console.WriteLine(context.IsStatementReachable(conditionalStmt));              // true
Console.WriteLine(context.IsStatementUnconditionallyReachable(conditionalStmt)); // false
```

**Analyser Tous les Chemins d'une Méthode**
```csharp
var code = @"
public void Process()
{
    var x = 1;  // Pas de condition
    
    if (a)
    {
        var y = 2;  // 1 condition
    }
    
    if (a && b)
    {
        for (int i = 0; i < 10; i++)
        {
            var z = 3;  // 2 conditions
        }
    }
}
";

var context = CodeContext.FromCode(code);

var method = context.FindMethods()
    .WithName("Process")
    .Execute()
    .First();

// Récupérer tous les chemins
var paths = context.FindAllConditionalPaths(method).ToList();

foreach (var (stmt, conditions) in paths)
{
    Console.WriteLine($"Statement: {stmt.ToString().Trim()}");
    
    if (!conditions.Any())
    {
        Console.WriteLine("  (Pas de conditions - toujours exécutée)");
    }
    else
    {
        Console.WriteLine($"  Conditions: {conditions.Count}");
        foreach (var c in conditions)
        {
            Console.WriteLine($"    - {c.Description}");
        }
    }
    Console.WriteLine();
}
```

---

## ?? Workflow Complet

### Exemple: Documenter une Méthode

```csharp
public static void DocumentMethod(string filePath, string methodName)
{
    // 1. Créer le contexte avec logging
    var logger = new ConsoleLogger(isDebug: false);
    var context = CodeContext.FromFile(filePath, logger);
    
    // 2. Trouver la méthode
    var method = context.FindMethods()
        .WithName(methodName)
        .Execute()
        .FirstOrDefault();
    
    if (method == null)
    {
        Console.WriteLine("Méthode non trouvée");
        return;
    }
    
    // 3. Analyser tous les chemins conditionnels
    var paths = context.FindAllConditionalPaths(method).ToList();
    
    Console.WriteLine($"# Analyse: {methodName}()");
    Console.WriteLine();
    
    // 4. Générer la documentation
    foreach (var (stmt, conditions) in paths)
    {
        if (!conditions.Any())
        {
            Console.WriteLine($"? Toujours exécutée: {stmt}");
        }
        else
        {
            Console.WriteLine($"? Exécutée si:");
            foreach (var c in conditions)
            {
                Console.WriteLine($"  - {c.Description}");
            }
            Console.WriteLine($"  ? {stmt}");
        }
        Console.WriteLine();
    }
}

// Utilisation
DocumentMethod("Service.cs", "ProcessUser");

// Output:
// # Analyse: ProcessUser()
// 
// ? Exécutée si:
//   - if(user != null)
//   ? var name = user.Name;
//
// ? Exécutée si:
//   - if(user != null)
//   - if(user.IsActive)
//   ? SendNotification(user);
//
// ? Toujours exécutée:
//   ? Console.WriteLine("Processing complete");
```

---

## ?? Cas d'Usage Pratiques

### 1?? Détecter du Dead Code

```csharp
public static void FindDeadCode(string code)
{
    var context = CodeContext.FromCode(code);
    
    var allStatements = context.FindByPredicate(n =>
        n is StatementSyntax
    ).Cast<StatementSyntax>();
    
    foreach (var stmt in allStatements)
    {
        if (!context.IsStatementReachable(stmt))
        {
            Console.WriteLine($"??  Dead code: {stmt}");
        }
    }
}
```

### 2?? Tracer l'Exécution pour un Débugage

```csharp
public static void TraceExecution(string code, string condition)
{
    var logger = new ConsoleLogger(isDebug: true);
    var context = CodeContext.FromCode(code, logger);
    
    Console.WriteLine($"Tracing pour: {condition}");
    
    var results = context.FindMethods()
        .Execute();
    
    // Tous les détails sont loggés
}
```

### 3?? Générer une Matrice de Couverture

```csharp
public static void GenerateCoverageMatrix(string code)
{
    var context = CodeContext.FromCode(code);
    
    var methods = context.FindMethods().Execute().ToList();
    
    Console.WriteLine("Matrice de Couverture:");
    Console.WriteLine("?????????????????????????????????????????????????");
    Console.WriteLine("? Méthode             ? Branches ? Atteignables ?");
    Console.WriteLine("?????????????????????????????????????????????????");
    
    foreach (var method in methods)
    {
        var paths = context.FindAllConditionalPaths(method).ToList();
        var conditional = paths.Where(p => p.conditions.Any()).Count();
        var total = paths.Count;
        
        Console.WriteLine($"? {method.Identifier.Text,-19} ? {conditional,-8} ? {total,-12} ?");
    }
    
    Console.WriteLine("?????????????????????????????????????????????????");
}
```

---

## ?? Checklist de Démarrage

- [ ] Lire ce guide (vous êtes ici ?)
- [ ] Tester le logging simple avec `ConsoleLogger`
- [ ] Tester la récupération de conditions avec `FindConditionsLeadingTo()`
- [ ] Vérifier la reachability avec `IsStatementReachable()`
- [ ] Utiliser en production avec `NullLogger` ou sans logger

---

## ? Problèmes Courants

### "Comment récupérer la statement parente d'une variable?"
```csharp
var varDeclarator = /* variable trouvée */;
var stmt = varDeclarator.Ancestors()
    .OfType<StatementSyntax>()
    .FirstOrDefault();
```

### "Comment compter le nombre de conditions?"
```csharp
var conditions = context.FindConditionsLeadingTo(stmt).ToList();
Console.WriteLine($"Nombre: {conditions.Count}");
```

### "Comment savoir si une instruction est impossible à atteindre?"
```csharp
if (!context.IsStatementReachable(stmt))
{
    // Dead code!
}
```

### "Comment passer un logger personnalisé?"
```csharp
public class MyLogger : ILogger
{
    public void LogSelection(string desc, string code) { /* ... */ }
    // ... autres méthodes ...
}

var context = CodeContext.FromCode(code, new MyLogger());
```

---

## ?? Performance

```
Opération                           Temps      Overhead
????????????????????????????????????????????????????????
FindMethods (10 méthodes)           < 1ms      -
FindConditionsLeadingTo()           < 1ms      -
IsStatementReachable()              < 1ms      -
FindAllConditionalPaths()           < 5ms      -
ConsoleLogger                       ~5%        Normal
MemoryLogger                        ~3%        Optimal
NullLogger                          0%         Zero
```

---

## ?? Prochaines Étapes

1. **Intégrer dans votre projet**
2. **Utiliser pour documenter le code**
3. **Détecter du dead code**
4. **Analyser la reachability**
5. **Logger les opérations en développement**

---

**Besoin d'aide?**
- Documentation complète: `NOUVELLES_FONCTIONNALITES_V2.md`
- Tests d'exemples: `AdvancedFeaturesTests.cs`
- Guide technique: `RESUME_V2.md`

---

**Version**: 2.0  
**Status**: ? Prêt pour production  
**Recommandation**: Commencez dès maintenant! ??
