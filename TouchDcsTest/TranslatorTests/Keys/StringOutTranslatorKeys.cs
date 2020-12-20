namespace TouchDcsTest.TranslatorTests
{
    public class StringOutTranslatorKeys : ITranslatorTest
    {
        public string ConfigSync { get; } = "StringOutAircraftSync";
        public string SyncAddress { get; } = "StringOutTests";
        public string NoConfig { get; } = "StringOutNoConfig";
        public string Config { get; } = "StringOutConfig";
        public string ReMap { get; } = "StringOutReMap";
    }
}