using System.Collections.Generic;

namespace Blaise2.Ast
{
    public abstract class AbstractAstNode
    {
        public AbstractAstNode Parent { get; set; }
        public virtual string Type => GetType().Name;
    }

    public partial class ProgramNode : AbstractAstNode, IVarOwner
    {
        public string ProgramName { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public List<ProcedureNode> Procedures { get; set; }
        public List<FunctionNode> Functions { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    public partial class VarDeclNode : AbstractAstNode
    {
        public string Identifier { get; set; }

        public BlaiseType BlaiseType { get; set; }

        public int Index { get; set; } = -1;
    }

    // file NT only references program, so no node

    // ProgramNode subsumes programDecl

    // ProgramNode subsumes varBlock

    // ProgramNode subsumes routines

    public partial class ProcedureNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        // using VarDeclNode to store argument info, not to be a node in the AST
        public List<VarDeclNode> Args { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    public partial class FunctionNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public BlaiseType ReturnType { get; set; }
        // using VarDeclNode to store argument info, not to be a node in the AST
        public List<VarDeclNode> Args { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    // BlaiseType objects mean we don't need the typeExpr tree

    // stat is not needed as we'll just use the different statement nodes

    public partial class BlockNode : AbstractAstNode
    {
        public List<AbstractAstNode> Stats { get; set; }
    }

    // writeln is just a special case of write

    public partial class WriteNode : AbstractAstNode
    {
        public AbstractAstNode Expression { get; set; }
        public bool WriteNewline { get; set; }
    }

    public partial class AssignmentNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public AbstractAstNode Expression { get; set; }
    }

    // argsList is subsumed by ProcedureNode and FunctionNode

    // RealNode and IntegerNode will be used directly instead of numericAtom

    public partial class FunctionCallNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public List<AbstractAstNode> ArgumentExpressions { get; set; }
    }

    // expression is not needed as we'll just use the individual alts' nodes

    public partial class IntegerNode : AbstractAstNode
    {
        public int IntValue { get; set; }
    }

    public partial class RealNode : AbstractAstNode
    {
        public double RealValue { get; set; }
    }

    public partial class StringNode : AbstractAstNode
    {
        public string StringValue { get; set; }
    }

    public partial class VarRefNode : AbstractAstNode
    {
        public string Identifier { get; set; }
    }

    public partial class BinaryOpNode : AbstractAstNode
    {
        public AbstractAstNode Lhs { get; set; }
        public AbstractAstNode Rhs { get; set; }
        public string Op { get; set; }
    }

    public partial class BooleanOpNode : AbstractAstNode
    {
        public AbstractAstNode Lhs { get; set; }
        public AbstractAstNode Rhs { get; set; }
        public string Op { get; set; }
    }
}