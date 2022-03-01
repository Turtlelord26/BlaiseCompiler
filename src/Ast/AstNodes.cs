using System.Collections.Generic;
using static Blaise2.Ast.BlaiseTypeEnum;

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
        public string Identifier { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public List<FunctionNode> Procedures { get; set; }
        public List<FunctionNode> Functions { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    public class VarDeclNode : AbstractAstNode
    {
        public string Identifier { get; set; }

        public BlaiseType BlaiseType { get; set; }
    }

    public partial class FunctionNode : ProgramNode
    {
        public bool IsFunction { get; set; }
        public BlaiseType ReturnType { get; set; }
        public List<VarDeclNode> Params { get; set; }
    }

    public class BlockNode : AbstractAstNode
    {
        public List<AbstractAstNode> Stats { get; set; }
    }

    public class WriteNode : AbstractAstNode
    {
        public AbstractAstNode Expression { get; set; }
        public bool WriteNewline { get; set; }
    }

    public class AssignmentNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public AbstractAstNode Expression { get; set; }
        public SymbolInfo VarInfo { get; set; }
    }

    public class LoopNode : AbstractAstNode
    {
        public LoopType LoopType { get; set; }
        public AbstractAstNode Condition { get; set; }
        public AbstractAstNode Body { get; set; }
    }

    public partial class ForLoopNode : LoopNode
    {
        public AssignmentNode Assignment { get; set; }
        public AssignmentNode Iteration { get; set; }
    }

    public class BinaryOpNode : AbstractAstNode, ITypedNode
    {
        public ITypedNode Left { get; set; }
        public ITypedNode Right { get; set; }
        public BlaiseOperator Operator { get; set; }
        public BlaiseType ExprType { get; set; }
        public BlaiseType LeftType { get; set; }
        public BlaiseType RightType { get; set; }

        public BlaiseType GetExprType() => ExprType;
    }

    public class BooleanOpNode : AbstractAstNode, ITypedNode
    {
        public ITypedNode Left { get; set; }
        public ITypedNode Right { get; set; }
        public BlaiseOperator Operator { get; set; }
        public BlaiseType LeftType { get; set; }
        public BlaiseType RightType { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = BOOLEAN
        };
    }

    public class FunctionCallNode : AbstractAstNode, ITypedNode
    {
        public string Identifier { get; set; }
        public bool IsFunction { get; set; }
        public List<ITypedNode> Arguments { get; set; }
        public FunctionNode CallTarget { get; set; }

        public BlaiseType GetExprType() => CallTarget.ReturnType;
    }

    public class IntegerNode : AbstractAstNode, ITypedNode
    {
        public int IntValue { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = INTEGER
        };
    }

    public class RealNode : AbstractAstNode, ITypedNode
    {
        public double RealValue { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = REAL
        };
    }

    public class VarRefNode : AbstractAstNode, ITypedNode
    {
        public string Identifier { get; set; }
        public SymbolInfo VarInfo { get; set; }

        public BlaiseType GetExprType() => VarInfo?.VarDecl.BlaiseType ?? BlaiseType.ErrorType;
    }

    public class BooleanNode : AbstractAstNode, ITypedNode
    {
        public bool BoolValue { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = BOOLEAN
        };
    }

    public class CharNode : AbstractAstNode, ITypedNode
    {
        public char CharValue { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = CHAR
        };
    }

    public class StringNode : AbstractAstNode, ITypedNode
    {
        public string StringValue { get; set; }

        public BlaiseType GetExprType() => new()
        {
            BasicType = STRING
        };
    }
}