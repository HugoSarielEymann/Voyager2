# ?? GUIDE D'UTILISATION - Property Mapping System

## ?? Concept Rapide

Le **Property Mapping System** permet de **mapper automatiquement les propriétés** lors de migrations Legacy.

```csharp
// Définir un mapping
var engine = new PropertyMappingEngine();
engine.AddMapping("ConsigneeName", "Consignee.Name");

// Appliquer à du code
var result = engine.TransformCode(legacyCode);
// Résultat: lblData.ConsigneeName ? lblData.Consignee.Name
```

---

## ?? TABLE DES MATIÈRES

1. [Installation et Setup](#installation)
2. [Utilisation Basique](#basique)
3. [Mappings Imbriqués](#nested)
4. [Mappings Multiples](#multiple)
5. [Contexte et Désambiguation](#contexte)
6. [Validation](#validation)
7. [Cas Réels](#casreels)

---

## Installation et Setup {#installation}

### Import
```csharp
using CodeSearcher.Editor.Mapping;
```

### Créer une Instance
```csharp
// Simple
var engine = new PropertyMappingEngine();

// Avec configuration
var engine = new PropertyMappingEngine(
    caseSensitive: true,      // Respecter la casse
    wholeWordOnly: true       // Ne pas remplacer partiel
);
```

---

## Utilisation Basique {#basique}

### Exemple 1: Mapping Simple 1-to-1
```csharp
var code = @"
public void Setup(User user)
{
    var age = user.Age;
    var name = user.Name;
}
";

var engine = new PropertyMappingEngine();
engine.AddMapping("Age", "AgeInYears");
engine.AddMapping("Name", "FullName");

var result = engine.TransformCode(code);

// Résultat:
// var age = user.AgeInYears;
// var name = user.FullName;
```

### Exemple 2: Remplacer Partout
```csharp
var code = @"
public class Service
{
    public void Process(Order order)
    {
        var status = order.Status;
        if (order.Status == ""Active"")
        {
            log.Info(order.Status);
        }
    }
}
";

var engine = new PropertyMappingEngine();
engine.AddMapping("Status", "OrderStatus.Status");

var result = engine.TransformCode(code);
// Remplace TOUTES les occurrences de 'Status'
```

---

## Mappings Imbriqués {#nested}

### Concept: Propriétés Imbriquées
```csharp
// AVANT: Propriété plate
customer.ConsigneeName

// APRÈS: Propriété imbriquée
customer.Consignee.Name
```

### Utilisation
```csharp
var code = @"
var consignee = order.ConsigneeName;
var email = order.ConsigneeEmail;
var phone = order.ConsigneePhone;
";

var engine = new PropertyMappingEngine();

// Les mappings imbriqués sont détectés automatiquement
engine.AddMapping("ConsigneeName", "Consignee.Name");
engine.AddMapping("ConsigneeEmail", "Consignee.Email");
engine.AddMapping("ConsigneePhone", "Consignee.Phone");

var result = engine.TransformCode(code);

// Résultat:
// var consignee = order.Consignee.Name;
// var email = order.Consignee.Email;
// var phone = order.Consignee.Phone;
```

### Profondeur Quelconque
```csharp
// Mapping très imbriqué
engine.AddMapping("AddressCity", "Addresses.Primary.Location.City");

// Transformation:
// customer.AddressCity ? customer.Addresses.Primary.Location.City
```

---

## Mappings Multiples {#multiple}

### Ajouter Plusieurs à la Fois
```csharp
var engine = new PropertyMappingEngine();

// Méthode 1: Une par une
engine.AddMapping("FirstName", "Name.First");
engine.AddMapping("LastName", "Name.Last");
engine.AddMapping("Email", "Contact.Email");

// Méthode 2: Tous à la fois
engine.AddMappings(
    new PropertyMapping("FirstName", "Name.First"),
    new PropertyMapping("LastName", "Name.Last"),
    new PropertyMapping("Email", "Contact.Email")
);
```

### Appliquer Tous les Mappings
```csharp
var code = @"
public class PersonController
{
    public void UpdatePerson(Person person)
    {
        person.FirstName = GetFirstName();
        person.LastName = GetLastName();
        person.Email = GetEmail();
    }
}
";

var engine = new PropertyMappingEngine();
engine.AddMapping("FirstName", "Name.First");
engine.AddMapping("LastName", "Name.Last");
engine.AddMapping("Email", "Contact.Email");

var result = engine.TransformCode(code);

// Résultat:
// person.Name.First = GetFirstName();
// person.Name.Last = GetLastName();
// person.Contact.Email = GetEmail();
```

---

## Contexte et Désambiguation {#contexte}

### Problème: Mappings Ambigus
```csharp
// Même propriété, contextes différents
var engine = new PropertyMappingEngine();
engine.AddMapping("Name", "Employee.Name");
engine.AddMapping("Name", "Company.Name");

// Lequel utiliser? Ambigüité!
```

### Solution: Ajouter un Contexte
```csharp
var engine = new PropertyMappingEngine();

engine.AddMappingWithContext(
    "Name", 
    "Employee.Name",
    context: "Employee",
    description: "For employee context"
);

engine.AddMappingWithContext(
    "Name",
    "Company.Name", 
    context: "Company",
    description: "For company context"
);

// Maintenant, vous pouvez distinguer:
var mappings = engine.FindMappings("Name");  // Trouve les 2
var mapping = engine.FindMapping("Name");     // Trouve le premier
```

---

## Validation {#validation}

### Vérifier les Problèmes
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("Name", "Name.First");  // Potentiel cycle
engine.AddMapping("Status", "Status.Active");  // Duplication ambigüe

var issues = engine.ValidateMappings();

foreach (var issue in issues)
{
    Console.WriteLine($"??  {issue}");
}

// Output:
// ??  Potential cycle: 'Name' maps to 'Name.First'
// ??  Duplicate mapping for 'Status' without context
```

### Obtenir un Rapport
```csharp
var report = engine.GetReport();
Console.WriteLine(report);

// Output:
// Property Mapping Report
// ======================
// Total Mappings: 3
// 
// Mappings:
//   • FirstName ? Name.First
//   • LastName ? Name.Last
//   • Email ? Contact.Email
```

---

## Cas Réels {#casreels}

### Cas 1: Migration WinForms ? MVVM
```csharp
// Code WinForms legacy
var legacyCode = @"
public class LegacyForm
{
    private string _userName;
    private string _userEmail;
    private DateTime _userCreated;
    
    public void Display(User user)
    {
        lblName.Text = _userName;
        lblEmail.Text = _userEmail;
        lblCreated.Text = _userCreated.ToString();
    }
}
";

// Créer les mappings pour la migration
var engine = new PropertyMappingEngine();
engine.AddMapping("_userName", "User.Profile.Name");
engine.AddMapping("_userEmail", "User.Profile.Email");
engine.AddMapping("_userCreated", "User.Metadata.CreatedDate");

// Transformer
var result = engine.TransformCode(legacyCode);

// Résultat: le code utilise maintenant User.Profile.Name, etc.
```

### Cas 2: Migration Schéma Base de Données
```csharp
// Schema dénormalisé ? normalisé
var dataAccessCode = @"
public class CustomerRepository
{
    public void Save(Customer customer)
    {
        var city = customer.City;
        var state = customer.State;
        var zipCode = customer.ZipCode;
        
        db.Execute(
            ""INSERT INTO Customers (City, State, Zip) VALUES (@city, @state, @zip)"",
            new { city, state, zip = zipCode }
        );
    }
}
";

var engine = new PropertyMappingEngine();
engine.AddMapping("City", "Address.City");
engine.AddMapping("State", "Address.State");
engine.AddMapping("ZipCode", "Address.ZipCode");

var result = engine.TransformCode(dataAccessCode);

// Résultat:
// var city = customer.Address.City;
// var state = customer.Address.State;
// var zipCode = customer.Address.ZipCode;
```

### Cas 3: Migration API Version
```csharp
// API v1 ? v2
var apiCode = @"
public class UserApiClient
{
    public void ProcessUser(UserModel user)
    {
        var created = user.CreatedDate;
        var modified = user.ModifiedDate;
        var version = user.Version;
        
        var entity = new UserDto
        {
            Created = user.CreatedDate,
            Modified = user.ModifiedDate,
            Version = user.Version
        };
    }
}
";

var engine = new PropertyMappingEngine();
engine.AddMapping("CreatedDate", "Metadata.CreatedAt");
engine.AddMapping("ModifiedDate", "Metadata.UpdatedAt");
engine.AddMapping("Version", "Versioning.Number");

var result = engine.TransformCode(apiCode);

// Résultat: tout utilise les nouveaux chemins
```

---

## ?? Intégration avec CodeSearcher

### Combiner Recherche et Mapping
```csharp
// 1. Chercher les propriétés anciennes
var context = CodeContext.FromCode(legacyCode);
var oldProperties = context.FindVariables()
    .WithNameContaining("Consignee")
    .Execute()
    .ToList();

// 2. Créer le mapping
var engine = new PropertyMappingEngine();
foreach (var prop in oldProperties)
{
    engine.AddMapping(
        prop.Identifier.Text,
        $"Consignee.{prop.Identifier.Text.Replace("Consignee", "")}"
    );
}

// 3. Transformer le code
var result = engine.TransformCode(legacyCode);

// 4. Optionnellement, utiliser Editor pour autres refactorings
var editor = CodeEditor.FromCode(legacyCode);
if (result.Success)
{
    editor.Replace("_cachedConsignee", "_consignee");
}
var finalResult = editor.Apply();
```

---

## ?? Bonnes Pratiques

### 1. Valider Avant de Transformer
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("Name", "Person.Name");
engine.AddMapping("Status", "Person.Status");

var issues = engine.ValidateMappings();
if (issues.Any())
{
    Console.WriteLine("Problèmes détectés:");
    foreach (var issue in issues)
        Console.WriteLine($"  - {issue}");
    return;
}

var result = engine.TransformCode(code);
```

### 2. Utiliser des Contextes pour l'Ambiguïté
```csharp
// ? Mauvais: Ambigü
engine.AddMapping("Name", "Employee.Name");
engine.AddMapping("Name", "Company.Name");

// ? Bon: Contexte explicite
engine.AddMappingWithContext("Name", "Employee.Name", "Employee");
engine.AddMappingWithContext("Name", "Company.Name", "Company");
```

### 3. Documenter les Mappings
```csharp
engine.AddMappingWithContext(
    "ConsigneeName",
    "Consignee.Name",
    "Legacy",
    "Migration from flat properties to nested object structure"
);
```

### 4. Générer un Rapport
```csharp
// Avant de transformer, générer un rapport
Console.WriteLine(engine.GetReport());

// Puis transformer
var result = engine.TransformCode(code);
```

---

## ?? Exemples d'Utilisation

### Configuration Basique
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("OldProperty", "NewObject.Property");
var result = engine.TransformCode(code);
```

### Configuration Complète
```csharp
var engine = new PropertyMappingEngine(caseSensitive: true, wholeWordOnly: true);

engine.AddMappings(
    new PropertyMapping("FirstName", "Name.First") 
        { Description = "User first name" },
    new PropertyMapping("LastName", "Name.Last")
        { Description = "User last name" },
    new PropertyMapping("Email", "Contact.Email")
        { Description = "User email address" }
);

var issues = engine.ValidateMappings();
if (!issues.Any())
{
    var result = engine.TransformCode(legacyCode);
    Console.WriteLine(engine.GetReport());
}
```

---

## ?? Utilisation Recommandée

### Workflow Complet
```csharp
// 1. Créer le moteur
var engine = new PropertyMappingEngine();

// 2. Ajouter les mappings
engine.AddMapping("OldProp1", "New.Prop1");
engine.AddMapping("OldProp2", "New.Prop2");

// 3. Valider
var issues = engine.ValidateMappings();
if (issues.Any())
{
    foreach (var issue in issues)
        Console.WriteLine($"??  {issue}");
    return;
}

// 4. Afficher le rapport
Console.WriteLine(engine.GetReport());

// 5. Transformer
var result = engine.TransformCode(legacyCode);

// 6. Vérifier
if (result.Success)
{
    Console.WriteLine($"? Transformé avec succès");
    Console.WriteLine($"?? {result.ReplacementsCount} remplacements");
    foreach (var change in result.Changes)
        Console.WriteLine($"  • {change}");
}
else
{
    Console.WriteLine($"? Erreur: {result.ErrorMessage}");
}
```

---

**Le Property Mapping System vous permet d'automatiser les migrations Legacy complexes!** ??
