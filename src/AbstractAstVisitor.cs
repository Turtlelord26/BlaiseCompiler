using System;

namespace Blaise2.Ast
{
    public abstract class AbstractAstVisitor<R>
    {
        public R VisitStatement(AbstractAstNode node) => node switch
        {
            AssignmentNode assign => VisitAssignment(assign),
            WriteNode write => VisitWrite(write),
            FunctionCallNode call => VisitCall(call),
            IfNode ifStat => VisitIf(ifStat),
            ForLoopNode forLoop => VisitForLoop(forLoop),
            LoopNode loop => VisitLoop(loop),
            SwitchNode switchStat => VisitSwitch(switchStat),
            ReturnNode ret => VisitReturn(ret),
            BlockNode block => VisitBlock(block),
            AbstractAstNode aan when aan.IsEmpty() => VisitEmpty(aan),
            _ => throw new InvalidOperationException($"Unrecognized node {node.GetType()} encountered while emitting.")
        };

        public R VisitExpression(AbstractTypedAstNode node) => node switch
        {
            LogicalOpNode logop => VisitLogicalOperator(logop),
            BooleanOpNode boolop => VisitBooleanOperator(boolop),
            BinaryOpNode binop => VisitBinaryOperator(binop),
            NotOpNode notop => VisitNotOperator(notop),
            FunctionCallNode call => VisitCall(call),
            VarRefNode varref => VisitVarRef(varref),
            BooleanNode booln => VisitBoolean(booln),
            CharNode charn => VisitChar(charn),
            IntegerNode intn => VisitInteger(intn),
            RealNode real => VisitReal(real),
            StringNode stringn => VisitString(stringn),
            AbstractTypedAstNode atan when atan.IsEmpty() => VisitEmpty(atan),
            _ => throw new InvalidOperationException($"Unrecognized node {node.GetType()} encountered while emitting.")
        };
        public abstract R VisitProgram(ProgramNode node);

        public abstract R VisitVarDecl(VarDeclNode node);

        public abstract R VisitFunction(FunctionNode node);

        public abstract R VisitBlock(BlockNode node);

        public abstract R VisitWrite(WriteNode node);

        public abstract R VisitAssignment(AssignmentNode node);

        public abstract R VisitReturn(ReturnNode node);

        public abstract R VisitCall(FunctionCallNode node);

        public abstract R VisitIf(IfNode node);

        public abstract R VisitLoop(LoopNode node);

        public abstract R VisitForLoop(ForLoopNode node);

        public abstract R VisitSwitch(SwitchNode node);

        public abstract R VisitInteger(IntegerNode node);

        public abstract R VisitReal(RealNode node);

        public abstract R VisitBoolean(BooleanNode node);

        public abstract R VisitChar(CharNode node);

        public abstract R VisitString(StringNode node);

        public abstract R VisitVarRef(VarRefNode node);

        public abstract R VisitBinaryOperator(BinaryOpNode node);

        public abstract R VisitBooleanOperator(BooleanOpNode node);

        public abstract R VisitLogicalOperator(LogicalOpNode node);

        public abstract R VisitNotOperator(NotOpNode node);

        public abstract R VisitEmpty(AbstractAstNode node);
    }
}