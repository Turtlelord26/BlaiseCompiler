using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {

        private const int LinearSearchThreshold = 3;
        private const int CaseCountSwitchabilityThreshold = 3;

        private string EmitSwitch(SwitchNode node) => node.Input.GetExprType() switch
        {
            { BasicType: CHAR or INTEGER } => EmitIntegralSwitch(node),
            { BasicType: REAL } => EmitCascadedIfSwitch(node),
            { BasicType: STRING } => EmitCascadedIfSwitch(node),//EmitStringSwitch(node),
            BlaiseType bt => throw new InvalidOperationException($"Encountered invalid switch input type {bt} while emitting.")
        };

        private string EmitIntegralSwitch(SwitchNode node)
        {
            var switchType = node.Input.GetExprType();
            var equalityTest = EqualityTestCil(switchType);
            var switchComponents = new SwitchEmissionComponents();
            var endLabel = MakeLabel();
            var defaultLabel = node.Default.IsEmpty() ? endLabel
                                                      : MakeLabel();
            var branchToDefault = @$"
    br.s {defaultLabel}";
            var defaultCase = MakeDefaultCase(node.Default, defaultLabel);
            var ending = @$"
    {endLabel}: nop";

            //Setup
            var hiddenSwitchLocal = MakeAndInjectLocalVar(switchType, node);
            var setup = @$"{EmitExpression(node.Input)}
    stloc {hiddenSwitchLocal}";

            //Branches and Cases
            node.Cases.Sort(new SwitchCaseNodeComparer(switchType));
            var buckets = BucketizeIntegralSwitchCases(node.Cases);
            foreach (var bucket in buckets)
            {
                switchComponents = bucket.Count >= CaseCountSwitchabilityThreshold
                    ? AddSwitchableBucketComponents(switchComponents, bucket, hiddenSwitchLocal, defaultLabel, endLabel)
                    : AddUnswitchableBucketComponents(switchComponents, bucket, hiddenSwitchLocal, equalityTest, endLabel);
            }
            return string.Join(string.Empty,
                               setup,
                               JoinBranchesWithBinarySearch(switchComponents, hiddenSwitchLocal),
                               branchToDefault,
                               string.Join(string.Empty, switchComponents.Stats),
                               defaultCase,
                               ending);
        }

        private string JoinBranchesWithBinarySearch(SwitchEmissionComponents switchComponents, string hiddenSwitchLocal) =>
            JoinBranchesWithBinarySearch(switchComponents, hiddenSwitchLocal, 0, switchComponents.Branches.Count - 1);

        private string JoinBranchesWithBinarySearch(SwitchEmissionComponents switchComponents, string hiddenSwitchLocal, int startIndex, int endIndex)
        {
            if (endIndex - startIndex < LinearSearchThreshold)
            {
                return EmitBucketsBranches(switchComponents.Branches, startIndex, endIndex);
            }
            var midpoint = (startIndex + endIndex + 1) / 2;
            var pivotValue = switchComponents.Values[midpoint - 1];
            var pivotLabel = MakeLabel();
            return EmitConditionalBranchToSecondHalf(hiddenSwitchLocal, pivotValue, pivotLabel)
                 + JoinBranchesWithBinarySearch(switchComponents, hiddenSwitchLocal, startIndex, midpoint - 1)
                 + @$"
    {pivotLabel}: nop"
                 + JoinBranchesWithBinarySearch(switchComponents, hiddenSwitchLocal, midpoint, endIndex);
        }

        private string EmitBucketsBranches(List<string> switchBranches, int startIndex, int endIndex) =>
            switchBranches.GetRange(startIndex, endIndex - startIndex + 1)
                          .Aggregate(string.Empty, (emit, branch) => emit += branch);

        private string EmitConditionalBranchToSecondHalf(string hiddenSwitchLocal, int pivotValue, string pivotLabel) => @$"
    
    ldloc {hiddenSwitchLocal}
    ldc.i4.s {pivotValue}
    bgt {pivotLabel}";

        private SwitchEmissionComponents AddSwitchableBucketComponents(SwitchEmissionComponents switchComponents, List<SwitchCaseNode> bucket, string hiddenSwitchLocal, string defaultLabel, string endLabel)
        {
            var firstCaseVal = (bucket[0].Case as IConstantNode).GetConstant().GetValueAsInt();
            var jumpLabels = new List<string>();
            var previousCaseVal = firstCaseVal;
            var nextCaseVal = firstCaseVal;
            foreach (var swCase in bucket)
            {
                nextCaseVal = (swCase.Case as IConstantNode).GetConstant().GetValueAsInt();
                while (previousCaseVal < nextCaseVal)
                {
                    jumpLabels.Add(defaultLabel);
                    previousCaseVal++;
                }
                var label = MakeLabel();
                jumpLabels.Add(label);
                previousCaseVal++;
                switchComponents.Stats.Add(@$"
    
    {label}: nop{EmitStat(swCase.Stat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, swCase.Stat)}");
            }
            switchComponents.Branches.Add(@$"
    
    ldloc {hiddenSwitchLocal}
    ldc.i4.s {firstCaseVal}
    sub
    switch ({string.Join(", ", jumpLabels)})");
            switchComponents.Values.Add(nextCaseVal);
            return switchComponents;
        }

        private SwitchEmissionComponents AddUnswitchableBucketComponents(SwitchEmissionComponents switchComponents, List<SwitchCaseNode> bucket, string hiddenSwitchLocal, string equalityTest, string endLabel) =>
            bucket.Aggregate(switchComponents, (components, swCase) => AddUnswitchableBucketCase(components, swCase, hiddenSwitchLocal, equalityTest, endLabel));

        private SwitchEmissionComponents AddUnswitchableBucketCase(SwitchEmissionComponents switchComponents, SwitchCaseNode swCase, string hiddenSwitchLocal, string equalityTest, string endLabel)
        {
            var label = MakeLabel();
            switchComponents.Values.Add((swCase.Case as IConstantNode).GetConstant().GetValueAsInt());
            switchComponents.Labels.Add(label);
            switchComponents.Branches.Add(@$"
    
    ldloc {hiddenSwitchLocal}{EmitExpression(swCase.Case)}
    {equalityTest} {label}");
            switchComponents.Stats.Add(@$"

    {label}: nop{EmitStat(swCase.Stat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, swCase.Stat)}");
            return switchComponents;
        }

        private List<List<SwitchCaseNode>> BucketizeIntegralSwitchCases(List<SwitchCaseNode> cases)
        {
            var count = cases.Count;
            var stack = new Stack<List<SwitchCaseNode>>(count);
            var listptr = 0;
            while (listptr < count)
            {
                List<SwitchCaseNode> bucket;
                if (IsRemainderDense(listptr, cases))
                {
                    bucket = cases.GetRange(listptr, count - listptr);
                    listptr = count;
                }
                else
                {
                    bucket = new() { cases[listptr] };
                }
                while (stack.Count > 0 && IsCombinationDense(stack.Peek(), bucket))
                {
                    bucket = CombineBuckets(stack.Pop(), bucket);
                }
                stack.Push(bucket);
                listptr++;
            }
            return stack.Reverse().ToList();
        }

        private bool IsRemainderDense(int listptr, List<SwitchCaseNode> cases)
        {
            var count = cases.Count - listptr;
            var range = (cases[cases.Count - 1].Case as IConstantNode).GetConstant().GetValueAsInt()
                      - (cases[listptr].Case as IConstantNode).GetConstant().GetValueAsInt();
            return isDense(count, range);
        }

        private bool IsCombinationDense(List<SwitchCaseNode> stackTop, List<SwitchCaseNode> newBucket)
        {
            var newBucketCount = newBucket.Count;
            var stackTopCount = stackTop.Count;
            var count = stackTopCount + newBucketCount;
            var range = (newBucket[newBucketCount - 1].Case as IConstantNode).GetConstant().GetValueAsInt()
                      - (stackTop[0].Case as IConstantNode).GetConstant().GetValueAsInt()
                      + 1;
            return isDense(count, range);
        }

        private bool isDense(int count, int range) => count * 2 > range;

        private List<SwitchCaseNode> CombineBuckets(List<SwitchCaseNode> stackTop, List<SwitchCaseNode> newBucket) =>
            stackTop.Concat(newBucket).ToList();

        private string EqualityTestCil(BlaiseType caseType) => caseType switch
        {
            { BasicType: STRING } => @"call bool [System.Private.CoreLib]System.String::op_Equality(string, string)
    brtrue.s",
            _ => "beq.s"
        };

        private string EmitCascadedIfSwitch(SwitchNode node)
        {
            var switchType = node.Input.GetExprType();
            var hiddenSwitchLocal = MakeAndInjectLocalVar(switchType, node);
            var endLabel = MakeLabel();
            var equalityTest = EqualityTestCil(switchType);
            var branchHandling = string.Empty;
            var cases = string.Empty;
            var setup = @$"{EmitExpression(node.Input)}
    stloc {hiddenSwitchLocal}";
            foreach (var st in node.Cases)
            {
                var label = MakeLabel();
                branchHandling += @$"
    ldloc {hiddenSwitchLocal}
    {EmitExpression(st.Case)}
    {equalityTest} {label}
    ";
                cases += @$"
    {label}: nop
    {EmitStat(st.Stat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, st.Stat)}";
            }
            if (node.Default.IsEmpty())
            {
                branchHandling += @$"
    br.s {endLabel}";
            }
            else
            {
                var defaultLabel = MakeLabel();
                branchHandling += @$"
    br.s {defaultLabel}";
                cases += @$"
    {defaultLabel}: nop
    {EmitStat(node.Default)}";
            }
            var ending = @$"
    {endLabel}: nop";
            return string.Join(string.Empty, setup, branchHandling, cases, ending);
        }

        private string MakeDefaultCase(AbstractAstNode defaultStat, string defaultLabel) => defaultStat switch
        {
            AbstractAstNode empty when empty.IsEmpty() => string.Empty,
            AbstractAstNode stat => @$"
    {defaultLabel}: nop
    {EmitStat(defaultStat)}"
        };
    }

    internal class SwitchEmissionComponents
    {
        public List<string> Labels { get; init; } = new();
        public List<string> Branches { get; init; } = new();
        public List<int> Values { get; init; } = new();
        public List<string> Stats { get; init; } = new();
    }
}