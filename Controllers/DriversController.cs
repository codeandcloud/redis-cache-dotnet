using CachingWebApi.Data;
using CachingWebApi.Models;
using CachingWebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CachingWebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DriversController : ControllerBase
{
    private readonly ILogger<DriversController> _logger;
    private readonly ICacheService _cacheService;    
    private readonly AppDbContext _dbContext;

    public DriversController(
        ILogger<DriversController> logger, 
        ICacheService cacheService, AppDbContext dbContext
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var drivers = _cacheService.GetData<List<Driver>>("drivers");
        if (drivers == null)
        {
            drivers = await _dbContext.Drivers.ToListAsync();
            _cacheService.SetData("drivers", drivers, DateTimeOffset.Now.AddMinutes(5));
        }

        return Ok(drivers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var driver = _cacheService.GetData<Driver>($"driver-{id}");
        if (driver == null)
        {
            driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver == null)
            {
                return NotFound();
            }
            _cacheService.SetData($"driver-{id}", driver, DateTimeOffset.Now.AddMinutes(5));
        }

        return Ok(driver);
    }

    [HttpPost]
    public async Task<IActionResult> Post(Driver driver)
    {
        var addedDriver = await _dbContext.Drivers.AddAsync(driver);
        await _dbContext.SaveChangesAsync();

        _cacheService.RemoveData("drivers");
        _cacheService.SetData($"driver-{addedDriver.Entity.Id}", addedDriver.Entity, DateTimeOffset.Now.AddMinutes(5));

        return CreatedAtAction(nameof(Get), new { id = driver.Id }, driver);
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, Driver driver)
    {
        var existingDriver = _dbContext.Drivers.FirstOrDefault(d => d.Id == id);
        if (existingDriver == null)
        {
            return NotFound();
        }

        existingDriver.Name = driver.Name;
        existingDriver.DriveNb = driver.DriveNb;
        _dbContext.SaveChanges();
        _cacheService.RemoveData("drivers");
        _cacheService.SetData($"driver-{existingDriver.Id}", existingDriver, DateTimeOffset.Now.AddMinutes(5));

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var driver = _dbContext.Drivers.FirstOrDefault(d => d.Id == id);
        if (driver == null)
        {
            return NotFound();
        }

        _dbContext.Drivers.Remove(driver);
        _dbContext.SaveChanges();
        _cacheService.RemoveData("drivers");
        _cacheService.RemoveData($"driver-{id}");

        return NoContent();
    }
}
