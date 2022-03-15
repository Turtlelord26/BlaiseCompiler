namespace Blaise2.Emitters.EmitterSubcomponents
{
    public class LabelFactory
    {
        public static LabelFactory Singleton = new();

        private int labelNum = 0;

        public string MakeLabel() => $"Label{labelNum++}";
    }
}