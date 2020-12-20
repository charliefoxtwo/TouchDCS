namespace TouchDcsTest.TranslatorTests
{
    public class IntegerOutTranslatorKeys : ITranslatorTest
    {
        public string ConfigSync { get; } = "IntegerOutAircraftSync";
        public string SyncAddress { get; } = "IntegerOutTests";
        public string NoConfig { get; } = "IntegerOutNoConfig";
        public string Config { get; } = "IntegerOutConfig";
        public string ReMap { get; } = "IntegerOutReMap";
        public string Multiply { get; } = "IntegerOutMultiply";
        public string Divide { get; } = "IntegerOutDivide";
        public string Add { get; } = "IntegerOutAdd";
        public string Subtract { get; } = "IntegerOutSubtract";
    }
}