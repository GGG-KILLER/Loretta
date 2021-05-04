using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Loretta.CodeAnalysis.Lua.Syntax;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Lua
{
    /// <summary>
    /// The base interface for scopes.
    /// </summary>
    [InternalImplementationOnly]
    public interface IScope
    {
        /// <summary>
        /// The kind of scope.
        /// </summary>
        ScopeKind Kind { get; }

        /// <summary>
        /// The syntax node that originated this scope.
        /// Not supported for the global scope.
        /// </summary>
        SyntaxNode? Node { get; }

        /// <summary>
        /// The parent scope (if any).
        /// </summary>
        IScope? Parent { get; }

        /// <summary>
        /// Contains the variables declared within the scope.
        /// As variables can be shadowed/redeclared, there may be multiple
        /// variables with the same name.
        /// </summary>
        IEnumerable<IVariable> DeclaredVariables { get; }

        /// <summary>
        /// Variables that are referenced by this scope.
        /// </summary>
        IEnumerable<IVariable> ReferencedVariables { get; }

        /// <summary>
        /// The goto labels contained within this scope.
        /// </summary>
        IEnumerable<IGotoLabel> GotoLabels { get; }
    }

    internal interface IScopeInternal : IScope
    {
        bool TryGetVariable(string name, [NotNullWhen(true)] out IVariableInternal? variable);
        IVariableInternal GetOrCreateVariable(VariableKind kind, string name, SyntaxNode? declaration = null);
        IVariableInternal CreateVariable(VariableKind kind, string name, SyntaxNode? declaration = null);
        void AddReferencedVariable(IVariableInternal variable);

        bool TryGetLabel(string name, [NotNullWhen(true)] out IGotoLabelInternal? label);
        IGotoLabelInternal GetOrCreateLabel(string name, GotoLabelStatementSyntax label);
        IGotoLabelInternal CreateLabel(string name, GotoLabelStatementSyntax label);
    }

    internal class Scope : IScopeInternal
    {
        protected readonly IDictionary<string, IVariableInternal> _variables = new Dictionary<string, IVariableInternal>(StringComparer.Ordinal);
        protected readonly ISet<IVariableInternal> _declaredVariables = new HashSet<IVariableInternal>();
        protected readonly ISet<IVariableInternal> _referencedVariables = new HashSet<IVariableInternal>();
        protected readonly IDictionary<string, IGotoLabelInternal> _labels = new Dictionary<string, IGotoLabelInternal>(StringComparer.Ordinal);

        public Scope(ScopeKind kind, SyntaxNode? node, IScopeInternal? parent)
        {
            Kind = kind;
            Node = node;
            Parent = parent;
            DeclaredVariables = SpecializedCollections.ReadOnlyEnumerable(_declaredVariables);
            ReferencedVariables = SpecializedCollections.ReadOnlyEnumerable(_referencedVariables);
            GotoLabels = SpecializedCollections.ReadOnlyEnumerable(_labels.Values);
        }

        public ScopeKind Kind { get; }

        public SyntaxNode? Node { get; }

        public IScopeInternal? Parent { get; }

        IScope? IScope.Parent => Parent;

        public IEnumerable<IVariableInternal> DeclaredVariables { get; }

        IEnumerable<IVariable> IScope.DeclaredVariables => DeclaredVariables;

        public IEnumerable<IVariableInternal> ReferencedVariables { get; }

        IEnumerable<IVariable> IScope.ReferencedVariables => ReferencedVariables;

        public IEnumerable<IGotoLabelInternal> GotoLabels { get; }

        IEnumerable<IGotoLabel> IScope.GotoLabels => GotoLabels;

        public bool TryGetVariable(string name, [NotNullWhen(true)] out IVariableInternal? variable) =>
            _variables.TryGetValue(name, out variable) || Parent?.TryGetVariable(name, out variable) is true;

        public IVariableInternal GetOrCreateVariable(VariableKind kind, string name, SyntaxNode? declaration = null)
        {
            RoslynDebug.Assert(Kind == ScopeKind.Global || kind != VariableKind.Global);
            RoslynDebug.Assert(!string.IsNullOrEmpty(name));

            if (!TryGetVariable(name, out var variable))
                variable = CreateVariable(kind, name, declaration);

            _referencedVariables.Add(variable);
            RoslynDebug.Assert(variable.Kind == kind);
            return variable;
        }

        public IVariableInternal CreateVariable(VariableKind kind, string name, SyntaxNode? declaration = null)
        {
            RoslynDebug.Assert(Kind == ScopeKind.Global || kind != VariableKind.Global);
            RoslynDebug.Assert(!string.IsNullOrEmpty(name));

            var variable = new Variable(kind, this, name, declaration);
            _variables[name] = variable;
            _declaredVariables.Add(variable);
            return variable;
        }

        public virtual void AddReferencedVariable(IVariableInternal variable)
        {
            if (_declaredVariables.Contains(variable))
                return;
            _referencedVariables.Add(variable);
            Parent?.AddReferencedVariable(variable);
        }

        public bool TryGetLabel(string name, [NotNullWhen(true)] out IGotoLabelInternal? label) =>
            _labels.TryGetValue(name, out label)
            || (Kind == ScopeKind.Block && Parent?.TryGetLabel(name, out label) is true);

        public IGotoLabelInternal GetOrCreateLabel(string name, GotoLabelStatementSyntax labelSyntax)
        {
            RoslynDebug.Assert(!string.IsNullOrEmpty(name));
            RoslynDebug.AssertNotNull(labelSyntax);

            if (!TryGetLabel(name, out var label))
                label = CreateLabel(name, labelSyntax);

            return label;
        }

        public IGotoLabelInternal CreateLabel(string name, GotoLabelStatementSyntax labelSyntax)
        {
            var label = new GotoLabel(name, labelSyntax);
            _labels[name] = label;
            return label;
        }
    }
}
