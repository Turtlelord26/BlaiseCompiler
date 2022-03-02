using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.BlaiseTypeEnum;
using static Blaise2.Ast.LoopType;
using static Blaise2.Ast.VarType;

namespace Blaise2.Emitters
{
    [SuppressMessage("Performance", "CA1822", Justification = "all methods must be instance methods to participate in dynamic dispatch")]
    public partial class CilEmitter
    {
        private const string nopDelimiter = @"
    nop";
        private int labelNum = 0;
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
            var exprType = node.Expression.GetExprType().ToCilType();
            var method = node.WriteNewline ? "WriteLine" : "Write";
            return @$"
    {Visit((dynamic)node.Expression)}
    call void [System.Console]System.Console::{method}({exprType})";
        }

        public string Visit(AssignmentNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            var varType = info.VarDecl.BlaiseType;
            var implicitConversion = varType.Equals(node.Expression.GetExprType()) ? "" : varType.ToCilType() switch
            {
                "float64" => "conv.r8",
                "int32" => "conv.i4",
                "char" => "conv.u2",
                _ => throw new InvalidOperationException($"Encountered unexpected CIL type {varType.ToCilType()} during {varType} assignment implicit casting.")
            };
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{Visit((dynamic)node.Expression)}
    {implicitConversion}
    stfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    {Visit((dynamic)node.Expression)}
    {implicitConversion}
    stloc {info.VarDecl.Identifier}",
                Argument => @$"
    {Visit((dynamic)node.Expression)}
    {implicitConversion}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public string Visit(IfNode node)
        {
            var thenLabel = MakeLabel();
            var endLabel = MakeLabel();
            return @$"
    {Visit((dynamic)node.Condition)}
    brtrue.s {thenLabel}
    {Visit((dynamic)node.ElseStat)}
    br.s {endLabel}
    {thenLabel}: nop
    {Visit((dynamic)node.ThenStat)}
    {endLabel}: nop";
        }

        public string Visit(LoopNode node)
        {
            if (node.LoopType == While || node.LoopType == Until)
            {
                var bodyLabel = MakeLabel();
                var endLabel = MakeLabel();
                var brEnd = node.LoopType == While ? "brfalse" : "brtrue";
                var brBody = node.LoopType == While ? "brtrue" : "brfalse";
                var conditionCil = Visit((dynamic)node.Condition);
                return @$"
    {conditionCil}
    {brEnd}.s {endLabel}
    {bodyLabel}: nop
    {Visit((dynamic)node.Body)}
    {conditionCil}
    {brBody}.s {bodyLabel}
    {endLabel}: nop";
            }
            throw new InvalidOperationException($"Invalid Loop Type {node.LoopType}");
        }

        public string Visit(ForLoopNode node)
        {
            if (node.LoopType == For)
            {
                var bodyLabel = MakeLabel();
                var endLabel = MakeLabel();
                var conditionCil = Visit((dynamic)node.Condition);
                return @$"
    {Visit(node.Assignment)}
    {conditionCil}
    brfalse.s {endLabel}
    {bodyLabel}: nop
    {Visit((dynamic)node.Body)}
    {Visit(node.Iteration)}
    {conditionCil}
    brtrue.s {bodyLabel}
    {endLabel}: nop";
            }
            throw new InvalidOperationException($"Invalid Loop Type {node.LoopType}");
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
    ldc.r8 {node.RealValue}";

        public string Visit(BooleanNode node) => @$"
    ldc.i4.{(node.BoolValue ? 1 : 0)}";

        public string Bisit(CharNode node) => @$"
    ldc.i4.s {(int)node.CharValue}";

        public string Visit(StringNode node) => @$"
    ldstr ""{node.StringValue}""";

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

        public string Visit(BinaryOpNode node)
        {
            var output = Visit((dynamic)node.Left);
            if (!node.LeftType.Equals(node.ExprType))
            {
                output += TypeConvert(node.LeftType, node.ExprType);
            }
            output += Visit((dynamic)node.Right);
            if (!node.RightType.Equals(node.ExprType))
            {
                output += TypeConvert(node.RightType, node.ExprType);
            }
            return output + ToCilOperator(node.Operator);
        }

        public string Visit(BooleanOpNode node)
        {
            var leftBasicType = node.LeftType.BasicType;
            var rightBasicType = node.RightType.BasicType;
            var output = Visit((dynamic)node.Left);
            if (leftBasicType < rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.LeftType, node.RightType);
            }
            output += Visit((dynamic)node.Right);
            if (leftBasicType > rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.RightType, node.LeftType);
            }
            return output + ToCilOperator(node.Operator);
        }

        public string Visit(AbstractAstNode node) => node.IsEmpty() ? "" : throw new InvalidOperationException($"Invalid node type {node.Type}");

        private string MakeLabel() => $"Label{labelNum++}";
    }
}