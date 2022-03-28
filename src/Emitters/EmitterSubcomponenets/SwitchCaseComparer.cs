using System;
using System.Collections.Generic;
using Blaise2.Ast;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Emitters.EmitterSubcomponents
{
    class SwitchCaseNodeComparer : IComparer<SwitchCaseNode>
    {
        private BlaiseType CaseType;

        public SwitchCaseNodeComparer(BlaiseType caseType) => CaseType = caseType;

        public int Compare(SwitchCaseNode a, SwitchCaseNode b)
        {
            return CaseType.BasicType switch
            {
                CHAR => (a.Case as IConstantNode).GetConstant().GetValueAsChar()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsChar()),
                INTEGER => (a.Case as IConstantNode).GetConstant().GetValueAsInt()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsInt()),
                REAL => (a.Case as IConstantNode).GetConstant().GetValueAsReal()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsReal()),
                STRING => (a.Case as IConstantNode).GetConstant().GetValueAsString().GetHashCode()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsString().GetHashCode()),
                _ => throw new InvalidOperationException($"Invalid switch case type {CaseType} assigned to Case comparer.")
            };
        }
    }
}