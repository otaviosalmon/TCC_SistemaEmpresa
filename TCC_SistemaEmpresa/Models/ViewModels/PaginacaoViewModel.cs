namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class PaginacaoViewModel
    {
        public const int RegistrosPorPagina = 12;

        public int PaginaAtual { get; private set; } = 1;

        public int TotalPaginas { get; private set; } = 1;

        public int TotalRegistros { get; private set; }

        public int RegistrosParaPular => (PaginaAtual - 1) * RegistrosPorPagina;

        public int PrimeiroRegistro => TotalRegistros == 0 ? 0 : RegistrosParaPular + 1;

        public int UltimoRegistro => Math.Min(PaginaAtual * RegistrosPorPagina, TotalRegistros);

        public bool TemPaginaAnterior => PaginaAtual > 1;

        public bool TemProximaPagina => PaginaAtual < TotalPaginas;

        public bool TemRegistros => TotalRegistros > 0;

        public static PaginacaoViewModel Criar(int pagina, int totalRegistros)
        {
            var registros = Math.Max(totalRegistros, 0);

            var totalPaginas = registros == 0
                ? 1
                : (int)Math.Ceiling(registros / (double)RegistrosPorPagina);

            return new PaginacaoViewModel
            {
                TotalRegistros = registros,
                TotalPaginas = totalPaginas,
                PaginaAtual = Math.Clamp(pagina, 1, totalPaginas)
            };
        }
    }

    public static class PaginacaoExtensoes
    {
        public static IQueryable<T> Pagina<T>(this IQueryable<T> consulta, PaginacaoViewModel paginacao) =>
            consulta
                .Skip(paginacao.RegistrosParaPular)
                .Take(PaginacaoViewModel.RegistrosPorPagina);

        public static IReadOnlyList<T> Pagina<T>(this IReadOnlyList<T> registros, PaginacaoViewModel paginacao) =>
            registros
                .Skip(paginacao.RegistrosParaPular)
                .Take(PaginacaoViewModel.RegistrosPorPagina)
                .ToList();
    }
}
