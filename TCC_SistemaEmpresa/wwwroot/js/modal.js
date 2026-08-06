
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
