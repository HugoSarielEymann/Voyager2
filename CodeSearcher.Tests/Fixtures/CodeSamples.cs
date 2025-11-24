namespace CodeSearcher.Tests.Fixtures
{
    /// <summary>
    /// Classe contenant du code C# d'exemple pour les tests
    /// </summary>
    public static class CodeSamples
    {
        public const string SimpleClass = @"
using System;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class SimpleService
    {
        public string GetName()
        {
            return ""Test"";
        }

        public async Task<string> GetNameAsync()
        {
            return ""Test"";
        }

        private void InternalMethod()
        {
        }
    }
}
";

        public const string MultipleMethodsClass = @"
using System;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public class UserService
    {
        public User GetUserById(int id)
        {
            return null;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return null;
        }

        [Obsolete]
        public void OldMethod()
        {
        }

        private Task<bool> ValidateUserAsync(User user)
        {
            return Task.FromResult(true);
        }

        public void ProcessUser(User user)
        {
            if (user == null)
            {
                return;
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = ""Default"";
        private string _internalField = ""internal"";
        public readonly string ReadOnlyField = ""readonly"";
    }
}
";

        public const string ClassWithInheritance = @"
using System;

namespace MyApp.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
    }

    public sealed class ConcreteEntity : BaseEntity, ICloneable
    {
        public string Name { get; set; }
        public object Clone() => this.MemberwiseClone();
    }
}
";

        public const string ComplexReturnStatements = @"
using System;
using System.Threading.Tasks;

namespace MyApp.Logic
{
    public class DataProcessor
    {
        public string ProcessData(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            return input.ToUpper();
        }

        public Task<int> CountAsync(string[] items)
        {
            return Task.FromResult(items.Length);
        }

        public void NoReturn()
        {
            Console.WriteLine(""No return"");
        }
    }
}
";

        public const string WithAttributes = @"
using System;

namespace MyApp.API
{
    [Serializable]
    public class PersonDto
    {
        [Obsolete]
        public string OldProperty { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        private int _age;
    }
}
";
    }
}
