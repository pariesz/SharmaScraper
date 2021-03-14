var customerTpv = {};

customerTpv.pay = function (idPaymentMethod, isTpvOnline) {
    var chkLegal = Scl.getByNameOrId('chkLegal');
    if (chkLegal != null && !chkLegal.checked) {
        alert(Scl.translate("Debe aceptar las condiciones legales para poder registrarse."));
        return;
    }
    if (this.disabled == null || this.disabled == false) {
        if (!isTpvOnline) {
            Scl.getByNameOrId("idPaymentMethod").value = idPaymentMethod;
            Scl.getByNameOrId("paymentForm").submit();
        } else {
            var sItems = Scl.getByNameOrId("sItems").value;
            var url = "tpvonline?idPaymentMethod=" + idPaymentMethod + "&sItems=" + sItems
            window.open(url, '_blank', 'width=1100,height=600,status=yes,toolbar=yes,menubar=yes,resizable=yes,scrollbars=yes');
        }
    }
    this.disabled = true;
};