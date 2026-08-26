(function () {
    const form = document.querySelector('[data-movimentacao-form]');
    if (!form) {
        return;
    }

    const produto = form.querySelector('[data-movimentacao-produto]');
    const tipo = form.querySelector('[data-movimentacao-tipo]');
    const quantidade = form.querySelector('[data-movimentacao-quantidade]');
    const antes = form.querySelector('[data-movimentacao-antes]');
    const natureza = form.querySelector('[data-movimentacao-natureza]');
    const depois = form.querySelector('[data-movimentacao-depois]');
    const aviso = form.querySelector('[data-movimentacao-aviso]');

    if (!produto || !tipo || !quantidade || !antes || !natureza || !depois) {
        return;
    }

    const rotulos = { ENTRADA: 'Entrada', SAIDA: 'Saída' };
    const vazio = '—';

    function opcaoSelecionada(campo) {
        return campo.options[campo.selectedIndex] || null;
    }

    function saldoDoProduto() {
        const opcao = opcaoSelecionada(produto);
        if (!opcao || opcao.dataset.saldo === '' || opcao.dataset.saldo === undefined) {
            return null;
        }

        const saldo = Number(opcao.dataset.saldo);
        return Number.isNaN(saldo) ? null : saldo;
    }

    function naturezaSelecionada() {
        const opcao = opcaoSelecionada(tipo);
        return opcao && opcao.dataset.natureza ? opcao.dataset.natureza : null;
    }

    function atualizar() {
        const saldo = saldoDoProduto();
        const direcao = naturezaSelecionada();
        const informada = Number(quantidade.value);
        const unidades = Number.isNaN(informada) ? 0 : informada;

        antes.textContent = saldo === null ? vazio : String(saldo);
        natureza.textContent = direcao ? rotulos[direcao] : vazio;

        depois.classList.remove('campo-valor-invalido');
        if (aviso) {
            aviso.textContent = '';
            aviso.classList.remove('campo-ajuda-alerta');
        }

        if (saldo === null || !direcao || unidades <= 0) {
            depois.textContent = vazio;
            return;
        }

        const projetado = saldo + (direcao === 'ENTRADA' ? unidades : -unidades);
        depois.textContent = String(projetado);

        if (projetado < 0) {
            depois.classList.add('campo-valor-invalido');
            if (aviso) {
                aviso.textContent = 'Saldo insuficiente: o estoque não pode ficar negativo.';
                aviso.classList.add('campo-ajuda-alerta');
            }
        }
    }

    produto.addEventListener('change', atualizar);
    tipo.addEventListener('change', atualizar);
    quantidade.addEventListener('input', atualizar);

    atualizar();
})();
