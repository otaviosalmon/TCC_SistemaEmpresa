
(function () {
    if (typeof $ === 'undefined' || !$.validator) {
        console.warn('validacao-ptbr.js carregado sem jQuery Validation na página.');
        return;
    }

    function paraNumero(valor) {
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
