namespace TCC_SistemaEmpresa.Models
{
    public class LogSistema
    {
        public int EmpresaId { get; set; }
        public int? UsuarioId { get; set; }
        // BIGINT no banco: ler esta coluna num int estoura InvalidCastException.
        public long Id { get; set; }
        public string Acao { get; set; }
        public string EntidadeAfetada { get; set; }
        public int? RegistroId { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string? Detalhes { get; set; }
        public Empresa Empresa { get; set; }
        // usuario_id é nullable (ação do sistema sem usuário logado).
        public Usuario? Usuario { get; set; }

    }
}
