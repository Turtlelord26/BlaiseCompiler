# Visualizations

`AstStringTree.cs` builds a LISP-like string representation of an abstract syntax tree.
`DotRenderer.cs` outputs a .dot file from an abstract syntax tree to the Blaise root directory.

.dot files are intended to be used with a program called graphviz - see [GraphViz](https://graphviz.org/).
With graphviz installed, one can use `dot expr.dot -Tpng -o expr.png` in a terminal to produce a .png graph representation of the Ast.

The core of AstStringTree was provided code, but I have largely rewritten it to interface with my base Ast visitor.