using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public abstract class AbstractAstNode
    {
        public static AbstractAstNode Empty { get; private set; } = new EmptyNode();

        public AbstractAstNode Parent { get; set; }
        public virtual string Type => GetType().Name;

        public bool IsEmpty() => Equals(Empty);

        private class EmptyNode : AbstractTypedAstNode
        {
            public override BlaiseType GetExprType() => BlaiseType.ErrorType;
        }
    }

    public abstract class AbstractTypedAstNode : AbstractAstNode
    {
        public abstract BlaiseType GetExprType();
    }

    public class ProgramNode : AbstractAstNode, IVarOwner
    {
        public string Identifier { get; set; }
        public List<VarDeclNode> VarDecls { get; set; }
        public List<FunctionNode> Procedures { get; set; }
        public List<FunctionNode> Functions { get; set; }
        public AbstractAstNode Stat { get; set; }

        public virtual SymbolInfo GetVarByName(string name) => VarDecls.Where(v => v.Identifier == name)
                                                                       .Select(v => new SymbolInfo { VarType = VarType.Global, VarDecl = v })
                                                                       .FirstOrDefault();
    }

    public class VarDeclNode : AbstractAstNode
    {
        public string Identifier { get; set; }

        public BlaiseType BlaiseType { get; set; }
    }

    public class FunctionNode : ProgramNode
    {
        public bool IsFunction { get; set; }
        public BlaiseType ReturnType { get; set; }
        public List<VarDeclNode> Params { get; set; }

        public override SymbolInfo GetVarByName(string name) => VarDecls.Where(v => v.Identifier == name)
                                                                        .Select(v => new SymbolInfo { VarType = VarType.Local, VarDecl = v })
                                                                        .FirstOrDefault()
                                                                ?? Params.Where(p => p.Identifier == name)
                                                                         .Select(p => new SymbolInfo { VarType = VarType.Argument, VarDecl = p })
                                                                         .FirstOrDefault();
    }

    public class BlockNode : AbstractAstNode
    {
        public List<AbstractAstNode> Stats { get; set; }
    }

    public class WriteNode : AbstractAstNode
    {
        public AbstractTypedAstNode Expression { get; set; }
        public bool WriteNewline { get; set; }
    }

    public class AssignmentNode : AbstractAstNode
    {
        public string Identifier { get; set; }
        public AbstractTypedAstNode Expression { get; set; }
        public SymbolInfo VarInfo { get; set; }
    }

    public class IfNode : AbstractAstNode
    {
        public AbstractTypedAstNode Condition { get; set; }
        public AbstractAstNode ThenStat { get; set; }
        public AbstractAstNode ElseStat { get; set; }
    }

    public class LoopNode : AbstractAstNode
    {
        public LoopType LoopType { get; set; }
        public AbstractTypedAstNode Condition { get; set; }
        public AbstractAstNode Body { get; set; }
    }

    public partial class ForLoopNode : LoopNode
    {
        public AssignmentNode Assignment { get; set; }
    }

    public class SwitchNode : AbstractAstNode
    {
        public AbstractTypedAstNode Input { get; set; }
        public List<SwitchCaseNode> Cases { get; set; }
        public AbstractAstNode Default { get; set; }
    }

    public class SwitchCaseNode : AbstractAstNode
    {
        public AbstractTypedAstNode Case { get; set; }
        public AbstractAstNode Stat { get; set; }
    }

    public class ReturnNode : AbstractAstNode
    {
        public AbstractTypedAstNode Expression { get; set; }
    }

    public class BinaryOpNode : AbstractTypedAstNode
    {
        public AbstractTypedAstNode Left { get; set; }
        public AbstractTypedAstNode Right { get; set; }
        public BlaiseOperator Operator { get; set; }
        public BlaiseType ExprType { get; set; }
        public BlaiseType LeftType { get; set; }
        public BlaiseType RightType { get; set; }

        public override BlaiseType GetExprType() => ExprType;
    }

    public class BooleanOpNode : BinaryOpNode
    {
        public override BlaiseType GetExprType() => new()
        {
            BasicType = BOOLEAN
        };
    }

    public class LogicalOpNode : BooleanOpNode { }

    public class NotOpNode : AbstractTypedAstNode
    {
        public AbstractTypedAstNode Expression { get; set; }

        public override BlaiseType GetExprType() => new BlaiseType()
        {
            BasicType = BOOLEAN
        };
    }

    public class FunctionCallNode : AbstractTypedAstNode
    {
        public string Identifier { get; set; }
        public bool IsFunction { get; set; }
        public List<AbstractTypedAstNode> Arguments { get; set; }
        public FunctionNode CallTarget { get; set; }

        public override BlaiseType GetExprType() => CallTarget.ReturnType;
    }

    public class IntegerNode : AbstractTypedAstNode, IConstantNode
    {
        public int IntValue { get; set; }

        public AstConstant GetConstant() => new(IntValue);

        public override BlaiseType GetExprType() => new()
        {
            BasicType = INTEGER
        };
    }

    public class RealNode : AbstractTypedAstNode, IConstantNode
    {
        public double RealValue { get; set; }

        public AstConstant GetConstant() => new(RealValue);

        public override BlaiseType GetExprType() => new()
        {
            BasicType = REAL
        };
    }

    public class VarRefNode : AbstractTypedAstNode
    {
        public string Identifier { get; set; }
        public SymbolInfo VarInfo { get; set; }

        public override BlaiseType GetExprType() => VarInfo?.VarDecl.BlaiseType ?? BlaiseType.ErrorType;
    }

    public class BooleanNode : AbstractTypedAstNode, IConstantNode
    {
        public bool BoolValue { get; set; }

        public AstConstant GetConstant() => new(BoolValue);

        public override BlaiseType GetExprType() => new()
        {
            BasicType = BOOLEAN
        };
    }

    public class CharNode : AbstractTypedAstNode, IConstantNode
    {
        public char CharValue { get; set; }

        public AstConstant GetConstant() => new(CharValue);

        public override BlaiseType GetExprType() => new()
        {
            BasicType = CHAR
        };
    }

    public class StringNode : AbstractTypedAstNode, IConstantNode
    {
        public string StringValue { get; set; }

        public AstConstant GetConstant() => new(StringValue);

        public override BlaiseType GetExprType() => new()
        {
            BasicType = STRING
        };
    }
}