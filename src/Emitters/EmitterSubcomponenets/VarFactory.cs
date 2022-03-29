using Blaise2.Ast;

namespace Blaise2.Emitters.EmitterSubcomponents
{
    public class VarFactory
    {
        public static VarFactory Singleton = new();

        private int anonymousVarNum = 0;

        public VarDeclNode MakeLocalVar(BlaiseType varType) => new VarDeclNode()
        {
            Identifier = $"___AnonVar_{anonymousVarNum++}",
            BlaiseType = varType.DeepCopy()
        };
    }
}