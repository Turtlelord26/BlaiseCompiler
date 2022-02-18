using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using static Blaise2.Ast.AstNodeExtensions;

namespace Blaise2.Ast
{
    public partial class AstGenerator : BlaiseBaseVisitor<AbstractAstNode>
    {
        protected override AbstractAstNode DefaultResult => base.DefaultResult;

        public override AbstractAstNode VisitFile([NotNull] BlaiseParser.FileContext context)
        {
            return Visit(context.children[0]);
        }

        public override AbstractAstNode VisitProgram([NotNull] BlaiseParser.ProgramContext context)
        {
            var routines = context.routines();

            var node = new ProgramNode().Build(n =>
            {
                n.ProgramName = context.programDecl().IDENTIFIER().GetText();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.Procedures = routines?._procs.Select(p => (ProcedureNode)VisitProcedure(p).WithParent(n)).ToList() ?? new List<ProcedureNode>();
                n.Functions = routines?._funcs.Select(f => (FunctionNode)VisitFunction(f).WithParent(n)).ToList() ?? new List<FunctionNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });

            return node;
        }

        public override AbstractAstNode VisitVarDecl([NotNull] BlaiseParser.VarDeclContext context)
        {
            return Build<VarDeclNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.BlaiseType = BuildBlaiseType(context.typeExpr());
            });
        }

        public override AbstractAstNode VisitProcedure([NotNull] BlaiseParser.ProcedureContext context)
        {
            var args = context.argsList()._v.Select(v => Build<VarDeclNode>(n =>
            {
                n.Identifier = v.IDENTIFIER().GetText();
                n.BlaiseType = BuildBlaiseType(v.typeExpr());
            }));

            return Build<ProcedureNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.Args = args.Select(a => a.WithParent(n)).ToList();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitFunction([NotNull] BlaiseParser.FunctionContext context)
        {
            var args = context.argsList()._v.Select(v => Build<VarDeclNode>(n =>
            {
                n.Identifier = v.IDENTIFIER().GetText();
                n.BlaiseType = BuildBlaiseType(v.typeExpr());
            }));

            return Build<FunctionNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.ReturnType = BuildBlaiseType(context.typeExpr());
                n.Args = args.Select(a => a.WithParent(n)).ToList();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitWrite([NotNull] BlaiseParser.WriteContext context)
        {
            return Build<WriteNode>(n =>
            {
                n.WriteNewline = false;
                n.Expression = VisitExpression(context.expression()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitWriteln([NotNull] BlaiseParser.WritelnContext context)
        {
            return Build<WriteNode>(n =>
            {
                n.WriteNewline = true;
                n.Expression = VisitExpression(context.expression()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitBlock([NotNull] BlaiseParser.BlockContext context)
        {
            return Build<BlockNode>(n =>
            {
                n.Stats = context._st.Select(s => VisitStat(s).WithParent(n)).ToList();
            });
        }

        public override AbstractAstNode VisitAssignment([NotNull] BlaiseParser.AssignmentContext context)
        {
            return Build<AssignmentNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.Expression = VisitExpression(context.expression()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitNumericAtom([NotNull] BlaiseParser.NumericAtomContext context)
        {
            if (context.INTEGER() != null)
            {
                return Build<IntegerNode>(n => { n.IntValue = int.Parse(context.INTEGER().GetText()); });
            }
            else if (context.REAL() != null)
            {
                return Build<RealNode>(n => { n.RealValue = double.Parse(context.REAL().GetText()); });
            }
            else
            {
                return null;
            }
        }

        public override AbstractAstNode VisitAtom([NotNull] BlaiseParser.AtomContext context)
        {
            if (context.STRING() != null)
            {
                return Build<StringNode>(n =>
                {
                    n.StringValue = context.STRING().GetText();
                });
            }
            else if (context.IDENTIFIER() != null)
            {
                return Build<VarRefNode>(n =>
                {
                    n.Identifier = context.IDENTIFIER().GetText();
                });
            }
            else
            {
                return VisitFunctionCall(context.functionCall());
            }
        }

        public override AbstractAstNode VisitFunctionCall([NotNull] BlaiseParser.FunctionCallContext context)
        {
            return Build<FunctionCallNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.ArgumentExpressions = context._arg.Select(a => VisitExpression(a).WithParent(n)).ToList();
            });
        }

        public override AbstractAstNode VisitExpression([NotNull] BlaiseParser.ExpressionContext context)
        {
            if (context.op != null)
            {
                return Build<BinaryOpNode>(n =>
                {
                    n.Lhs = VisitExpression(context.lhs).WithParent(n);
                    n.Rhs = VisitExpression(context.rhs).WithParent(n);
                    n.Op = context.op.Text;
                });
            }
            else if (context.boolop != null)
            {
                return Build<BooleanOpNode>(n =>
                {
                    n.Lhs = VisitExpression(context.lhs).WithParent(n);
                    n.Rhs = VisitExpression(context.rhs).WithParent(n);
                    n.Op = context.op.Text;
                });
            }
            else if (context.inner != null)
            {
                return VisitExpression(context.inner);
            }
            else if (context.numericAtom() != null)
            {
                return VisitNumericAtom(context.numericAtom());
            }
            else
            {
                return VisitAtom(context.atom());
            }
        }
    }
}