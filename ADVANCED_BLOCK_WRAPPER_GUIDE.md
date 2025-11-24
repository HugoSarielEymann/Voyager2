# ?? ADVANCED BLOCK WRAPPER - Sélection de Bloc et Primitives de Contrôle

## ?? Concept

Système avancé pour:
1. **Sélectionner un bloc de code** entre deux points (types, variables, indices, commentaires)
2. **Envelopper ce bloc** avec n'importe quelle primitive de contrôle (if, foreach, while, for, try-catch, lock, using, etc.)

---

## ?? SÉLECTEURS DE BLOC

### 1. Sélectionner Entre Deux Types de Variable

```csharp
var code = @"
public void GenerateToto()
{
    List<HuData> data = new List<HuData>();
    Console.WriteLine(""code à wrapper"");
    var toto = new object();
}
";

var selector = new CodeBlockSelector(code, "GenerateToto");
var selected = selector.SelectBetweenVariables("List<HuData>", "object");

// Résultat: le block entre la déclaration de List et la déclaration de object
```

### 2. Sélectionner Entre Deux Noms de Variable

```csharp
var selector = new CodeBlockSelector(code, "MyMethod");
var selected = selector.SelectBetweenVariableNames("firstName", "lastName");

// Résultat: tout le code entre 'firstName' et 'lastName'
```

### 3. Sélectionner Entre Deux Indices

```csharp
var selector = new CodeBlockSelector(code, "MyMethod");
var selected = selector.SelectBetweenIndices(2, 5);

// Résultat: les statements aux index 2, 3, 4, 5
```

### 4. Sélectionner les Statements Contenant un Identifiant

```csharp
var selector = new CodeBlockSelector(code, "MyMethod");
var selected = selector.SelectStatementsContainingIdentifier("data");

// Résultat: tous les statements mentionnant 'data'
```

### 5. Sélectionner Entre Deux Commentaires

```csharp
var selector = new CodeBlockSelector(code, "MyMethod");
var selected = selector.SelectBetweenComments("// START", "// END");

// Résultat: le bloc entre les deux commentaires
```

---

## ?? PRIMITIVES DE CONTRÔLE

### If Statement
```csharp
ControlFlowBlockWrapper.Create(code, "Process")
    .SelectBetweenNames("items", "result")
    .WithIf("items != null")
    .ModifiedCode;

// Résultat:
// if (items != null)
// {
//     // code sélectionné
// }
```

### Foreach Loop
```csharp
ControlFlowBlockWrapper.Create(code, "Process")
    .SelectBetweenNames("items", "result")
    .WithForEach("var", "item", "items")
    .ModifiedCode;

// Résultat:
// foreach (var item in items)
// {
//     // code sélectionné
// }
```

### While Loop
```csharp
ControlFlowBlockWrapper.Create(code, "Loop")
    .SelectBetweenNames("i", "done")
    .WithWhile("i < 10")
    .ModifiedCode;

// Résultat:
// while (i < 10)
// {
//     // code sélectionné
// }
```

### For Loop
```csharp
var wrapper = new ControlFlowBlockWrapper(code, "Loop");
var selected = new CodeBlockSelector(code, "Loop").SelectBetweenIndices(1, 3);

wrapper.WrapSelectedBlock(
    selected,
    new PrimitiveWrapperConfig(ControlFlowPrimitive.For)
    {
        VariableDeclaration = "int i = 0; i < 10; i++"
    }
).ModifiedCode;

// Résultat:
// for (int i = 0; i < 10; i++)
// {
//     // code sélectionné
// }
```

### Do-While Loop
```csharp
ControlFlowBlockWrapper.Create(code, "Loop")
    .SelectBetweenNames("start", "end")
    .WithDoWhile("condition")
    .ModifiedCode;

// Résultat:
// do
// {
//     // code sélectionné
// } while (condition)
```

### Try-Catch
```csharp
ControlFlowBlockWrapper.Create(code, "RiskyOp")
    .SelectBetweenNames("items", "result")
    .WithTryCatch("Console.WriteLine(\"Error\")")
    .ModifiedCode;

// Résultat:
// try
// {
//     // code sélectionné
// }
// catch (Exception ex)
// {
//     Console.WriteLine("Error");
// }
```

### Lock (Thread-Safe)
```csharp
ControlFlowBlockWrapper.Create(code, "ThreadSafe")
    .SelectBetweenNames("items", "complete")
    .WithLock("_lockObj")
    .ModifiedCode;

// Résultat:
// lock (_lockObj)
// {
//     // code sélectionné
// }
```

### Using (Resource Management)
```csharp
ControlFlowBlockWrapper.Create(code, "FileOp")
    .SelectBetweenNames("file", "done")
    .WithUsing("var stream = File.OpenRead(\"data.txt\")")
    .ModifiedCode;

// Résultat:
// using (var stream = File.OpenRead("data.txt"))
// {
//     // code sélectionné
// }
```

### Checked (Overflow Detection)
```csharp
ControlFlowBlockWrapper.Create(code, "Calculate")
    .SelectBetweenNames("a", "b")
    .WithChecked()
    .ModifiedCode;

// Résultat:
// checked
// {
//     // code sélectionné
// }
```

### Unchecked (Ignore Overflow)
```csharp
ControlFlowBlockWrapper.Create(code, "Calculate")
    .SelectBetweenNames("a", "b")
    .WithUnchecked()
    .ModifiedCode;

// Résultat:
// unchecked
// {
//     // code sélectionné
// }
```

---

## ?? EXEMPLES RÉELS COMPLETS

### Cas 1: Wrapper un Bloc de Données entre List et Object

```csharp
var code = @"
public class Service
{
    public void GenerateToto()
    {
        List<HuData> data = new List<HuData>();
        Console.WriteLine(""Processing"");
        data.ForEach(x => Process(x));
        var toto = new object();
    }
}
";

// Wrapper le bloc entre 'List<HuData>' et 'object' dans un foreach
var result = ControlFlowBlockWrapper.Create(code, "GenerateToto")
    .SelectBetweenTypes("List<HuData>", "object")
    .WithForEach("HuData", "item", "data")
    .ModifiedCode;

// Résultat: le code entre List et object est enveloppé dans foreach
```

### Cas 2: Rendre Thread-Safe une Section Critique

```csharp
var code = @"
public class ThreadSafeCollection
{
    public void AddItems()
    {
        var items = new List<int>();
        items.Add(1);
        items.Add(2);
        items.Add(3);
        var complete = true;
    }
}
";

// Rendre thread-safe
var result = ControlFlowBlockWrapper.Create(code, "AddItems")
    .SelectBetweenNames("items", "complete")
    .WithLock("_lockObj")
    .ModifiedCode;

// Résultat: tous les Add sont dans un lock
```

### Cas 3: Ajouter Error Handling à du Code Risqué

```csharp
var code = @"
public class Calculator
{
    public void Calculate()
    {
        var items = new int[5];
        items[0] = 1;
        items[1] = 2;
        items[100] = 3;  // IndexOutOfRangeException!
        var done = true;
    }
}
";

// Ajouter try-catch
var result = ControlFlowBlockWrapper.Create(code, "Calculate")
    .SelectBetweenTypes("int[]", "bool")
    .WithTryCatch("Console.WriteLine(\"Array access error\")")
    .ModifiedCode;

// Résultat: les accès au array sont protégés par try-catch
```

### Cas 4: Processer une Collection Conditionnellement

```csharp
var code = @"
public class DataProcessor
{
    public void ProcessOrders(List<Order> orders)
    {
        var orderList = orders;
        var count = orderList.Count();
        var names = string.Join("", "", orderList.Select(o => o.Name));
        var total = orderList.Sum(o => o.Amount);
        string status = ""completed"";
    }
}
";

// Wrapper dans un if
var result = ControlFlowBlockWrapper.Create(code, "ProcessOrders")
    .SelectBetweenNames("orderList", "status")
    .WithIf("orderList != null && orderList.Count > 0")
    .ModifiedCode;

// Résultat: le traitement est conditionnel
```

---

## ??? STRUCTURE DES CLASSES

### CodeBlockSelector
Sélectionne des blocs selon différents critères:
- `SelectBetweenVariables(type1, type2)` - Entre deux types
- `SelectBetweenVariableNames(name1, name2)` - Entre deux noms
- `SelectBetweenIndices(start, end)` - Entre indices
- `SelectStatementsContainingIdentifier(id)` - Contenant un identifiant
- `SelectBetweenComments(c1, c2)` - Entre commentaires

### ControlFlowBlockWrapper
Wrapper fluent pour sélectionner et envelopper:
- `.SelectBetweenTypes(...).WithIf(...)`
- `.SelectBetweenNames(...).WithForEach(...)`
- `.SelectBetweenIndices(...).WithLock(...)`

### PrimitiveWrapperConfig
Configuration pour une primitive:
- `Primitive` - Le type (If, Foreach, While, etc.)
- `Expression` - Condition ou expression
- `VariableDeclaration` - Déclaration (foreach, for, using)
- `InitializationCode` - Code d'init (try-catch)
- `FinalizationCode` - Code de finalisation

---

## ?? TESTS DISPONIBLES

15+ tests couvrant:
- ? Sélection entre types
- ? Sélection entre noms
- ? Sélection entre indices
- ? Sélection par identifiant
- ? Wrapper If
- ? Wrapper ForEach
- ? Wrapper While
- ? Wrapper For
- ? Wrapper Try-Catch
- ? Wrapper Lock
- ? Wrapper Using
- ? Wrapper Checked/Unchecked
- ? Cas réels complets

---

## ? AVANTAGES

? **Sélection Flexible** - 5 façons de sélectionner un bloc  
? **Primitives Complètes** - Tous les types de boucle/contrôle  
? **API Fluente** - Chaînage natural
? **Type-Safe** - Enum pour les primitives  
? **Réutilisable** - PrimitiveWrapperConfig peut être customisée  
? **Production-Ready** - Tests complets  

---

## ?? UTILISATION RAPIDE

```csharp
// 1. Sélectionner un bloc
var code = /* votre code */;
var wrapper = ControlFlowBlockWrapper.Create(code, "MyMethod");

// 2. Choisir comment sélectionner
var selected = wrapper
    .SelectBetweenTypes("List<T>", "string")    // OU
    .SelectBetweenNames("start", "end")         // OU
    .SelectBetweenIndices(2, 5);                // OU
    // ...

// 3. Choisir comment wrapper
var result = wrapper
    .WithIf("condition")                        // OU
    .WithForEach("var", "item", "collection")  // OU
    .WithLock("lockObj")                        // OU
    // ...

// 4. Récupérer le code transformé
var transformedCode = result.ModifiedCode;
```

---

**Un système puissant pour refactoriser des blocs de code complexes!** ??
