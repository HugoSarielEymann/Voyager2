using CodeSearcher.Editor.Mapping;
using Xunit;

namespace CodeSearcher.Tests.Editor.Mapping
{
    /// <summary>
    /// Tests pour le syst�me de Property Mapping
    /// Valide que le syst�me peut mapper automatiquement les propri�t�s legacy vers les nouvelles
    /// </summary>
    public class PropertyMappingEngineTests
    {
        #region 1. Tests de Mapping Simple

        [Fact]
        public void AddMapping_SimpleMapping_StoresMapping()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMapping("OldName", "NewName");

            // Assert
            var mapping = engine.FindMapping("OldName");
            Assert.NotNull(mapping);
            Assert.Equal("OldName", mapping.OldPropertyName);
            Assert.Equal("NewName", mapping.NewPropertyPath);
        }

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
            Assert.DoesNotContain("user.OldName", result.TransformedCode);
            Assert.Equal(2, result.ReplacementsCount);
        }

        #endregion

        #region 2. Tests de Mapping Imbriqu� (Nested)

        [Fact]
        public void AddMapping_NestedMapping_IdentifiesAsNested()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMapping("ConsigneeName", "Consignee.Name");

            // Assert
            var mapping = engine.FindMapping("ConsigneeName");
            Assert.True(mapping.IsNested);
            Assert.Equal("Consignee", mapping.MappedObjectName);
        }

        [Fact]
        public void TransformCode_NestedMapping_ReplacesWithNestedPath()
        {
            // Arrange
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

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("order.Consignee.Name", result.TransformedCode);
            Assert.DoesNotContain("ConsigneeName", result.TransformedCode);
            Assert.Equal(2, result.ReplacementsCount);
        }

        [Fact]
        public void TransformCode_DeepNesting_ReplacesMultipleLevels()
        {
            // Arrange
            var code = @"
public class Service
{
    public string GetCity(Customer customer)
    {
        return customer.AddressCity;
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("AddressCity", "Address.Location.City");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("customer.Address.Location.City", result.TransformedCode);
        }

        #endregion

        #region 3. Tests de Mapping avec Types

        [Fact]
        public void AddMapping_WithTypes_StoresTypeInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMapping("Age", "AgeInYears", "int", "decimal");

            // Assert
            var mapping = engine.FindMapping("Age");
            Assert.Equal("int", mapping.OldType);
            Assert.Equal("decimal", mapping.NewType);
        }

        [Fact]
        public void TransformCode_DifferentTypes_ReplacesCorrectly()
        {
            // Arrange
            var code = @"
public class Person
{
    public int GetAge()
    {
        return this.Age;
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Age", "AgeInYears", "int", "decimal");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("this.AgeInYears", result.TransformedCode);
        }

        #endregion

        #region 4. Tests de Mappings Multiples

        [Fact]
        public void AddMappings_MultipleMappings_StoresAll()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMappings(
                new PropertyMapping("FirstName", "Name.First"),
                new PropertyMapping("LastName", "Name.Last"),
                new PropertyMapping("Email", "Contact.Email")
            );

            // Assert
            Assert.Equal(3, engine.GetAllMappings().Count);
        }

        [Fact]
        public void TransformCode_MultipleMappings_AppliesAll()
        {
            // Arrange
            var code = @"
public class User
{
    public void Setup(Person person)
    {
        var first = person.FirstName;
        var last = person.LastName;
        var email = person.Email;
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("FirstName", "Name.First");
            engine.AddMapping("LastName", "Name.Last");
            engine.AddMapping("Email", "Contact.Email");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("person.Name.First", result.TransformedCode);
            Assert.Contains("person.Name.Last", result.TransformedCode);
            Assert.Contains("person.Contact.Email", result.TransformedCode);
            Assert.Equal(3, result.ReplacementsCount);
        }

        #endregion

        #region 5. Tests avec Contexte

        [Fact]
        public void AddMappingWithContext_WithContext_StoresContext()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMappingWithContext("Name", "Employee.Name", "Employee");

            // Assert
            var mapping = engine.FindMapping("Name");
            Assert.Equal("Employee", mapping.Context);
        }

        [Fact]
        public void FindMappings_MultipleContexts_FindsAll()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMappingWithContext("Name", "Employee.Name", "Employee");
            engine.AddMappingWithContext("Name", "Company.Name", "Company");

            // Act
            var mappings = engine.FindMappings("Name").ToList();

            // Assert
            Assert.Equal(2, mappings.Count);
        }

        #endregion

        #region 6. Tests de Validations

        [Fact]
        public void ValidateMappings_Duplicates_IdentifiesDuplicates()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "Person.Name");
            engine.AddMapping("Name", "Employee.Name");  // Duplicate sans contexte

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.NotEmpty(issues);
            Assert.Contains(issues, i => i.Contains("Duplicate"));
        }

        [Fact]
        public void ValidateMappings_Cycles_IdentifiesCycles()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "Name.First");  // Potentiel cycle

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.NotEmpty(issues);
            Assert.Contains(issues, i => i.Contains("cycle"));
        }

        [Fact]
        public void ValidateMappings_ValidMappings_NoIssues()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("OldName", "NewName");
            engine.AddMapping("OldEmail", "NewEmail");

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.Empty(issues);
        }

        #endregion

        #region 7. Tests de Gestion d'�tat

        [Fact]
        public void Clear_ClearsMappings_RemovesAll()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("OldName", "NewName");
            engine.AddMapping("OldEmail", "NewEmail");

            // Act
            engine.Clear();

            // Assert
            Assert.Empty(engine.GetAllMappings());
        }

        [Fact]
        public void RemoveMapping_RemovesSpecificMapping()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("OldName", "NewName");
            engine.AddMapping("OldEmail", "NewEmail");

            // Act
            var removed = engine.RemoveMapping("OldName");

            // Assert
            Assert.True(removed);
            Assert.Single(engine.GetAllMappings());
            Assert.Null(engine.FindMapping("OldName"));
        }

        #endregion

        #region 8. Cas R�els de Migration

        [Fact]
        public void RealWorld_LegacyFormToMVVM_TransformsCorrectly()
        {
            // Arrange - Migration WinForms ? MVVM
            var legacyCode = @"
public class LegacyForm
{
    private string _userName;
    private string _userEmail;
    private string _userPhone;
    
    public void UpdateUI(User user)
    {
        lblName.Text = _userName;
        lblEmail.Text = _userEmail;
        lblPhone.Text = _userPhone;
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("_userName", "UserProfile.Name");
            engine.AddMapping("_userEmail", "UserProfile.Email");
            engine.AddMapping("_userPhone", "UserProfile.Phone");

            // Act
            var result = engine.TransformCode(legacyCode);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("UserProfile.Name", result.TransformedCode);
            Assert.Contains("UserProfile.Email", result.TransformedCode);
            Assert.Contains("UserProfile.Phone", result.TransformedCode);
            Assert.Equal(6, result.ReplacementsCount);  // 6 occurrences (2x chaque: d�claration + utilisation)
        }

        [Fact]
        public void RealWorld_DatabaseSchemaRefactor_TransformsComplexMappings()
        {
            // Arrange - Migration sch�ma d�normalis� ? normalis�
            var code = @"
public class CustomerRepository
{
    public void SaveCustomer(Customer customer)
    {
        var city = customer.City;
        var state = customer.State;
        var zip = customer.ZipCode;
        
        UpdateAddress(customer.City, customer.State, customer.ZipCode);
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("City", "Address.City");
            engine.AddMapping("State", "Address.State");
            engine.AddMapping("ZipCode", "Address.ZipCode");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.DoesNotContain(".City", result.TransformedCode.Split("Address").First());
            Assert.Contains("Address.City", result.TransformedCode);
            Assert.Contains("Address.State", result.TransformedCode);
            Assert.Contains("Address.ZipCode", result.TransformedCode);
            Assert.Equal(6, result.ReplacementsCount);  // 6 occurrences (2x City, 2x State, 2x ZipCode)
        }

        [Fact]
        public void RealWorld_APIVersionUpgrade_MapPropertiesAcrossVersions()
        {
            // Arrange - Migration API v1 ? v2
            var apiV1Code = @"
public class ApiClient
{
    public void ProcessUser(UserV1 user)
    {
        var created = user.CreatedDate;
        var modified = user.ModifiedDate;
        var status = user.IsActive;
        
        Log($""User {user.Id} created {user.CreatedDate}"");
    }
}
";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("CreatedDate", "Metadata.CreatedAt");
            engine.AddMapping("ModifiedDate", "Metadata.UpdatedAt");
            engine.AddMapping("IsActive", "Status.IsActive");

            // Act
            var result = engine.TransformCode(apiV1Code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("user.Metadata.CreatedAt", result.TransformedCode);
            Assert.Contains("user.Metadata.UpdatedAt", result.TransformedCode);
            Assert.Contains("user.Status.IsActive", result.TransformedCode);
        }

        #endregion

        #region 9. Tests de Rapport

        [Fact]
        public void GetReport_GeneratesReport_IncludesMappingInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("OldName", "NewName");
            engine.AddMapping("OldEmail", "NewEmail");

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("Total Mappings: 2", report);
            Assert.Contains("OldName", report);
            Assert.Contains("NewName", report);
        }

        #endregion

        #region 10. Tests de Configuration

        [Fact]
        public void TransformCode_CaseSensitive_RespectsCaseSensitivity()
        {
            // Arrange
            var code = @"
var name = oldName;
var Name = OldName;
";
            var engine = new PropertyMappingEngine(caseSensitive: true);
            engine.AddMapping("oldName", "newName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.Contains("newName", result.TransformedCode);
            Assert.Contains("OldName", result.TransformedCode);  // Pas chang� (diff�rent cas)
        }

        [Fact]
        public void TransformCode_WholeWordOnly_IgnoresPartialMatches()
        {
            // Arrange
            var code = @"
var firstName = oldName;
var surname = firstName;
";
            var engine = new PropertyMappingEngine(wholeWordOnly: true);
            engine.AddMapping("oldName", "newName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.Contains("newName", result.TransformedCode);
            Assert.Contains("firstName", result.TransformedCode);  // Ne contient pas oldName, pas chang�
        }

        #endregion

        #region 11. Tests de Mapping avec Informations Source/Cible Compl�tes

        [Fact]
        public void AddMappingWithParentContext_FullInfo_StoresAllInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            engine.AddMappingWithParentContext(
                oldPropertyName: "ConsigneeName",
                oldPropertyType: "string",
                oldParentName: "Order",
                oldParentType: "class",
                newPropertyName: "Name",
                newPropertyType: "string",
                newParentName: "Consignee",
                newParentType: "class",
                description: "Migration vers mod�le DDD"
            );

            // Assert
            var mapping = engine.FindMapping("ConsigneeName");
            Assert.NotNull(mapping);
            Assert.Equal("ConsigneeName", mapping.Source.PropertyName);
            Assert.Equal("string", mapping.Source.PropertyType);
            Assert.Equal("Order", mapping.Source.ParentName);
            Assert.Equal("class", mapping.Source.ParentType);
            Assert.Equal("Name", mapping.Target.PropertyName);
            Assert.Equal("string", mapping.Target.PropertyType);
            Assert.Equal("Consignee", mapping.Target.ParentName);
            Assert.Equal("Consignee.Name", mapping.NewPropertyPath);
        }

        [Fact]
        public void AddMapping_WithSourceAndTargetInfo_CreatesCompleteMapping()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            var source = new SourcePropertyInfo
            {
                PropertyName = "ShipperAddress",
                PropertyType = "string",
                ParentName = "Shipment",
                ParentType = "class"
            };
            var target = new TargetPropertyInfo
            {
                PropertyName = "Street",
                PropertyType = "string",
                ParentName = "Shipper.Address",
                ParentType = "record"
            };

            // Act
            engine.AddMapping(source, target, context: "LegacyMigration");

            // Assert
            var mapping = engine.FindMapping("ShipperAddress");
            Assert.NotNull(mapping);
            Assert.Equal("LegacyMigration", mapping.Context);
            Assert.Equal("Shipper.Address.Street", mapping.NewPropertyPath);
            Assert.True(mapping.IsNested);
        }

        [Fact]
        public void FindMapping_WithParentContext_FindsCorrectMapping()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMappingWithParentContext(
                "Name", "string", "Employee", "class",
                "FullName", "string", "Person", "class"
            );
            engine.AddMappingWithParentContext(
                "Name", "string", "Company", "class",
                "CompanyName", "string", null, null
            );

            // Act
            var employeeMapping = engine.FindMapping("Name", "Employee");
            var companyMapping = engine.FindMapping("Name", "Company");

            // Assert
            Assert.NotNull(employeeMapping);
            Assert.NotNull(companyMapping);
            Assert.Equal("Person.FullName", employeeMapping.NewPropertyPath);
            Assert.Equal("CompanyName", companyMapping.NewPropertyPath);
        }

        [Fact]
        public void PropertyMapping_ToString_IncludesAllInfo()
        {
            // Arrange
            var mapping = new PropertyMapping(
                new SourcePropertyInfo
                {
                    PropertyName = "OldProp",
                    PropertyType = "int"
                },
                new TargetPropertyInfo
                {
                    PropertyName = "NewProp",
                    PropertyType = "long",
                    ParentName = "Container"
                }
            );

            // Act
            var str = mapping.ToString();

            // Assert
            Assert.Contains("OldProp", str);
            Assert.Contains("Container.NewProp", str);
            Assert.Contains("int", str);
            Assert.Contains("long", str);
        }

        #endregion

        #region 12. Tests d'Analyse S�mantique Roslyn

        [Fact]
        public void TransformCode_WithSemanticAnalysis_TransformsCorrectly()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Process(Order order)
    {
        var name = order.ConsigneeName;
        Console.WriteLine(order.ConsigneeName);
    }
}
";
            var engine = new PropertyMappingEngine(caseSensitive: true, wholeWordOnly: true, useSemanticAnalysis: true);
            engine.AddMapping("ConsigneeName", "Consignee.Name");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("order.Consignee.Name", result.TransformedCode);
            Assert.DoesNotContain("ConsigneeName", result.TransformedCode);
        }

        [Fact]
        public void TransformCode_SemanticAnalysis_HandlesMultipleMappings()
        {
            // Arrange
            var code = @"
public class Migration
{
    public void Convert(LegacyOrder legacy)
    {
        var consignee = legacy.ConsigneeName;
        var shipper = legacy.ShipperName;
        var address = legacy.ShipperAddress;
    }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            engine.AddMapping("ConsigneeName", "Consignee.Name");
            engine.AddMapping("ShipperName", "Shipper.Name");
            engine.AddMapping("ShipperAddress", "Shipper.Address.Street");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("legacy.Consignee.Name", result.TransformedCode);
            Assert.Contains("legacy.Shipper.Name", result.TransformedCode);
            Assert.Contains("legacy.Shipper.Address.Street", result.TransformedCode);
            Assert.Equal(3, result.ReplacementsCount);
        }

        #endregion

        #region 13. Tests de Validation Am�lior�e

        [Fact]
        public void ValidateMappings_TypeMismatch_ReportsIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Age", "Age", "int", "string");

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.Contains(issues, i => i.Contains("Type mismatch"));
        }

        [Fact]
        public void ValidateMappings_CompatibleNumericTypes_NoIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Count", "Count", "int", "long");

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.DoesNotContain(issues, i => i.Contains("Type mismatch"));
        }

        [Fact]
        public void ValidateMappings_DuplicateWithSameParent_ReportsIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMappingWithParentContext(
                oldPropertyName: "Name",
                oldPropertyType: "string",
                oldParentName: "Employee",
                oldParentType: "class",
                newPropertyName: "NewName1",
                newPropertyType: "string"
            );
            
            var mapping2 = new PropertyMapping("Name", "NewName2");
            mapping2.Source.ParentName = "Employee";
            engine.AddMapping(mapping2);

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.Contains(issues, i => i.Contains("Duplicate") && i.Contains("Employee"));
        }

        #endregion

        #region 14. Tests de D�tection Automatique de Mappings

        [Fact]
        public void MappingDetector_DetectSuggestedMappings_FindsSimilarProperties()
        {
            // Arrange
            var oldCode = @"
public class LegacyOrder
{
    public string ConsigneeName { get; set; }
    public string ConsigneeAddress { get; set; }
}
";
            var newCode = @"
public class Order
{
    public Consignee Consignee { get; set; }
}
public class Consignee
{
    public string Name { get; set; }
    public string Address { get; set; }
}
";
            var detector = new MappingDetector();

            // Act
            var suggestions = detector.DetectSuggestedMappings(oldCode, newCode);

            // Assert
            Assert.NotEmpty(suggestions);
            // Should find some suggested mappings based on name similarity
        }

        [Fact]
        public void MappingDetector_ExtractsPropertyInfo_IncludesTypeAndParent()
        {
            // Arrange
            var code = @"
public class Customer
{
    public string FirstName { get; set; }
    public int Age { get; set; }
}
";
            var detector = new MappingDetector();

            // Act - using suggested mappings with same code should find exact matches
            var suggestions = detector.DetectSuggestedMappings(code, code);

            // Assert
            Assert.NotEmpty(suggestions);
            var firstNameMapping = suggestions.FirstOrDefault(s => s.Source.PropertyName == "FirstName");
            Assert.NotNull(firstNameMapping);
            Assert.Equal("string", firstNameMapping.Source.PropertyType);
            Assert.Equal("Customer", firstNameMapping.Source.ParentName);
        }

        #endregion

        #region 15. Tests de Rapport Am�lior�

        [Fact]
        public void GetReport_WithFullMappingInfo_IncludesParentInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMappingWithParentContext(
                "OldProp", "string", "OldClass", "class",
                "NewProp", "string", "NewClass", "record",
                "Test migration"
            );

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("OldProp", report);
            Assert.Contains("Source Parent: OldClass", report);
            Assert.Contains("Target Parent: NewClass", report);
            Assert.Contains("Test migration", report);
        }

        [Fact]
        public void GetReport_WithSemanticAnalysis_ShowsConfiguration()
        {
            // Arrange
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            engine.AddMapping("A", "B");

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("Semantic Analysis: Enabled", report);
        }

        #endregion

        #region 16. Tests de D�sambigu�sation par Chemin de Types

        [Fact]
        public void AddMappingWithTypePath_StoresTypePathInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act - Mapping avec chemin de types complet
            engine.AddMappingWithTypePath(
                oldPropertyName: "Name",
                oldPropertyType: "string",
                oldTypePath: "Order.Sender",         // ? Chemin de types
                newPropertyPath: "Sender.Address.Name",
                newPropertyType: "string",
                description: "Sender Name to Address"
            );

            // Assert
            var mapping = engine.FindMapping("Name");
            Assert.NotNull(mapping);
            Assert.Equal("Order.Sender", mapping.Source.TypePath);
            Assert.Equal("Sender.Address.Name", mapping.NewPropertyPath);
        }

        [Fact]
        public void FindBestMapping_WithTypePath_ReturnsCorrectMapping()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            
            // Deux mappings pour "Name" avec des chemins de types diff�rents
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Sender",
                "Sender.Address.Name", "string"
            );
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Consignee",
                "Consignee.Address.Name", "string"
            );

            // Act
            var senderMapping = engine.FindBestMapping("Name", "string", "Sender", "Order.Sender");
            var consigneeMapping = engine.FindBestMapping("Name", "string", "Consignee", "Order.Consignee");

            // Assert
            Assert.NotNull(senderMapping);
            Assert.NotNull(consigneeMapping);
            Assert.Equal("Sender.Address.Name", senderMapping.NewPropertyPath);
            Assert.Equal("Consignee.Address.Name", consigneeMapping.NewPropertyPath);
        }

        [Fact]
        public void MappingMatchRule_Priority_TypePathHigherThanParent()
        {
            // Arrange
            var ruleWithTypePath = new MappingMatchRule
            {
                PropertyName = "Name",
                PropertyType = "string",
                TypePath = "Order.Sender"
            };
            var ruleWithParentOnly = new MappingMatchRule
            {
                PropertyName = "Name",
                PropertyType = "string",
                ParentType = "Sender"
            };
            var ruleWithNameOnly = new MappingMatchRule
            {
                PropertyName = "Name"
            };

            // Act & Assert
            Assert.True(ruleWithTypePath.Priority > ruleWithParentOnly.Priority);
            Assert.True(ruleWithParentOnly.Priority > ruleWithNameOnly.Priority);
        }

        [Fact]
        public void MappingMatchRule_Matches_ExactTypePath()
        {
            // Arrange
            var rule = new MappingMatchRule
            {
                PropertyName = "Name",
                PropertyType = "string",
                TypePath = "Order.Sender"
            };

            // Act & Assert
            Assert.True(rule.Matches("Name", "string", "Sender", "Order.Sender"));
            Assert.True(rule.Matches("Name", "string", "Sender", "MyApp.Order.Sender")); // Suffixe OK
            Assert.False(rule.Matches("Name", "string", "Consignee", "Order.Consignee"));
            Assert.False(rule.Matches("Address", "string", "Sender", "Order.Sender")); // Nom diff�rent
        }

        [Fact]
        public void TransformCode_WithTypePath_DisambiguatesCorrectly()
        {
            // Arrange - Cas r�el: Sender.Name et Consignee.Name doivent �tre mapp�s diff�remment
            // Note: Pour que cela fonctionne, les TypePath doivent correspondre exactement
            // au chemin syntaxique extrait (order.Sender, order.Consignee)
            var code = @"
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        // Ces deux 'Name' sont diff�rents et doivent �tre mapp�s diff�remment
        string senderName = order.Sender.Name;
        string consigneeName = order.Consignee.Name;
        
        Console.WriteLine($""From: {order.Sender.Name}, To: {order.Consignee.Name}"");
    }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            
            // Mapping avec chemin de types qui correspond au chemin syntaxique
            // Le typePath doit matcher le suffixe du chemin dans le code
            engine.AddMappingWithTypePath(
                "Name", "string", "Sender",  // Matche "*.Sender"
                "Address.FullName", "string"
            );
            engine.AddMappingWithTypePath(
                "Name", "string", "Consignee",  // Matche "*.Consignee"
                "Contact.Name", "string"
            );

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.ReplacementsCount >= 2, $"Expected at least 2 replacements, got {result.ReplacementsCount}");
            // V�rifie que les transformations ont �t� enregistr�es
            Assert.True(result.Changes.Count >= 2, $"Expected at least 2 changes, got: {string.Join(", ", result.Changes)}");
        }

        [Fact]
        public void TransformCode_FlatPropertiesToNested_TransformsCorrectly()
        {
            // Arrange - Migration de propri�t�s plates vers structure imbriqu�e
            var legacyCode = @"
public class LegacyOrderProcessor
{
    public void Process(Order order)
    {
        // Code legacy avec propri�t�s plates
        var senderName = order.SenderName;
        var senderAddress = order.SenderAddress;
        var consigneeName = order.ConsigneeName;
        var consigneeAddress = order.ConsigneeAddress;
    }
}
";
            var engine = new PropertyMappingEngine();
            
            // Mappings simples (noms uniques, pas besoin de TypePath)
            engine.AddMapping("SenderName", "Sender.Address.Name");
            engine.AddMapping("SenderAddress", "Sender.Address.Street");
            engine.AddMapping("ConsigneeName", "Consignee.Address.Name");
            engine.AddMapping("ConsigneeAddress", "Consignee.Address.Street");

            // Act
            var result = engine.TransformCode(legacyCode);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("order.Sender.Address.Name", result.TransformedCode);
            Assert.Contains("order.Sender.Address.Street", result.TransformedCode);
            Assert.Contains("order.Consignee.Address.Name", result.TransformedCode);
            Assert.Contains("order.Consignee.Address.Street", result.TransformedCode);
            Assert.DoesNotContain("SenderName", result.TransformedCode);
        }

        [Fact]
        public void GetReport_WithTypePath_ShowsTypePathInfo()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Sender",
                "Sender.Address.Name", "string",
                "Migration DDD"
            );

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("Type Path: Order.Sender", report);
            Assert.Contains("Match Priority:", report);
            Assert.Contains("Migration DDD", report);
        }

        [Fact]
        public void ValidateMappings_DuplicateTypePath_ReportsIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            
            // Deux mappings avec le m�me TypePath = conflit
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Sender",
                "Path1.Name", "string"
            );
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Sender",  // ? M�me TypePath
                "Path2.Name", "string"
            );

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.Contains(issues, i => i.Contains("Duplicate") && i.Contains("Order.Sender"));
        }

        #endregion

        #region 17. Tests de Priorit� de Matching

        [Fact]
        public void FindBestMapping_MostSpecificWins()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            
            // Mapping g�n�rique (faible priorit�)
            engine.AddMapping("Name", "DefaultName");
            
            // Mapping avec type parent (priorit� moyenne)
            engine.AddMappingWithParentContext(
                "Name", "string", null, "Sender",
                "SenderName", "string"
            );
            
            // Mapping avec TypePath complet (haute priorit�)
            engine.AddMappingWithTypePath(
                "Name", "string", "Order.Sender.Contact",
                "ContactName", "string"
            );

            // Act
            var genericMatch = engine.FindBestMapping("Name", null, null, null);
            var parentMatch = engine.FindBestMapping("Name", "string", "Sender", "Sender");
            var typePathMatch = engine.FindBestMapping("Name", "string", "Contact", "Order.Sender.Contact");

            // Assert
            Assert.Equal("DefaultName", genericMatch?.NewPropertyPath);
            Assert.Equal("ContactName", typePathMatch?.NewPropertyPath); // TypePath gagne
        }

        #endregion

        #region 18. Tests de Cas Limites et Edge Cases

        [Fact]
        public void TransformCode_EmptyCode_ReturnsError()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "NewName");

            // Act
            var result = engine.TransformCode("");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TransformCode_NoMappings_ReturnsOriginalCode()
        {
            // Arrange
            var code = "var x = obj.Name;";
            var engine = new PropertyMappingEngine();

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(code, result.TransformedCode);
            Assert.Contains("No mappings", result.Changes.First());
        }

        [Fact]
        public void TransformCode_NoMatchingProperties_ReturnsOriginalCode()
        {
            // Arrange
            var code = "var x = obj.UnknownProperty;";
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "NewName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.ReplacementsCount);
        }

        [Fact]
        public void AddMapping_NullOldName_ThrowsException()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => engine.AddMapping(null!, "NewName"));
        }

        [Fact]
        public void AddMapping_EmptyNewPath_ThrowsException()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => engine.AddMapping("OldName", ""));
        }

        [Fact]
        public void RemoveMapping_NonExistent_ReturnsFalse()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "NewName");

            // Act
            var result = engine.RemoveMapping("NonExistent");

            // Assert
            Assert.False(result);
            Assert.Single(engine.GetAllMappings());
        }

        #endregion

        #region 19. Tests de TypePath avec Cas Complexes

        [Fact]
        public void MappingMatchRule_Matches_PartialTypePath()
        {
            // Arrange - Le TypePath peut �tre un suffixe du chemin complet
            var rule = new MappingMatchRule
            {
                PropertyName = "Street",
                TypePath = "Address"
            };

            // Act & Assert
            Assert.True(rule.Matches("Street", null, "Address", "Order.Sender.Address"));
            Assert.True(rule.Matches("Street", null, "Address", "Customer.BillingAddress"));
            Assert.False(rule.Matches("Street", null, "Contact", "Order.Sender.Contact"));
        }

        [Fact]
        public void FindBestMapping_MultipleMatchesWithDifferentPriorities_ReturnsHighestPriority()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            
            // Ordre d'ajout n'importe pas, priorit� compte
            engine.AddMapping("City", "Location.City");  // Priorit� basse
            engine.AddMappingWithTypePath("City", "string", "Address", "Geo.City", "string");  // Priorit� haute

            // Act
            var simpleMatch = engine.FindBestMapping("City", null, null, null);
            var typePathMatch = engine.FindBestMapping("City", "string", "Address", "Order.Address");

            // Assert
            Assert.Equal("Location.City", simpleMatch?.NewPropertyPath);
            Assert.Equal("Geo.City", typePathMatch?.NewPropertyPath);
        }

        [Fact]
        public void TransformCode_ChainedMemberAccess_TransformsCorrectly()
        {
            // Arrange - Acc�s cha�n� profond
            var code = @"
public class Service
{
    public void Process(Order order)
    {
        var name = order.Customer.Address.City;
    }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            engine.AddMappingWithTypePath("City", "string", "Address", "Location.CityName", "string");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Location.CityName", result.TransformedCode);
        }

        [Fact]
        public void TransformCode_ThisKeyword_TransformsCorrectly()
        {
            // Arrange
            var code = @"
public class Person
{
    public string Name { get; set; }
    public void Display() { Console.WriteLine(this.Name); }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            engine.AddMapping("Name", "FullName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("this.FullName", result.TransformedCode);
        }

        #endregion

        #region 20. Tests d'Int�gration R�alistes

        [Fact]
        public void RealWorld_CompleteOrderMigration_TransformsAllProperties()
        {
            // Arrange - Sc�nario complet de migration Order legacy vers DDD
            var legacyCode = @"
public class OrderProcessor
{
    public void ProcessOrder(LegacyOrder order)
    {
        // Propri�t�s Sender
        var senderName = order.SenderName;
        var senderStreet = order.SenderStreet;
        var senderCity = order.SenderCity;
        
        // Propri�t�s Consignee
        var consigneeName = order.ConsigneeName;
        var consigneeStreet = order.ConsigneeStreet;
        var consigneeCity = order.ConsigneeCity;
        
        // M�tadonn�es
        var created = order.CreatedDate;
        var status = order.OrderStatus;
    }
}
";
            var engine = new PropertyMappingEngine();
            
            // Sender mappings
            engine.AddMapping("SenderName", "Sender.Contact.Name");
            engine.AddMapping("SenderStreet", "Sender.Address.Street");
            engine.AddMapping("SenderCity", "Sender.Address.City");
            
            // Consignee mappings
            engine.AddMapping("ConsigneeName", "Consignee.Contact.Name");
            engine.AddMapping("ConsigneeStreet", "Consignee.Address.Street");
            engine.AddMapping("ConsigneeCity", "Consignee.Address.City");
            
            // Metadata mappings
            engine.AddMapping("CreatedDate", "Metadata.CreatedAt");
            engine.AddMapping("OrderStatus", "Status.Current");

            // Act
            var result = engine.TransformCode(legacyCode);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(8, result.ReplacementsCount);
            
            // Verify Sender
            Assert.Contains("order.Sender.Contact.Name", result.TransformedCode);
            Assert.Contains("order.Sender.Address.Street", result.TransformedCode);
            Assert.Contains("order.Sender.Address.City", result.TransformedCode);
            
            // Verify Consignee
            Assert.Contains("order.Consignee.Contact.Name", result.TransformedCode);
            Assert.Contains("order.Consignee.Address.Street", result.TransformedCode);
            Assert.Contains("order.Consignee.Address.City", result.TransformedCode);
            
            // Verify Metadata
            Assert.Contains("order.Metadata.CreatedAt", result.TransformedCode);
            Assert.Contains("order.Status.Current", result.TransformedCode);
            
            // Verify no legacy properties remain
            Assert.DoesNotContain("SenderName", result.TransformedCode);
            Assert.DoesNotContain("ConsigneeName", result.TransformedCode);
        }

        [Fact]
        public void RealWorld_MixedMappingStrategies_WorksTogether()
        {
            // Arrange - Utilisation de diff�rentes strat�gies de mapping ensemble
            var code = @"
public class MigrationService
{
    public void Migrate(Order order)
    {
        // Simple mapping
        var id = order.OrderId;
        
        // Nested mapping
        var email = order.CustomerEmail;
        
        // TypePath mapping (sender vs consignee)
        var senderPhone = order.Sender.Phone;
        var consigneePhone = order.Consignee.Phone;
    }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            
            // Simple
            engine.AddMapping("OrderId", "Id");
            
            // Nested
            engine.AddMapping("CustomerEmail", "Customer.Contact.Email");
            
            // TypePath pour d�sambigu�ser Phone
            engine.AddMappingWithTypePath("Phone", "string", "Sender", "ContactInfo.PhoneNumber", "string");
            engine.AddMappingWithTypePath("Phone", "string", "Consignee", "ContactDetails.Phone", "string");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("order.Id", result.TransformedCode);
            Assert.Contains("order.Customer.Contact.Email", result.TransformedCode);
        }

        #endregion

        #region 21. Tests de Validation Avanc�e

        [Fact]
        public void ValidateMappings_NullableTypeCompatible_NoIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "Name", "string", "string?");

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.DoesNotContain(issues, i => i.Contains("Type mismatch"));
        }

        [Fact]
        public void ValidateMappings_EmptyPropertyName_ReportsIssue()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            var mapping = new PropertyMapping
            {
                Source = new SourcePropertyInfo { PropertyName = "" },
                Target = new TargetPropertyInfo { PropertyName = "Valid" }
            };
            engine.AddMapping(mapping);

            // Act
            var issues = engine.ValidateMappings();

            // Assert
            Assert.Contains(issues, i => i.Contains("empty"));
        }

        [Fact]
        public void GetReport_EmptyMappings_ShowsNoMappingsMessage()
        {
            // Arrange
            var engine = new PropertyMappingEngine();

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("No mappings defined", report);
        }

        [Fact]
        public void GetReport_WithValidationIssues_ShowsIssues()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "Name.First");  // Cycle potentiel

            // Act
            var report = engine.GetReport();

            // Assert
            Assert.Contains("Validation Issues", report);
            Assert.Contains("cycle", report.ToLower());
        }

        #endregion

        #region 22. Tests de SourcePropertyInfo et TargetPropertyInfo

        [Fact]
        public void SourcePropertyInfo_FullPath_CombinesParentAndProperty()
        {
            // Arrange
            var info = new SourcePropertyInfo
            {
                PropertyName = "Name",
                ParentName = "Customer"
            };

            // Act & Assert
            Assert.Equal("Customer.Name", info.FullPath);
        }

        [Fact]
        public void SourcePropertyInfo_FullPath_WithoutParent_ReturnsPropertyOnly()
        {
            // Arrange
            var info = new SourcePropertyInfo { PropertyName = "Name" };

            // Act & Assert
            Assert.Equal("Name", info.FullPath);
        }

        [Fact]
        public void SourcePropertyInfo_TypeQualifiedKey_IncludesTypePath()
        {
            // Arrange
            var info = new SourcePropertyInfo
            {
                PropertyName = "Name",
                PropertyType = "string",
                TypePath = "Order.Sender"
            };

            // Act
            var key = info.TypeQualifiedKey;

            // Assert
            Assert.Equal("Order.Sender.Name:string", key);
        }

        [Fact]
        public void SourcePropertyInfo_ToString_IncludesType()
        {
            // Arrange
            var info = new SourcePropertyInfo
            {
                PropertyName = "Age",
                PropertyType = "int",
                ParentName = "Person"
            };

            // Act
            var str = info.ToString();

            // Assert
            Assert.Contains("Person.Age", str);
            Assert.Contains("int", str);
        }

        [Fact]
        public void TargetPropertyInfo_FullPath_WithDeepNesting()
        {
            // Arrange
            var info = new TargetPropertyInfo
            {
                PropertyName = "Street",
                ParentName = "Customer.Address.Billing"
            };

            // Act & Assert
            Assert.Equal("Customer.Address.Billing.Street", info.FullPath);
        }

        #endregion

        #region 23. Tests de MappingDetector Avanc�s

        [Fact]
        public void MappingDetector_EmptyOldCode_ReturnsEmpty()
        {
            // Arrange
            var detector = new MappingDetector();

            // Act
            var suggestions = detector.DetectSuggestedMappings("", "public class A { public string Name { get; set; } }");

            // Assert
            Assert.Empty(suggestions);
        }

        [Fact]
        public void MappingDetector_EmptyNewCode_ReturnsEmpty()
        {
            // Arrange
            var detector = new MappingDetector();

            // Act
            var suggestions = detector.DetectSuggestedMappings("public class A { public string Name { get; set; } }", "");

            // Assert
            Assert.Empty(suggestions);
        }

        [Fact]
        public void MappingDetector_SimilarNames_FindsMatches()
        {
            // Arrange
            var oldCode = @"
public class OldModel
{
    public string CustomerName { get; set; }
    public string CustomerAddress { get; set; }
}
";
            var newCode = @"
public class NewModel
{
    public string Name { get; set; }
    public string Address { get; set; }
}
";
            var detector = new MappingDetector();

            // Act
            var suggestions = detector.DetectSuggestedMappings(oldCode, newCode);

            // Assert
            Assert.NotEmpty(suggestions);
        }

        [Fact]
        public void MappingDetector_FieldsAndProperties_ExtractsBoth()
        {
            // Arrange
            var code = @"
public class Mixed
{
    private string _name;
    public int Age { get; set; }
}
";
            var detector = new MappingDetector();

            // Act
            var suggestions = detector.DetectSuggestedMappings(code, code);

            // Assert
            Assert.True(suggestions.Count >= 2);
        }

        #endregion

        #region 24. Tests de Performance et Limites

        [Fact]
        public void TransformCode_LargeNumberOfMappings_PerformsCorrectly()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            for (int i = 0; i < 100; i++)
            {
                engine.AddMapping($"Prop{i}", $"NewProp{i}");
            }

            var code = @"
public class Test
{
    public void Method(Obj o)
    {
        var x = o.Prop50;
    }
}
";

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("NewProp50", result.TransformedCode);
        }

        [Fact]
        public void TransformCode_LargeCode_PerformsCorrectly()
        {
            // Arrange
            var engine = new PropertyMappingEngine();
            engine.AddMapping("Name", "NewName");

            // G�n�rer un code avec beaucoup de lignes
            var codeBuilder = new System.Text.StringBuilder();
            codeBuilder.AppendLine("public class Test {");
            for (int i = 0; i < 50; i++)
            {
                codeBuilder.AppendLine($"    public void Method{i}(Obj o) {{ var x = o.Name; }}");
            }
            codeBuilder.AppendLine("}");

            // Act
            var result = engine.TransformCode(codeBuilder.ToString());

            // Assert
            Assert.True(result.Success);
            Assert.Equal(50, result.ReplacementsCount);
        }

        #endregion

        #region 25. Tests de Configuration du Moteur

        [Fact]
        public void PropertyMappingEngine_CaseInsensitive_MatchesDifferentCases()
        {
            // Arrange
            var code = "var x = obj.NAME;";
            var engine = new PropertyMappingEngine(caseSensitive: false);
            engine.AddMapping("name", "newName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("newName", result.TransformedCode);
        }

        [Fact]
        public void PropertyMappingEngine_WholeWordFalse_MatchesPartialWords()
        {
            // Arrange
            var code = "var myNameValue = 1;";
            var engine = new PropertyMappingEngine(caseSensitive: true, wholeWordOnly: false);
            engine.AddMapping("Name", "FullName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("myFullNameValue", result.TransformedCode);
        }

        [Fact]
        public void PropertyMappingEngine_SemanticAnalysisEnabled_UsesRoslyn()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method(Obj o) { var x = o.Name; }
}
";
            var engine = new PropertyMappingEngine(useSemanticAnalysis: true);
            engine.AddMapping("Name", "NewName");

            // Act
            var result = engine.TransformCode(code);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("o.NewName", result.TransformedCode);
        }

        #endregion
    }
}
