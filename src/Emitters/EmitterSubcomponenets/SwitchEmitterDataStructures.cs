using System.Collections.Generic;
using Blaise2.Ast;

namespace Blaise2.Emitters.EmitterSubcomponents
{
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

    public class SwitchBranchData
    {
        public List<ILabeledBranch> Branches { get; init; } = new();
        public List<LabeledStat> Stats { get; init; } = new();
    }

    public interface ILabeledBranch { }

    public struct JumpTable : ILabeledBranch
    {
        public List<string> Jumps;
        public int FirstValue;
    }

    public struct LabeledBranch : ILabeledBranch
    {
        public string Label;
        public AstConstant Value;
    }

    public struct StringBranch : ILabeledBranch
    {
        public string BranchLabel;
        public string CaseLabel;
        public string Value;
        public int Hash;
    }

    public struct LabeledStat
    {
        public AbstractAstNode Stat;
        public string Label;
    }
}