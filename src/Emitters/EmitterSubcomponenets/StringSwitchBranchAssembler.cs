using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Emitters.EmitterSubcomponents
{
    public class StringSwitchBranchAssembler
    {
        public static SwitchBranchData AssembleStringSwitchBranches(List<SwitchCaseNode> cases) =>
            cases.Aggregate(new SwitchBranchData(), (switchComponents, swCase) => AddStringCase(switchComponents, swCase));

        private static SwitchBranchData AddStringCase(SwitchBranchData switchComponents, SwitchCaseNode swCase)
        {
            var branchLabel = MakeLabel();
            var caseLabel = MakeLabel();
            var value = (swCase.Case as IConstantNode).GetConstant().GetValueAsString();
            switchComponents.Branches.Add(new StringBranch()
            {
                BranchLabel = branchLabel,
                CaseLabel = caseLabel,
                Value = value,
                Hash = value.GetHashCode()
            });
            switchComponents.Stats.Add(new()
            {
                Label = caseLabel,
                Stat = swCase.Stat
            });
            return switchComponents;
        }

        private static string MakeLabel() => LabelFactory.Singleton.MakeLabel();
    }
}