using System;
using Blaise2.Ast;
using static Blaise2.Ast.BinaryOperator;
using static Blaise2.Ast.BooleanOperator;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {
        private static string Preamble()
        {
            return @"
// Preamble
.assembly _ { }
.assembly extern System.Collections {}
.assembly extern System.Console {}
.assembly extern System.Private.CoreLib {}
";
        }

        private static string Postamble()
        {
            return @"
  .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
  {
    ldarg.0
    call instance void [System.Private.CoreLib]System.Object::.ctor()
    ret
  }

} // end of class
";
        }

        // This function searches up the AST, looking for a node that is
        // an IVarOwner.  It then asks the IVarOwner if it knows about the
        // variable in question.  If not, then we continue our search up
        // the tree.
        private static SymbolInfo FindVariable(AbstractAstNode curNode, string variableName)
        {
            var ptr = curNode;

            while (ptr is not null)
            {
                if (ptr is IVarOwner vo)
                {
                    var res = vo.GetVarByName(variableName);
                    if (res is not null)
                    {
                        return res;
                    }
                }
                ptr = ptr.Parent;
            }

            throw new InvalidOperationException($"unknown variable {variableName}");
        }

        private static AbstractAstNode FindFunction(AbstractAstNode curNode, string funcName)
        {
            while (curNode is not null)
            {
                if (curNode is FunctionNode)
                {
                    var func = curNode as FunctionNode;
                    if (func.Identifier.Equals(funcName))
                    {
                        return func;
                    }
                }
                curNode = curNode.Parent;
            }
            throw new InvalidOperationException($"Target not found for function call to {funcName}");
        }

        private static string ToCilOperator(BinaryOperator op) => op switch
        {
            Pow => throw new NotImplementedException("Exponentiation unimplemented."),
            Mul => @"
    mul",
            Div => @"
    div",
            Add => @"
    add",
            Sub => @"
    sub",
            _ => throw new InvalidOperationException($"Invalid Binary Operator {op}")
        };

        private static string ToCilOperator(BooleanOperator op) => op switch
        {
            Gt => @"
    cgt",
            Lt => @"
    clt",
            Eq => @"
    ceq",
            Ne => @"
    ceq
    ldc.i4.0
    ceq",
            Gte => @"
    clt
    ldc.i4.0
    ceq",
            Lte => @"
    cgt
    ldc.i4.0
    ceq",
            _ => throw new InvalidOperationException($"Invalid Boolean Operator {op}")
        };
    }
}