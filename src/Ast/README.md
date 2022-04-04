# Ast

This space contains classes that build or modify the abstract syntax tree.

### Ast Generator

`AstGenerator.cs` builds the abstract syntax tree by visiting ANTLR's generated parse tree.
`AstGeneratorUtils.cs` contains some static utilities used by the generator to interpret and translate the parse tree.

### Data Classes

`BlaiseOperator.cs` is an enum that specifies an operator. It is used by operator AstNodes to record which operator they contain.
`BlaiseType.cs` is boxed type information for variables, parameters, arguments, etc, capable of specifying primitive types or extended types, such as arrays.
`BlaiseTypeEnum.cs` is an enum that specifies a primitive type.