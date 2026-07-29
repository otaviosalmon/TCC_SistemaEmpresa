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
    }
}
