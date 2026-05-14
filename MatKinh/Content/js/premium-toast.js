$(document).ready(function () {
    var AUTO_HIDE_DELAY = 4200;

    $(".premium-toast").each(function () {
        var toast = $(this);

        var timer = setTimeout(function () {
            hideToast(toast);
        }, AUTO_HIDE_DELAY);

        toast.on("mouseenter", function () {
            clearTimeout(timer);
        });

        toast.on("mouseleave", function () {
            timer = setTimeout(function () {
                hideToast(toast);
            }, 1800);
        });
    });

    $(document).on("click", ".premium-toast__close", function () {
        var toast = $(this).closest(".premium-toast");
        hideToast(toast);
    });

    function hideToast(toast) {
        if (!toast || toast.length === 0 || toast.data("hiding")) {
            return;
        }

        toast.data("hiding", true);
        toast.addClass("premium-toast--hide");

        setTimeout(function () {
            toast.remove();
        }, 260);
    }
});