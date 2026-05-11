using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Users.HelperClasses;

namespace StockTraderBackend.Users.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
/// <remarks>
/// Provides basic authentication functionality for the stock trading simulator.
/// 
/// NOTE:
/// - Authentication is currently handled via simple password comparison.
/// - Passwords are stored and returned in plain text (educational/demo purposes only).
/// - No token issuance (JWT) is implemented in this version.
/// </remarks>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly UserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/>.
    /// </summary>
    /// <param name="userRepository">Repository used for user storage and lookup.</param>
    public AuthController(IPortfolioService portfolioService, UserService userService)
    {
        _portfolioService = portfolioService;
        _userService = userService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <remarks>
    /// Creates a new user using the provided username, password, and email.
    /// 
    /// This endpoint does not check for duplicate usernames or emails.
    /// Passwords are not hashed in the current implementation.
    /// </remarks>
    /// <param name="request">Registration payload containing username, password, and email.</param>
    /// <returns>The created user object.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Invalid registration request.</response>
    [HttpPost("register", Name = "RegisterUser")]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<User>> RegisterUser([FromBody] RegisterUserRequest request)
    {
        try
        {
            var user = await _userService.registerUser(request);
            return Created("", user);
            
        }
        catch (ArgumentException ex)
        {
            if (ex.Message == "email")
                return  BadRequest("Email already in use");
            if (ex.Message == "username")
                return  BadRequest("Username already in use");
            return StatusCode(400, ex.Message);
        }
    }

    /// <summary>
    /// Authenticate a user.
    /// </summary>
    /// <remarks>
    /// Attempts to authenticate a user using either:
    /// - Email + password, or
    /// - Username + password.
    ///
    /// If credentials are invalid or missing, returns 401 Unauthorized.
    /// 
    /// NOTE:
    /// This endpoint currently returns the full user object upon success
    /// and does not generate authentication tokens.
    /// </remarks>
    /// <param name="request">Login request containing either email or username and a password.</param>
    /// <returns>The authenticated user if credentials are valid.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials or missing login identifier.</response>
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<User>> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.loginUser(request);
        if (user == null)
        {
            return Unauthorized();
        }
        return Ok(user);
    }
}
