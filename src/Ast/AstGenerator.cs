using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using static Blaise2.Ast.AstNodeExtensions;

namespace Blaise2.Ast
{
    public partial class AstGenerator : BlaiseBaseVisitor<AbstractAstNode>
    {
        protected override AbstractAstNode DefaultResult => AbstractAstNode.Empty;

        public override AbstractAstNode VisitFile([NotNull] BlaiseParser.FileContext context) => Visit(context.children[0]);

        public override AbstractAstNode VisitProgram([NotNull] BlaiseParser.ProgramContext context)
        {
            var routines = context.routines();
            return Build<ProgramNode>(n =>
            {
                n.Identifier = context.programDecl().IDENTIFIER().GetText();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList()
                            ?? new List<VarDeclNode>();
                n.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
                n.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitVarDecl([NotNull] BlaiseParser.VarDeclContext context) => Build<VarDeclNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.BlaiseType = BuildBlaiseType(context.typeExpr());
        });

        public override AbstractAstNode VisitProcedure([NotNull] BlaiseParser.ProcedureContext context)
        {
            var args = context.paramsList()?._var.Select(v => Build<VarDeclNode>(n =>
            {
                n.Identifier = v.IDENTIFIER().GetText();
                n.BlaiseType = BuildBlaiseType(v.typeExpr());
            }));
            return Build<FunctionNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.IsFunction = false;
                n.Params = args?.Select(a => a.WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitFunction([NotNull] BlaiseParser.FunctionContext context)
        {
            var args = context.paramsList()?._var.Select(v => Build<VarDeclNode>(n =>
            {
                n.Identifier = v.IDENTIFIER().GetText();
                n.BlaiseType = BuildBlaiseType(v.typeExpr());
            }));
            return Build<FunctionNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.IsFunction = true;
                n.ReturnType = BuildBlaiseType(context.typeExpr());
                n.Params = args?.Select(a => a.WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.VarDecls = context.varBlock()?._decl.Select(d => (VarDeclNode)VisitVarDecl(d).WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitWrite([NotNull] BlaiseParser.WriteContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = false;
            n.Expression = VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitWriteln([NotNull] BlaiseParser.WritelnContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = true;
            n.Expression = VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitBlock([NotNull] BlaiseParser.BlockContext context)
        {
            int statCount = context._st.Count;
            if (statCount == 0)
            {
                return AbstractAstNode.Empty;
            }
            if (statCount == 1)
            {
                return VisitStat(context._st[0]);
            }
            return Build<BlockNode>(n =>
            {
                n.Stats = context._st.Select(s => VisitStat(s).WithParent(n)).ToList();
            });
        }

        public override AbstractAstNode VisitAssignment([NotNull] BlaiseParser.AssignmentContext context) => Build<AssignmentNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.Expression = VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitProcedureCall([NotNull] BlaiseParser.ProcedureCallContext context) => MakeCallNode(context.call(), false);

        public override AbstractAstNode VisitFunctionCall([NotNull] BlaiseParser.FunctionCallContext context) => MakeCallNode(context.call(), true);

        private AbstractAstNode MakeCallNode([NotNull] BlaiseParser.CallContext context, bool isFunction) => Build<FunctionCallNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.IsFunction = isFunction;
            n.Arguments = context.argsList()?._args.Select(a => VisitExpression(a).WithParent(n)).ToList()
                          ?? new List<AbstractAstNode>();
        });

        public override AbstractAstNode VisitExpression([NotNull] BlaiseParser.ExpressionContext context)
        {
            if (context.binop is not null)
            {
                return Build<BinaryOpNode>(n =>
                {
                    n.Left = VisitExpression(context.left).WithParent(n);
                    n.Right = VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.binop.Text];
                });
            }
            else if (context.boolop is not null)
            {
                return Build<BooleanOpNode>(n =>
                {
                    n.Left = VisitExpression(context.left).WithParent(n);
                    n.Right = VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.binop.Text];
                });
            }
            else if (context.inner is not null)
            {
                return VisitExpression(context.inner);
            }
            else if (context.functionCall() is not null)
            {
                return VisitFunctionCall(context.functionCall());
            }
            else if (context.numericAtom() is not null)
            {
                return VisitNumericAtom(context.numericAtom());
            }
            else if (context.atom() is not null)
            {
                return VisitAtom(context.atom());
            }
            else
            {
                throw new InvalidOperationException($"Invalid Expression {context.GetText()}");
            }
        }

        public override AbstractAstNode VisitNumericAtom([NotNull] BlaiseParser.NumericAtomContext context)
        {
            if (context.INTEGER() is not null)
            {
                return Build<IntegerNode>(n => { n.IntValue = int.Parse(context.INTEGER().GetText()); });
            }
            else if (context.REAL() is not null)
            {
                return Build<RealNode>(n => { n.RealValue = double.Parse(context.REAL().GetText()); });
            }
            else
            {
                throw new InvalidOperationException($"Invalid Numeric Atom {context.GetText()}");
            }
        }

        public override AbstractAstNode VisitAtom([NotNull] BlaiseParser.AtomContext context)
        {
            if (context.IDENTIFIER() is not null)
            {
                return Build<VarRefNode>(n =>
                {
                    n.Identifier = context.IDENTIFIER().GetText();
                });
            }
            else if (context.BOOLEAN() is not null)
            {
                return Build<BooleanNode>(n =>
                {
                    n.BoolValue = context.BOOLEAN().GetText() == "true" ? true : false;
                });
            }
            else if (context.CHAR() is not null)
            {
                return Build<CharNode>(n =>
                {
                    n.CharValue = context.CHAR().GetText()[1];
                });
            }
            else if (context.STRING() is not null)
            {
                return Build<StringNode>(n =>
                {
                    n.StringValue = context.STRING().GetText()[1..^1];
                });
            }
            else
            {
                throw new InvalidOperationException($"Invalid Char Atom {context.GetText()}");
            }
        }
    }
}