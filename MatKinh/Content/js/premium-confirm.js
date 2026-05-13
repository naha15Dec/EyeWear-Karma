$(document).ready(function () {
    var pendingConfirmElement = null;

    $(document).on("click", "[data-confirm]", function (e) {
        var element = $(this);

        if (element.data("confirmed") === true) {
            element.removeData("confirmed");
            return true;
        }

        e.preventDefault();

        pendingConfirmElement = element;

        var message = element.attr("data-confirm") || "Bạn xác nhận thao tác này?";
        var title = element.attr("data-confirm-title") || "Bạn xác nhận thao tác này?";
        var okText = element.attr("data-confirm-ok") || "Xác nhận";

        $("#globalConfirmTitle").text(title);
        $("#globalConfirmMessage").text(message);
        $("#globalConfirmOk").text(okText);

        $("#globalConfirmModal").modal("show");

        return false;
    });

    $("#globalConfirmOk").on("click", function () {
        if (!pendingConfirmElement) {
            $("#globalConfirmModal").modal("hide");
            return;
        }

        var element = pendingConfirmElement;
        pendingConfirmElement = null;

        $("#globalConfirmModal").modal("hide");

        if (element.is("button[type='submit']")) {
            element.closest("form").submit();
            return;
        }

        if (element.is("a")) {
            window.location.href = element.attr("href");
            return;
        }

        element.data("confirmed", true);
        element.trigger("click");
    });
});