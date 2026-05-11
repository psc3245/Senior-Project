using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Users.HelperClasses;

namespace StockTraderBackend.Users.Controllers;

/// <summary>
/// User management endpoints for the StockTraderBackend.
/// </summary>
/// <remarks>
/// Provides basic CRUD operations for users. In the current implementation, users are managed via
/// an in-memory repository (data resets when the server restarts).
/// </remarks>
[ApiController]
[Route("users")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private UserRepository _userRepository;

    /// <summary>
    /// Creates a new <see cref="UserController"/>.
    /// </summary>
    /// <param name="userRepository">Repository used to store and retrieve users.</param>
    public UserController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    /// <remarks>
    /// Returns the full set of users currently stored by the backend.
    /// Intended primarily for development/testing scenarios.
    /// </remarks>
    /// <returns>A list of all users.</returns>
    /// <response code="200">Returns the list of users.</response>
    [HttpGet(Name = "GetUsers")]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<User>> Get()
    {
        return await _userRepository.getUsersAsync();
    }



    /// <summary>
    /// Update an existing user.
    /// </summary>
    /// <remarks>
    /// Attempts to update the user identified by <paramref name="userId"/>.
    /// If the user does not exist, returns 404.
    ///
    /// NOTE: This endpoint currently accepts a full <see cref="User"/> object in the body.
    /// </remarks>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="user">The updated user object.</param>
    /// <returns>Ok("") if successful, or NotFound if the user does not exist.</returns>
    /// <response code="200">User was updated successfully.</response>
    /// <response code="404">User with the given ID was not found.</response>
    [HttpPut("{userId}", Name = "UpdateUser")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> UpdateUser([FromRoute] Guid userId, [FromBody] User user)
    {
        User? old = await _userRepository.getUserByIdAsync(userId);
        if (old == null)
        {
            return NotFound();
        }

        _userRepository.updateUserAsync(user);
        return Ok("");
    }

    /// <summary>
    /// Delete a user.
    /// </summary>
    /// <remarks>
    /// Deletes the user identified by <paramref name="userId"/>.
    /// If the user does not exist, returns 404.
    /// </remarks>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>NoContent if deleted, or NotFound if the user does not exist.</returns>
    /// <response code="204">User was deleted successfully.</response>
    /// <response code="404">User with the given ID was not found.</response>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser([FromRoute] Guid userId)
    {
        User? old = await _userRepository.getUserByIdAsync(userId);
        if (old == null)
        {
            return NotFound();
        }

        _userRepository.deleteUserAsync(old);
        return NoContent();
    }

    [HttpGet("/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult TestRoute()
    {
        return Ok("Test Passed Again");
    }
}
