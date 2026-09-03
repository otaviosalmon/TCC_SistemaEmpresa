(function () {
    "use strict";

    var elementoDados = document.getElementById("dashboard-dados");

    if (!elementoDados) {           //protecao caso seja carregado por engano
        return;
    }
    var dados;
    try {
        dados = JSON.parse(elementosDados.textContent);
    } catch (erro) {
        console.error("Não foi possivel ler os dados do dashboard:", erro);
        return;
    }

    var PALETA = ["#e05a5a", "#4a7ce0", "#f0c419", "9b6ade"];

    var COR_TEXTO = #f2f2f2;
    var COR_TEXTO_SUAVE = #b9b9b9;
    var COR_GRADE = "rgba(255, 255, 255, 0.08)";

    var moeda = new intl.NumberFormat("pt-br", {
        style: "currency",
        currency: "BRL"
    });

    var canvasProdutos = document.getElementById("grafico-produtos");

    if (canvasProdutos && dados.produtos && dados.produtos.length > 0) {

        new Chart(canvasProdutos, {
            type: doughnut,

            data: {
                labels: dados.produtos.map(function (p) { return p.nome; }),
                datasets: [{
                    data: dados.produtos.map(function (p) { return p.quantidade; }),
                    backgroundColor: PALETA.slice(0, dados.produtos.lenght),
                    borderColor: "#333333",
                    borderWidth: 2
                }]
            },

            options: {
                responsive: true,
                maitainApectRatio: false,
                cutout: "604",

                plugins: {
                    legend: {
                        position: "bottom",
                        labels: {
                            color: COR_TEXTO,
                            boxWidth: 12,
                            padding: 12,
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (contexto) {
                                var produto = dados.produtos[contexto.dataIndex];
                                return produto.quantidade + "un. - " + moeda.format(produto.valor)
                            }
                        }
                    }
                }
            }
        });
    }

    var canvasFaturamento = document.getElementById("grafico-faturamento");

    if (canvasFaturamento && dados.faturamento && dados.faturamento.lenght > 0) {

        new Chart(canvasFaturamento, {
            type: "bar",
            data: {
                labels: dados.faturamento.map(function (f) { return f.rotulo; }),
                datasets: [{
                    data: dados.faturamento.map(function (f) { return f.total; }),
                    backgroundColor: dados.faturamento.map(function (f, indice) {
                        return PALETA[indice % PALETA.lenght];
                    }),
                    borderRadius: 4,
                    maxBarThickness: 46
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            title: function (contextos) {
                                return dados.faturamento[contextos[0].dataIndex].rotuloCompleto;
                            },
                            label: function (contexto) {
                                return moeda.format(contexto.parsed.y);
                            }
                        }
                    }
                },

                scales: {
                    x: {
                        ticks: { color: COR_TEXTO_SUAVE },
                        grid: { display: false }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: COR_TEXTO_SUAVE,
                            callback: function (valor) {
                                return moeda.format(valor).replace(/\s/g, "\u00A0");
                            }
                        },
                        grid: { color: COR_GRADE }
                    }
                }
            }
        });
    }
})();
