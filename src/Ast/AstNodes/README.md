# AstNodes

This space contains the nodes of the abstract syntax tree in `AstNodes.cs`, along with utilities.

### Interfaces

IVarOwner indicates a node that is allowed to declare variables, and requires such nodes to implement a method to retrieve information about a variable by name.

IConstantNode indicates a node that holds a literal, and requires implementation of a method to fetch that literal as an AstConstant.

### Extensions

`AstNodeExtensions.cs` contains Builder pattern methods used by the Ast generator.

### Data

`AstConstant.cs` is a boxed literal. It can be a boolean, integer, real, char, or string.
`AstConstantComparer.cs` provides the ability to compare AstConstants without unboxing them.
`ConstType.cs` is an enum that is referenced to determine the type of value held in an AstConstant.

`VarType.cs` is a provided enum that classifies a variable as global, argument, or local.
`SymbolInfo.cs` is a provided data structure that packages a VarDeclNode with a VarType value.

`LoopType.cs` is an enum that classifies a LoopNode as while, for, or until.