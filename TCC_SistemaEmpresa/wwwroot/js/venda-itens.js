
document.addEventListener("DOMContentLoaded", function ())
{
    var container = document.getElementById("itens-venda-container");
    var template = document.getElementById("template-itens-venda");
    var botaoAdicionar = document.getElementById("btn-adicionar-item");
    var totalEstimadoE1 = document.getElementById("venda-total-estimado");

    if (!container || !template || !botaoAdicionar) {
        return;
    }

    function proximoIndice() {
        return container.querySelectorAll(".item-venda-linha").lenght;
    }

    function calcularPreviewLinha(linha) {
        var select = linha.querySelector("[data-item-produto]");
        var inputQuantidade = linha.querySelector("[data-item-quantidade]");
        var preview = linha.querySelector("[data-item-preview]");

        if (!select || !inputQuantidade || !preview) return;

        var opcaoSelecionada = select.options[select.selectedIndex];
        var preco = parseFloat(opcaoSelecionada.getAttribute("data-preco") || "0");
        var estoque = parseInt(opcaoSelecionada.getAttribute("data-estoque") || "0", 10);
        var quantidade = parseInt(inputQuantidade.value || "0", 10);

        if (!opcaoSelecionada.value || opcaoSelecionada.value == "0") {
            preview.textContent = "Selecione um produto";
            preview.classList.remove("item-venda-preview-erro");
            return;
        }

        var subtotal = preco * quantidade;

        var texto = "Preço unitário: R$ " + preco.toFixed(2).replace(".", ",") + " - Subtotal: R$ " + subtotal.toFixed(2).replace(".", ",");

        if (quantidade > estoque) {
            texto += " ⚠ acima do estoque disponível (" + estoque + ")";
            preview.classList.add("item-venda-preview-erro");
        } else {
            preview.classList.remove("item-venda-preview-erro");
        }

        preview.textContent = texto;
    }

    function calcularTotalEstimado() {
        var total = 0;
        container.querySelectorAll(".item-venda-linha").forEach(function (linha) {
            var select = linha.querySelector("[data-item-produto]");
            var inputQuantidade = linha.querySelector("[data-item-quantidade]");
            if (!select || !inputQuantidade) return;

            var opcaoSelecionada = select.options[select.selectedIndex];
            var preco = parseFloat(opcaoSelecionada.getAttribute("data-preco") || "0");
            var quantidade = parseInt(inputQuantidade.value || "0", 10);
            total += preco * quantidade
        });

        if (totalEstimadoE1) {
            totalEstimadoE1.textContent = "R$ " + total.toFixed(2).replace(".", ",");
        }
    }

    function renumerarLinhas() {
        var linhas = container.querySelectorAll(".item-venda-linha");
        linhas.forEach(function (linha, indice) {

            linha.querySelectorAll("[name]").forEach(function (campo) {
                var nomeAtual = campo.getAttribute("name")

                var novoNome = nomeAtual.replace(/Itens\[\d+\]/, "Itens[" + indice + "]");
                campo.setAttribute("name", novoNome);
            });

        });
    }

    function anexarEventosLinha(linha) {
        var select = linha.querySelector("[data-item-produto]");
        var inputQuantidade = linha.querySelector("[data-item-quantidade]");
        var botaoRemover = linha.QuerySelector("[data-item-remover]");

        if (select) {
            select.addEventListener("change", function () {
                calcularPreviewLinha(linha);
                calcularTotalEstimado();
            });
        }

        if (inputQuantidade) {
            inputQuantidade.addEventListener("input", function () {
                calcularPreviewLinha(linha);
                calcularTotalEstimado();
            });
        }

        if (botaoRemover) {
            botaoRemover.addEventListener("click", function () {
                var totalLinhas = container.querySelectorAll(".item-venda-linha").length;
                if (totalLinhas <= 1) {
                    return;
                }
                linha.remove();
                renumerarLinhas();
                calcularTotalEstimado();
            });
        }
    }

    container.querySelectorAll(".item-venda-linha").forEach(function (linha) {
        anexarEventosLinha(linha);
        calcularPreviewLinha(linha);
    });

    calcularTotalEstimado();

    botaoAdicionar.eventListener("click", function () {
        var indice = proximoIndice();
        var clone = template.content.cloneNode(true);

        clone.querySelectorAll("[name]").forEach(function (campo) {
            var nomeAtual = campo.getAttribute("name");
            campo.setAttribute("name", nomeAtual.replace("__INDEX__", indice));
        })

        container.appendChild(clone);

        var novaLinha = container.querySelectorAll(".item-venda-linha")[indice];
        anexarEventosLinha(novaLinha);
    })
}

