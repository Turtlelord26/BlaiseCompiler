using System;
using System.Linq;

namespace Blaise2.Ast
{
    public class FunctionReturnEvaluator
    {
        public static bool Visit(FunctionNode node) => Visit((dynamic)node.Stat);

        public static bool Visit(AssignmentNode node) => false;

        public static bool Visit(WriteNode node) => false;

        public static bool Visit(FunctionCallNode node) => false;

        public static bool Visit(IfNode node) => Visit((dynamic)node.ThenStat)
                                                 & Visit((dynamic)node.ElseStat);

        public static bool Visit(LoopNode node) => Visit((dynamic)node.Body);

        public static bool Visit(SwitchNode node) => node.Cases.Aggregate(true, (valid, next) => valid & Visit((dynamic)next.Stat));

        public static bool Visit(ReturnNode node) => true;

        public static bool Visit(BlockNode node) => Visit((dynamic)node.Stats[node.Stats.Count - 1]);

        public static bool Visit(AbstractAstNode node) => node.IsEmpty() ? false : throw new InvalidOperationException($"Function return evaluator encountered unexpected node type {node.Type}.");
    }
}