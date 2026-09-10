namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class CargoListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<CargoLinhaViewModel> Cargos { get; set; }
            = Array.Empty<CargoLinhaViewModel>();

        public PaginacaoViewModel Paginacao { get; set; } = PaginacaoViewModel.Criar(1, 0);
    }

    public class CargoLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal? SalarioBase { get; set; }

        public decimal? PerComissaoBase { get; set; }

        public bool Ativo { get; set; }

        public int QuantidadeFuncionarios { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeFuncionarios == 0;
    }
}