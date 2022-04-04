# Blaise Compiler

A compiler that transforms a dialect of Pascal into CIL.
Originally a project for `CPSC 5400: Compiler Principles and Design`, a graduate-level course at Seattle University, but greatly expanded from original specifications.

Source code is lexed and parsed with ANTLR, using the grammar specified in `src/Blaise.g4`. The project must be built to generate the ANTLR code.
The compiler then builds an Abstract Syntax Tree from ANTLR's parse tree and performs a number of semantic analysis and optimization passes.
Valid ASTs are then handed off to the Emitter for translation into CIL.
For programs that are syntactically valid but semantically invalid, the compiler will collect and emit error feedback for all semantic errors it can find. For lexical or syntactic errors, only ANTLR's errors are given.

For visualization and debugging purposes, the compiler also writes the AST to a .dot file. This can be transformed into a human-readable image with Graphviz.

The compiler is also capable of assembling and running the CIL, thanks to some professor-provided code in `src/Internal/`.
I/O behavior is controlled by the driver class `Program.cs`. Adjustment to that class control where programs come from and are compiled to or run and output to.

Subfolders that are not primarily my code are labeled so.

Program Flow:
- Source code is passed into the compiler.
- The compiler runs ANTLR over the source to produce a parse tree.
- The Ast Generator converts the parse tree into an abstract syntax tree.
- The Ast Evaluator checks the Ast for semantic validity.
- The Ast Evaluator checks that all code paths in procedures/functions return.
- The Ast Folder collapses constant expressions like `2 + 3` and degenerate control flow structures like `while (false)`.
- The CIL Emitter converts the transformed Ast into a string of CIL. In this default setup, the CIL is then compiled into .NET object code and run, capturing anything printed to standard out.

# Blaise

Blaise is a dialect of Pascal, and mostly follows standard Pascal conventions. Blaise is statically-typed and whitespace-insensitive.

A Blaise Program is declared with the `program` keyword, an identifier, and a semicolon like so:
```
program ProgramName;

//variable declarations

//functions and procedures

//program body
```

A variable declaration section has a single `var` keyword followed by some number of `name : type` declarations delimited by semicolons.
Variables can only be declared in the variable declaration section of a program or as function parameters. If a variable is to be used in the program body, it must be declared here.
```
var VarName1 : integer;
    VarName2 : string;
    VarName3 : boolean;
```

A procedure is a function that returns void / no value. A function must always return a value. Function calls are not allowed as statements, only in expressions. Procedure calls must be statements.

A procedure is declared with the `procedure` keyword, followed by an identifier, a parenthesized list of zero or more parameters, and a semicolon. Parameters follow the same `name : type` format as variable declarations and are delimited by semicolons.
When calling a procedure (or function), arguments are provided as a comma-delimited list of identifiers or literals.
```
procedure ProcedureName(ParamName1 : integer; ParamName2: string);

//variable declarations

//prodecure body
```

Similarly, a function is declared with the `function` keyword and has an additional `: ReturnType` specification after the parameters.
```
function FunctionName(ParamName1 : integer; ParamName2: string): string;

//variable declarations

//function body
```

Idiomatic Pascal does not use an explicit return statement, instead running a function or procedure body to its end and then returning (if a function, returning the value most recently assigned to a special variable of the same name as the function).
Blaise instead implements the `return` statement well-known to modern programmers. Prodecures simply `return;`, while functions `return "a value";`.
However, procedures are not required to explicitly return. If the return evaluator detects any code path in a procedure body that reaches the end without returning, it will add an implicit return there.

Idiomatic pascal also allows for inner functions/procedures as members of functions/procedures, to arbitrary depth. Blaise has not yet fully implemented this.

A program, proceure, or function body contains a single statement or a block of statements. Blocks are simply zero or more statements in sequence, enclosed by the `begin` and `end` keywords and delimited by semicolons.

The final statement in a procedure/function body is followed by a `;`.
The final statement in a program body is followed by a `.` instead of a `;`, for historical reasons.

### Simple statements:

Block
```
begin
    [[statement]];
    [[statement]];
    [[statement]];
    [[statement]];
end
```

Assignment
```
Identifier := [[expression]]
```

Write
```
write [[expression]]
```

Write line
```
writeln [[expression]]
```

Procedure call
```
ProcedureIdentifier([[arguments]])
```

Return
```
return [[optional expression]]
```

### Control flow statements:

If-then
```
if ([[expression]])
then
    [[statement]];
```

If-then-else
```
if ([[expression]])
then
    [[statement]]
else
    [[statement]];
```

While loop
```
while ([[expression]]) do
    [[statement]]
```

Until loop
```
repeat
    [[statement]];
    [[statement]];
    [[statement]];
    [[statement]];
until ([[expression]])
```
Until loops are weird in that they explicitly use a list of one or more statements instead of requiring the use of block. This is Pascal convention.

For loop
```
for VariableIdentifier := [[an integer]] [[to/downto]] [[a limit integer]] do
    [[statement]]
```
For loops assign an integer to a variable (which must be previously declared), and increment (if `to`) or decrement (if `downto`) that variable each iteration of the loop body. The loop condition is either `variable < limit` or `variable limit` depending on `to`/`downto`. The loop body is not executed when the variable is equal to the limit.
Example:
```
for x := 0 to 10 do:
    y := y + x * x;
```

Case/Switch
```
case ([[expression]]) of
    [[expression]] : [[statement]];
    [[expression]] : [[statement]];
    [[expression]] : [[statement]];
else
    [[statement]];
end
```
The case statement (as referred to in pascal, though known better as switch in C-like langauges) consists of an input expression and one or more cases, which determine a statement to execute based on the input.
The input expression is required to evaluate to a char, integer, real, or string.
The case expressions are required to be literals of the same type as the input.
The `else` branch (corresponding to case default in C-like languages) is optional.

### Types and literals

Blaise supports expressions of type boolean, char, integer, real, and string. These are the same as .NET boolean, char, int32, double, and string types.
Boolean literals are either `true` or `false`.
Integer literals are one or more digits and an optional sign, eg. `0`, `+12`, `-45`.
Real literals are one or more digits on both sides of a decimal point and an optional sign, eg. `1.0`, `+0.5`, `-3.141592653589793238462643383279`
Char literals are a single character in single quotes, eg `'a'`, `'$'`, `'3'`, `'\n'`
String literals are zero or more than one caracter in single quotes, eg. `'Hello, World!'`, `'\r\n'`, `''`
- Blaise recognizes all escape characters recognized by .NET.

### Expressions

Expressions are either literals, function calls, variable identifiers, or operations on those.

#### Operators

Order of precedence: mathematical operators, then boolean operators, then logical operators.

Mathematical operators follow order of operations.
Boolean operators are evaluated left-to-right.
Logical operators are evaluated in order: Not, And, Or.

##### Mathematical operators

Addition: chars, integers, reals, strings
```
[[expression]] + [[expression]]
```

Subtraction: chars, integers, reals
```
[[expression]] + [[expression]]
```

Multiplication: chars, integers, reals
```
[[expression]] * [[expression]]
```

Division: chars, integers, reals
```
[[expression]] / [[expression]]
```

Exponentiation: chars, integers, reals
```
[[expression]] ^ [[expression]]
```

Parenthesization:
```
([[expression]])
```

##### Boolean operators

Greater than:
```
[[expression]] > [[expression]]
```

Less than:
```
[[expression]] < [[expression]]
```

Equals:
```
[[expression]] = [[expression]]
```

Not equals:
```
[[expression]] <> [[expression]]
```

Greater than or equal:
```
[[expression]] >= [[expression]]
```

Less than or equal:
```
[[expression]] <= [[expression]]
```

##### Logical operators

And:
```
[[expression]] and [[expression]]
```

Or:
```
[[expresion]] or [[expression]]
```

Not:
```
not [[expression]]
```

### Future Development

- [ ] Language support for extended types: array and set
- [ ] Support multiple variable names per type, eg. `var x, y : integer`
- [ ] Support inner functions
- [ ] Semantic analysis: detect unreachable code in procedures/functions with multiple return points (implicit or explicit)
- [ ] Compiler optimization: function inlining
- [ ] Compiler optimization: avoid emitting functions that are never called