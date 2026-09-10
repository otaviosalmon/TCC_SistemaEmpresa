-- ================================================================
--  USUÁRIO DE TESTE — para validar a tela de login
--
--  Credenciais:   usuário: Teste
--                 senha:   123
--
--  Pré-requisito: tcc_database.sql já executado (schema criado).
--  O script é idempotente: pode rodar quantas vezes quiser.
--
--  ⚠️  APENAS PARA DESENVOLVIMENTO. Senha '123' não vai para produção.
-- ================================================================

USE SistemaGestaoComercial;
GO

-- ----------------------------------------------------------------
-- O hash abaixo NÃO é um valor aleatório: foi gerado por
-- PasswordHasher.GerarHash("Teste", "123") — o mesmo código que o
-- AccountController usa para conferir a senha no login.
--
-- Formato:  PBKDF2-SHA256$<iteracoes>$<hash base64>
--
-- Como o hash é derivado de (usuário + senha), ele só vale para o
-- username 'Teste'. Renomear o usuário invalida a senha.
--
-- Para gerar o hash de outro usuário, chame no C#:
--     PasswordHasher.GerarHash("outro_user", "outra_senha")
-- e cole o retorno aqui.
-- ----------------------------------------------------------------

DECLARE @EmpresaId    INT          = 1;
DECLARE @Username     VARCHAR(50)  = 'Teste';
DECLARE @Email        VARCHAR(150) = 'teste@empresa.com';
DECLARE @Role         VARCHAR(30)  = 'ADMIN';
DECLARE @PasswordHash VARCHAR(255) = 'PBKDF2-SHA256$210000$F4v35/exZJufSyJxCpjBusvrJvzCyh10/rn8lM8CDi8=';

-- ================================================================
-- 1. Empresa do usuário (Tb_Usuario.empresa_id é NOT NULL + FK)
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Tb_Empresa WHERE id = @EmpresaId)
BEGIN
    -- IDENTITY_INSERT: força o id = 1 para bater com o seed.sql.
    SET IDENTITY_INSERT Tb_Empresa ON;

    INSERT INTO Tb_Empresa (id, nome, cnpj, cidade, estado, ativo)
    VALUES (@EmpresaId, 'Empresa de Desenvolvimento', '00000000000000', 'São Paulo', 'SP', 1);

    SET IDENTITY_INSERT Tb_Empresa OFF;

    PRINT 'Empresa de desenvolvimento criada (id = 1).';
END
ELSE
BEGIN
    PRINT 'Empresa id = 1 já existe — mantida.';
END

-- ================================================================
-- 2. Usuário de teste
-- ================================================================
-- UQ_Usuario_Username é GLOBAL (username): a busca é só por username.
IF EXISTS (SELECT 1 FROM Tb_Usuario                                    -- o login já existe...
            WHERE username   = @Username
              AND empresa_id <> @EmpresaId)                            -- ...em OUTRA empresa?
BEGIN
    THROW 50002, 'O login informado já pertence a outra empresa. Altere @Username e regere o hash.', 1; -- não sobrescreve usuário alheio
END
ELSE IF EXISTS (SELECT 1 FROM Tb_Usuario WHERE username = @Username)   -- existe na própria empresa de teste
BEGIN
    -- Regrava o hash. Útil ao reexecutar depois de mudar o PasswordHasher.
    UPDATE Tb_Usuario
       SET password_hash = @PasswordHash,                              -- hash novo
           role          = @Role,                                      -- perfil do teste
           ativo         = 1                                           -- garante acesso
     WHERE username = @Username;                                       -- username basta: é único

    PRINT 'Usuário ''Teste'' já existia — hash, perfil e status atualizados.';
END
ELSE
BEGIN
    INSERT INTO Tb_Usuario (empresa_id, username, email, password_hash, role, ativo) -- login livre: cria
    VALUES (@EmpresaId, @Username, @Email, @PasswordHash, @Role, 1);

    PRINT 'Usuário ''Teste'' criado com sucesso.';
END
GO

-- ================================================================
-- 3. Conferência
-- ================================================================
SELECT
    id,
    empresa_id,
    username,
    email,
    role,
    ativo,
    password_hash
FROM Tb_Usuario
WHERE username = 'Teste';
GO

PRINT 'Pronto. Acesse a tela de login com usuário "Teste" e senha "123".';
GO
