using System;
using System.Collections.Generic;
using BiosConfiguration.JsonConverters;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Global

namespace BiosConfiguration
{
    [JsonConverter(typeof(OutputConverter))]
    public abstract class BiosOutput
    {
        public int Address { get; set; }

        // TODO: wtf is this?
        public string Suffix { get; set; } = null!;

        public string Type { get; set; } = null!;

        private static readonly Dictionary<string, Type> Types = new()
        {
            [OutputInteger.OutputType] = typeof(OutputInteger),
            [OutputString.OutputType] = typeof(OutputString),
        };

        public static Type GetTypeForType(in string type)
        {
            return Types[type];
        }
    }
}