(function () {
    const totalDigitos = 11;

    function soDigitos(texto) {
        return (texto || '').replace(/\D/g, '');
    }

    function digitosDoCpf(texto) {
        return soDigitos(texto).slice(0, totalDigitos);
    }

    function formatar(digitos) {
        if (digitos.length <= 3)
            return digitos;

        if (digitos.length <= 6)
            return digitos.slice(0, 3) + '.' + digitos.slice(3);

        if (digitos.length <= 9)
            return digitos.slice(0, 3) + '.' + digitos.slice(3, 6) + '.' + digitos.slice(6);

        return digitos.slice(0, 3) + '.' + digitos.slice(3, 6) + '.'
             + digitos.slice(6, 9) + '-' + digitos.slice(9);
    }

    function contarDigitosAte(texto, posicao) {
        return Math.min(soDigitos(texto.slice(0, posicao)).length, totalDigitos);
    }

    function posicaoAposDigito(textoFormatado, quantidade) {
        if (quantidade === 0)
            return 0;

        let contados = 0;

        for (let indice = 0; indice < textoFormatado.length; indice++) {
            if (/\d/.test(textoFormatado[indice])) {
                contados++;

                if (contados === quantidade)
                    return indice + 1;
            }
        }

        return textoFormatado.length;
    }

    function aplicar(campo) {
        const anterior = campo.value;
        const formatado = formatar(digitosDoCpf(anterior));

        if (formatado === anterior)
            return;

        const digitosAteCursor = contarDigitosAte(anterior, campo.selectionStart ?? anterior.length);

        campo.value = formatado;

        if (campo === document.activeElement) {
            const posicao = posicaoAposDigito(formatado, digitosAteCursor);
            campo.setSelectionRange(posicao, posicao);
        }
    }

    document.querySelectorAll('[data-mascara="cpf"]').forEach(function (campo) {
        aplicar(campo);
        campo.addEventListener('input', function () { aplicar(campo); });
        campo.addEventListener('blur', function () { aplicar(campo); });
    });
})();
