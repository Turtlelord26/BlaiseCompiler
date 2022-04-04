## src

The source directory directly holds the entry point `Program.cs`, the controller `Compiler.cs`, the grammar `Blaise.g4`, and the abstract AstVisitor base class.

`Program.cs` controls I/O and calls the compiler with a source code string and output settings.
`Compiler.cs` runs ANTLR and the AST generator and visitors in sequence, ending with the emitter.
`Blaise.g4` specifies the lexer and parser rules for ANTLR.
`AbstractAstVisitor.cs` provides polymorphic method dispatch for statements and expressions, and enforces complete tree visitation on subclasses.

Subdirectories:
- `Ast` contains structural, visitor, and utility classes related to the abstract syntax tree.
- `Emitters` contains the CIL emitter class and utilities.
- `Errors` contains provided Exception classes used to detect ANTLR errors.
- `Internal` contains provided code to compile and execute the compiler's CIL output.
- `Visualizations` includes utilities to output human-readable versions of ASTs.