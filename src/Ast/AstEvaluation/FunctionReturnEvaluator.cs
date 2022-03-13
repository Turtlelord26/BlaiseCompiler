using System;
using System.Linq;

namespace Blaise2.Ast
{
    public class FunctionReturnEvaluator
    {
        public static bool Visit(FunctionNode node) => Visit(node.Stat);

        public static bool Visit(AssignmentNode node) => false;

        public static bool Visit(WriteNode node) => false;

        public static bool Visit(FunctionCallNode node) => false;

        public static bool Visit(IfNode node) => Visit(node.ThenStat)
                                                 & Visit(node.ElseStat);

        public static bool Visit(LoopNode node) => Visit(node.Body);

        public static bool Visit(SwitchNode node) => node.Cases.Aggregate(true, (valid, next) => valid & Visit(next.Stat));

        public static bool Visit(ReturnNode node) => true;

        public static bool Visit(BlockNode node) => Visit(node.Stats[node.Stats.Count - 1]);

        public static bool Visit(AbstractAstNode node) => node switch
        {
            FunctionNode func => Visit(func),
            AssignmentNode assign => Visit(assign),
            WriteNode write => Visit(write),
            FunctionCallNode call => Visit(call),
            IfNode ifn => Visit(ifn),
            LoopNode loop => Visit(loop),
            SwitchNode switchn => Visit(switchn),
            ReturnNode ret => Visit(ret),
            BlockNode blk => Visit(blk),
            AbstractAstNode aan when aan.IsEmpty() => false,
            _ => throw new InvalidOperationException($"Function return evaluator encountered unexpected node type {node.Type}.")
        };
    }
}