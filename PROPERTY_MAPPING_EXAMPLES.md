# ?? EXEMPLES DÉTAILLÉS - Property Mapping System

## ?? Exemples Pratiques et Testés

Tous les exemples suivants sont basés sur la suite de tests et fonctionnent à 100%.

---

## ?? EXEMPLE 1: Migration Simple (1-to-1)

### Problème
Vous avez des propriétés avec anciens noms et vous voulez les renommer partout.

### Code Legacy
```csharp
public class UserForm
{
    public void Display(User user)
    {
        lblName.Text = user.OldName;
        lblEmail.Text = user.OldEmail;
        lblAge.Text = user.OldAge.ToString();
    }
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("OldName", "Name");
engine.AddMapping("OldEmail", "Email");
engine.AddMapping("OldAge", "Age");

var result = engine.TransformCode(legacyCode);

// Résultat:
// user.Name
// user.Email
// user.Age
```

### Test Associé
```csharp
[Fact]
public void TransformCode_SimpleReplacement_ReplacesAllOccurrences()
{
    // Arrange
    var code = @"
public class Service
{
    public void Setup(User user)
    {
        var name = user.OldName;
        var label = lblName.Text = user.OldName;
    }
}
";
    var engine = new PropertyMappingEngine();
    engine.AddMapping("OldName", "NewName");

    // Act
    var result = engine.TransformCode(code);

    // Assert
    Assert.True(result.Success);
    Assert.Contains("user.NewName", result.TransformedCode);
    Assert.Equal(2, result.ReplacementsCount);
}
```

---

## ?? EXEMPLE 2: Propriétés Imbriquées (Nested)

### Problème
Les propriétés simples deviennent imbriquées dans un objet.

### Code Legacy
```csharp
public class OrderForm
{
    public void Display(Order order)
    {
        lblConsignee.Text = order.ConsigneeName;
        lblEmail.Text = order.ConsigneeEmail;
        lblPhone.Text = order.ConsigneePhone;
    }
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();

// Détecte automatiquement que c'est imbriqué
engine.AddMapping("ConsigneeName", "Consignee.Name");
engine.AddMapping("ConsigneeEmail", "Consignee.Email");
engine.AddMapping("ConsigneePhone", "Consignee.Phone");

var result = engine.TransformCode(legacyCode);

// Résultat:
// order.Consignee.Name
// order.Consignee.Email
// order.Consignee.Phone
```

### Propriétés de Mapping
```csharp
var mapping = engine.FindMapping("ConsigneeName");
Console.WriteLine($"Is Nested: {mapping.IsNested}");           // true
Console.WriteLine($"Mapped Object: {mapping.MappedObjectName}"); // "Consignee"
```

### Test Associé
```csharp
[Fact]
public void TransformCode_NestedMapping_ReplacesWithNestedPath()
{
    var code = @"
public class Form
{
    public void Display(Order order)
    {
        lblConsignee.Text = order.ConsigneeName;
        Console.WriteLine(order.ConsigneeName);
    }
}
";
    var engine = new PropertyMappingEngine();
    engine.AddMapping("ConsigneeName", "Consignee.Name");

    var result = engine.TransformCode(code);

    Assert.True(result.Success);
    Assert.Contains("order.Consignee.Name", result.TransformedCode);
    Assert.Equal(2, result.ReplacementsCount);
}
```

---

## ?? EXEMPLE 3: Imbrication Profonde

### Problème
La propriété est imbriquée à plusieurs niveaux de profondeur.

### Code Legacy
```csharp
public class CustomerRepository
{
    public string GetCity(Customer customer)
    {
        return customer.City;  // Propriété plate
    }
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("City", "Address.Location.City");

var result = engine.TransformCode(code);

// Résultat:
// return customer.Address.Location.City;
```

### Autres Exemples d'Imbrication Profonde
```csharp
engine.AddMapping("Street", "Address.Location.Street");
engine.AddMapping("ZipCode", "Address.Location.PostalCode");
engine.AddMapping("Country", "Address.Location.CountryInfo.Name");

// Remplace:
customer.Street ? customer.Address.Location.Street
customer.ZipCode ? customer.Address.Location.PostalCode
customer.Country ? customer.Address.Location.CountryInfo.Name
```

---

## ?? EXEMPLE 4: Mappings Avec Types

### Problème
Le type de la propriété change pendant la migration.

### Code Legacy
```csharp
public class Settings
{
    public int Timeout { get; set; }  // En secondes, int
}
```

### Nouveau Code
```csharp
public class AppConfig
{
    public decimal TimeoutMs { get; set; }  // En millisecondes, decimal
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping(
    oldName: "Timeout",
    newPath: "Config.TimeoutMs",
    oldType: "int",
    newType: "decimal"
);

// Trace aussi le changement de type
var mapping = engine.FindMapping("Timeout");
Console.WriteLine($"{mapping.OldType} ? {mapping.NewType}");
```

### Code Transformé
```csharp
// AVANT
int timeout = settings.Timeout;

// APRÈS
int timeout = settings.Config.TimeoutMs;
// (Note: conversion de type à faire manuellement avec refactoring additionnel)
```

---

## ?? EXEMPLE 5: Mappings Multiples et Complexes

### Problème
Migration d'une classe entière avec plusieurs propriétés groupées.

### Code Legacy
```csharp
public class PersonForm
{
    private Person person;
    
    public void Update()
    {
        person.FirstName = txtFirstName.Text;
        person.LastName = txtLastName.Text;
        person.Email = txtEmail.Text;
        person.Phone = txtPhone.Text;
        person.BirthDate = dtpBirth.Value;
    }
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();

// Grouper les propriétés par objet
engine.AddMappings(
    // Nom (groupé dans Name)
    new PropertyMapping("FirstName", "Profile.Name.First"),
    new PropertyMapping("LastName", "Profile.Name.Last"),
    
    // Contact (groupé dans Contact)
    new PropertyMapping("Email", "Profile.Contact.Email"),
    new PropertyMapping("Phone", "Profile.Contact.Phone"),
    
    // Données personnelles (groupé dans PersonalData)
    new PropertyMapping("BirthDate", "PersonalData.DateOfBirth")
);

// Valider avant
var issues = engine.ValidateMappings();
Assert.Empty(issues);

var result = engine.TransformCode(legacyCode);

// Résultat:
// person.Profile.Name.First
// person.Profile.Name.Last
// person.Profile.Contact.Email
// person.Profile.Contact.Phone
// person.PersonalData.DateOfBirth
```

---

## ?? EXEMPLE 6: Contexte et Désambiguation

### Problème
Même propriété "Name" existe pour Employee et Company.

### Code Legacy
```csharp
public class DataProcessor
{
    public void ProcessEmployee(Employee employee)
    {
        var name = employee.Name;
    }
    
    public void ProcessCompany(Company company)
    {
        var name = company.Name;
    }
}
```

### Solution
```csharp
var engine = new PropertyMappingEngine();

// Ajouter avec contexte
engine.AddMappingWithContext(
    "Name",
    "Profile.FullName",
    context: "Employee",
    description: "Employee full name"
);

engine.AddMappingWithContext(
    "Name",
    "Details.CompanyName",
    context: "Company",
    description: "Company official name"
);

// Trouver par contexte
var employeeMapping = engine.FindMappings("Name")
    .FirstOrDefault(m => m.Context == "Employee");
// Résultat: "Profile.FullName"

var companyMapping = engine.FindMappings("Name")
    .FirstOrDefault(m => m.Context == "Company");
// Résultat: "Details.CompanyName"
```

### Test Associé
```csharp
[Fact]
public void FindMappings_MultipleContexts_FindsAll()
{
    var engine = new PropertyMappingEngine();
    engine.AddMappingWithContext("Name", "Employee.Name", "Employee");
    engine.AddMappingWithContext("Name", "Company.Name", "Company");

    var mappings = engine.FindMappings("Name").ToList();

    Assert.Equal(2, mappings.Count);
}
```

---

## ?? EXEMPLE 7: Validation et Rapports

### Détection des Problèmes
```csharp
var engine = new PropertyMappingEngine();

// Problème 1: Cycle potentiel
engine.AddMapping("Name", "Name.First");

// Problème 2: Duplication sans contexte
engine.AddMapping("Status", "Status.Active");
engine.AddMapping("Status", "Status.Inactive");

// Valider
var issues = engine.ValidateMappings();

foreach (var issue in issues)
{
    Console.WriteLine($"??  {issue}");
}

// Output:
// ??  Potential cycle: 'Name' maps to 'Name.First'
// ??  Duplicate mapping for 'Status' without context
```

### Générer un Rapport
```csharp
var engine = new PropertyMappingEngine();
engine.AddMapping("FirstName", "Name.First");
engine.AddMapping("LastName", "Name.Last");
engine.AddMapping("Email", "Contact.Email");

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

## ?? EXEMPLE 8: Configuration et Options

### Sensibilité à la Casse
```csharp
// Case-sensitive (défaut)
var engine1 = new PropertyMappingEngine(caseSensitive: true);
engine1.AddMapping("Name", "Person.Name");

var result1 = engine1.TransformCode("var name = user.Name;");
// Remplace: user.Name (respecte la casse)

// Case-insensitive
var engine2 = new PropertyMappingEngine(caseSensitive: false);
engine2.AddMapping("name", "person.name");

var result2 = engine2.TransformCode("var name = user.Name;");
// Remplace aussi: user.Name (ignore la casse)
```

### Mots Entiers Uniquement
```csharp
// Whole word only (défaut)
var engine = new PropertyMappingEngine(wholeWordOnly: true);
engine.AddMapping("Name", "Person.Name");

var result = engine.TransformCode("var firstName = user.Name;");
// firstName n'est pas remplacé (ne contient pas "Name" en entier)
```

---

## ?? EXEMPLE 9: Workflow Complet Réel

### Migration WinForms ? MVVM

```csharp
// Code legacy WinForms
var legacyCode = @"
public class UserForm : Form
{
    private string _cachedName;
    private string _cachedEmail;
    private DateTime _cachedCreated;
    
    private User _currentUser;
    
    public void DisplayUser(User user)
    {
        _cachedName = user.Name;
        _cachedEmail = user.Email;
        _cachedCreated = user.CreatedDate;
        
        lblName.Text = _cachedName;
        lblEmail.Text = _cachedEmail;
        lblDate.Text = _cachedCreated.ToString(""dd/MM/yyyy"");
    }
}
";

// Créer le moteur
var engine = new PropertyMappingEngine();

// Ajouter les mappings pour la migration
engine.AddMappings(
    new PropertyMapping("_cachedName", "UserViewModel.Profile.Name")
    {
        Description = "User name moved to ViewModel profile"
    },
    new PropertyMapping("_cachedEmail", "UserViewModel.Profile.Email")
    {
        Description = "User email moved to ViewModel profile"
    },
    new PropertyMapping("_cachedCreated", "UserViewModel.Metadata.CreatedDate")
    {
        Description = "User creation date moved to ViewModel metadata"
    },
    new PropertyMapping("Name", "Profile.Name"),
    new PropertyMapping("Email", "Profile.Email"),
    new PropertyMapping("CreatedDate", "Metadata.CreatedDate")
);

// Valider
var issues = engine.ValidateMappings();
if (issues.Any())
{
    Console.WriteLine("Problèmes trouvés:");
    foreach (var issue in issues)
        Console.WriteLine($"  - {issue}");
    return;
}

// Afficher le rapport
Console.WriteLine(engine.GetReport());

// Transformer
var result = engine.TransformCode(legacyCode);

if (result.Success)
{
    Console.WriteLine($"? Transformation réussie!");
    Console.WriteLine($"?? {result.ReplacementsCount} remplacements effectués");
    Console.WriteLine("\nChangements:");
    foreach (var change in result.Changes)
        Console.WriteLine($"  • {change}");
    
    // Sauvegarder le code transformé
    File.WriteAllText("UserForm_Migrated.cs", result.TransformedCode);
}
else
{
    Console.WriteLine($"? Erreur: {result.ErrorMessage}");
}
```

---

## ?? RÉSUMÉ DES EXEMPLES

| Exemple | Cas | Fichier Test |
|---------|-----|-------------|
| 1 | Simple 1-to-1 | TransformCode_SimpleReplacement |
| 2 | Imbriqué 1 niveau | TransformCode_NestedMapping |
| 3 | Imbriqué N niveaux | TransformCode_DeepNesting |
| 4 | Avec types | TransformCode_DifferentTypes |
| 5 | Multiples | TransformCode_MultipleMappings |
| 6 | Contexte | FindMappings_MultipleContexts |
| 7 | Validation | ValidateMappings_* |
| 8 | Configuration | TransformCode_CaseSensitive |
| 9 | Workflow | RealWorld_LegacyFormToMVVM |

---

**Tous les exemples sont testés et fonctionnels!** ?
