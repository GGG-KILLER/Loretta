using System.Runtime.CompilerServices;

namespace Loretta.CodeAnalysis.Lua
{
    /// <summary>
    /// A file's scope.
    /// </summary>
    [InternalImplementationOnly]
    public interface IFileScope : IScope
    {
        /// <summary>
        /// The implicit <c>args</c> that's available in all files.
        /// </summary>
        IVariable ArgsVariable { get; }

        /// <summary>
        /// The implicit vararg that's available in all files
        /// </summary>
        IVariable VarArgParameter { get; }
    }

    internal class FileScope : Scope, IFileScope
    {
        public FileScope(SyntaxNode node, IScopeInternal? parent) : base(ScopeKind.File, node, parent)
        {
            ArgsVariable = CreateVariable(VariableKind.Parameter, "args");
            VarArgParameter = CreateVariable(VariableKind.Parameter, "...");
        }

        public IVariableInternal ArgsVariable { get; }

        IVariable IFileScope.ArgsVariable => ArgsVariable;

        public IVariableInternal VarArgParameter { get; }

        IVariable IFileScope.VarArgParameter => VarArgParameter;
    }
}
