    [ApiController]
    [Route("api/roles")]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;
        private readonly IRolePermissionService _rolePermissions;

        public RolesController(IRoleService roleService, IRolePermissionService rolePermissions)
        {
            _roleService = roleService;
            _rolePermissions = rolePermissions;
        }

        [HttpGet("paginated")]
        [ProducesResponseType(typeof(Result<PagedMetadata<RoleResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PagedMetadata<RoleResponseDto>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllPaginated([FromQuery] QueryParams query)
        {
            var result = await _roleService.GetAllPaginatedAsync(query);
            return HandleResult(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<RoleResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<RoleResponseDto>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roleService.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.Roles.View)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _roleService.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.Roles.Manage)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(Result<RoleResponseDto>.BadRequestResponse("Invalid request."));

            var result = await _roleService.CreateAsync(dto, GetCallerId());
            return HandleResult(result);
        }

        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.Roles.Manage)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(Result<RoleResponseDto>.BadRequestResponse("Invalid request."));

            var result = await _roleService.UpdateAsync(id, dto);
            return HandleResult(result);
        }

        [HttpPatch("{id:int}/toggle-status")]
        [RequirePermission(PermissionCodes.Roles.Manage)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result<RoleResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _roleService.ToggleStatusAsync(id);
            return HandleResult(result);
        }

        [HttpGet("{roleId:int}/permissions")]
        [RequirePermission(PermissionCodes.Roles.View)]
        [ProducesResponseType(typeof(RolePermissionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissions([FromRoute] int roleId)
        {
            var result = await _rolePermissions.GetRolePermissionsAsync(roleId);
            return HandleResult(result);
        }

        [HttpPost("{roleId:int}/permissions/{permissionId:int}")]
        [RequirePermission(PermissionCodes.Roles.Manage)]
        public async Task<IActionResult> RequestPermission([FromRoute] int roleId, [FromRoute] int permissionId)
        {
            var result = await _rolePermissions.RequestPermissionAsync(roleId, permissionId, GetCallerId());
            return HandleResult(result);
        }

        [HttpDelete("{roleId:int}/permissions/{permissionId:int}")]
        [RequirePermission(PermissionCodes.Roles.Manage)]
        public async Task<IActionResult> RemovePermission([FromRoute] int roleId, [FromRoute] int permissionId)
        {
            var result = await _rolePermissions.RemovePermissionAsync(roleId, permissionId);
            return HandleResult(result);
        }

        //[HttpPut("{roleId:int}/permissions")]
        //[RequirePermission(PermissionCodes.Roles.Manage)]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> SetRolePermissions(
        //    [FromRoute] int roleId, [FromBody] SetRolePermissionsDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var result = await _rolePermissions.SetRolePermissionsAsync(roleId, dto);
        //    return HandleResult(result);
        //}

        //[HttpPost("{roleId:int}/permissions/{permissionId:int}")]
        //[RequirePermission(PermissionCodes.Roles.Manage)]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> AddPermission(
        //    [FromRoute] int roleId, [FromRoute] int permissionId)
        //{
        //    var result = await _rolePermissions.AddPermissionAsync(roleId, permissionId);
        //    return HandleResult(result);
        //}
    }