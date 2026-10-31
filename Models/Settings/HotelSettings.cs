namespace HotelManagement.Models.Settings;

public class HotelSettings
{
    public string HotelName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string GSTNumber { get; set; } = "";
    public decimal TaxRatePercent { get; set; } = 18.0m;
}