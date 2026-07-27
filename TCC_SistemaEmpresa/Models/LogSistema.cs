namespace TCC_SistemaEmpresa.Models
{
    public class LogSistema
    {
        public int EmpresaId { get; set; }
        public int? UsuarioId { get; set; }
        public int Id { get; set; }
        public string Acao { get; set; }
        public string EntidadeAfetada { get; set; }
        public int? RegistroId { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string? Detalhes { get; set; }
        public Empresa Empresa { get; set; }
        public Usuario Usuario { get; set; }

    }
}
