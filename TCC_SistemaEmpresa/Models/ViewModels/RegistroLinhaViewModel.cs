using Microsoft.AspNetCore.Mvc.Rendering;

namespace TCC_SistemaEmpresa.Models.ViewModels
{
    public class RegistroListaViewModel
    {
        public string? Busca { get; set; }

        public string? Entidade { get; set; }

        public string? Acao { get; set; }

        public DateTime? DataInicial { get; set; }

        public DateTime? DataFinal { get; set; }

        public IEnumerable<SelectListItem> Entidades { get; set; }
            = Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> Acoes { get; set; }
            = Enumerable.Empty<SelectListItem>();

        public IReadOnlyList<RegistroLinhaViewModel> Registros { get; set; }
            = Array.Empty<RegistroLinhaViewModel>();

        public PaginacaoViewModel Paginacao { get; set; } = PaginacaoViewModel.Criar(1, 0);
    }

    public class RegistroLinhaViewModel
    {
        public long Id { get; set; }

        public int? UsuarioId { get; set; }

        public string? Usuario { get; set; }

        public string Acao { get; set; } = string.Empty;

        public string EntidadeAfetada { get; set; } = string.Empty;

        public int? RegistroId { get; set; }

        public DateTime DataHora { get; set; }

        public string? Detalhes { get; set; }

        public string UsuarioRotulo =>
            string.IsNullOrWhiteSpace(Usuario) ? "Sistema" : Usuario;

        public string DataHoraFormatada => DataHora.ToString("dd/MM/yyyy HH:mm:ss");

        public string RegistroRotulo => RegistroId.HasValue ? $"#{RegistroId}" : "—";

        public string DetalhesResumo =>
            string.IsNullOrWhiteSpace(Detalhes)
                ? "—"
                : Detalhes.Length <= 90 ? Detalhes : Detalhes[..90] + "…";
    }
}