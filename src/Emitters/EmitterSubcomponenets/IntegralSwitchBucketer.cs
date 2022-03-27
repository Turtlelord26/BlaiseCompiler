using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Emitters.EmitterSubcomponents
{
    public class IntegralSwitchBucketer
    {
        public List<Bucket> BucketizeIntegralSwitch(SwitchNode node)
        {
            node.Cases.Sort(new SwitchCaseNodeComparer(node.Input.GetExprType()));
            return BucketizeIntegralSwitchCases(node.Cases);
        }

        private List<Bucket> BucketizeIntegralSwitchCases(List<SwitchCaseNode> cases)
        {
            var count = cases.Count;
            var stack = new Stack<Bucket>(count);
            var listptr = 0;
            while (listptr < count)
            {
                if (IsRemainderDense(listptr, cases))
                {
                    stack.Push(CombineWithStackedBucketsIfDense(stack, new Bucket(cases.GetRange(listptr, count - listptr))));
                    listptr = count;
                }
                else
                {
                    stack.Push(CombineWithStackedBucketsIfDense(stack, new Bucket(cases[listptr])));
                    listptr++;
                }
            }
            return stack.Reverse().ToList();
        }

        private Bucket CombineWithStackedBucketsIfDense(Stack<Bucket> stack, Bucket bucket) => stack.Count switch
        {
            > 0 when IsCombinationDense(stack.Peek(), bucket) =>
                CombineWithStackedBucketsIfDense(stack, stack.Pop().Combine(bucket)),
            _ =>
                bucket
        };

        private bool IsRemainderDense(int listptr, List<SwitchCaseNode> cases)
        {
            var count = cases.Count - listptr;
            var range = (cases[cases.Count - 1].Case as IConstantNode).GetConstant().GetValueAsInt()
                      - (cases[listptr].Case as IConstantNode).GetConstant().GetValueAsInt();
            return isDense(count, range);
        }

        private bool IsCombinationDense(Bucket stackTop, Bucket newBucket)
        {
            var newBucketCount = newBucket.Cases.Count;
            var stackTopCount = stackTop.Cases.Count;
            var count = stackTopCount + newBucketCount;
            var range = (newBucket.Cases[newBucketCount - 1].Case as IConstantNode).GetConstant().GetValueAsInt()
                      - (stackTop.Cases[0].Case as IConstantNode).GetConstant().GetValueAsInt()
                      + 1;
            return isDense(count, range);
        }

        private bool isDense(int count, int range) => count * 2 > range;
    }

    public class Bucket
    {
        public List<SwitchCaseNode> Cases { get; init; }

        public Bucket(List<SwitchCaseNode> list) => Cases = list;

        public Bucket(SwitchCaseNode node) => Cases = new() { node };

        public Bucket Combine(Bucket other)
        {
            this.Cases.AddRange(other.Cases);
            return this;
        }
    }
}