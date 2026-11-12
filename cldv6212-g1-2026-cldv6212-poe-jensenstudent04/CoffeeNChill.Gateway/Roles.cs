namespace CoffeeNChill.Gateway
{
    public static class Roles
    {

        public const string Customer = "Customer";

        public const string Staff = "Staff";

        public const string Admin = "Admin";

        // Combined role policy string for authorization attributes
        public const string AdminOrStaff = "Admin,Staff";

        // Readonly array containing all available roles for loop validation
        public static readonly string[] All = new string[] { Customer, Staff, Admin };

    }
}
