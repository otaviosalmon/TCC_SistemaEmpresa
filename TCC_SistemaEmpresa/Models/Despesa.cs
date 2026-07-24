using System.Runtime.CompilerServices;

namespace TCC_SistemaEmpresa.Models
{
    public class Despesa
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int CategoriaDespesaId { get; set; }
        public int UsuarioId { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataDespesa { get; set; }
        public bool Fixa { get; set; }


    }
}
