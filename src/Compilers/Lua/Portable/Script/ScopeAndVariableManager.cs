﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Loretta.CodeAnalysis.Lua
{
    internal partial class ScopeAndVariableManager
    {
        private readonly ImmutableArray<SyntaxTree> _trees;
        private State? _state;

        public ScopeAndVariableManager(ImmutableArray<SyntaxTree> trees)
        {
            _trees = trees;
        }

        public State GetLazyState()
        {
            if (_state is null)
            {
                Interlocked.CompareExchange(ref _state, CalculateState(_trees), null);
            }

            return _state;
        }

        private static State CalculateState(ImmutableArray<SyntaxTree> trees)
        {
            var rootScope = new Scope(ScopeKind.Global, null, null);
            var variables = ImmutableDictionary.CreateBuilder<SyntaxNode, IVariable>();
            var scopes = ImmutableDictionary.CreateBuilder<SyntaxNode, IScope>();
            var labels = ImmutableDictionary.CreateBuilder<SyntaxNode, IGotoLabel>();

            foreach (var tree in trees)
            {
                AddTree(
                    tree,
                    rootScope,
                    variables,
                    scopes,
                    labels);
            }

            return new State(
                rootScope,
                variables.ToImmutable(),
                scopes.ToImmutable(),
                labels.ToImmutable());
        }

        private static void AddTree(
            SyntaxTree tree,
            Scope rootScope,
            IDictionary<SyntaxNode, IVariable> variable,
            IDictionary<SyntaxNode, IScope> scopes,
            IDictionary<SyntaxNode, IGotoLabel> labels)
        {
            var node = tree.GetRoot();
            var fileScope = new FileScope(node, rootScope);
            var scopesAndVariableWalker = new ScopeAndVariableWalker(
                rootScope,
                fileScope,
                variable,
                scopes);
            scopesAndVariableWalker.Visit(node);
            var labelWalker = new GotoLabelWalker(scopes, labels);
            labelWalker.Visit(node);
            var gotoWalker = new GotoWalker(scopes, labels);
            gotoWalker.Visit(node);

        }
    }
}
