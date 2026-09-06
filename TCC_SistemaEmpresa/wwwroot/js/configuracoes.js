(function () {
    "use strict";

    var formulario = document.querySelector("[data-aparencia-form]");

    if (!formulario) {
        return;
    }

    var raiz = document.documentElement;
    var aviso = document.querySelector("[data-tema-aviso]");

    var atributos = {
        fonte: "data-fonte",
        densidade: "data-densidade"
    };

    var enviando = false;
    var pendente = false;

    function aplicar(grupo, valor) {
        if (grupo === "tema") {
            if (window.temaAplicacao) {
                window.temaAplicacao.aplicar(valor);
            }
            return;
        }

        if (atributos[grupo]) {
            raiz.setAttribute(atributos[grupo], valor);
        }
    }

    function mostrarAviso(falhou) {
        if (aviso) {
            aviso.hidden = !falhou;
        }
    }

    function salvar() {
        if (enviando) {
            pendente = true;
            return;
        }

        enviando = true;

        fetch(formulario.action, {
            method: "POST",
            body: new FormData(formulario),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (resposta) {
                if (!resposta.ok) {
                    throw new Error(resposta.status);
                }

                mostrarAviso(false);
            })
            .catch(function () {
                mostrarAviso(true);
            })
            .then(function () {
                enviando = false;

                if (pendente) {
                    pendente = false;
                    salvar();
                }
            });
    }

    formulario.addEventListener("change", function (evento) {
        var opcao = evento.target;

        if (opcao.type !== "radio") {
            return;
        }

        aplicar(opcao.name, opcao.value);
        salvar();
    });
})();
