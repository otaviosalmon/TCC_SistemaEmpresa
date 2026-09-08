(function () {
    "use strict";

    var elementoDados = document.getElementById("previsao-dados");

    if (!elementoDados) {
        return;
    }

    var dados;
    try {
        dados = JSON.parse(elementoDados.textContent);

    } catch (erro) {
        console.error("Não foi possivel ler os dados da previsão:", erro);
        return;
    }

    var canvas = document.getElementById("grafico-previsao");

    if (!canvas) {
        return;
    }

    var COR_FATURAMENTO = "#4caf50";
    var COR_DESPESAS = "#e05a5a";
    var COR_TEXTO = "#f2f2f2";
    var COR_TEXTO_SUAVE = "#b9b9b9";
    var COR_GRADE = "rgba(255, 255, 255, 0.08)";

    var moeda = new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL"
    });

    var historico = dados.faturamentoHistorico || [];
    var previstoFaturamento = dados.faturamentoPrevisto || [];
    var previstoDespesas = dados.despesasPrevistas || [];

    var rotulos = historico.map(function (p) { return p.rotulo; });

    if (dados.temPrevisao) {
        previstoFaturamento.forEach(function (p) {
            rotulo.push(p.rotulo);
            rotulosCompletos.push(p.rotuloCompleto);
        }
    }

    var rotulosCompletos = historico.map(function (p) { return p.rotuloCompleto; });

    var qtdHistorico = historico.length;

    function serieRealizada(pontos) {
        var valores = pontos.map(function (p) { return p.total; });

        if (dados.temPrevisao) {
            for (var i = 0; i < previstoFaturamento.length; i++) {
                valores.push(null)
            }
        }

        return valores;
    }

    function seriePrevista(pontosHistoricos, pontosPrevistos) {
        var valores = [];

        for (var i = 0; i < qtdHistorico; i++) {
            valores.push(null);
        }

        if (qtdHistorico > 0) {
            valores[qtdHistorico - 1] = pontosHistoricos[qtdHistorico - 1].total;
        }

        pontosPrevistos.forEach(function (p) { valores.push(p.total); });

        return valores;
    }

    var datasets = [
        {
            label: "Faturamento realizado",
            data: serieRealizada(historico),
            borderColor: COR_FATURAMENTO,
            backgroundColor: COR_FATURAMENTO,
            borderwidth: 2,
            tension: 0.25,
            pointRadius: 3,
            spanGaps: false
        },
        {
            label: "Despesas Realizadas",
            data: serieRealizada(dados.despesasHistorico || []),
            borderColor: COR_DESPESAS,
            backgroundColor: COR_DESPESAS,
            borderWidth: 2,
            tension: 0.25,
            pointRadius: 3,
            spanGaps: false
        }
    ];

    if (dados.temPrevisao) {
        datasets.push({
            label: "Faturamento previsto",
            data: seriePrevista(historico, previstoFaturamento),
            borderColor: COR_FATURAMENTO,
            backgroundColor: COR_FATURAMENTO,
            borderWidth: 2,
            borderDash: [6, 6],
            tension: 0,
            pointRadius: 3,
            pointStyle: "rectRot",
            spanGaps: false
        });

        datasets.push({
            label: "Despesas previstas",
            data: seriePrevista(dados.despesasHistorico || [], previstoDespesas),
            borderColor: COR_DESPESAS,
            backgroundColor: COR_DESPESAS,
            borderWidth: 2,
            borderDash: [6, 6],
            tension: 0,
            pointRadius: 3,
            pointStyle: "rectRot",
            spanGaps: false
        });
    }

    new Chart(canvas, {
        type: "line",

        data: {
            labels: rotulos,
            datasets: datasets
        },

        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: "index",
                intersect: false
            },

            plugins: {
                legend: {
                    position: "bottom",
                    lables: {
                        color: COR_TEXTO,
                        boxWidth: 14,
                        padding: 14,
                        usePointStyle: true
                    }
                },
                tooltip: {
                    callbacks: {
                        title: function (contextos) {
                            return rotulosCompletos[contextos[0].dataIndex];
                        },

                        label: function (contexto) {
                            if (contexto.parsed.y === null) {
                                return null;
                            }

                            return contexto.dataset.label + ": " + moeda.format(contexto.parsed.y);
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
                        autoSkip: true,
                        maxTicksLimit: 16,
                        font: { size: 11 }
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
})();