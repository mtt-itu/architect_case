using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    private readonly PasswordHasher<Customer> _passwordHasher = new();

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAll()
    {
        return await db.Customers
            .AsNoTracking()
            .Select(x => ToResponse(x))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return customer is null ? NotFound() : ToResponse(customer);
    }

    [HttpPost("register")]
    public async Task<ActionResult<CustomerResponse>> Register(RegisterCustomerRequest request)
    {
        var usernameExists = await db.Customers.AnyAsync(x => x.Username == request.Username);
        if (usernameExists)
        {
            return BadRequest("Username already exists.");
        }

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Username = request.Username
        };
        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, ToResponse(customer));
    }

    [HttpPost("login")]
    public async Task<ActionResult<CustomerResponse>> Login(LoginRequest request)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Username == request.Username);
        if (customer is null)
        {
            return Unauthorized("Invalid username or password.");
        }

        var result = _passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid username or password.");
        }

        return ToResponse(customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        var usernameExists = await db.Customers.AnyAsync(x => x.Id != id && x.Username == request.Username);
        if (usernameExists)
        {
            return BadRequest("Username already exists.");
        }

        customer.FullName = request.FullName;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;
        customer.Username = request.Username;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static CustomerResponse ToResponse(Customer customer)
    {
        return new CustomerResponse(customer.Id, customer.FullName, customer.Email, customer.PhoneNumber, customer.Username);
    }
}

public record RegisterCustomerRequest(string FullName, string Email, string PhoneNumber, string Username, string Password);
public record LoginRequest(string Username, string Password);
public record UpdateCustomerRequest(string FullName, string Email, string PhoneNumber, string Username, string? Password);
public record CustomerResponse(int Id, string FullName, string Email, string PhoneNumber, string Username);
