using JJCore.Domain.Enumeradores;

namespace JJCore.Domain.Mensagens
{
    public static class MensagemErro
    {
        public const string FalhaServidor = "Ocorreu uma falha interna ao processar sua solicitação. Tente novamente mais tarde.";
        public const string FalhaLoginGoogle = "Token do Google inválido.";
        public const string UsuarioNaoEncontrado = "Usuário não encontrado.";
        public const string UsuarioSemPermissao = "Usuário não tem permissão para esta operação.";
        public const string RegistroNaoEncontrado = "Nenhum registro encontrado.";
        public const string FalhaAoSalvar = "Não foi possível salvar o registro.";
        public const string FalhaAoAdicionar = "Não foi possível adicionar o registro.";
        public const string FalhaAoAtualizar = "Não foi possível atualizar o registro.";
        public const string FalhaAoDeletar = "Não foi possível deletar o registro.";
        public const string IDInvalido = "O código identificador informado é inválido.";
        public const string RegistroEmUso = "Este registro não pode ser excluído pois está em uso.";
        public const string RegistroNaoExiste = "O registro informada não existe.";
        public const string ContaObrigatoria = "A Conta é obrigatória.";
        public const string CategoriaObrigatoria = "A Categoria é obrigatória.";
        public const string TransacaoObrigatoria = "O Código da Transação é obrigatório.";

        public static string NomeInvalido(eFormatacaoTexto formatacao, int minCaracteres = 0)
        {
            string regraFormato = formatacao switch
            {
                eFormatacaoTexto.Letras => "use apenas letras",
                eFormatacaoTexto.LetrasNumeros => "use apenas letras e números",
                _ => "formato inválido"
            };

            string regraTamanho = minCaracteres > 0
                ? $" e tenha no mínimo {minCaracteres} caracteres"
                : "";

            return $"O nome é inválido. Por favor, {regraFormato}{regraTamanho}.";
        }
        public static string CampoObrigatorio(string campo)
        {
            return string.IsNullOrEmpty(campo)
                ? "Este campo é de preenchimento obrigatório."
                : $"O campo '{campo}' é obrigatório.";
        }
    }
}
