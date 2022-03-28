using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Emitters.EmitterSubcomponents
{
    public class SwitchBranchAssembler
    {
        private const int CaseCountSwitchabilityThreshold = 3;

        public static SwitchBranchData AssembleIntegralSwitchBranches(List<Bucket> buckets, string defaultLabel)
        {
            var switchComponents = new SwitchBranchData();
            foreach (var bucket in buckets)
            {
                switchComponents = bucket.Cases.Count >= CaseCountSwitchabilityThreshold
                    ? AddSwitchableBucketComponents(switchComponents, bucket.Cases, defaultLabel)
                    : AddUnswitchableBucketComponents(switchComponents, bucket.Cases);
            }
            return switchComponents;
        }

        public static SwitchBranchData AssembleNonintegralSwitchBranches(List<SwitchCaseNode> cases) =>
            cases.Aggregate(new SwitchBranchData(), (switchComponents, swCase) => AddCase(switchComponents, swCase));

        private static SwitchBranchData AddSwitchableBucketComponents(SwitchBranchData switchComponents, List<SwitchCaseNode> bucket, string defaultLabel)
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
                switchComponents.Stats.Add(new()
                {
                    Label = label,
                    Stat = swCase.Stat
                });
            }
            switchComponents.Branches.Add(new JumpTable()
            {
                Jumps = jumpLabels,
                FirstValue = firstCaseVal
            });
            return switchComponents;
        }

        private static SwitchBranchData AddUnswitchableBucketComponents(SwitchBranchData switchComponents, List<SwitchCaseNode> bucket) =>
            bucket.Aggregate(switchComponents, (components, swCase) => AddCase(components, swCase));

        private static SwitchBranchData AddCase(SwitchBranchData switchComponents, SwitchCaseNode swCase)
        {
            var label = MakeLabel();
            switchComponents.Branches.Add(new LabeledBranch()
            {
                Label = label,
                Value = (swCase.Case as IConstantNode).GetConstant()
            });
            switchComponents.Stats.Add(new()
            {
                Label = label,
                Stat = swCase.Stat
            });
            return switchComponents;
        }

        private static string MakeLabel() => LabelFactory.Singleton.MakeLabel();
    }
}