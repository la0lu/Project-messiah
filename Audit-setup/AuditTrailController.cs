using Microsoft.AspNetCore.Mvc;
using WealthMart.Api.DTOs.AuditTrail;
using WealthMart.Api.Services.AuditTrail;
using WealthMart.Api.Common; // adjust to match Result<T> / PagedMetadata<T> namespace

namespace WealthMart.Api.Controllers
{
    [Route("api/[controller]")]
    public class AuditTrailController : BaseApiController
    {
        private readonly IAuditTrailService _auditTrailService;

        public AuditTrailController(IAuditTrailService auditTrailService)
        {
            _auditTrailService = auditTrailService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedMetadata<AuditTrailListDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaged([FromQuery] AuditTrailFilterDto filter)
        {
            var result = await _auditTrailService.GetPagedAsync(filter);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<AuditTrailDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var result = await _auditTrailService.GetByIdAsync(id);
            return HandleResult(result);
        }
    }
}
