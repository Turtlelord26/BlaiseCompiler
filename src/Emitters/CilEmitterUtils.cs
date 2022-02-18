using System;
using Blaise2.Ast;

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
    }
}