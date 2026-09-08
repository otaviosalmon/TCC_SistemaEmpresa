from fastapi import FastAPI
from pydantic import BaseModel, Field
from typing import List
import pandas as pd
import numpy as np
from sklearn.linear_model import LinearRegression
from sklearn.metrics import r2_score #metrica de erro

class PontoHistorico(BaseModel):
    """Um mês fechado do histórico. Espelha PontoSerieDto.cs no C#"""

    ano: int 
    mes: int = Field(ge = 1, le = 12)
    valor: float

class SerieHistorica(BaseModel):
    nome: str
    pontos: List[PontoHistorico]

class RequisicaoPrevisao(BaseModel):
    meses_previsao: int = Field(ge = 1,le = 24)
    series: List[SerieHistorica]

class PontoPrevisto(BaseModel):
    ano: int
    mes: int
    valor: float

class SeriePrevista(BaseModel):
    nome: str
    suficiente: bool
    coeficiente_angular: float
    intercepto: float
    r2: float
    pontos: List[PontoPrevisto]

class RespostaPrevisao(BaseModel):
    series: List[SeriePrevista]

MINIMO_PONTOS = 3

#sem DateTime pq não e necessario dia
def avancar_mes(ano: int, mes: int, passos: int) -> tuple[int, int]: 
    total = ano * 12 + (mes - 1) + passos
    return total // 12, (total % 12) + 1

def prever_serie(serie: SerieHistorica, meses_previsao: int) -> SeriePrevista:
    if len(serie.pontos) < MINIMO_PONTOS:
        return SeriePrevista(
            nome =  serie.nome,
            suficiente = False,
            coeficiente_angular = 0.0,
            intercepto = 0.0,
            r2 = 0.0,
            pontos = [],
        )
    df = pd.DataFrame([ponto.model_dump() for ponto in serie.pontos])   
    df = df.sort_values(["ano", "mes"]).reset_index(drop=True)  #e pra vir ordenado, mas se n vier isso impede de quebrar
    df["indice_mes"] = range(len(df))

    x = df[["indice_mes"]].to_numpy().reshape(-1,1)
    y = df["valor"].to_numpy()

    modelo = LinearRegression()
    modelo.fit(x,y)

    coeficiente = float(modelo.coef_[0])
    intercepto = float(modelo.intercept_)
    r2 = float(r2_score(y, modelo.predict(x)))

    ultimo_indice = int(df["indice_mes"].iloc[-1])
    ultimo_ano = int(df["ano"].iloc[-1])
    ultimo_mes = int(df["mes"].iloc[-1])

    indices_futuros = np.arange(
        ultimo_indice + 1,
        ultimo_indice + 1 + meses_previsao
    ).reshape(-1,1)

    valores_previstos = modelo.predict(indices_futuros)
    pontos_previstos : List[PontoPrevisto] = []

    for passo, valor in enumerate(valores_previstos, start=1):
        ano_futuro, mes_futuro = avancar_mes(ultimo_ano, ultimo_mes, passo)

        valor_ajustado = max(0.0, float(valor))
        pontos_previstos.append(PontoPrevisto(
            ano = ano_futuro,
            mes = mes_futuro,
            valor = round(valor_ajustado, 2),
        ))
    return SeriePrevista(
        nome = serie.nome,
        suficiente = True,
        coeficiente_angular = round(coeficiente, 2),
        intercepto = round(intercepto,2),
        r2 = round(r2, 4),
        pontos = pontos_previstos,
    )

app = FastAPI(
    title = "L.O Solutions - Camada Analítica",
    description = "Previsão de faturamento e despesas por regressão linear.",
    version = "1.0.0",
)

@app.get("/saude")
def saude():
    """apenas para diagnotisco e saber se a aplicação subiu"""
    return {"status": "ok", "servico": "previsao"}

@app.post("/previsao", response_model=RespostaPrevisao)
def previsao(requisicao: RequisicaoPrevisao) -> RespostaPrevisao:
    resultados = [
        prever_serie(serie, requisicao.meses_previsao)
        for serie in requisicao.series
    ]

    return RespostaPrevisao(series = resultados)
    


