using System.Collections.Generic;

namespace Blaise2.Ast
{
    public abstract class AbstractAstNode
    {
        public static AbstractAstNode Empty { get; private set; } = new EmptyNode();

        public AbstractAstNode Parent { get; set; }
        public virtual string Type => GetType().Name;

        public bool IsEmpty() => Equals(Empty);

        private class EmptyNode : AbstractAstNode { }
    }

    public partial class ProgramNode : AbstractAstNode, IVarOwner
    {
        public string ProgramName { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public List<FunctionNode> Procedures { get; set; }
        public List<FunctionNode> Functions { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    public class VarDeclNode : AbstractAstNode
    {
        public string Identifier { get; set; }

        public BlaiseType BlaiseType { get; set; }

        public int Index { get; set; } = -1;
    }

    // file NT only references program, so no node

    // ProgramNode subsumes programDecl

    // ProgramNode subsumes varBlock

    // ProgramNode subsumes routines

    public partial class FunctionNode : AbstractAstNode, IVarOwner
    {
        public string Identifier { get; set; }
        public bool IsFunction { get; set; }
        public BlaiseType ReturnType { get; set; }
        // using VarDeclNode to store argument info, not to be a node in the AST
        public List<VarDeclNode> Params { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    // BlaiseType objects mean we don't need the typeExpr tree

    // stat is not needed as we'll just use the different statement nodes

    public class BlockNode : AbstractAstNode
    {
        public List<AbstractAstNode> Stats { get; set; }
    }

    // writeln is just a special case of write

    public class WriteNode : AbstractAstNode
    {
        public AbstractAstNode Expression { get; set; }
        public bool WriteNewline { get; set; }
    }

    public class AssignmentNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public AbstractAstNode Expression { get; set; }
    }

    // argsList is subsumed by ProcedureNode and FunctionNode

    // RealNode and IntegerNode will be used directly instead of numericAtom

    public class FunctionCallNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public bool IsFunction { get; set; }
        public List<AbstractAstNode> Arguments { get; set; }
    }

    // expression is not needed as we'll just use the individual alts' nodes

    public class IntegerNode : AbstractAstNode
    {
        public int IntValue { get; set; }
    }

    public class RealNode : AbstractAstNode
    {
        public double RealValue { get; set; }
    }

    public class VarRefNode : AbstractAstNode
    {
        public string Identifier { get; set; }
    }

    public class BooleanNode : AbstractAstNode
    {
        public bool BoolValue { get; set; }
    }

    public class CharNode : AbstractAstNode
    {
        public char CharValue { get; set; }
    }

    public class StringNode : AbstractAstNode
    {
        public string StringValue { get; set; }
    }

    public class BinaryOpNode : AbstractAstNode
    {
        public AbstractAstNode Left { get; set; }
        public AbstractAstNode Right { get; set; }
        public BinaryOperator Operator { get; set; }
    }

    public class BooleanOpNode : AbstractAstNode
    {
        public AbstractAstNode Left { get; set; }
        public AbstractAstNode Right { get; set; }
        public BooleanOperator Operator { get; set; }
    }
}