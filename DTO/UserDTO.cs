namespace Configuration.DTOs
{
    // Запрос на регистрацию
    public class RegisterRequestDto
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
    }

    // Запрос на вход
    public class LoginRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }

    }

    public class UserDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}