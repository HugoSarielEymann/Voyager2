# ?? PROPERTY MAPPING - Système de Mapping Automatique pour Migrations Legacy

## ?? Concept

Permettre de **mapper automatiquement** les propriétés d'un ancien type vers un nouveau type lors de migrations de code Legacy.

**Exemples:**
```csharp
// Ancien code
private string _toto;
lblData.ConsigneeName;

// Nouveau code (après mapping)
public string Toto { get; set; }  // transformé en
lblData.Consignee.Name;           // transformé en
```

---

## ?? Cas d'Usage Réels

### Cas 1: Propriété Simple ? Propriété dans Objet
```csharp
// AVANT: Une propriété simple au niveau classe
class OldClass
{
    private string _userName;  // ? Accès direct
}

usage: lblName.Text = _userName;

// APRÈS: La propriété est maintenant dans un objet
class NewClass
{
    private User _user;  // ? Objet contenant la propriété
}

usage: lblName.Text = _user.Name;  // ? Mapping transformé
```

### Cas 2: Propriété Chaînée (Nested)
```csharp
// AVANT
lblAddress.Text = address.Street;

// APRÈS (la propriété est plus profonde)
lblAddress.Text = company.Location.Address.Street;
```

### Cas 3: Type Différent
```csharp
// AVANT
int age = _personAge;

// APRÈS (le type a changé)
decimal age = _person.AgeInYears;  // Type changé
```

---

## ??? Architecture du Système

### 1. Interface de Mapping
```csharp
public interface IPropertyMapper
{
    /// Ajouter un mapping de propriété
    void AddMapping(PropertyMapping mapping);
    
    /// Trouver un mapping par ancien nom
    PropertyMapping? FindMapping(string oldPropertyName);
    
    /// Transformer le code avec les mappings
    string TransformCode(string code, List<PropertyMapping> mappings);
}

public class PropertyMapping
{
    public string OldPropertyName { get; set; }      // "ConsigneeName"
    public string NewPropertyPath { get; set; }       // "Consignee.Name"
    public string? OldType { get; set; }             // "string" (optionnel)
    public string? NewType { get; set; }             // "string" (optionnel)
    public bool IsNested { get; set; }               // true si plusieurs niveaux
    public string? MappedObjectName { get; set; }    // "Consignee" si nested
}
```

### 2. Mapper Principal
```csharp
public class PropertyMappingEngine
{
    private List<PropertyMapping> _mappings = new();
    
    public void AddMapping(string oldName, string newPath, 
                          string? oldType = null, string? newType = null)
    {
        var mapping = new PropertyMapping
        {
            OldPropertyName = oldName,
            NewPropertyPath = newPath,
            OldType = oldType,
            NewType = newType,
            IsNested = newPath.Contains('.')
        };
        _mappings.Add(mapping);
    }
    
    public TransformResult TransformCode(string code)
    {
        var result = code;
        var changes = new List<string>();
        
        foreach (var mapping in _mappings)
        {
            // Remplacer toutes les occurrences
            var pattern = $@"\b{mapping.OldPropertyName}\b";
            var replacement = mapping.NewPropertyPath;
            
            if (Regex.IsMatch(result, pattern))
            {
                result = Regex.Replace(result, pattern, replacement);
                changes.Add($"Mapped '{mapping.OldPropertyName}' ? '{mapping.NewPropertyPath}'");
            }
        }
        
        return new TransformResult
        {
            Success = true,
            TransformedCode = result,
            Changes = changes
        };
    }
}
```

---

## ?? Stratégies de Mapping

### Stratégie 1: Mapping Simple (1-to-1)
```csharp
mapper.AddMapping("ConsigneeName", "Consignee.Name");

// Transforme:
lblData.ConsigneeName;  ?  lblData.Consignee.Name
```

### Stratégie 2: Mapping avec Type
```csharp
mapper.AddMapping(
    oldName: "Age",
    newPath: "Person.AgeInYears",
    oldType: "int",
    newType: "decimal"
);

// Transforme:
int age = user.Age;  ?  decimal age = user.Person.AgeInYears;
```

### Stratégie 3: Mapping Multiple (même propriété dans objets différents)
```csharp
mapper.AddMapping("Name", "Employee.Name");
mapper.AddMapping("Name", "Company.Name");  // Conflit potentiel!

// Solution: Contexte
mapper.AddMappingWithContext("Name", "Employee.Name", context: "Employee");
mapper.AddMappingWithContext("Name", "Company.Name", context: "Company");
```

### Stratégie 4: Mapping avec Transformation
```csharp
mapper.AddMappingWithTransform(
    oldName: "DateString",
    newPath: "Person.DateOfBirth",
    transform: code => code.Replace(
        "Person.DateOfBirth",
        "DateTime.Parse(Person.DateOfBirth)"
    )
);
```

---

## ?? Intégration avec CodeSearcher

### Pattern Complet: Chercher + Mapper + Transformer
```csharp
// 1. Chercher les propriétés anciennes
var context = CodeContext.FromCode(code);
var oldProperties = context.FindVariables()
    .WithName("ConsigneeName")
    .IsPublic()
    .Execute()
    .ToList();

// 2. Créer le moteur de mapping
var mapper = new PropertyMappingEngine();
mapper.AddMapping("ConsigneeName", "Consignee.Name");
mapper.AddMapping("AddressStreet", "Address.Street");

// 3. Transformer le code
var editor = CodeEditor.FromCode(code);
var result = mapper.TransformCode(code);
editor.Replace("lblData.ConsigneeName", "lblData.Consignee.Name");
var finalResult = editor.Apply();
```

---

## ?? Exemples d'Implémentation

### Exemple 1: Migration Simple
```csharp
var code = @"
public class LegacyUI
{
    private string _userName;
    private string _userEmail;
    
    public void UpdateUI(User user)
    {
        lblName.Text = user._userName;
        lblEmail.Text = user._userEmail;
    }
}
";

var mapper = new PropertyMappingEngine();
mapper.AddMapping("_userName", "UserInfo.Name");
mapper.AddMapping("_userEmail", "UserInfo.Email");

var transformed = mapper.TransformCode(code);
// Résultat: lblName.Text = user.UserInfo.Name;
//           lblEmail.Text = user.UserInfo.Email;
```

### Exemple 2: Migration Complexe (Nested)
```csharp
var code = @"
public class OldService
{
    public string GetAddressCity(Customer customer)
    {
        return customer.AddressCity;  // Ancien: propriété plate
    }
}
";

var mapper = new PropertyMappingEngine();
mapper.AddMapping(
    oldName: "AddressCity",
    newPath: "Address.Location.City"
);

var transformed = mapper.TransformCode(code);
// Résultat: return customer.Address.Location.City;
```

### Exemple 3: Migration avec Conversion de Type
```csharp
var code = @"
public class OldCalculator
{
    public int GetDiscount(Order order)
    {
        return order.DiscountPercent;  // Ancien: int
    }
}
";

var mapper = new PropertyMappingEngine();
mapper.AddMapping(
    oldName: "DiscountPercent",
    newPath: "Pricing.DiscountRate",
    oldType: "int",
    newType: "decimal"
);

var transformed = mapper.TransformCode(code);
// Plus tard, un refactoring additionnel pour le type:
// return order.Pricing.DiscountRate;
```

---

## ?? Cas Réels de Migration

### Cas Réel 1: Refactorisation de Classe
```csharp
// AVANT: Toutes les propriétés directes
class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}

// APRÈS: Propriétés groupées
class Person
{
    public Name FullName { get; set; }
    public Contact ContactInfo { get; set; }
}

// Mappings:
var mappings = new Dictionary<string, string>
{
    { "FirstName", "FullName.First" },
    { "LastName", "FullName.Last" },
    { "Email", "ContactInfo.Email" },
    { "PhoneNumber", "ContactInfo.Phone" }
};

// Transformer tout le codebase:
var mapper = new PropertyMappingEngine();
foreach (var (old, newPath) in mappings)
    mapper.AddMapping(old, newPath);

var result = mapper.TransformCode(legacyCode);
```

### Cas Réel 2: Migration Framework
```csharp
// AVANT: WinForms legacy
class LegacyForm
{
    private TextBox lblUserName;
    private TextBox lblUserEmail;
    
    private string _cachedName;
    private string _cachedEmail;
}

// APRÈS: MVVM moderne
class ModernViewModel
{
    private User _user;  // Objet injecté
    
    public string Name => _user.Profile.Name;
    public string Email => _user.Profile.Email;
}

// Mappings:
mapper.AddMapping("_cachedName", "User.Profile.Name");
mapper.AddMapping("_cachedEmail", "User.Profile.Email");
mapper.AddMapping("lblUserName.Text", "NameBinding");
mapper.AddMapping("lblUserEmail.Text", "EmailBinding");
```

### Cas Réel 3: Migration Base de Données
```csharp
// AVANT: Schema dénormalisé
class DataAccess
{
    public string GetCustomerCity(int customerId)
    {
        return _db.ExecuteScalar("SELECT City FROM Customers WHERE Id = " + customerId);
    }
}

// APRÈS: ORM avec relations
class Repository
{
    public string GetCustomerCity(int customerId)
    {
        return _db.Customers.Find(customerId).Address.City;
    }
}

// Mappings permettant la transition:
mapper.AddMapping("City", "Address.City");
mapper.AddMapping("State", "Address.State");
mapper.AddMapping("ZipCode", "Address.ZipCode");
```

---

## ?? Détection Automatique de Mappings (Optionnel)

### Technique d'Heuristique
```csharp
public class MappingDetector
{
    /// Détecter automatiquement les mappings possibles
    public List<PropertyMapping> DetectMappings(
        string oldCode, string newCode)
    {
        var oldProperties = ExtractProperties(oldCode);
        var newProperties = ExtractProperties(newCode);
        
        var suggestedMappings = new List<PropertyMapping>();
        
        foreach (var oldProp in oldProperties)
        {
            // Chercher une propriété similaire dans le nouveau code
            var matchedNew = newProperties
                .FirstOrDefault(np => IsSimilar(oldProp, np));
            
            if (matchedNew != null)
            {
                suggestedMappings.Add(new PropertyMapping
                {
                    OldPropertyName = oldProp,
                    NewPropertyPath = matchedNew
                });
            }
        }
        
        return suggestedMappings;
    }
    
    private bool IsSimilar(string oldName, string newPath)
    {
        // Heuristique: même racinne du mot, même type probable
        return newPath.Contains(oldName.ToLower()) ||
               oldName.ToLower().Contains(newPath.ToLower());
    }
}
```

---

## ?? Matrice de Mapping

Visualiser les mappings comme une matrice:

```
?????????????????????????????????????????????????????
? Ancien Nom      ? Nouveau Path         ? Type     ?
?????????????????????????????????????????????????????
? ConsigneeName   ? Consignee.Name       ? string   ?
? ConsigneeEmail  ? Consignee.Email      ? string   ?
? AddressCity     ? Address.Location.City? string   ?
? DateCreated     ? Metadata.CreatedDate ? DateTime ?
? IsArchived      ? Status.IsArchived    ? bool     ?
?????????????????????????????????????????????????????
```

---

## ? Avantages du Système

? **Automatisation**: Pas besoin de refactoriser manuellement  
? **Traçabilité**: Logs de tous les changements  
? **Flexibilité**: Support de mappings complexes et nichés  
? **Réutilisabilité**: Même mapping utilisé partout  
? **Testabilité**: Chaque mapping peut être testé  
? **Évolutivité**: Ajouter/supprimer des mappings facilement  

---

## ?? Prochaines Étapes

1. **Implémenter** `PropertyMappingEngine`
2. **Tester** avec des cas réels
3. **Intégrer** avec CodeSearcher.Editor
4. **Ajouter** détection automatique (optionnel)
5. **Documenter** tous les patterns

---

## ?? Utilisation Recommandée

### Phase 1: Identifier les Mappings
```csharp
// Analyser ancien et nouveau code
var detector = new MappingDetector();
var suggestedMappings = detector.DetectMappings(oldCode, newCode);
```

### Phase 2: Valider les Mappings
```csharp
// Vérifier que les mappings ont du sens
// (avec UI ou fichier de configuration)
var validatedMappings = ValidateMappings(suggestedMappings);
```

### Phase 3: Appliquer les Mappings
```csharp
// Appliquer à tout le codebase
var mapper = new PropertyMappingEngine();
foreach (var mapping in validatedMappings)
    mapper.AddMapping(mapping.OldPropertyName, mapping.NewPropertyPath);

var transformedCode = mapper.TransformCode(legacyCode);
```

---

**Ce système offre une solution complète pour les migrations Legacy complexes!** ??
