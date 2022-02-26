using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.VarType;

namespace Blaise2.Emitters
{
    [SuppressMessage("Performance", "CA1822", Justification = "all methods must be instance methods to participate in dynamic dispatch")]
    public partial class CilEmitter
    {
        private const string nopDelimiter = @"
    nop";
        private string Cil = string.Empty;
        private string ProgramName;

        public string Visit(ProgramNode node)
        {
            ProgramName = node.Identifier;

            Cil += Preamble();

            Cil += $@"
.class public auto ansi beforefieldinit {node.Identifier}
    extends [System.Private.CoreLib]System.Object
{{";
            var fields = string.Join('\n', node.VarDecls.OfType<VarDeclNode>().Select(f => $@"
    .field public {f.BlaiseType.ToCilType()} {f.Identifier}"));
            Cil += @$"
    {fields}";

            // TODO: emit routines as methods

            Cil += $@"
  // Methods
  .method public hidebysig static void main() cil managed
  {{
    .entrypoint
    .locals init (
      [0] class {ProgramName}
    )

    nop
    newobj instance void {ProgramName}::.ctor()
    stloc.0";

            Cil += Visit((dynamic)node.Stat);

            Cil += @"
    nop
    ret
  } // end of main
";
            Cil += Postamble();

            return Cil;
        }

        public string Visit(FunctionNode node)
        {

            var isFunction = node.IsFunction;
            var returnType = isFunction ? node.ReturnType.ToCilType() : "void";
            var output = @$"
    .method public hidebysig 
        instance {returnType} {node.Identifier} (";
            output += string.Join("", node.Params.Select(p => @$"
            {p.BlaiseType.ToCilType()} {p.Identifier}"));
            output += @"
        ) cil managed
    {";
            if (isFunction | node.VarDecls.Count > 0)
            {
                var index = 0;
                output += @$"
        .locals init (";
                if (isFunction)
                {
                    output += @$"
            [0] {node.ReturnType.ToCilType()}";
                    index++;
                }
                foreach (var v in node.VarDecls)
                {
                    output += @$"
        [{index}] {v.BlaiseType.ToCilType()} {v.Identifier}";
                    index++;
                }
                output += @"
        )
        nop";
            }
            //Make a findable VarDecl for the return value if its a function
            if (isFunction)
            {
                node.VarDecls.Add(Build<VarDeclNode>(n =>
                {
                    n.Identifier = node.Identifier;
                    n.BlaiseType = node.ReturnType;
                }));
            }
            //THIS IS WHERE FUNCS AND PROCS WOULD GO
            output += Visit((dynamic)node.Stat);
            output += @"
        nop
        ldloc.0
        ret
    }";
            return output;
        }

        public string Visit(BlockNode node) => string.Join(nopDelimiter, node.Stats.Select(s => Visit((dynamic)s)));

        public string Visit(WriteNode node)
        {
            var exprType = (node.Expression as ITypedNode).GetExprType().ToCilType();
            var method = node.WriteNewline ? "WriteLine" : "Write";
            return @$"
    {Visit((dynamic)node.Expression)}
    call void [System.Console]System.Console::{method}({exprType})";
        }

        public string Visit(AssignmentNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{Visit((dynamic)node.Expression)}
    stfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"{Visit((dynamic)node.Expression)}
    stloc {info.VarDecl.Identifier}",
                Argument => @$"{Visit((dynamic)node.Expression)}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public string Visit(LoopNode node)
        {
            throw new NotImplementedException();
        }

        public string Visit(FunctionCallNode node)
        {
            //Put Call target on node later? Need to calc it during semantic analysis anyway.
            var target = node.CallTarget;
            if (target is FunctionNode)
            {
                var func = target as FunctionNode;
                var paramTypes = string.Join(' ', func.Params.Select(p => p.BlaiseType.ToCilType()));
                var argExprs = string.Join('\n', node.Arguments.Select(a => Visit((dynamic)a)).Reverse());
                return @$"
    ldarg.0
    {argExprs}
    call instance {func.ReturnType.ToCilType()} {ProgramName}::{func.Identifier}({paramTypes})";
            }
            else
            {
                var proc = target as FunctionNode;
                var paramTypes = string.Join(' ', proc.Params.Select(p => p.BlaiseType.ToCilType()));
                var argExprs = string.Join('\n', node.Arguments.Select(a => Visit((dynamic)a)).Reverse());
                return @$"
    ldarg.0
    {argExprs}
    call instance void {ProgramName}::{proc.Identifier}({paramTypes})";
            }
        }

        public string Visit(IntegerNode node) => @$"
    ldc.i4.s {node.IntValue}";

        public string Visit(RealNode node) => @$"
    ldc.r4 {node.RealValue}";

        public string Visit(StringNode node) => @$"
    ldstr {node.StringValue}";

        public string Visit(VarRefNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            return info.VarType switch
            {
                Global => @$"
    ldloc.0
    ldfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    ldloc {info.VarDecl.Identifier}",
                Argument => @$"
    ldarg {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public string Visit(BinaryOpNode node) => $"{Visit((dynamic)node.Left)}{Visit((dynamic)node.Right)}{ToCilOperator(node.Operator)}";

        public string Visit(BooleanOpNode node) => $"{Visit((dynamic)node.Left)}{Visit((dynamic)node.Right)}{ToCilOperator(node.Operator)}";
    }
}