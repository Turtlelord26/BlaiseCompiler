using System.Text;
using System.Collections.Generic;

namespace Blaise2.Ast
{
    public class BlaiseSignature
    {
        public string Identifier { get; set; }
        public BlaiseType ReturnType { get; set; }
        public List<BlaiseType> Parameters { get; set; }

        public override bool Equals(object obj)
        {
            var signature = obj as BlaiseSignature;
            return Identifier.Equals(signature?.Identifier)
                    & Parameters.Equals(signature?.Parameters);
        }

        public override int GetHashCode()
        {
            return 31 * Identifier.GetHashCode()
                    + Parameters.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder s = new();
            s.Append(Identifier + "(");
            foreach (var param in Parameters)
            {
                s.Append(param);
            }
            s.Append(") : " + ReturnType);
            return s.ToString();
        }
    }
}