using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ease_intro_api.Helpers;

public static class DatabaseErrors
{
    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: 1062 };
}
