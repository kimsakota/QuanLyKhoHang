using Microsoft.AspNetCore.Mvc;
using QuanLyKho.API.Models;
using QuanLyKho.API.Services;

namespace QuanLyKho.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(JwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public ActionResult<TokenResponse> Login([FromBody] LoginRequest request)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Replace this with a proper identity provider or database lookup.
        if (request.Username != "admin" || request.Password != "admin123")
        {
            return Unauthorized("Invalid username or password");
        }

        var token = _jwtTokenService.CreateToken(request.Username);
        return Ok(token);
    }
}
