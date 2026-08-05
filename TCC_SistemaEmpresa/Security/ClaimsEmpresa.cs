namespace TCC_SistemaEmpresa.Security
{
    /// <summary>
    /// Nomes das claims próprias do sistema, gravadas no cookie de autenticação.
    /// </summary>
    public static class ClaimsEmpresa
    {
        /// <summary>
        /// Empresa do usuário logado. Base do isolamento de dados exigido pelo RNF39 —
        /// toda consulta a entidade de negócio deve ser filtrada por este valor.
        /// </summary>
        public const string EmpresaId = "EmpresaId";

        /// <summary>
        /// Nome da empresa do usuário logado. Guardado no cookie só para exibição
        /// (cabeçalho da barra lateral) — evita uma consulta ao banco a cada página.
        /// Nunca use este valor para filtrar dados; para isso vale o <see cref="EmpresaId"/>.
        /// </summary>
        public const string EmpresaNome = "EmpresaNome";
    }
}
