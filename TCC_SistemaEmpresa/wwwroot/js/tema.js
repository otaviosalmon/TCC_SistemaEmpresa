(function () {
    "use strict";

    var raiz = document.documentElement;
    var consultaClaro = window.matchMedia("(prefers-color-scheme: light)");
    var preferencia = raiz.getAttribute("data-tema") || "escuro";

    function resolver(tema) {
        if (tema !== "sistema") {
            return tema;
        }

        return consultaClaro.matches ? "claro" : "escuro";
    }

    function aplicar(tema) {
        preferencia = tema;
        raiz.setAttribute("data-tema", resolver(tema));
    }

    consultaClaro.addEventListener("change", function () {
        if (preferencia === "sistema") {
            aplicar(preferencia);
        }
    });

    aplicar(preferencia);

    window.temaAplicacao = { aplicar: aplicar };
})();
