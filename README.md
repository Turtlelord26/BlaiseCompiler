[![Open in Visual Studio Code](https://classroom.github.com/assets/open-in-vscode-f059dc9a6f8d3a56e377f745f24479a46679e63a5d9fe6f495e02850cd0d8118.svg)](https://classroom.github.com/online_ide?assignment_repo_id=7072287&assignment_repo_type=AssignmentRepo)
# Blaise2

Starting with a given set of AST nodes, write an emitter to compile a simple program:

```
program Printing;

var x : integer;

begin
    x := 12;
    writeln( 4 * x + 3 );
end.
```

# Explanation

We're starting from a fresh set of code to make sure we're all on the same page going forward. This code has a set of AST nodes in [src/Ast/AstNodes.cs](src/Ast/AstNodes.cs). These may look like yours from the previous assignment, or they may not; but they should look similar enough that you won't have any problems. (That said, if you want to change them, go for it! The only files you cannot change are the files under [tests/](tests/).)

Look for the `TODO` comments in [src/Emitters/CilEmitter.cs](src/Emitters/CilEmitter.cs) and [src/Emitters/AstNodeUtils.cs](src/Emitters/AstNodeUtils.cs). I'm not going to tell you where other code is; cmd-click or ctrl-click is your friend! When in doubt, click through to see what a function does.

# Assignment

As before, you just have to pass the tests; in this case, [CompilerTests.cs](tests/CompilerTests.cs) is the important one. This test requires your compiler to emit correct CIL, which gets assembled and run.
