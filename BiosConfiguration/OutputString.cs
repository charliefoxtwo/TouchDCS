namespace BiosConfiguration
{
    public class OutputString : BiosOutput
    {
        public static string OutputType => "string";

        public int MaxLength { get; set; }
    }
}