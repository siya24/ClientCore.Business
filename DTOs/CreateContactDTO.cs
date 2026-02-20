namespace ClientCore.Business.DTOs
{
    public class CreateContactDTO
    {
        [StringLength(200)]
        public required string Name { get; set; }

        [StringLength(200)]
        public required string Surname { get; set; }

        [StringLength(500)]
        [EmailAddress]
        public required string Email { get; set; }
       
    }
}
