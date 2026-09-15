using Microsoft.EntityFrameworkCore;
using TCC_SistemaEmpresa.Data;

namespace TCC_SistemaEmpresa.Services
{
    public class ExpiracaoLogBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger <ExpiracaoLogBackgroundService> _logger;

        public ExpiracaoLogBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger <ExpiracaoLogBackgroundService> logger )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var dataLimite = DateTime.Now.AddMonths(-12);
                        var linhasRemovidas = await context.Database.ExecuteSqlInterpolatedAsync(
                            $"DELETE FROM Tb_Log_Sistema WHERE data_Hora < {dataLimite}", stoppingToken);

                        _logger.LogInformation("Expiração de log após 12 meses. Linhas removidas: {Linhas} ", linhasRemovidas);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao executar expiração de logs.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            }
        }
    }
}
