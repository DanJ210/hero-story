using System.Security.Claims;
using HeroStory.Api.DTOs.Jobs;
using HeroStory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HeroStory.Api.Controllers; [ApiController][Authorize][Route("api/jobs")] public class GenerationJobsController : ControllerBase { private readonly AppDbContext _dbContext; public GenerationJobsController(AppDbContext dbContext) { _dbContext = dbContext; } [HttpGet("{jobId:guid}")] public async Task<ActionResult<JobDto>> GetJob(Guid jobId, CancellationToken cancellationToken) { var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException()); var job = await _dbContext.GenerationJobs.Where(x => x.Id == jobId && x.Scene.Session.UserId == userId).Select(x => new JobDto(x.Id, x.SceneId, x.SessionId, x.Status, x.AttemptCount, x.ErrorDetail, x.CreatedAt, x.UpdatedAt, x.CompletedAt)).SingleOrDefaultAsync(cancellationToken); return job is null ? NotFound() : Ok(job); } }
