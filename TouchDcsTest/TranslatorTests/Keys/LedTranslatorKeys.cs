namespace TouchDcsTest.TranslatorTests
{
    public class LedTranslatorKeys : ITranslatorTest
    {
        public string ConfigSync { get; } = "LedAircraftSync";
        public string SyncAddress { get; } = "LedTests";
        public string NoConfig { get; } = "LedNoConfig";
        public string Config { get; } = "LedConfig";
        public string NoConfigNeedsClamp { get; } = "LedNoConfigNeedsClamp";
        public string ConfigNeedsClamp { get; } = "LedConfigNeedsClamp";
    }
}