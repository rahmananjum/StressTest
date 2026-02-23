namespace StressTest.Core.Models;

public class Portfolio
{
    public int PortId { get; set; }
    public string PortName { get; set; } = string.Empty;
    public string PortCountry { get; set; } = string.Empty;
    public string PortCcy { get; set; } = string.Empty;
}
