namespace TouchDcsTest.TranslatorTests.Keys
{
    public class LedTranslatorKeys : ITranslatorTest
    {
        public string SyncAddress { get; } = "LedTests";
        public string NoConfig { get; } = "LedNoConfig";
        public string Config { get; } = "LedConfig";
        public string NoConfigNeedsClamp { get; } = "LedNoConfigNeedsClamp";
        public string ConfigNeedsClamp { get; } = "LedConfigNeedsClamp";
    }
}