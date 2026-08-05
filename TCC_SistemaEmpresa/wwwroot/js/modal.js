// Popup de confirmação e de aviso, sobre o <dialog> nativo do HTML.
//
// A tela declara UM <dialog> e qualquer botão com data-modal-texto vira gatilho.
// O texto, o título e o rótulo do botão vêm dos data-attributes do gatilho, então
// não há mensagem escrita aqui dentro — este arquivo não conhece nenhuma tela.
//
// Dois modos, decididos pela presença de data-modal-form:
//   com    -> confirmação: o botão de confirmar submete aquele formulário;
//   sem    -> aviso: só o botão de fechar (usado quando a ação está bloqueada).
(function () {
    var dialogo = document.querySelector('[data-modal]');
    if (!dialogo || typeof dialogo.showModal !== 'function') {
        return;
    }

    var titulo = dialogo.querySelector('[data-modal-titulo-alvo]');
    var texto = dialogo.querySelector('[data-modal-texto-alvo]');
    var botaoConfirmar = dialogo.querySelector('[data-modal-confirmar-alvo]');
    var formularioAlvo = null;

    Array.prototype.forEach.call(
        document.querySelectorAll('[data-modal-texto]'),
        function (gatilho) {
            gatilho.addEventListener('click', function () {
                titulo.textContent = gatilho.dataset.modalTitulo || 'Confirmação';
                texto.textContent = gatilho.dataset.modalTexto || '';

                var idFormulario = gatilho.dataset.modalForm;
                formularioAlvo = idFormulario ? document.getElementById(idFormulario) : null;

                if (formularioAlvo) {
                    botaoConfirmar.textContent = gatilho.dataset.modalConfirmar || 'Confirmar';
                    botaoConfirmar.hidden = false;
                } else {
                    // Aviso puro: não há o que confirmar.
                    botaoConfirmar.hidden = true;
                }

                dialogo.showModal();
            });
        }
    );

    botaoConfirmar.addEventListener('click', function () {
        if (formularioAlvo) {
            formularioAlvo.submit();
        }
    });

    Array.prototype.forEach.call(
        dialogo.querySelectorAll('[data-modal-fechar]'),
        function (botao) {
            botao.addEventListener('click', function () {
                dialogo.close();
            });
        }
    );

    // Clique no fundo escuro fecha. O <dialog> recebe o clique do ::backdrop como
    // se fosse nele mesmo, então basta conferir se caiu fora da área do conteúdo.
    dialogo.addEventListener('click', function (evento) {
        if (evento.target !== dialogo) {
            return;
        }

        var area = dialogo.getBoundingClientRect();
        var foraDaCaixa = evento.clientX < area.left || evento.clientX > area.right
            || evento.clientY < area.top || evento.clientY > area.bottom;

        if (foraDaCaixa) {
            dialogo.close();
        }
    });
})();
