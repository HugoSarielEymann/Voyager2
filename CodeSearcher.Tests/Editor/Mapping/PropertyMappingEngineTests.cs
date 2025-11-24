using CodeSearcher.Editor.Mapping;
using Xunit;

namespace CodeSearcher.Tests.Editor.Mapping
{
    /// <summary>
    /// Tests pour le système de Property Mapping
    /// Valide que le système peut mapper automatiquement les propriétés legacy vers les nouvelles
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

        #region 2. Tests de Mapping Imbriqué (Nested)

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

        #region 7. Tests de Gestion d'État

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

        #region 8. Cas Réels de Migration

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
            Assert.Equal(6, result.ReplacementsCount);  // 6 occurrences (2x chaque: déclaration + utilisation)
        }

        [Fact]
        public void RealWorld_DatabaseSchemaRefactor_TransformsComplexMappings()
        {
            // Arrange - Migration schéma dénormalisé ? normalisé
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
            Assert.Contains("OldName", result.TransformedCode);  // Pas changé (différent cas)
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
            Assert.Contains("firstName", result.TransformedCode);  // Ne contient pas oldName, pas changé
        }

        #endregion
    }
}
