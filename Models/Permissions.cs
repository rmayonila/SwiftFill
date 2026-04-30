namespace SwiftFill.Models
{
    public static class Permissions
    {
        public static class Shipments
        {
            public const string View = "Permissions.Shipments.View";
            public const string Create = "Permissions.Shipments.Create";
            public const string Edit = "Permissions.Shipments.Edit";
            public const string Delete = "Permissions.Shipments.Delete";
        }

        public static class Inventory
        {
            public const string View = "Permissions.Inventory.View";
            public const string Edit = "Permissions.Inventory.Edit";
        }

        public static class Finance
        {
            public const string View = "Permissions.Finance.View";
            public const string Edit = "Permissions.Finance.Edit";
        }

        public static class Hubs
        {
            public const string View = "Permissions.Hubs.View";
            public const string Edit = "Permissions.Hubs.Edit";
        }

        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Edit = "Permissions.Users.Edit";
        }

        public static class Delivery
        {
            public const string View = "Permissions.Delivery.View";
            public const string Edit = "Permissions.Delivery.Edit";
        }

        public static List<string> GetAll()
        {
            return new List<string>
            {
                Shipments.View, Shipments.Create, Shipments.Edit, Shipments.Delete,
                Inventory.View, Inventory.Edit,
                Finance.View, Finance.Edit,
                Hubs.View, Hubs.Edit,
                Users.View, Users.Edit,
                Delivery.View, Delivery.Edit
            };
        }
    }
}
