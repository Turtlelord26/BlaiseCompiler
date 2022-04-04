# Ast Evaluation

This space contains Ast visitors used in semantic analysis and optimization, and associated utilities.

### Ast Evaluator

`AstEvaluator.cs` is the first pass of the Ast after its construction, and checks the program for semantic validity.
The evaluator checks for
- Identifiers are not reserved words
- Variables are declared before use
- Allowability of type casts
- All code paths in a procedure/function return
- Adds explicit return to procedures with an implicit terminal return
- If and Loop conditions are boolean typed
- For loop iterator start and limit values are integer typed.
- Case inputs are an allowed type, and individual cases are literals of the same type
- Returns have expressions of the correct type if inside functions, don't if inside procedures, and aren't in the main program body at all.
- Operands are of allowable and compatible types for their operator.
- Call arguments match procedure/function parameters in type and order.

When it encounters a problem, the evaluator will note an error and attempt to continue. When it finishes, it will indicate by boolean return that the program is not valid.
When `Compiler.cs` sees a false return from the evaluator, it will fetch the list of errors, output them, and then throw an exception (due to the testing infrastructure, as opposed to terminating).
If the Ast passes the Evaluator, it moves on to next visitor.
`AstEvaluator.cs` itself contains a visitor for each AstNode.
`AstEvaluatorUtils.cs` contains utility functions.

### Ast Folder

This optimization visitor collapses expressions of literals, such as `1 + 4` => `5`, and degenerate control flow structures, like `case (14) of ...` or `if (true) ...`.

### Other Utilities

`BlaiseKeywords.cs` contains all reserved words in Blaise, against which identifiers are checked.
`BlaiseSignature.cs` is a data structure for procedure/function signatures.
`BlaiseSignatureFactory.cs` is a factory pattern for producing BlaiseSignatures from FunctionNodes.
`FunctionReturnEvaluator.cs` is a custom, partial visitor on the Ast that determines if all code paths in a procedure/function return.
`ReferenceResolver.cs` contains static utilities for resolving variable and function identifiers to their AstNodes and recording them.
`TypeResolver.cs` contains static utilities for resolving and recording the type of expression nodes.