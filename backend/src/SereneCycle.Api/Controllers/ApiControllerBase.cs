using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Common;

namespace SereneCycle.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// JWT'deki kullanıcı kimliği. [Authorize] ile korunan uçlarda
    /// her zaman doludur.
    /// </summary>
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            return Guid.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException(
                    "Token'da geçerli bir kullanıcı kimliği yok.");
        }
    }

    /// <summary>
    /// Değer döndüren servis sonucunu 200 OK'a, hatayı durum koduna çevirir.
    /// Uçların hepsi bu tek dönüşümden geçsin diye burada: aksi hâlde aynı
    /// üçlü ifade her aksiyonda tekrar yazılır ve biri er geç ayrışır.
    /// </summary>
    protected ActionResult<T> OkOrProblem<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>Değer döndürmeyen servis sonucunu 204'e ya da hataya çevirir.</summary>
    protected ActionResult NoContentOrProblem(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? NoContent() : Problem(result);
    }

    /// <summary>Servis katmanının hata sınıfını HTTP durum koduna çevirir.</summary>
    protected ActionResult Problem(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.ErrorCode switch
        {
            ErrorCode.NotFound => NotFound(new { error = result.Error }),
            ErrorCode.Conflict => Conflict(new { error = result.Error }),
            ErrorCode.Unauthorized => Unauthorized(new { error = result.Error }),
            ErrorCode.Forbidden =>
                StatusCode(StatusCodes.Status403Forbidden,
                    new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
