using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.DM.Tests;

/// <summary>
/// Source guards for where the HD-MD chassis's address comes from. A chassis that connects to the processor on its
/// IP ID has no address in its IP table entry, so ConnectedIpList is empty; the processor's own IP table shows where it
/// connected from, and how the read went is published as a status point.
/// </summary>
public class HdMdDeviceInfoSourceTests
{
    private static string Source() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chassis", "HdMdNxM4KzEController.cs")));

    [Fact]
    public void The_address_falls_back_to_the_processor_s_ip_table()
    {
        var source = Source();

        source.Should().Contain("CrestronConsole.SendControlSystemCommand(\"ipt -t\", ref table)");
        source.Should().Contain("CrestronConsoleReply.ParseIpTableAddress(table, _chassis.ID)");
        source.Should().Contain("var ip = ChassisAddress();");
    }

    [Fact]
    public void How_the_read_went_is_a_status_point()
    {
        var source = Source();

        source.Should().Contain("DeviceInfoReadFeedback = new StringFeedback(\"deviceInfoRead\", () => _deviceInfoRead);");
        source.Should().Contain("AddFeedbackToList(DeviceInfoReadFeedback);");
    }
}
