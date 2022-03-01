using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.LoopType;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class AstEvaluator
    {
        public static bool EvaluateAst(AbstractAstNode node) => Evaluate((dynamic)node);

        public static List<string> Errors { get; private set; } = new();

        private static bool Evaluate(ProgramNode node)
        {
            var valid = ContainsNoDuplicateVariables(node.VarDecls)
                        & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures));
            foreach (var proc in node.Procedures)
            {
                valid = valid & Evaluate(proc);
            }
            foreach (var func in node.Functions)
            {
                valid = valid & Evaluate(func);
            }
            valid = valid & Evaluate((dynamic)node.Stat);
            return valid;
        }

        private static bool Evaluate(BlockNode node)
        {
            var valid = true;
            foreach (var stat in node.Stats)
            {
                valid = valid & Evaluate((dynamic)stat);
            }
            return valid;
        }

        private static bool Evaluate(WriteNode node) => Evaluate((dynamic)node.Expression);

        private static bool Evaluate(AssignmentNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            return node.VarInfo is not null
                 & Evaluate((dynamic)node.Expression)
                 & TypeResolver.ResolveType((dynamic)node.Expression)
                               .Equals(node.VarInfo?.VarDecl.BlaiseType);
        }

        private static bool Evaluate(FunctionNode node)
        {
            var valid = node.IsFunction && node.ReturnType is not null
                      | !node.IsFunction;
            var varDecls = node.VarDecls.Concat(node.Params).ToList();
            valid = valid
                    & ContainsNoDuplicateVariables(varDecls)
                    & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures));
            foreach (var proc in node.Procedures)
            {
                valid = valid & Evaluate(proc);
            }
            foreach (var func in node.Functions)
            {
                valid = valid & Evaluate(func);
            }
            valid = valid & Evaluate((dynamic)node.Stat);
            return valid;
        }

        private static bool Evaluate(LoopNode node)
        {
            var valid = Evaluate((dynamic)node.Condition)
                      & Evaluate((dynamic)node.Body);
            if ((node.Condition as ITypedNode).GetExprType().BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve loop condition to a bool.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(ForLoopNode node) => Evaluate((LoopNode)node)
                                                        & Evaluate(node.Assignment)
                                                        & Evaluate(node.Iteration);

        private static bool Evaluate(BinaryOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (TypeResolver.ResolveType(node).IsValid())
            {
                //Collapse tree if possible
                return valid;
            }
            else
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }

        }

        private static bool Evaluate(BooleanOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (TypeResolver.ResolveType(node).IsValid())
            {
                //Collapse tree if possible
                return valid;
            }
            else
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }
        }

        /*private static bool Evaluate(LogicalOperatorNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            var leftType = ResolveType(node.Left);
            var rightType = ResolveType(node.Left);
            valid = leftType.BasicType is BOOLEAN
                  & rightType.BasicType is BOOLEAN;
            //Collapse tree if possible
            return valid;
        }

        private static bool Evaluate(NotOperatorNode node) => Evaluate(node.Expr)
                                                            & ResolveType(node.Expr).BasicType == BOOLEAN;
            //Collapse tree if possible*/

        private static bool Evaluate(FunctionCallNode node)
        {
            node.CallTarget = ReferenceResolver.FindFunction(node, node.Identifier);
            return !node.CallTarget.IsEmpty()
                && GetArgumentTypes(node).Equals(GetFunctionSignature(node.CallTarget).Parameters);
        }

        /*private static bool Evaluate(IfNode node)
        {
            return Evaluate((dynamic)node.Condition)
                & ResolveType(node.Condition).BasicType == TBoolean
                & Evaluate((dynamic)node.ThenStat)
                & Evaluate((dynamic)node.ElseStat);
        }

        private static bool Evaluate(LoopNode node)
        {
            return Evaluate((dynamic)node.Condition)
                & ResolveType(node.Condition).BasicType == TBoolean
                & node.LoopType == LoopType.For ? EvaluateFor(node) : true
                & Evaluate((dynamic)node.Body);
        }

        private static bool EvaluateFor(LoopNode node) => !node.Assignment.IsEmpty()
                                                        & Evaluate((dynamic)node.Assignment);

        private static bool Evaluate(SwitchNode node)
        {
            var allowedSwitchTypes = new HashSet<BasicType> { TInt, TReal, TChar, TString };
            var switchType = ResolveType(node.Input).BasicType;
            var valid = allowedSwitchTypes.Contains(switchType);
            foreach (var c in node.Cases.OfType<SwitchCaseNode>())
            {
                valid = valid & Evaluate(c);
                if (switchType != ResolveType(c.Case).BasicType)
                {
                    valid = false;
                    Console.WriteLine($"Detected type mismatch in case statement: case {c.Case} does not match type {switchType}.");
                }
            }
            return valid;
        }

        private static bool Evaluate(SwitchCaseNode node) => Evaluate((dynamic)node.Case)
                                                           & Evaluate((dynamic)node.Stat);*/

        private static bool Evaluate(IntegerNode node) => true;

        private static bool Evaluate(RealNode node) => true;

        private static bool Evaluate(BooleanNode node) => true;

        private static bool Evaluate(VarRefNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            if (node.VarInfo is null)
            {
                Errors.Append($"Cannot resolve variable {node.Identifier}");
                return false;
            }
            return true;
        }

        private static bool Evaluate(CharNode node) => true;

        private static bool Evaluate(StringNode node) => true;

        private static bool Evaluate(AbstractAstNode node) => node.IsEmpty() ? true
            : throw new InvalidOperationException($"Unrecognized node type {node.GetType()}");

        private static bool ContainsNoDuplicateFunctionSignatures(IEnumerable<FunctionNode> functions)
        {
            var valid = true;
            var seen = new HashSet<BlaiseSignature>();
            foreach (var node in functions)
            {
                var sig = GetFunctionSignature(node);
                if (!seen.Add(sig))
                {
                    Console.WriteLine($"Duplicate Function {sig} detected in {node.Parent.GetType()} {(node.Parent as ProgramNode).Identifier}");
                    valid = false;
                }
            }
            return valid;
        }

        private static bool ContainsNoDuplicateVariables(List<VarDeclNode> varDecls)
        {
            var valid = true;
            var seen = new HashSet<string>();
            foreach (var node in varDecls)
            {
                if (!seen.Add(node.Identifier))
                {
                    Console.WriteLine($"Duplicate variable {node.Identifier} detected in {node.Parent.GetType()} {(node.Parent as ProgramNode).Identifier}");
                    valid = false;
                }
            }
            return valid;
        }

        private static BlaiseSignature GetFunctionSignature(FunctionNode function) => new BlaiseSignature()
        {
            Identifier = function.Identifier,
            ReturnType = function.ReturnType,
            Parameters = function.Params.Select(p => (p as VarDeclNode).BlaiseType).ToList()
        };

        private static List<BlaiseType> GetArgumentTypes(FunctionCallNode call) => call.Arguments.OfType<VarDeclNode>()
                                                                                                 .Select(n => n.BlaiseType)
                                                                                                 .ToList();
    }
}