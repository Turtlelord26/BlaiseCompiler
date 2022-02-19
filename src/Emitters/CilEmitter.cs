using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.VarType;

namespace Blaise2.Emitters
{
    [SuppressMessage("Performance", "CA1822", Justification = "all methods must be instance methods to participate in dynamic dispatch")]
    public partial class CilEmitter : AstVisitorBase<string>
    {
        private const string nopDelimiter = @"
    nop";
        private string Cil = string.Empty;
        private string ProgramName;

        public override string Visit(ProgramNode node)
        {
            ProgramName = node.ProgramName;

            Cil += Preamble();

            Cil += $@"
.class public auto ansi beforefieldinit {node.ProgramName}
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

        public override string Visit(ProcedureNode node)
        {
            var output = @$"
    .method public hidebysig 
        instance void {node.Identifier} (";
            output += string.Join("", node.Args.Select(p => @$"
            {p.BlaiseType.ToCilType()} {p.Identifier}"));
            output += @"
        ) cil managed
    {";
            if (node.VarDecls.Count > 0)
            {
                output += @"
        .locals init (";
                var index = 0;
                foreach (var v in node.VarDecls)
                {
                    v.Index = index;
                    output += @$"
            [{v.Index}] {v.BlaiseType.ToCilType()} {v.Identifier}";
                }
                index++;
                output += @"
        )
        nop";
            }
            //THIS IS WHERE FUNCS AND PROCS WOULD GO
            output += Visit((dynamic)node.Stat);
            output += @"
        nop
        ret
    }";
            return output;
        }

        public override string Visit(FunctionNode node)
        {
            //Make a local for the return value
            node.VarDecls.Add(Build<VarDeclNode>(n =>
            {
                n.Identifier = node.Identifier;
                n.BlaiseType = node.ReturnType;
                n.Index = 0;
            }));
            var output = @$"
    .method public hidebysig 
        instance {node.ReturnType.ToCilType()} {node.Identifier} (";
            output += string.Join("", node.Args.Select(p => @$"
            {p.BlaiseType.ToCilType()} {p.Identifier}"));
            output += @"
        ) cil managed
    {";
            output += @$"
        .locals init (
            [0] {node.ReturnType.ToCilType()}";
            if (node.VarDecls.Count > 0)
            {
                var index = 1;
                foreach (var v in node.VarDecls)
                {
                    v.Index = index;
                    output += @$"
            [{v.Index}] {v.BlaiseType.ToCilType()} {v.Identifier}";
                }
                index++;
            }
            output += @"
        )
        nop";
            //THIS IS WHERE FUNCS AND PROCS WOULD GO
            output += Visit((dynamic)node.Stat);
            output += @"
        nop
        ldloc.0
        ret
    }";
            return output;
        }

        public override string Visit(BlockNode node) => string.Join(nopDelimiter, node.Stats.Select(s => Visit((dynamic)s)));

        public override string Visit(WriteNode node)
        {
            var output = "\n" + Visit((dynamic)node.Expression);
            //Need to pull type information from the child node to pass to WriteLine for more general use.
            if (node.WriteNewline)
            {
                output += @"
    call void [System.Console]System.Console::WriteLine(int32)";
            }
            else
            {
                output += @"
    call void [System.Console]System.Console::Write(int32)";
            }
            return output;
        }

        public override string Visit(AssignmentNode node)
        {
            var info = FindVariable(node, node.Identifier);
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{Visit((dynamic)node.Expression)}
    stfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"{Visit((dynamic)node.Expression)}
    stloc.{info.VarDecl.Index}",
                Argument => @$"{Visit((dynamic)node.Expression)}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public override string Visit(FunctionCallNode node)
        {
            //Put Call target on node later? Need to calc it during semantic analysis anyway.
            var target = FindFunction(node, node.Identifier);
            if (target is FunctionNode)
            {
                var func = target as FunctionNode;
                var paramTypes = string.Join(' ', func.Args.Select(p => p.BlaiseType.ToCilType()));
                var argExprs = string.Join('\n', node.ArgumentExpressions.Select(a => Visit((dynamic)a)).Reverse());
                return @$"
    ldarg.0
    {argExprs}
    call instance {func.ReturnType.ToCilType()} {ProgramName}::{func.Identifier}({paramTypes})";
            }
            else
            {
                var proc = target as FunctionNode;
                var paramTypes = string.Join(' ', proc.Args.Select(p => p.BlaiseType.ToCilType()));
                var argExprs = string.Join('\n', node.ArgumentExpressions.Select(a => Visit((dynamic)a)).Reverse());
                return @$"
    ldarg.0
    {argExprs}
    call instance void {ProgramName}::{proc.Identifier}({paramTypes})";
            }
        }

        public override string Visit(IntegerNode node) => @$"
    ldc.i4.s {node.IntValue}";

        public override string Visit(RealNode node) => @$"
    ldc.r4 {node.RealValue}";

        public override string Visit(StringNode node) => @$"
    ldstr {node.StringValue}";

        public override string Visit(VarRefNode node)
        {
            var info = FindVariable(node, node.Identifier);
            return info.VarType switch
            {
                Global => @$"
    ldloc.0
    ldfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    ldloc.{info.VarDecl.Index}",
                Argument => @$"
    ldarg.{info.VarDecl.Index}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public override string Visit(BinaryOpNode node) => $"{Visit((dynamic)node.Lhs)}{Visit((dynamic)node.Rhs)}{ToCilOperator(node.Op)}";

        public override string Visit(BooleanOpNode node) => $"{Visit((dynamic)node.Lhs)}{Visit((dynamic)node.Rhs)}{ToCilOperator(node.Op)}";
    }
}