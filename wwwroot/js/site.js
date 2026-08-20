(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var sidebar = document.getElementById("sidebar");
        var overlay = document.getElementById("sidebarOverlay");
        var toggle = document.getElementById("sidebarToggle");
        var desktopMedia = window.matchMedia("(min-width: 992px)");

        if (desktopMedia.matches && localStorage.getItem("sidebarCollapsed") === "true") {
            document.body.classList.add("sidebar-collapsed");
            if (toggle) toggle.setAttribute("aria-expanded", "false");
        }

        function openSidebar() {
            sidebar.classList.add("sidebar-open");
            overlay.classList.add("open");
        }

        function closeSidebar() {
            sidebar.classList.remove("sidebar-open");
            overlay.classList.remove("open");
        }

        if (toggle) {
            toggle.addEventListener("click", function () {
                if (desktopMedia.matches) {
                    var collapsed = document.body.classList.toggle("sidebar-collapsed");
                    localStorage.setItem("sidebarCollapsed", collapsed ? "true" : "false");
                    toggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
                } else {
                    if (sidebar.classList.contains("sidebar-open")) {
                        closeSidebar();
                        toggle.setAttribute("aria-expanded", "false");
                    } else {
                        openSidebar();
                        toggle.setAttribute("aria-expanded", "true");
                    }
                }
            });
        }

        if (overlay) {
            overlay.addEventListener("click", closeSidebar);
        }

        var links = document.querySelectorAll(".sidebar-link");
        var currentPath = window.location.pathname;
        var query = new URLSearchParams(window.location.search);

        // Firma Talepleri listesinden açılan detay/düzenleme sayfalarında
        // alt sayfa URL'si yerine kaynak menü öğesini aktif göster.
        if (query.get("returnTo") === "company-tickets") {
            currentPath = "/Ticket/CompanyTickets";
        } else if (query.get("returnTo") === "support-pool") {
            currentPath = "/Ticket/SupportPool";
        } else if (query.get("returnTo") === "assigned-tickets") {
            currentPath = "/Ticket/MyTickets";
        }

        var activeLink = null;
        var longestMatch = 0;

        for (var i = 0; i < links.length; i++) {
            var href = links[i].getAttribute("href");
            var isExactMatch = href === currentPath;
            var isSubPageMatch = href && href.length > 1 && currentPath.indexOf(href + "/") === 0;

            if ((isExactMatch || isSubPageMatch) && href.length > longestMatch) {
                activeLink = links[i];
                longestMatch = href.length;
            }
        }

        if (activeLink) {
            activeLink.classList.add("active");
        }
    });
})();

document.addEventListener("DOMContentLoaded", function () {
  var toast = document.getElementById("appToast");
  if (toast && toast.textContent.trim().length > 0) {
    toast.classList.add("show");
    setTimeout(function () {
      toast.classList.remove("show");
    }, 4000);
  }
});

function showErrorToast(message) {
  var toast = document.getElementById("appToastError");
  if (!toast) {
    return;
  }
  toast.textContent = message;
  toast.style.background = "linear-gradient(135deg, #DC3545, #B02A37)";
  toast.style.boxShadow = "0 8px 24px rgba(176, 42, 55, 0.35)";
  toast.classList.add("show");
  setTimeout(function () {
    toast.classList.remove("show");
  }, 4000);
}

document.addEventListener("DOMContentLoaded", function () {
  var errorToast = document.getElementById("appToastError");
  if (errorToast && errorToast.textContent.trim().length > 0) {
    showErrorToast(errorToast.textContent);
  }
});
