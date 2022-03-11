using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.LoopType;
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
        private List<VarDeclNode> MainLocals = new();

        public string Visit(ProgramNode node)
        {
            ProgramName = node.Identifier;
            var body = Visit((dynamic)node.Stat);
            var fields = node.VarDecls.Aggregate("", (cil, v) => cil += $@"
    .field public {v.BlaiseType.ToCilType()} {v.Identifier}");
            var methods = node.Procedures.Concat(node.Functions).Aggregate("\n", (cil, next) => cil += Visit(next));
            var localIndex = 1;
            var mainLocals = MainLocals.Aggregate("", (cil, l) => cil += $",\n\t\t[{localIndex++}] {l.BlaiseType.ToCilType()} {l.Identifier}");
            return @$"{Preamble()}
.class public auto ansi beforefieldinit {node.Identifier}
    extends [System.Private.CoreLib]System.Object
{{{fields}

    // Methods
    
    {methods}

  .method public hidebysig static void main() cil managed
  {{
    .entrypoint
    .locals init (
        [0] class {ProgramName}{mainLocals}
    )

    nop
    newobj instance void {ProgramName}::.ctor()
    stloc.0
    {body}
    nop
    ret
  }} // end of main
  {Postamble()}
";
        }

        public string Visit(FunctionNode node)
        {
            var body = Visit((dynamic)node.Stat);
            var isFunction = node.IsFunction;
            var returnType = isFunction ? node.ReturnType.ToCilType() : "void";
            var paramsList = string.Join(",\n\t\t", node.Params.Select(p => $"{p.BlaiseType.ToCilType()} {p.Identifier}"));
            var index = 1;
            var locals = string.Join("", node.VarDecls.Select(v => $",\n\t\t[{index++}] {v.BlaiseType.ToCilType()} {v.Identifier}"));
            var output = @$"
  .method public hidebysig 
    instance {returnType} {node.Identifier} (
        {paramsList}
    ) cil managed
  {{
    .locals init (
        [0] class {ProgramName}{locals}
    )
    ldarg.0
    stloc.0
    {body}
  }}";
            //THIS IS WHERE INNER FUNCS AND PROCS WOULD GO
            if (node.Functions.Count() > 0 | node.Procedures.Count() > 0)
            {
                throw new NotImplementedException("Can't emit nested functions yet");
            }
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
            var exprType = node.Expression.GetExprType();
            var typeConversion = varType.Equals(exprType) ? ""
                                                          : TypeConvert(exprType, varType, node);
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{Visit((dynamic)node.Expression)}
    {typeConversion}
    stfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    {Visit((dynamic)node.Expression)}
    {typeConversion}
    stloc {info.VarDecl.Identifier}",
                Argument => @$"
    {Visit((dynamic)node.Expression)}
    {typeConversion}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public string Visit(IfNode node)
        {
            var thenLabel = MakeLabel();
            var returns = FunctionReturnEvaluator.Visit(node);
            var output = @$"
    {Visit((dynamic)node.Condition)}
    brtrue.s {thenLabel}";
            var elseStat = @$"
    {Visit((dynamic)node.ElseStat)}";
            var thenStat = @$"
    {thenLabel}: nop
    {Visit((dynamic)node.ThenStat)}";
            if (!returns)
            {
                var endLabel = MakeLabel();
                elseStat += @$"
    br.s {endLabel}";
                thenStat += @$"
    {endLabel}: nop";
            }
            return output + elseStat + thenStat;
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

        public string Visit(SwitchNode node)
        {
            //Try reversing the order of function and program emitters. Visit and store the contents, then visit the varblocks.
            //Use the contents to add more vardeclnodes to the tree as needed. Then catch later in VisitFunction/program.
            //For later: jump table if max-min < 2x num cases, then if not cut the bottom or top case off based on dist from next until
            //  you have a set matching the condition (hybrid emit) or less than half the cases, in which case do if tree.
            throw new NotImplementedException("Figure out how to add more locals while in the emitter? Or before ... somewhere?");
            /*var endLabel = MakeLabel();
            var branchHandling = "";
            var cases = "";
            var ending = "";
            foreach (var st in node.Cases)
            {
                var label = MakeLabel();
                branchHandling += @$"
    ";
                cases += @$"
    {label}
    {Visit((dynamic)st.Stat)}
    br.s {endLabel}";
            }
            if (!node.Default.IsEmpty())
            {
                var defaultLabel = MakeLabel();
                branchHandling += @$"
    br.s {defaultLabel}";
                ending += @$"
    {defaultLabel}: nop
    {Visit((dynamic)node.Default)}";
            }
            else
            {
                branchHandling += @$"
    br.s {endLabel}";
            }
            ending += @$"
    {endLabel}: nop";
            return String.Join(String.Empty, branchHandling, cases, ending);*/
        }

        public string Visit(FunctionCallNode node)
        {
            var callTarget = node.CallTarget;
            var paramTypes = string.Join(", ", callTarget.Params.Select(p => p.BlaiseType.ToCilType()));
            var argExprs = string.Join('\n', node.Arguments.Select(a => Visit((dynamic)a)));
            var returnType = callTarget.IsFunction ? callTarget.ReturnType.ToCilType() : "void";
            return @$"
    ldloc.0
    {argExprs}
    call instance {returnType} {ProgramName}::{callTarget.Identifier}({paramTypes})";
        }

        public string Visit(ReturnNode node) => @$"{Visit((dynamic)node.Expression)}
    ret";

        public string Visit(IntegerNode node) => @$"
    ldc.i4.s {node.IntValue}";

        public string Visit(RealNode node) => @$"
    ldc.r8 {node.RealValue}";

        public string Visit(BooleanNode node) => @$"
    ldc.i4.{(node.BoolValue ? 1 : 0)}";

        public string Visit(CharNode node) => @$"
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
                output += TypeConvert(node.LeftType, node.ExprType, node);
            }
            output += Visit((dynamic)node.Right);
            if (!node.RightType.Equals(node.ExprType))
            {
                output += TypeConvert(node.RightType, node.ExprType, node);
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
                output += TypeConvert(node.LeftType, node.RightType, node);
            }
            output += Visit((dynamic)node.Right);
            if (leftBasicType > rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.RightType, node.LeftType, node);
            }
            return output + ToCilOperator(node.Operator);
        }

        public string Visit(LogicalOpNode node)
        {
            return $"{Visit((dynamic)node.Left)}{Visit((dynamic)node.Right)}{ToCilOperator(node.Operator)}";
        }

        public string Visit(NotOpNode node)
        {
            return @$"{Visit((dynamic)node.Expression)}{ToCilOperator(BlaiseOperator.Not)}";
        }

        public string Visit(AbstractAstNode node) => node.IsEmpty() ? "" : throw new InvalidOperationException($"Invalid node type {node.Type}");
    }
}