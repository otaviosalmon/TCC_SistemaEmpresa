(function () {
    "use strict";

    var elementoDados = document.getElementById("dashboard-dados");

    if (!elementoDados) {           //protecao caso seja carregado por engano
        return;
    }
    var dados;
    try {
        dados = JSON.parse(elementoDados.textContent);
    } catch (erro) {
        console.error("Não foi possivel ler os dados do dashboard:", erro);
        return;
    }

    var estilo = getComputedStyle(document.documentElement);

    function token(nome) {
        return estilo.getPropertyValue(nome).trim();
    }

    var PALETA = ["--grafico-1", "--grafico-2", "--grafico-3", "--grafico-4", "--grafico-5"].map(token);

    var COR_TEXTO = token("--cor-texto");
    var COR_TEXTO_SUAVE = token("--cor-texto-suave");
    var COR_GRADE = token("--cor-grade-grafico");

    var moeda = new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL"
    });

    var canvasProdutos = document.getElementById("grafico-produtos");

    if (canvasProdutos && dados.produtos && dados.produtos.length > 0) {

        new Chart(canvasProdutos, {
            type: "doughnut",

            data: {
                labels: dados.produtos.map(function (p) { return p.nome; }),
                datasets: [{
                    data: dados.produtos.map(function (p) { return p.quantidade; }),
                    backgroundColor: PALETA.slice(0, dados.produtos.length),
                    borderColor: token("--cor-superficie"),
                    borderWidth: 2
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "60%",

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

    if (canvasFaturamento && dados.faturamento && dados.faturamento.length > 0) {

        new Chart(canvasFaturamento, {
            type: "bar",
            data: {
                labels: dados.faturamento.map(function (f) { return f.rotulo; }),
                datasets: [{
                    data: dados.faturamento.map(function (f) { return f.total; }),
                    backgroundColor: dados.faturamento.map(function (f, indice) {
                        return PALETA[indice % PALETA.length];
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
                        ticks: {
                            color: COR_TEXTO_SUAVE,
                            maxRotation: 0,
                            minRotation: 0,
                            autoSkip: false,
                            font: { size: 12 }
                        },
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
