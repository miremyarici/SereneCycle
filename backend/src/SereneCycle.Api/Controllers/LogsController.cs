using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SereneCycle.Application.Logs;

namespace SereneCycle.Api.Controllers;

[Route("logs")]
[Authorize]
public class LogsController(ILogService logService) : ApiControllerBase
{
    /// <summary>
    /// Adet kaydı ekranındaki belirti çipleri. İstemci id gönderir,
    /// metin sunucudan gelir ki iki taraf ayrışmasın.
    /// </summary>
    [HttpGet("symptoms")]
    [ProducesResponseType(
        typeof(IReadOnlyList<SymptomOption>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SymptomOption>>> GetSymptoms(
        CancellationToken cancellationToken) =>
        Ok(await logService.GetSymptomOptionsAsync(cancellationToken));

    /// <summary>
    /// Bir günün kaydı. Kayıt yoksa da boş varsayılanlarla 200 döner;
    /// ekran "yeni kayıt" ve "düzenleme" için aynı yolu kullanabilsin.
    /// </summary>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyLogResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyLogResponse>> Get(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var result =
            await logService.GetAsync(CurrentUserId, date, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    /// <summary>
    /// Günün kaydını üzerine yazar. Kanama işaretlenmişse ve bu gün
    /// süregelen bir adetin parçası değilse yeni bir döngü başlatılır.
    /// </summary>
    [HttpPut("{date}")]
    [ProducesResponseType(typeof(DailyLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DailyLogResponse>> Save(
        DateOnly date,
        SaveDailyLogRequest request,
        CancellationToken cancellationToken)
    {
        var result = await logService.SaveAsync(
            CurrentUserId, date, request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }
}
