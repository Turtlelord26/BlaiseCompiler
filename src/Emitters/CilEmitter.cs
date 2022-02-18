using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Emitters
{
    [SuppressMessage("Performance", "CA1822", Justification = "all methods must be instance methods to participate in dynamic dispatch")]
    public partial class CilEmitter
    {
        private string Cil = string.Empty;
        private string ProgramName;

        public string Visit(ProgramNode node)
        {
            ProgramName = node.ProgramName;

            Cil += Preamble();

            Cil += $@"
.class public auto ansi beforefieldinit {node.ProgramName}
    extends [System.Private.CoreLib]System.Object
{{";
            foreach (var varDecl in node.VarDecls.OfType<VarDeclNode>())
            {
                Cil += $@"
    .field public {varDecl.BlaiseType.ToCilType()} {varDecl.Identifier}";
            }

            // TODO: emit routines as methods

            Cil += $@"
  // Methods
  .method public hidebysig static void main() cil managed
  {{
    .entrypoint
    .locals init (
      [0] class {ProgramName}
    )

    newobj instance void {ProgramName}::.ctor()
    stloc.0

";

            Cil += Visit((dynamic)node.Stat);

            Cil += @"
    ret
  } // end of main
";
            Cil += Postamble();

            return Cil;
        }

        public string Visit(BlockNode node)
        {
            var output = "";

            foreach (var stat in node.Stats)
            {
                output += Visit((dynamic)stat);
            }

            return output;
        }

        // TODO: fill in other Visit(...) methods as necessary
    }
}