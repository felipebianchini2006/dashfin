using Finance.Domain.Entities;

namespace Finance.Application.Abstractions;

public interface IPasswordHasher
{
  string HashPassword(User user, string password);
  bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
}

