using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;
using Blaise2.Emitters.EmitterSubcomponents;
using static Blaise2.Ast.ConstType;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {
        private const int LinearSearchThreshold = 3;
        private const string IntegralEqualityCheck = "beq";

        private string EmitNumericSwitch(SwitchNode node)
        {
            var switchType = node.Input.GetExprType();
            var endLabel = MakeLabel();
            var defaultLabel = node.Default.IsEmpty() ? endLabel
                                                      : MakeLabel();
            var branchToDefault = @$"
    br.s {defaultLabel}";
            var defaultCase = MakeDefaultCase(node.Default, defaultLabel);
            var ending = @$"
    {endLabel}: nop";
            var switchLocal = MakeAndInjectLocalVar(switchType, node);
            var setup = @$"{EmitExpression(node.Input)}
    stloc {switchLocal}";
            var branchData = MakeBranchData(node, defaultLabel);
            return string.Join(string.Empty,
                               setup,
                               EmitBranchesWithBinarySearch(branchData.Branches, switchLocal),
                               branchToDefault,
                               EmitLabeledStats(branchData.Stats, endLabel),
                               defaultCase,
                               ending);
        }

        private SwitchBranchData MakeBranchData(SwitchNode node, string defaultLabel) => node.Input.GetExprType() switch
        {
            { BasicType: CHAR or INTEGER } => IntegralSwitchData(node, defaultLabel),
            { BasicType: REAL } => NonintegralSwitchData(node),
            BlaiseType bt => throw new InvalidOperationException($"Encountered invalid switch input type {bt} while emitting.")
        };

        private SwitchBranchData IntegralSwitchData(SwitchNode node, string defaultLabel)
        {
            var buckets = IntegralSwitchBucketer.BucketizeIntegralSwitch(node);
            return SwitchBranchAssembler.AssembleIntegralSwitchBranches(buckets, defaultLabel);
        }

        private SwitchBranchData NonintegralSwitchData(SwitchNode node) =>
            SwitchBranchAssembler.AssembleNonintegralSwitchBranches(node.Cases);

        private string EmitLabeledStats(List<LabeledStat> labeledStats, string endLabel) =>
            string.Join(string.Empty, labeledStats.Select(labeledStat => EmitLabeledStat(labeledStat, endLabel)));

        private string EmitLabeledStat(LabeledStat labeledStat, string endLabel) => @$"
    
    {labeledStat.Label}: nop{EmitStat(labeledStat.Stat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, labeledStat.Stat)}";

        private string EmitBranchesWithBinarySearch(List<ILabeledBranch> branches, string switchLocal) =>
            EmitBranchesWithBinarySearch(branches, switchLocal, 0, branches.Count - 1);

        private string EmitBranchesWithBinarySearch(List<ILabeledBranch> branches, string switchLocal, int startIndex, int endIndex)
        {
            if (endIndex - startIndex < LinearSearchThreshold)
            {
                return EmitBucketsBranches(branches, switchLocal, startIndex, endIndex);
            }
            var midpoint = (startIndex + endIndex + 1) / 2;
            var pivotValue = GetBranchValue(branches[midpoint - 1]);
            var pivotLabel = MakeLabel();
            var pivotLabelLine = @$"
    {pivotLabel}: nop";
            return string.Join(string.Empty,
                               EmitConditionalBranchToSecondHalf(switchLocal, pivotValue, pivotLabel),
                               EmitBranchesWithBinarySearch(branches, switchLocal, startIndex, midpoint - 1),
                               pivotLabelLine,
                               EmitBranchesWithBinarySearch(branches, switchLocal, midpoint, endIndex));
        }

        private AstConstant GetBranchValue(ILabeledBranch labeledBranch) => labeledBranch switch
        {
            JumpTable table => new(table.FirstValue),
            LabeledBranch branch => branch.Value,
            _ => throw new InvalidOperationException($"Encountered unrecognized ILabeledBranch {labeledBranch.GetType()}")
        };

        private string EmitBucketsBranches(List<ILabeledBranch> branches, string switchLocal, int startIndex, int endIndex) =>
            branches.GetRange(startIndex, endIndex - startIndex + 1)
                    .Aggregate(string.Empty, (emit, branch) => emit += GetBranchCil(branch, switchLocal));

        private string GetBranchCil(ILabeledBranch labeledBranch, string hiddenSwitchLocal) => labeledBranch switch
        {
            JumpTable table => @$"
    
    ldloc {hiddenSwitchLocal}
    ldc.i4.s {table.FirstValue}
    sub
    switch ({string.Join(", ", table.Jumps)})",
            LabeledBranch branch => @$"
    
    ldloc {hiddenSwitchLocal}
    {ConstantToCil(GetBranchValue(branch))}
    {IntegralEqualityCheck} {branch.Label}",
            _ => throw new InvalidOperationException($"Encountered unexpected ILabeledBranch {labeledBranch.GetType()}")
        };

        private string EmitConditionalBranchToSecondHalf(string hiddenSwitchLocal, AstConstant pivotValue, string pivotLabel) => @$"
    
    ldloc {hiddenSwitchLocal}
    {ConstantToCil(pivotValue)}
    bgt {pivotLabel}";

        private string ConstantToCil(AstConstant value) => value.ConstType switch
        {
            CharConst or IntConst => $"ldc.i4 {value.GetValueAsInt()}",
            RealConst => $"ldc.r8 {value.GetValueAsReal()}",
            _ => throw new InvalidOperationException($"{value.ConstType} is not a supported numeric switch constant type.")
        };

        private string MakeDefaultCase(AbstractAstNode defaultStat, string defaultLabel) => defaultStat switch
        {
            AbstractAstNode empty when empty.IsEmpty() => string.Empty,
            AbstractAstNode stat => @$"
    {defaultLabel}: nop
    {EmitStat(defaultStat)}"
        };
    }
}