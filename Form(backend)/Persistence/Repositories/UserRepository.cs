using Form.Entities;
using Form.Interface;
using Form.Interfaces;
using Form.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Form.Repositories;

public class UserRepository(AppDbContext _context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task UpdateAsync(User user)
    {
        // Use Update(user) only on the scalar columns we own — do NOT let EF
        // walk the RoleAssignments navigation and issue UPDATE statements for
        // rows it didn't load in this request. Calling Update() on a detached
        // entity that has navigation properties marks every child as Modified,
        // which causes DbUpdateConcurrencyException when those UPDATE commands
        // touch 0 rows (the FK UserId may differ from what's in the DB).
        //
        // Instead: look up the tracked row, copy only the fields that can
        // legitimately change, and save. RoleAssignments are never touched here.
        var tracked = await _context.Users.FindAsync(user.Id);
        if (tracked is null) return;

        tracked.PasswordHash = user.PasswordHash;
        tracked.IsActive = user.IsActive;
        tracked.Email = user.Email;

        await _context.SaveChangesAsync();
    }
    public async Task<User?> GetByIdAsync(Guid id) =>
    await _context.Users.Include(u => u.RoleAssignments).FirstOrDefaultAsync(u => u.Id == id);
    public async Task<List<User>> GetByRoleAsync(UserRole role) =>
    await _context.Users
        .Where(u => u.RoleAssignments.Any(ra => ra.Role == role))
        .ToListAsync();
    // COUNT, not GetByRoleAsync().Count — the difference matters: this becomes
    // a single SQL COUNT(*) query, never pulling full User rows (with their
    // RoleAssignments collections) into memory just to count them.
    public async Task<int> CountByRoleAsync(UserRole role) =>
        await _context.Users.CountAsync(u => u.RoleAssignments.Any(ra => ra.Role == role));
    public async Task SetActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return;
        user.IsActive = isActive;
        await _context.SaveChangesAsync();
    }
    public async Task<List<Guid>> GetUserIdsByRoleAsync(UserRole role) =>
    await _context.Users
        .AsNoTracking()
        .Where(u => u.IsActive &&
                    u.RoleAssignments.Any(ra => ra.Role == role))
        .Select(u => u.Id)
        .ToListAsync();

    public async Task<List<Guid>> GetUserIdsByRolesAsync(IEnumerable<UserRole> roles)
    {
        var set = roles.Distinct().ToList();
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive &&
                        u.RoleAssignments.Any(ra => set.Contains(ra.Role)))
            .Select(u => u.Id)
            .ToListAsync();
    }

    public async Task<List<Guid>> GetAllUserIdsAsync() =>
        await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();
}