using System;
using System.Text;

namespace Core
{
    public class ConfigException : Exception
    {
        private readonly string _configFile;
        private readonly string? _property;
        private readonly string? _value;

        public ConfigException(string configFile, string message) : base(message)
        {
            _configFile = configFile;
            _property = null;
            _value = null;
        }

        public ConfigException(string configFile, string property, string value, string message) : base(message)
        {
            _configFile = configFile;
            _property = property;
            _value = value;
        }

        private string SpecificMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Error in file: {_configFile}");
            if (_property is not null && _value is not null)
            {
                sb.AppendLine($"{_property}: {_value}");
            }

            return sb.ToString();
        }
        public override string Message => $"{SpecificMessage()}{base.Message}";
    }
}