using FoodFast.Data;
using FoodFast.Data.Models;
using FoodFast.DTOs;
using FoodFast.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FoodFast.Controllers
{
    // ==========================================
    // FEATURE 1: CUSTOMER ACCOUNT MANAGEMENT
    // Pattern: REQUEST/RESPONSE (REST API)
    // Reasoning: Immediate confirmation needed, CRUD operations
    // ==========================================
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;
        private readonly FoodFastDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, IAuthService authService, FoodFastDbContext context)
        {
            _userManager = userManager;
            _authService = authService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);


            // Assign Identity Role
            await _userManager.AddToRoleAsync(user, model.Role.ToString());

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var token = _authService.GenerateJWT(user);
                return Ok(new { token });
            }
            return Unauthorized();
        }


        [Authorize]
        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto model)
        {
            var userId = User.FindFirstValue("uid");
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound("User not found");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Profile updated successfully");
        }




        [HttpPost("payment-method")]
        [Authorize]
        public async Task<IActionResult> AddPaymentMethod([FromBody] PaymentMethodDto dto)
        {
            // Encrypt sensitive payment data
            var encryptedCard = _authService.EncryptPaymentData(dto.CardNumber);

            var paymentMethod = new PaymentMethod
            {
                UserId = User.FindFirst("UserId").Value,
                CardLastFour = dto.CardNumber.Substring(dto.CardNumber.Length - 4),
                CardType = "detect the type",//To-Do DetectCardType(dto.CardNumber),
                EncryptedData = encryptedCard,
                IsDefault = dto.IsDefault
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment method added successfully" });
        }
    }
}
