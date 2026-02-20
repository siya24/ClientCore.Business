namespace ClientCore.Business.DTOs
{
    public class GetClientDTO:ClientShared
    {
        public required string Id { get; set; }
        public required string Code { get; set; }
        public  int TotalContacts { get; set; }
    }
}
