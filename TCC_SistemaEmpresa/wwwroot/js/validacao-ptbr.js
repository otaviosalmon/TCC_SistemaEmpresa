// Realinha o jQuery Validation ao formato pt-BR.
//
// A aplicação roda com cultura pt-BR (Program.cs), então o servidor espera
// "10.510,50". O jQuery Validation, porém, valida número no padrão americano
// e recusaria esse valor no navegador antes de chegar ao POST.
//
// Carregar SEMPRE depois de _ValidationScriptsPartial: sem jquery.validate
// na página, não há o que sobrescrever.
(function () {
    if (typeof $ === 'undefined' || !$.validator) {
        console.warn('validacao-ptbr.js carregado sem jQuery Validation na página.');
        return;
    }

    function paraNumero(valor) {
        // "10.510,50" -> 10510.50
        return parseFloat(String(valor).replace(/\./g, '').replace(',', '.'));
    }

    $.validator.methods.number = function (value, element) {
        return this.optional(element)
            || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
    };

    $.validator.methods.range = function (value, element, param) {
        var numero = paraNumero(value);
        return this.optional(element) || (numero >= param[0] && numero <= param[1]);
    };
})();
