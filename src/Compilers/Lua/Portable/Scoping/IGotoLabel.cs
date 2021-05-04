using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Loretta.CodeAnalysis.Lua.Syntax;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Lua
{
    /// <summary>
    /// The interface for a goto label.
    /// </summary>
    [InternalImplementationOnly]
    public interface IGotoLabel
    {
        /// <summary>
        /// The label's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The label's location.
        /// </summary>
        GotoLabelStatementSyntax LabelSyntax { get; }

        /// <summary>
        /// The nodes that jump to this label.
        /// </summary>
        IEnumerable<GotoStatementSyntax> JumpSyntaxes { get; }
    }

    internal interface IGotoLabelInternal : IGotoLabel
    {
        void AddJump(GotoStatementSyntax jump);
    }

    internal class GotoLabel : IGotoLabelInternal
    {
        private readonly IList<GotoStatementSyntax> _jumps = new List<GotoStatementSyntax>();

        public GotoLabel(string name, GotoLabelStatementSyntax label)
        {
            RoslynDebug.Assert(!string.IsNullOrEmpty(name));
            RoslynDebug.AssertNotNull(label);

            Name = name;
            LabelSyntax = label;
            JumpSyntaxes = SpecializedCollections.ReadOnlyEnumerable(_jumps);
        }

        public string Name { get; }

        public GotoLabelStatementSyntax LabelSyntax { get; }

        public IEnumerable<GotoStatementSyntax> JumpSyntaxes { get; }

        public void AddJump(GotoStatementSyntax jump)
        {
            RoslynDebug.AssertNotNull(jump);
            _jumps.Add(jump);
        }
    }
}
