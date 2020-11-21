// ReSharper disable UnusedMember.Global
namespace BiosConfiguration
{
    public class InputAction : BiosInput
    {
        public const string InterfaceType = "action";

        // TODO: enumify?
        public string Argument { get; set; } = null!;
    }
}