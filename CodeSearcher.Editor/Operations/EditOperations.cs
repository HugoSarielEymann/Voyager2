using CodeSearcher.Editor.Abstractions;
using CodeSearcher.Editor.Strategies;

namespace CodeSearcher.Editor.Operations
{
    /// <summary>
    /// Opération pour renommer une entité
    /// </summary>
    public class RenameOperation : IEditOperation
    {
        private readonly string _oldName;
        private readonly string _newName;
        private readonly string _entityType;
        private readonly RenameStrategy _strategy;

        public string Description => $"Rename {_entityType} '{_oldName}' to '{_newName}'";

        public RenameOperation(string oldName, string newName, string entityType)
        {
            _oldName = oldName;
            _newName = newName;
            _entityType = entityType;
            _strategy = new RenameStrategy();
        }

        public EditResult Execute(string code)
        {
            return _strategy.Rename(code, _oldName, _newName, _entityType);
        }
    }

    /// <summary>
    /// Opération pour wrapper du code
    /// </summary>
    public class WrapOperation : IEditOperation
    {
        private readonly string _methodName;
        private readonly string _wrapperType;
        private readonly string _wrapperCode;
        private readonly WrapperStrategy _strategy;

        public string Description => $"Wrap method '{_methodName}' with {_wrapperType}";

        public WrapOperation(string methodName, string wrapperType, string wrapperCode = "")
        {
            _methodName = methodName;
            _wrapperType = wrapperType;
            _wrapperCode = wrapperCode ?? "";
            _strategy = new WrapperStrategy();
        }

        public EditResult Execute(string code)
        {
            return _strategy.Wrap(code, _methodName, _wrapperType, _wrapperCode);
        }
    }

    /// <summary>
    /// Opération pour remplacer du code
    /// </summary>
    public class ReplaceOperation : IEditOperation
    {
        private readonly string _oldCode;
        private readonly string _newCode;

        public string Description => "Replace code snippet";

        public ReplaceOperation(string oldCode, string newCode)
        {
            _oldCode = oldCode;
            _newCode = newCode;
        }

        public EditResult Execute(string code)
        {
            if (!code.Contains(_oldCode))
            {
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = "Code snippet not found"
                };
            }

            var modifiedCode = code.Replace(_oldCode, _newCode);

            return new EditResult
            {
                Success = true,
                ModifiedCode = modifiedCode,
                Changes = new() { "Replaced code snippet" }
            };
        }
    }
}
