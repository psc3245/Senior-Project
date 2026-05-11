using StockTraderBackend.Users.HelperClasses;
using BCrypt.Net;

namespace StockTraderBackend.Users;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public async Task<User> registerUser(RegisterUserRequest request)
    {
        if (await _userRepository.getUserByEmailAsync(request.email) != null) 
            throw new ArgumentException("email");
        
        if (await _userRepository.getUserByUsernameAsync(request.username) != null)
            throw new ArgumentException("username");
            
        var user = new User(request.username, BCrypt.Net.BCrypt.HashPassword(request.password), request.email);

        await _userRepository.addUserAsync(user);

        return user;
    }

    public async Task<User?> loginUser(LoginRequest request)
    {
        User? u;
        if (request.email != null)
        {
            u = await _userRepository.getUserByEmailAsync(request.email);
        }
        else if (request.username != null)
        {
            u = await _userRepository.getUserByUsernameAsync(request.username);
        }
        else throw new ArgumentException("Must include username or email");
        
        if (u != null && BCrypt.Net.BCrypt.Verify(request.password, u.password))
        {
            return u;
        }

        throw new ArgumentException("Wrong password");
    }
}