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

        public override AbstractAstNode VisitProgram([NotNull] BlaiseParser.ProgramContext context)
        {
            var routines = context.routines();
            return Build<ProgramNode>(n =>
            {
                n.Identifier = context.programDecl().IDENTIFIER().GetText();
                n.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(n)).OfType<VarDeclNode>().ToList()
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
            var routines = context.routines();
            return Build<FunctionNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.IsFunction = false;
                n.Params = args?.Select(a => a.WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(n)).OfType<VarDeclNode>().ToList()
                            ?? new List<VarDeclNode>();
                n.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
                n.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
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
            var routines = context.routines();
            return Build<FunctionNode>(n =>
            {
                n.Identifier = context.IDENTIFIER().GetText();
                n.IsFunction = true;
                n.ReturnType = BuildBlaiseType(context.typeExpr());
                n.Params = args?.Select(a => a.WithParent(n)).ToList() ?? new List<VarDeclNode>();
                n.VarDecls = context.varBlock()?._decl.Select(d => VisitVarDecl(d).WithParent(n)).OfType<VarDeclNode>().ToList()
                            ?? new List<VarDeclNode>();
                n.Procedures = routines?._procs.Select(p => VisitProcedure(p).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
                n.Functions = routines?._funcs.Select(f => VisitFunction(f).WithParent(n)).OfType<FunctionNode>().ToList()
                            ?? new List<FunctionNode>();
                n.Stat = VisitStat(context.stat()).WithParent(n);
            });
        }

        public override AbstractAstNode VisitWrite([NotNull] BlaiseParser.WriteContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = false;
            n.Expression = (ITypedNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitWriteln([NotNull] BlaiseParser.WritelnContext context) => Build<WriteNode>(n =>
        {
            n.WriteNewline = true;
            n.Expression = (ITypedNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitBlock([NotNull] BlaiseParser.BlockContext context) => MakeBlock(context._st);


        public override AbstractAstNode VisitAssignment([NotNull] BlaiseParser.AssignmentContext context) => Build<AssignmentNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.Expression = (ITypedNode)VisitExpression(context.expression()).WithParent(n);
        });

        public override AbstractAstNode VisitIfThenElse([NotNull] BlaiseParser.IfThenElseContext context)
        {
            var elseStatContext = context.elseSt;
            return Build<IfNode>(i =>
            {
                i.Condition = (ITypedNode)VisitExpression(context.condition).WithParent(i);
                i.ThenStat = VisitStat(context.thenSt).WithParent(i);
                i.ElseStat = elseStatContext is not null ? VisitStat(elseStatContext).WithParent(i) : AbstractAstNode.Empty;
            });
        }

        public override AbstractAstNode VisitLoop([NotNull] BlaiseParser.LoopContext context)
        {
            if (context.whileDo() is not null)
            {
                return VisitWhileDo(context.whileDo());
            }
            else if (context.forDo() is not null)
            {
                return VisitForDo(context.forDo());
            }
            else if (context.repeatUntil() is not null)
            {
                return VisitRepeatUntil(context.repeatUntil());
            }
            else
            {
                throw new InvalidOperationException($"Invalid Loop {context.GetText()}");
            }
        }

        public override AbstractAstNode VisitSwitchSt([NotNull] BlaiseParser.SwitchStContext context)
        {
            return Build<SwitchNode>(s =>
            {
                s.Input = (ITypedNode)VisitExpression(context.on).WithParent(s);
                s.Cases = context.switchCase().Select(c => (SwitchCaseNode)VisitSwitchCase(c).WithParent(s)).ToList();
                s.Default = context.defaultCase is not null ? VisitStat(context.defaultCase).WithParent(s) : AbstractAstNode.Empty;
            });
        }

        public override AbstractAstNode VisitSwitchCase([NotNull] BlaiseParser.SwitchCaseContext context)
        {
            return Build<SwitchCaseNode>(c =>
            {
                c.Case = (ITypedNode)VisitSwitchAtom(context.alt).WithParent(c);
                c.Stat = VisitStat(context.st).WithParent(c);
            });
        }

        public override AbstractAstNode VisitWhileDo([NotNull] BlaiseParser.WhileDoContext context) => Build<LoopNode>(n =>
        {
            n.LoopType = While;
            n.Condition = (ITypedNode)VisitExpression(context.condition).WithParent(n);
            n.Body = VisitStat(context.st).WithParent(n);
        });

        public override AbstractAstNode VisitForDo([NotNull] BlaiseParser.ForDoContext context) => Build<ForLoopNode>(n =>
        {
            n.LoopType = For;
            n.Assignment = (AssignmentNode)VisitAssignment(context.init).WithParent(n);
            n.Iteration = Build<AssignmentNode>(a =>
            {
                a.Identifier = n.Assignment.Identifier;
                a.Expression = Build<BinaryOpNode>(e =>
                {
                    e.Left = Build<VarRefNode>(v => v.Identifier = n.Assignment.Identifier).WithParent(e);
                    e.Right = Build<IntegerNode>(i => i.IntValue = 1).WithParent(e);
                    e.Operator = context.direction.Text.Equals("downto") ? BlaiseOperator.Sub : BlaiseOperator.Add;
                }).WithParent(a);
            }).WithParent(n);
            n.Condition = Build<BooleanOpNode>(c =>
            {
                c.Left = Build<VarRefNode>(v => v.Identifier = n.Assignment.Identifier).WithParent(c);
                c.Right = (ITypedNode)VisitExpression(context.limit).WithParent(c);
                c.Operator = context.direction.Text.Equals("downto") ? BlaiseOperator.Gt : BlaiseOperator.Lt;
            }).WithParent(n);
            n.Body = VisitStat(context.st).WithParent(n);
        });

        public override AbstractAstNode VisitRepeatUntil([NotNull] BlaiseParser.RepeatUntilContext context) => Build<LoopNode>(n =>
        {
            n.LoopType = Until;
            n.Condition = (ITypedNode)VisitExpression(context.condition).WithParent(n);
            n.Body = MakeBlock(context._st).WithParent(n);
        });

        public override AbstractAstNode VisitRet([NotNull] BlaiseParser.RetContext context) => Build<ReturnNode>(n =>
        {
            n.Expression = context.expression() is not null ? (ITypedNode)VisitExpression(context.expression()).WithParent(n)
                                                            : (ITypedNode)AbstractAstNode.Empty;
        });

        public override AbstractAstNode VisitProcedureCall([NotNull] BlaiseParser.ProcedureCallContext context) => MakeCallNode(context.call(), false);

        public override AbstractAstNode VisitFunctionCall([NotNull] BlaiseParser.FunctionCallContext context) => MakeCallNode(context.call(), true);

        public override AbstractAstNode VisitExpression([NotNull] BlaiseParser.ExpressionContext context)
        {
            if (context.binop is not null)
            {
                return Build<BinaryOpNode>(n =>
                {
                    n.Left = (ITypedNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (ITypedNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.binop.Text];
                });
            }
            else if (context.boolop is not null)
            {
                return Build<BooleanOpNode>(n =>
                {
                    n.Left = (ITypedNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (ITypedNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.boolop.Text];
                });
            }
            else if (context.logop != null)
            {
                return Build<LogicalOpNode>(n =>
                {
                    n.Left = (ITypedNode)VisitExpression(context.left).WithParent(n);
                    n.Right = (ITypedNode)VisitExpression(context.right).WithParent(n);
                    n.Operator = OpMap[context.logop.Text];
                });
            }
            else if (context.negated != null)
            {
                return Build<NotOpNode>(n =>
                {
                    n.Expression = (ITypedNode)VisitExpression(context.negated).WithParent(n);
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
            var sign = (context.sign?.Text.Equals("-") ?? false) ? -1 : 1;
            if (context.INTEGER() is not null)
            {
                return Build<IntegerNode>(n => { n.IntValue = sign * int.Parse(context.INTEGER().GetText()); });
            }
            else if (context.REAL() is not null)
            {
                return Build<RealNode>(n => { n.RealValue = sign * double.Parse(context.REAL().GetText()); });
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
                    n.BoolValue = context.BOOLEAN().GetText() == "true";
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
                throw new InvalidOperationException($"Invalid Atom {context.GetText()}");
            }
        }

        private AbstractAstNode MakeBlock([NotNull] IList<BlaiseParser.StatContext> stats)
        {
            int statCount = stats.Count;
            if (statCount == 0)
            {
                return AbstractAstNode.Empty;
            }
            if (statCount == 1)
            {
                return VisitStat(stats[0]);
            }
            return Build<BlockNode>(n =>
            {
                n.Stats = stats.Select(s => VisitStat(s).WithParent(n)).ToList();
            });
        }

        private AbstractAstNode MakeCallNode([NotNull] BlaiseParser.CallContext context, bool isFunction) => Build<FunctionCallNode>(n =>
        {
            n.Identifier = context.IDENTIFIER().GetText();
            n.IsFunction = isFunction;
            n.Arguments = context.argsList()?._args.Select(a => VisitExpression(a).WithParent(n)).OfType<ITypedNode>().ToList()
                          ?? new List<ITypedNode>();
        });
    }
}