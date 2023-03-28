# Emitter

This space contains the emitter that converts the abstract syntax tree to CIL, and utilities.

### Cil Emitter

This is a large class of highly-coupled components that resists break-up into smaller classes. The following files are all `partial class CilEmitter`:

`CilEmitter.cs` visits the abstract syntax tree and assembles a string of CIL
`CilEmitterUtils.cs` contains utilities for CilEmitter.
`CilEmitter_NumericSwitch.cs` contains logic for emitting case statements on char, integer, or real typed inputs.
`CilEmitter_StringSwitch.cs` contains logic for emitting case statements on string inputs.

A number of optimizations are applied to case statements during emission. 
- Char- and integer-typed case statements are bucketized based on density and use a jump table via the CIL `switch` command on buckets of at least 3 cases.
- Nonstring case statements sort their cases and use binary search to get to the correct jump.
- String case statements of at least 7 cases sort and binary search on the string hashes to get to the correct string equality check.

I am deeply appreciative of the switch emission comments in [roslyn](https://github.com/dotnet/roslyn), on whose logic I based these optimizations.

`BlaiseTypeExtensions.cs` is a simple extension method to convert a BlaiseType to its equivalent CIL text.

### Emitter Subcomponents

Every scrap of functionality I could pull out of the emitter into a black box utility class.

`LabelFactory.cs` produces a unique CIL label string each time it is called.
`VarFactory.cs` produces a uniue CIL variable name each time it is called.
`SwitchEmitterDataStructures.cs` contains several data structures used to transform lists of switch cases into more optimal forms.
`SwitchCaseComparer.cs` is an IComparer used to sort Lists of SwitchCaseNodes.
`IntegralSwitchBucketer.cs` converts a list of SwitchCaseNodes into a list of Buckets of SwitchCaseNodes.
`SwitchBranchAssembler.cs` converts Buckets of SwitchCaseNodes into a list of ILabeledBranches.
`StringSwitchBranchAssembler.cs` converts a list of SwitchCaseNodes into a list of StringBranches.
