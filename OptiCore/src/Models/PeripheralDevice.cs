namespace OptiCore.Models
{
    public enum PeripheralCategory { Keyboard, Mouse, Controller, AudioInterface, NIC, USBController, Other }

    public class PeripheralDevice
    {
        public PeripheralCategory Category { get; set; }
        public string FriendlyName { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string VID { get; set; } = "";
        public string PID { get; set; } = "";
        // 0 = polling rate unknown; > 0 = measured or known from VID table
        public int PollingRateHz { get; set; }
        // True only when OptiCore can change the rate (currently: Razer via HID feature report)
        public bool PollingRateSoftwareControllable { get; set; }
        public bool IsHighPollingRate => PollingRateHz > 1000;
    }
}
