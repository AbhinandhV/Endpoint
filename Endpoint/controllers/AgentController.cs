using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Endpoint.Data;
using Endpoint.Models;

namespace Endpoint.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AgentController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // POST /api/agent/register - Agent registers itself
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AgentRegistrationRequest request)
    {
        var existing = await _db.AgentRegistrations
            .FirstOrDefaultAsync(a => a.AgentId == request.AgentId);

        if (existing != null)
        {
            // Update existing registration
            existing.MachineName = request.MachineName;
            existing.IpAddress = request.IpAddress;
            existing.OsVersion = request.OsVersion;
            existing.LastSeenAt = DateTime.UtcNow;
        }
        else
        {
            // New registration
            _db.AgentRegistrations.Add(new AgentRegistration
            {
                AgentId = request.AgentId,
                MachineName = request.MachineName,
                IpAddress = request.IpAddress,
                OsVersion = request.OsVersion,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Registered", agentId = request.AgentId });
    }

    // GET /api/agent/poll/{agentId} - Agent polls for pending commands
    [HttpGet("poll/{agentId}")]
    public async Task<IActionResult> Poll(string agentId)
    {
        // Update last seen
        var agent = await _db.AgentRegistrations
            .FirstOrDefaultAsync(a => a.AgentId == agentId);
        
        if (agent != null)
        {
            agent.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Get pending commands for this agent
        var commands = await _db.PendingCommands
            .Where(c => c.AgentId == agentId && c.Status == "Pending")
            .OrderBy(c => c.CreatedAt)
            .Take(5)  // Max 5 commands at a time
            .ToListAsync();

        if (commands.Count == 0)
        {
            return NoContent();
        }

        // Mark as running
        foreach (var cmd in commands)
        {
            cmd.Status = "Running";
            cmd.StartedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        return Ok(commands.Select(c => new
        {
            c.Id,
            c.ActionId,
            c.ActionName,
            c.Script
        }));
    }

    // POST /api/agent/result - Agent reports command result
    [HttpPost("result")]
    public async Task<IActionResult> ReportResult([FromBody] CommandResultRequest request)
    {
        var command = await _db.PendingCommands.FindAsync(request.CommandId);
        if (command == null)
        {
            return NotFound();
        }

        command.Status = request.Status;
        command.Output = request.Output;
        command.Error = request.Error;
        command.DurationMs = request.DurationMs;
        command.CompletedAt = DateTime.UtcNow;

        // Also add to action history for dashboard visibility
        _db.ActionHistory.Add(new ActionHistory
        {
            ActionId = command.ActionId,
            ActionName = command.ActionName,
            MachineName = command.AgentId,
            Status = request.Status,
            Output = request.Output,
            Error = request.Error,
            DurationMs = request.DurationMs,
            StartedAt = command.StartedAt ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            User = command.RequestedBy ?? "agent"
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/agent/list - Get all registered agents (for dashboard)
    [HttpGet("list")]
    public async Task<IActionResult> ListAgents()
    {
        var agents = await _db.AgentRegistrations
            .OrderByDescending(a => a.LastSeenAt)
            .Select(a => new
            {
                a.AgentId,
                a.MachineName,
                a.IpAddress,
                a.OsVersion,
                a.RegisteredAt,
                a.LastSeenAt,
                IsOnline = (DateTime.UtcNow - a.LastSeenAt).TotalMinutes < 2,
                PendingCommands = _db.PendingCommands.Count(c => c.AgentId == a.AgentId && c.Status == "Pending")
            })
            .ToListAsync();

        return Ok(agents);
    }

    // POST /api/agent/queue - Queue a command for an agent (from dashboard)
    [HttpPost("queue")]
    public async Task<IActionResult> QueueCommand([FromBody] QueueCommandRequest request)
    {
        var command = new PendingCommand
        {
            AgentId = request.AgentId,
            ActionId = request.ActionId,
            ActionName = request.ActionName,
            Script = request.Script,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            RequestedBy = User.Identity?.Name ?? "api"
        };

        _db.PendingCommands.Add(command);
        await _db.SaveChangesAsync();

        return Ok(new { commandId = command.Id, status = "Queued" });
    }

    // GET /api/agent/commands/{agentId} - Get command history for an agent
    [HttpGet("commands/{agentId}")]
    public async Task<IActionResult> GetCommands(string agentId, [FromQuery] int limit = 20)
    {
        var commands = await _db.PendingCommands
            .Where(c => c.AgentId == agentId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(commands);
    }
}

public class AgentRegistrationRequest
{
    public string AgentId { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? OsVersion { get; set; }
}

public class CommandResultRequest
{
    public int CommandId { get; set; }
    public string Status { get; set; } = "";
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int DurationMs { get; set; }
}

public class QueueCommandRequest
{
    public string AgentId { get; set; } = "";
    public string ActionId { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Script { get; set; } = "";
}
