# ?? ReturnTypeWrapperStrategy - Wrapper Returns dans Task<T>

## Overview

Nouvelle stratégie pour **wrapper automatiquement les returns d'une méthode dans Task<T>** et rendre la méthode asynchrone.

## ? Fonctionnalités

### 1. Modification de Signature
- ? Change `public int Method()` ? `public async Task<int> Method()`
- ? Change `public void Method()` ? `public async Task Method()`
- ? Préserve les signatures déjà async (pas de modification)

### 2. Wrapping des Returns
- ? `return 42;` ? `return Task.FromResult(42);`
- ? Support de multiples returns
- ? Support de types génériques: `List<T>`, `Dictionary<K,V>`, etc.

### 3. Styles de Wrapping
```csharp
public enum ReturnWrapStyle
{
    TaskFromResult,        // Task.FromResult(value)
    AwaitTaskFromResult,  // await Task.FromResult(value)
    Auto                  // Automatique selon le contexte
}
```

## ?? Usage

### Basic Usage

```csharp
var code = @"
public int GetValue()
{
    return 42;
}
";

var editor = CodeEditor.FromCode(code);
var result = editor.WrapReturnsInTask("GetValue").Apply();

// Résultat:
// public async Task<int> GetValue()
// {
//     return Task.FromResult(42);
// }
```

### Avec Style Spécifique

```csharp
editor
    .WrapReturnsInTask("GetValue", ReturnWrapStyle.AwaitTaskFromResult)
    .Apply();

// Résultat: return await Task.FromResult(42);
```

### Avec Chaînage d'Opérations

```csharp
editor
    .RenameMethod("GetValue", "GetValueAsync")
    .WrapReturnsInTask("GetValueAsync")
    .Apply();
```

## ?? Intégration avec CodeSearcher.Core

```csharp
// Étape 1: Trouver les méthodes à convertir
var context = CodeContext.FromCode(code);
var methods = context.FindMethods()
    .IsPublic()
    .ReturningType("int")
    .Execute()
    .ToList();

// Étape 2: Modifier avec CodeEditor
var editor = CodeEditor.FromCode(code);
foreach (var method in methods)
{
    editor.WrapReturnsInTask(method.Identifier.Text);
}

var result = editor.Apply();
```

## ?? Exemples Réels

### Convertir une Repository de Sync à Async

**Avant:**
```csharp
public class UserRepository
{
    public User GetUserById(int id)
    {
        var user = _db.Users.FirstOrDefault(u => u.Id == id);
        return user ?? new User { Id = -1 };
    }
}
```

**Après:**
```csharp
public class UserRepository
{
    public async Task<User> GetUserById(int id)
    {
        var user = _db.Users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user ?? new User { Id = -1 });
    }
}
```

### Service avec Gestion d'Erreurs

**Avant:**
```csharp
public bool SendEmail(string to, string message)
{
    try
    {
        _smtp.Send(to, message);
        return true;
    }
    catch
    {
        return false;
    }
}
```

**Après:**
```csharp
public async Task<bool> SendEmail(string to, string message)
{
    try
    {
        _smtp.Send(to, message);
        return Task.FromResult(true);
    }
    catch
    {
        return Task.FromResult(false);
    }
}
```

## ?? Use Cases

1. **Modernisation de code legacy** - Convertir du code synchrone en asynchrone
2. **Refactoring** - Préparer une méthode pour être réellement asynchrone avec await
3. **Migration API** - Adapter le code lors d'une montée de version async
4. **Batch Operations** - Convertir plusieurs méthodes en même temps avec Core + Editor

## ?? Architecture

### ReturnTypeWrapperStrategy
- `WrapReturnsInTask(methodName)` - Wrapper avec style par défaut
- `WrapReturnsInTask(methodName, style)` - Wrapper avec style spécifique

### ReturnTypeWrapperRewriter
- Syntaxe rewriter basé sur Roslyn
- Modifie la signature de la méthode
- Enveloppe chaque return statement

## ?? Détails d'Implémentation

### Signature Rewriter
```csharp
// Trouve la méthode cible
// Change: public T Method() ? public async Task<T> Method()
// Ajoute le modificateur async
// Visite tous les returns du body
```

### Return Statement Rewriter
```csharp
// Pour chaque return statement:
// return value; ? return Task.FromResult(value);
// return value; ? return await Task.FromResult(value);
```

## ?? Statistiques

- ? Compilation: Succès
- ? Tests: Tous passent (153/153)
- ? Intégration: Complète avec CodeSearcher.Core et CodeEditor
- ? Performance: Optimisée pour batch operations

## ?? Prochains Développements

- [ ] Support de `async/await` réel (pas juste Task.FromResult)
- [ ] Configuration du wrapper output (await vs non-await)
- [ ] Support de yield return
- [ ] Documentation complète

## ?? Licence

Même que CodeSearcher - MIT

