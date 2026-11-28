namespace remon
{
    public class OrderDisplay
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Article { get; set; }
        public string Status { get; set; }
        public string Address { get; set; }
        public string RequestDate { get; set; }
        public string DeliveryDate { get; set; }
        public bool CanDelete { get; set; }
    }

    public class StatusDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class PickupPointDisplay
    {
        public int Id { get; set; }
        public string Address { get; set; }
    }
}