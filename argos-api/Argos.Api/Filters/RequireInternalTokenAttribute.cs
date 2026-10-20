using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Filters;

/// <summary>Aplica <see cref="InternalTokenFilter"/> (resuelto por DI) al controller o acción.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireInternalTokenAttribute() : ServiceFilterAttribute(typeof(InternalTokenFilter));
