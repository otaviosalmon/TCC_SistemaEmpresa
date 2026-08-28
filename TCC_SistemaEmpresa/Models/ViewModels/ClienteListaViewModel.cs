namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class ClienteListaViewModel
    {
        public string? Busca { get; set; }

        public string Situacao { get; set; } = SituacaoFiltro.Todos;

        public IReadOnlyList<ClienteLinhaViewModel> Clientes { get; set; }
            = Array.Empty<ClienteLinhaViewModel>();
    }

    public class ClienteLinhaViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Telefone { get; set; }

        public string? Email { get; set; }

        public bool Ativo { get; set; }

        public int QuantidadeVendas { get; set; }

        public bool PodeExcluir => !Ativo && QuantidadeVendas == 0;
    }
}
