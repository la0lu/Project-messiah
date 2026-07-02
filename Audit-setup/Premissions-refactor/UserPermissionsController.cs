  [Route("api/users/{userId:int}")]
  [ApiController]
  public class UserPermissionsController : BaseApiController
  {
      private readonly IUserRoleService _userRoles;
      private readonly IUserPermissionOverrideService _overrides;

      public UserPermissionsController(
          IUserRoleService userRoles,
          IUserPermissionOverrideService overrides)
      {
          _userRoles = userRoles;
          _overrides = overrides;
      }

      // -------------------------------------------------------------------------
      // User Roles
      // -------------------------------------------------------------------------

      [HttpGet("roles")]
      [RequirePermission(PermissionCodes.Users.View)]
      [ProducesResponseType(typeof(UserRolesDto), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> GetUserRoles([FromRoute] int userId)
      {
          var result = await _userRoles.GetUserRolesAsync(userId);
          return HandleResult(result);
      }

      [HttpPost("roles/{roleId:int}")]
      [RequirePermission(PermissionCodes.Users.AssignRoles)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> RequestRole([FromRoute] int userId, [FromRoute] int roleId)
      {
          var result = await _userRoles.RequestRoleAsync(userId, roleId, GetCallerId());
          return HandleResult(result);
      }

      [HttpDelete("roles/{roleId:int}")]
      [RequirePermission(PermissionCodes.Users.AssignRoles)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> RemoveRole([FromRoute] int userId, [FromRoute] int roleId)
      {
          var result = await _userRoles.RemoveRoleAsync(userId, roleId);
          return HandleResult(result);
      }

      //[HttpPut("roles")]
      //[RequirePermission(PermissionCodes.Users.AssignRoles)]
      //[ProducesResponseType(StatusCodes.Status200OK)]
      //[ProducesResponseType(StatusCodes.Status400BadRequest)]
      //[ProducesResponseType(StatusCodes.Status404NotFound)]
      //public async Task<IActionResult> SetUserRoles([FromRoute] int userId, [FromBody] AssignUserRolesDto dto)
      //{
      //    if (!ModelState.IsValid)
      //        return BadRequest(ModelState);

      //    var result = await _userRoles.SetUserRolesAsync(userId, dto, GetCallerId());
      //    return HandleResult(result);
      //}

      //[HttpPost("roles/{roleId:int}")]
      //[RequirePermission(PermissionCodes.Users.AssignRoles)]
      //[ProducesResponseType(StatusCodes.Status200OK)]
      //[ProducesResponseType(StatusCodes.Status404NotFound)]
      //public async Task<IActionResult> AddRole([FromRoute] int userId, [FromRoute] int roleId)
      //{
      //    var result = await _userRoles.AddRoleAsync(userId, roleId, GetCallerId());
      //    return HandleResult(result);
      //}


      // -------------------------------------------------------------------------
      // Permission Overrides
      // -------------------------------------------------------------------------

      [HttpGet("overrides")]
      [RequirePermission(PermissionCodes.Users.View)]
      [ProducesResponseType(typeof(List<UserPermissionOverrideDto>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> GetOverrides([FromRoute] int userId)
      {
          var result = await _overrides.GetOverridesAsync(userId);
          return HandleResult(result);
      }

      [HttpPost("overrides")]
      [RequirePermission(PermissionCodes.Users.AssignOverrides)]
      public async Task<IActionResult> RequestOverride([FromRoute] int userId, [FromBody] SetPermissionOverrideDto dto)
      {
          if (!ModelState.IsValid) return BadRequest(ModelState);
          var result = await _overrides.RequestOverrideAsync(userId, dto, GetCallerId());
          return HandleResult(result);
      }

      [HttpDelete("overrides/{permissionId:int}")]
      [RequirePermission(PermissionCodes.Users.AssignOverrides)]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> RemoveOverride(
          [FromRoute] int userId, [FromRoute] int permissionId)
      {
          var result = await _overrides.RemoveOverrideAsync(userId, permissionId);
          return HandleResult(result);
      }

      //[HttpPost("overrides")]
      //[RequirePermission(PermissionCodes.Users.AssignOverrides)]
      //[ProducesResponseType(typeof(UserPermissionOverrideDto), StatusCodes.Status200OK)]
      //[ProducesResponseType(StatusCodes.Status400BadRequest)]
      //[ProducesResponseType(StatusCodes.Status404NotFound)]
      //public async Task<IActionResult> SetOverride([FromRoute] int userId, [FromBody] SetPermissionOverrideDto dto)
      //{
      //    if (!ModelState.IsValid)
      //        return BadRequest(ModelState);

      //    var result = await _overrides.SetOverrideAsync(userId, dto, GetCallerId());
      //    return HandleResult(result);
      //}

      // -------------------------------------------------------------------------
      // Effective Permissions
      // -------------------------------------------------------------------------

      [HttpGet("permissions/effective")]
      [RequirePermission(PermissionCodes.Users.View)]
      [ProducesResponseType(typeof(UserEffectivePermissionsDto), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> GetEffectivePermissions([FromRoute] int userId)
      {
          var result = await _overrides.GetEffectivePermissionsAsync(userId);
          return HandleResult(result);
      }
  }