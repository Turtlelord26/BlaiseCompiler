using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;
using Blaise2.Emitters.EmitterSubcomponents;

namespace Blaise2.Emitters
{
    public partial class CilEmitter : AbstractAstVisitor<string>
    {
        private const int MinimumStringCasesToUseHashing = 7;
        private const string StringEqualityCheck = @"call bool [System.Private.CoreLib]System.String::op_Equality(string, string)
    brtrue.s";

        private string EmitStringSwitch(SwitchNode node)
        {
            var switchType = node.Input.GetExprType();
            var useHashing = node.Cases.Count >= MinimumStringCasesToUseHashing;
            var endLabel = MakeLabel();
            var defaultLabel = node.Default.IsEmpty() ? endLabel
                                                      : MakeLabel();
            var branchToDefault = @$"
    br.s {defaultLabel}";
            var ending = @$"
    {endLabel}: nop";
            var switchLocal = MakeAndInjectLocalVar(switchType, node);
            var stringHashLocal = useHashing ? MakeAndInjectLocalVar(new() { BasicType = BlaiseTypeEnum.INTEGER }, node) : string.Empty;
            var setup = MakeStringSwitchSetup(node.Input, switchLocal, stringHashLocal, useHashing);
            node.Cases.Sort(new SwitchCaseNodeComparer(switchType));
            var branchData = StringSwitchData(node.Cases);
            return string.Join(string.Empty,
                               setup,
                               EmitStringBranchesWithBinarySearch(branchData.Branches, stringHashLocal, switchLocal),
                               branchToDefault,
                               EmitLabeledStats(branchData.Stats, endLabel),
                               MakeDefaultCase(node.Default, defaultLabel),
                               ending);
        }

        private string MakeStringSwitchSetup(AbstractTypedAstNode input, string switchLocal, string stringHashLocal, bool useHashing) =>
            useHashing
            ? @$"{VisitExpression(input)}
    dup
    stloc {switchLocal}
    callvirt instance int32 [System.Private.CoreLib]System.Object::GetHashCode()
    stloc {stringHashLocal}"
            : @$"{VisitExpression(input)}
    stloc {switchLocal}";

        private SwitchBranchData StringSwitchData(List<SwitchCaseNode> cases) =>
            StringSwitchBranchAssembler.AssembleStringSwitchBranches(cases);

        private string EmitStringBranchesWithBinarySearch(List<ILabeledBranch> branches, string stringHashLocal, string switchLocal)
        {
            var useBinarySearchOnHashes = branches.Count >= MinimumStringCasesToUseHashing;
            var branchesCil = string.Empty;
            if (useBinarySearchOnHashes)
            {
                branchesCil += EmitBinarySearchOnHashedBranches(branches, stringHashLocal, 0, branches.Count - 1);
            }
            branchesCil += branches.Aggregate(string.Empty, (cil, branch) => cil += GetStringBranchCil(branch, switchLocal, useBinarySearchOnHashes));
            return branchesCil;
        }

        private string EmitBinarySearchOnHashedBranches(List<ILabeledBranch> branches,
                                                        string stringHashLocal,
                                                        int startIndex,
                                                        int endIndex)
        {
            if (endIndex - startIndex < LinearSearchThreshold)
            {
                return EmitHashedBranches(branches, stringHashLocal, startIndex, endIndex);
            }
            var midpoint = (startIndex + endIndex + 1) / 2;
            var pivotValue = GetStringBranchHash(branches[midpoint - 1]);
            var pivotLabel = MakeLabel();
            var pivotLabelLine = @$"
    
    {pivotLabel}: nop";
            return string.Join(string.Empty,
                               EmitHashedConditionalBranchToSecondHalf(stringHashLocal, pivotValue, pivotLabel),
                               EmitBinarySearchOnHashedBranches(branches, stringHashLocal, startIndex, midpoint - 1),
                               pivotLabelLine,
                               EmitBinarySearchOnHashedBranches(branches, stringHashLocal, midpoint, endIndex));
        }

        private string EmitHashedConditionalBranchToSecondHalf(string switchHashLocal, int pivotValue, string pivotLabel) => @$"
    
    ldloc {switchHashLocal}
    ldc.i4 {pivotValue}
    bgt {pivotLabel}";

        private string EmitHashedBranches(List<ILabeledBranch> branches, string stringHashLocal, int startIndex, int endIndex) =>
            branches.GetRange(startIndex, endIndex - startIndex + 1)
                    .Aggregate(string.Empty, (emit, branch) => emit += GetHashBranchCil(branch, stringHashLocal));

        private string GetHashBranchCil(ILabeledBranch labeledBranch, string stringHashLocalVar) => labeledBranch switch
        {
            StringBranch branch => @$"
    
    ldloc {stringHashLocalVar}
    ldc.i4 {GetStringBranchHash(branch)}
    {IntegralEqualityCheck} {branch.BranchLabel}",
            _ => throw new InvalidOperationException($"Encountered unexpected ILabeledBranch {labeledBranch.GetType()} while emitting string switch.")
        };

        private int GetStringBranchHash(ILabeledBranch labeledBranch) => labeledBranch switch
        {
            StringBranch branch => branch.Hash,
            _ => throw new InvalidOperationException($"Encountered unexpected ILabeledBranch {labeledBranch.GetType()} while emitting string switch.")
        };

        private string GetStringBranchCil(ILabeledBranch labeledBranch, string switchLocalVar, bool prependLabel) => labeledBranch switch
        {
            StringBranch branch => @$"
    
    {(prependLabel ? branch.BranchLabel + ": " : "")}ldloc {switchLocalVar}
    ldstr ""{branch.Value}""
    {StringEqualityCheck} {branch.CaseLabel}",
            _ => throw new InvalidOperationException($"Encountered unexpected ILabeledBranch {labeledBranch.GetType()} while emitting string switch.")
        };
    }
}