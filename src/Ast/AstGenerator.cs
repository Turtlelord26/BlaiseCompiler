using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.LoopType;

namespace Blaise2.Ast
{
    public partial class AstGenerator : BlaiseBaseVisitor<AbstractAstNode>
    {
        protected override AbstractAstNode DefaultResult => AbstractAstNode.Empty;

        public override AbstractAstNode VisitFile([NotNull] BlaiseParser.FileContext context) => Visit(context.children[0]);

        public override AbstractAstNode VisitProgram([NotNull] BlaiseParser.ProgramContext context) => Build<ProgramNode>(program =>
        {
            var routines = context.routines();
            program.Identifier = context.programDecl().IDENTIFIER().GetText();
            program.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(program)).OfType<VarDeclNode>().ToList()
                               ?? new List<VarDeclNode>();
            program.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(program)).OfType<FunctionNode>().ToList()
                                 ?? new List<FunctionNode>();
            program.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(program)).OfType<FunctionNode>().ToList()
                                ?? new List<FunctionNode>();
            program.Stat = VisitStat(context.stat()).WithParent(program);
        });

        public override AbstractAstNode VisitVarDecl([NotNull] BlaiseParser.VarDeclContext context) => Build<VarDeclNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.BlaiseType = BuildBlaiseType(context.typeExpr());
        });

        public override AbstractAstNode VisitProcedure([NotNull] BlaiseParser.ProcedureContext context) => Build<FunctionNode>(proc =>
        {
            var parameters = context.paramsList()?._var.Select(parameter => Build<VarDeclNode>(d =>
            {
                d.Identifier = parameter.IDENTIFIER().GetText();
                d.BlaiseType = BuildBlaiseType(parameter.typeExpr());
            }));
            var routines = context.routines();
            proc.Identifier = context.IDENTIFIER().GetText();
            proc.IsFunction = false;
            proc.Params = parameters?.Select(a => a.WithParent(proc)).ToList()
                          ?? new List<VarDeclNode>();
            proc.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(proc)).OfType<VarDeclNode>().ToList()
                            ?? new List<VarDeclNode>();
            proc.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(proc)).OfType<FunctionNode>().ToList()
                              ?? new List<FunctionNode>();
            proc.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(proc)).OfType<FunctionNode>().ToList()
                             ?? new List<FunctionNode>();
            proc.Stat = VisitStat(context.stat()).WithParent(proc);
        });

        public override AbstractAstNode VisitFunction([NotNull] BlaiseParser.FunctionContext context) => Build<FunctionNode>(func =>
        {
            var parameters = context.paramsList()?._var.Select(parameter => Build<VarDeclNode>(d =>
            {
                d.Identifier = parameter.IDENTIFIER().GetText();
                d.BlaiseType = BuildBlaiseType(parameter.typeExpr());
            }));
            var routines = context.routines();
            func.Identifier = context.IDENTIFIER().GetText();
            func.IsFunction = true;
            func.ReturnType = BuildBlaiseType(context.typeExpr());
            func.Params = parameters?.Select(a => a.WithParent(func)).ToList()
                       ?? new List<VarDeclNode>();
            func.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(func)).OfType<VarDeclNode>().ToList()
                         ?? new List<VarDeclNode>();
            func.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(func)).OfType<FunctionNode>().ToList()
                           ?? new List<FunctionNode>();
            func.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(func)).OfType<FunctionNode>().ToList()
                          ?? new List<FunctionNode>();
            func.Stat = VisitStat(context.stat()).WithParent(func);
        });

        public override AbstractAstNode VisitWrite([NotNull] BlaiseParser.WriteContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = false;
            n.Expression = (AbstractTypedAstNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitWriteln([NotNull] BlaiseParser.WritelnContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = true;
            n.Expression = (AbstractTypedAstNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitBlock([NotNull] BlaiseParser.BlockContext context) => MakeBlock(context._st);

        public override AbstractAstNode VisitAssignment([NotNull] BlaiseParser.AssignmentContext context) => Build<AssignmentNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.Expression = (AbstractTypedAstNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitIfThenElse([NotNull] BlaiseParser.IfThenElseContext context) => Build<IfNode>(n =>
        {
            n.Condition = (AbstractTypedAstNode)VisitExpression(context.condition).WithParent(n);
            n.ThenStat = VisitStat(context.thenSt).WithParent(n);
            n.ElseStat = context switch
            {
                { elseSt: not null } => VisitStat(context.elseSt).WithParent(n),
                _ => AbstractAstNode.Empty
            };
        });

        public override AbstractAstNode VisitLoop([NotNull] BlaiseParser.LoopContext context) => context switch
        {
            { whileContext: not null } => VisitWhileDo(context.whileContext),
            { forContext: not null } => VisitForDo(context.forContext),
            { untilContext: not null } => VisitRepeatUntil(context.untilContext),
            _ => throw new InvalidOperationException($"Invalid Loop {context.GetText()}")
        };

        public override AbstractAstNode VisitSwitchSt([NotNull] BlaiseParser.SwitchStContext context) => Build<SwitchNode>(n =>
        {
            n.Input = (AbstractTypedAstNode)VisitExpression(context.on).WithParent(n);
            n.Cases = context.switchCase().Select(c => (SwitchCaseNode)VisitSwitchCase(c).WithParent(n)).ToList();
            n.Default = context switch
            {
                { defaultCase: not null } => VisitStat(context.defaultCase).WithParent(n),
                _ => AbstractAstNode.Empty
            };
        });

        public override AbstractAstNode VisitSwitchCase([NotNull] BlaiseParser.SwitchCaseContext context) => Build<SwitchCaseNode>(n =>
        {
            n.Case = (AbstractTypedAstNode)VisitSwitchAtom(context.alt).WithParent(n);
            n.Stat = VisitStat(context.st).WithParent(n);
        });

        public override AbstractAstNode VisitWhileDo([NotNull] BlaiseParser.WhileDoContext context) => Build<LoopNode>(n =>
        {
            n.LoopType = While;
            n.Condition = (AbstractTypedAstNode)VisitExpression(context.condition).WithParent(n);
            n.Body = VisitStat(context.st).WithParent(n);
        });

        public override AbstractAstNode VisitForDo([NotNull] BlaiseParser.ForDoContext context) => Build<ForLoopNode>(forNode =>
        {
            var down = context.direction.Text.Equals("downto");
            forNode.LoopType = For;
            forNode.Assignment = (AssignmentNode)VisitAssignment(context.init).WithParent(forNode);
            forNode.Iteration = Build<AssignmentNode>(assignment =>
            {
                assignment.Identifier = forNode.Assignment.Identifier;
                assignment.Expression = Build<BinaryOpNode>(increment =>
                {
                    increment.Left = Build<VarRefNode>(v => v.Identifier = forNode.Assignment.Identifier).WithParent(increment);
                    increment.Right = Build<IntegerNode>(i => i.IntValue = 1).WithParent(increment);
                    increment.Operator = down ? BlaiseOperator.Sub : BlaiseOperator.Add;
                }).WithParent(assignment);
            }).WithParent(forNode);
            forNode.Condition = Build<BooleanOpNode>(condition =>
            {
                condition.Left = Build<VarRefNode>(v => v.Identifier = forNode.Assignment.Identifier).WithParent(condition);
                condition.Right = (AbstractTypedAstNode)VisitExpression(context.limit).WithParent(condition);
                condition.Operator = down ? BlaiseOperator.Gt : BlaiseOperator.Lt;
            }).WithParent(forNode);
            forNode.Body = VisitStat(context.st).WithParent(forNode);
        });

        public override AbstractAstNode VisitRepeatUntil([NotNull] BlaiseParser.RepeatUntilContext context) => Build<LoopNode>(n =>
        {
            n.LoopType = Until;
            n.Condition = (AbstractTypedAstNode)VisitExpression(context.condition).WithParent(n);
            n.Body = MakeBlock(context._st).WithParent(n);
        });

        public override AbstractAstNode VisitRet([NotNull] BlaiseParser.RetContext context) => Build<ReturnNode>(n =>
            n.Expression = context switch
            {
                { retExpr: not null } => (AbstractTypedAstNode)VisitExpression(context.retExpr).WithParent(n),
                _ => (AbstractTypedAstNode)AbstractAstNode.Empty
            });

        public override AbstractAstNode VisitProcedureCall([NotNull] BlaiseParser.ProcedureCallContext context) => MakeCallNode(context.call(), false);

        public override AbstractAstNode VisitFunctionCall([NotNull] BlaiseParser.FunctionCallContext context) => MakeCallNode(context.call(), true);

        public override AbstractAstNode VisitExpression([NotNull] BlaiseParser.ExpressionContext context) => context switch
        {
            { binop: not null } => Build<BinaryOpNode>(n =>
                {
                    n.Left = (AbstractTypedAstNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (AbstractTypedAstNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.binop.Text];
                }),
            { boolop: not null } => Build<BooleanOpNode>(n =>
                {
                    n.Left = (AbstractTypedAstNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (AbstractTypedAstNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.boolop.Text];
                }),
            { logop: not null } => Build<LogicalOpNode>(n =>
                {
                    n.Left = (AbstractTypedAstNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (AbstractTypedAstNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.logop.Text];
                }),
            { negated: not null } => Build<NotOpNode>(n => n.Expression = (AbstractTypedAstNode)VisitExpression(context.negated).WithParent(n)),
            { inner: not null } => VisitExpression(context.inner),
            { funcCall: not null } => VisitFunctionCall(context.funcCall),
            { numeric: not null } => VisitNumericAtom(context.numeric),
            { atomic: not null } => VisitAtom(context.atomic),
            _ => throw new InvalidOperationException($"Invalid Expression {context.GetText()}")
        };

        public override AbstractAstNode VisitNumericAtom([NotNull] BlaiseParser.NumericAtomContext context)
        {
            var sign = context.sign switch
            {
                { Text: "-" } => -1,
                _ => 1
            };
            return context switch
            {
                { intValue: not null } => Build<IntegerNode>(n => n.IntValue = sign * int.Parse(context.intValue.Text)),
                { realValue: not null } => Build<RealNode>(n => n.RealValue = sign * double.Parse(context.realValue.Text)),
                _ => throw new InvalidOperationException($"Invalid Numeric Atom {context.GetText()}")
            };
        }

        public override AbstractAstNode VisitAtom([NotNull] BlaiseParser.AtomContext context) => context switch
        {
            { id: not null } => Build<VarRefNode>(n => n.Identifier = context.id.Text),
            { boolValue: not null } => Build<BooleanNode>(n => n.BoolValue = context.boolValue.Text == "true"),
            { charValue: not null } => Build<CharNode>(n => n.CharValue = context.charValue.Text[1]),
            { stringValue: not null } => Build<StringNode>(n => n.StringValue = context.stringValue.Text[1..^1]),
            _ => throw new InvalidOperationException($"Invalid Atom {context.GetText()}")
        };

        private AbstractAstNode MakeBlock([NotNull] IList<BlaiseParser.StatContext> stats) => stats.Count switch
        {
            0 => AbstractAstNode.Empty,
            1 => VisitStat(stats[0]),
            _ => Build<BlockNode>(n => n.Stats = stats.Select(s => VisitStat(s).WithParent(n)).ToList())
        };

        private AbstractAstNode MakeCallNode([NotNull] BlaiseParser.CallContext context, bool isFunction) => Build<FunctionCallNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.IsFunction = isFunction;
            n.Arguments = context.argsList()?._args.Select(a => VisitExpression(a).WithParent(n)).OfType<AbstractTypedAstNode>().ToList()
                          ?? new List<AbstractTypedAstNode>();
        });
    }
}