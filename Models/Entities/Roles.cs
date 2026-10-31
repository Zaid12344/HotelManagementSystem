namespace HotelManagement.Models.Entities;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string FrontDesk = "FrontDesk";
    public const string Housekeeping = "Housekeeping";
    public const string Accountant = "Accountant";

    public static readonly string[] All = { Admin, Manager, FrontDesk, Housekeeping, Accountant };
}