using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Lua
{
    /// <summary>
    /// A function's scope.
    /// </summary>
    [InternalImplementationOnly]
    public interface IFunctionScope : IScope
    {
        /// <summary>
        /// The parameters
        /// </summary>
        IEnumerable<IVariable> Parameters { get; }

        /// <summary>
        /// Contains the variables that are captured by this scope.
        /// Variables captured by the scope are variables that weren't declared
        /// on the scope but are used in it.
        /// </summary>
        IEnumerable<IVariable> CapturedVariables { get; }
    }

    internal interface IFunctionScopeInternal : IScopeInternal, IFunctionScope
    {
        IVariableInternal AddParameter(string name, SyntaxNode declaration);
    }

    internal class FunctionScope : Scope, IFunctionScopeInternal
    {
        private readonly IList<IVariableInternal> _parameters = new List<IVariableInternal>();
        private readonly HashSet<IVariableInternal> _capturedVariables = new HashSet<IVariableInternal>();

        public FunctionScope(SyntaxNode node, IScopeInternal? parent) : base(ScopeKind.Function, node, parent)
        {
            Parameters = SpecializedCollections.ReadOnlyEnumerable(_parameters);
            CapturedVariables = SpecializedCollections.ReadOnlyEnumerable(_capturedVariables);
        }

        public IEnumerable<IVariableInternal> Parameters { get; }

        IEnumerable<IVariable> IFunctionScope.Parameters => Parameters;

        public IEnumerable<IVariableInternal> CapturedVariables { get; }

        IEnumerable<IVariable> IFunctionScope.CapturedVariables => CapturedVariables;

        public IVariableInternal AddParameter(string name, SyntaxNode declaration)
        {
            var parameter = CreateVariable(VariableKind.Parameter, name, declaration);
            _parameters.Add(parameter);
            return parameter;
        }

        public override void AddReferencedVariable(IVariableInternal variable)
        {
            if (_declaredVariables.Contains(variable))
                return;
            _capturedVariables.Add(variable);
            variable.AddCapturingScope(this);
            base.AddReferencedVariable(variable);
        }
    }
}
