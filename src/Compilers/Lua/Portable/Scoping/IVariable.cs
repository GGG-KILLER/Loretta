using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Lua
{
    /// <summary>
    /// The base interface for variables
    /// </summary>
    [InternalImplementationOnly]
    public interface IVariable
    {
        /// <summary>
        /// The kind of this variable.
        /// </summary>
        VariableKind Kind { get; }

        /// <summary>
        /// The containing scope.
        /// </summary>
        IScope ContainingScope { get; }

        /// <summary>
        /// The variable's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The node where this variable is declared.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> if it is a global or implicit variable.
        /// </remarks>
        SyntaxNode? Declaration { get; }

        /// <summary>
        /// The scopes that reference this variable.
        /// </summary>
        IEnumerable<IScope> ReferencingScopes { get; }

        /// <summary>
        /// All scopes that capture this variable as an upvalue.
        /// </summary>
        IEnumerable<IScope> CapturingScopes { get; }

        /// <summary>
        /// All locations this variable is read from.
        /// </summary>
        IEnumerable<SyntaxNode> ReadLocations { get; }

        /// <summary>
        /// All locations this variable is written to.
        /// </summary>
        IEnumerable<SyntaxNode> WriteLocations { get; }
    }

    internal interface IVariableInternal : IVariable
    {
        void AddReferencingScope(IScopeInternal scope);
        void AddCapturingScope(IScopeInternal scope);
        void AddReadLocation(SyntaxNode node);
        void AddWriteLocation(SyntaxNode node);
    }

    internal class Variable : IVariableInternal
    {
        private readonly ISet<IScopeInternal> _referencingScopes = new HashSet<IScopeInternal>();
        private readonly ISet<IScopeInternal> _capturingScopes = new HashSet<IScopeInternal>();
        private readonly IList<SyntaxNode> _readLocations = new List<SyntaxNode>();
        private readonly IList<SyntaxNode> _writeLocations = new List<SyntaxNode>();

        public Variable(VariableKind kind, IScopeInternal containingScope, string name, SyntaxNode? declaration)
        {
            RoslynDebug.AssertNotNull(containingScope);
            RoslynDebug.AssertNotNull(name);

            Kind = kind;
            ContainingScope = containingScope;
            Name = name;
            Declaration = declaration;
            ReferencingScopes = SpecializedCollections.ReadOnlyEnumerable(_referencingScopes);
            CapturingScopes = SpecializedCollections.ReadOnlyEnumerable(_capturingScopes);
            ReadLocations = SpecializedCollections.ReadOnlyEnumerable(_readLocations);
            WriteLocations = SpecializedCollections.ReadOnlyEnumerable(_writeLocations);
        }

        public VariableKind Kind { get; }

        public IScopeInternal ContainingScope { get; }

        IScope IVariable.ContainingScope => ContainingScope;

        public string Name { get; }

        public SyntaxNode? Declaration { get; }

        public IEnumerable<IScopeInternal> ReferencingScopes { get; }

        IEnumerable<IScope> IVariable.ReferencingScopes => ReferencingScopes;

        public IEnumerable<IScopeInternal> CapturingScopes { get; }

        IEnumerable<IScope> IVariable.CapturingScopes => CapturingScopes;

        public IEnumerable<SyntaxNode> ReadLocations { get; }

        public IEnumerable<SyntaxNode> WriteLocations { get; }

        public void AddReferencingScope(IScopeInternal scope) =>
            _referencingScopes.Add(scope);

        public void AddCapturingScope(IScopeInternal scope) =>
            _capturingScopes.Add(scope);

        public void AddReadLocation(SyntaxNode node) =>
            _readLocations.Add(node);

        public void AddWriteLocation(SyntaxNode node) =>
            _writeLocations.Add(node);
    }
}
