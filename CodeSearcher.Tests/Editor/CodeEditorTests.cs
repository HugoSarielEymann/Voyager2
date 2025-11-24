using CodeSearcher.Editor;

namespace CodeSearcher.Tests.Editor
{
    public class CodeEditorTests
    {
        private const string SampleCode = @"
using System;

namespace TestApp
{
    public class UserService
    {
        public User GetUser(int id)
        {
            return new User { Id = id, Name = ""Default"" };
        }

        public void ProcessUser(User user)
        {
            Console.WriteLine(user.Name);
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
";

        [Fact]
        public void RenameMethod_SuccessfullyRenamesMethod()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor.RenameMethod("GetUser", "FetchUser").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("FetchUser", result.ModifiedCode);
            Assert.DoesNotContain("GetUser(", result.ModifiedCode);
        }

        [Fact]
        public void RenameClass_SuccessfullyRenamesClass()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor.RenameClass("User", "Person").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("class Person", result.ModifiedCode);
            Assert.Contains("new Person", result.ModifiedCode);
        }

        [Fact]
        public void RenameProperty_SuccessfullyRenamesProperty()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor.RenameProperty("Name", "FullName").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("FullName", result.ModifiedCode);
        }

        [Fact]
        public void Replace_SuccessfullyReplacesCodeSnippet()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor
                .Replace(
                    "new User { Id = id, Name = \"Default\" }",
                    "CreateDefaultUser(id)"
                )
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("CreateDefaultUser(id)", result.ModifiedCode);
        }

        [Fact]
        public void ChainedOperations_SuccessfullyAppliesMultipleOperations()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor
                .RenameMethod("GetUser", "FetchUser")
                .RenameMethod("ProcessUser", "HandleUser")
                .RenameClass("User", "Person")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("FetchUser", result.ModifiedCode);
            Assert.Contains("HandleUser", result.ModifiedCode);
            Assert.Contains("class Person", result.ModifiedCode);
            Assert.Equal(3, result.Changes.Count);
        }

        [Fact]
        public void SaveToFile_SavesModifiedCode()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cs");

            try
            {
                // Act
                editor.RenameMethod("GetUser", "FetchUser");
                var applyResult = editor.Apply();
                editor.SaveToFile(tempFile);

                // Assert
                Assert.True(File.Exists(tempFile));
                var savedContent = File.ReadAllText(tempFile);
                Assert.Contains("FetchUser", savedContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Reset_RestoresOriginalCode()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);
            var originalCode = editor.GetCode();

            // Act
            editor.RenameMethod("GetUser", "FetchUser");
            editor.Reset();
            var resetCode = editor.GetCode();

            // Assert
            Assert.Equal(originalCode, resetCode);
        }

        [Fact]
        public void Clear_ResetsToOriginalCode()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            editor.RenameMethod("GetUser", "FetchUser");
            var result = editor.Apply();
            var modifiedCode = editor.GetCode();
            editor.Clear();
            var clearedCode = editor.GetCode();

            // Assert
            Assert.NotNull(result.ModifiedCode);
            // Clear should reset to original code
            Assert.Equal(SampleCode, clearedCode);
            Assert.NotEqual(modifiedCode, clearedCode);
        }

        [Fact]
        public void GetChangeLog_ReturnsAllChanges()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            editor
                .RenameMethod("GetUser", "FetchUser")
                .RenameClass("User", "Person")
                .Apply();

            var changeLog = editor.GetChangeLog();

            // Assert
            Assert.NotEmpty(changeLog);
            Assert.Contains("Renamed method", changeLog[0]);
            Assert.Contains("Renamed class", changeLog[1]);
        }

        [Fact]
        public void InvalidOperation_ReturnsFailureResult()
        {
            // Arrange
            var editor = CodeEditor.FromCode(SampleCode);

            // Act
            var result = editor.Replace("NonExistentCode", "NewCode").Apply();

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
