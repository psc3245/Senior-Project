using StockTraderBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace StockTraderBackend.Users;

public class UserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task addUserAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<User?> getUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.userId == userId, ct);
    }

    public async Task<User?> getUserByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.email == email, ct);
    }

    public async Task<User?> getUserByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.username == username, ct);
    }

    public async Task<List<User>> getUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .ToListAsync(ct);
    }

    public async Task updateUserAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task deleteUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var old = await _db.Users
            .FirstOrDefaultAsync(u => u.userId == userId, ct);

        if (old == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        _db.Users.Remove(old);
        await _db.SaveChangesAsync(ct);
    }

    public async Task deleteUserAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}