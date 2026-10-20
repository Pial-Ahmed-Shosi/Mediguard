(() => {
    "use strict";

    // ─── Constants ────────────────────────────────────────────────────
    const CART_KEY = "mg_cart";

    // ─── DOM refs ─────────────────────────────────────────────────────
    const searchInput     = document.getElementById("search");
    const locationInput   = document.getElementById("location");
    const pharmacyInput   = document.getElementById("pharmacy");
    const filterBtn       = document.getElementById("btnFilter");
    const productGrid     = document.getElementById("productGrid");
    const noResults       = document.getElementById("noResultsMessage");
    const countBadge      = document.getElementById("productCountBadge");
    const catContainer    = document.getElementById("categoryPillContainer");
    const noticeBox       = document.getElementById("mg-cart-notice");
    const noticeText      = document.getElementById("mg-cart-notice-text");
    const noticeClose     = document.getElementById("mg-cart-notice-close");

    // ─── Cart helpers (localStorage) ──────────────────────────────────
    function loadCart() {
        try { return JSON.parse(localStorage.getItem(CART_KEY)) || []; }
        catch { return []; }
    }

    function saveCart(cart) {
        localStorage.setItem(CART_KEY, JSON.stringify(cart));
    }

    function addToCart(item) {
        const cart = loadCart();
        const existing = cart.find(c => c.productId === item.productId);
        if (existing) {
            existing.quantity = (existing.quantity || 1) + 1;
        } else {
            cart.push({ ...item, quantity: 1 });
        }
        saveCart(cart);
        updateCartBadge();
    }

    function updateCartBadge() {
        const cart = loadCart();
        const total = cart.reduce((acc, i) => acc + (i.quantity || 1), 0);
        const badge = document.getElementById("cartCountBadge");
        if (badge) badge.textContent = total;
    }

    // ─── Notification toast ───────────────────────────────────────────
    let noticeTimer = null;

    function showNotice(message) {
        if (!noticeBox || !noticeText) return;
        noticeText.textContent = message;
        noticeBox.hidden = false;
        clearTimeout(noticeTimer);
        noticeTimer = setTimeout(() => { noticeBox.hidden = true; }, 7000);
    }

    if (noticeClose) {
        noticeClose.addEventListener("click", () => {
            noticeBox.hidden = true;
            clearTimeout(noticeTimer);
        });
    }

    // ─── Filtering ────────────────────────────────────────────────────
    let activeCategory = "All";

    function getProductCols() {
        return Array.from(productGrid?.querySelectorAll(".product-item-col") ?? []);
    }

    function applyFilters() {
        const searchTerm = (searchInput?.value ?? "").trim().toLowerCase();
        const cols = getProductCols();
        let visible = 0;

        cols.forEach(col => {
            const brand    = (col.dataset.brand    ?? "");
            const generic  = (col.dataset.generic  ?? "");
            const category = (col.dataset.category ?? "");

            const matchesSearch =
                !searchTerm ||
                brand.includes(searchTerm) ||
                generic.includes(searchTerm);

            const matchesCategory =
                activeCategory === "All" ||
                category === activeCategory;

            const show = matchesSearch && matchesCategory;
            col.style.display = show ? "" : "none";
            if (show) visible++;
        });

        if (noResults) noResults.classList.toggle("d-none", visible > 0);
        if (countBadge) countBadge.textContent = `${visible} ${visible === 1 ? "item" : "items"} found`;
    }

    filterBtn?.addEventListener("click", applyFilters);
    searchInput?.addEventListener("keydown", e => { if (e.key === "Enter") applyFilters(); });

    // ─── Category pill navigation ─────────────────────────────────────
    catContainer?.addEventListener("click", e => {
        const pill = e.target.closest(".mg-pill-btn");
        if (!pill) return;

        catContainer.querySelectorAll(".mg-pill-btn").forEach(p => p.classList.remove("active"));
        pill.classList.add("active");
        activeCategory = pill.dataset.category ?? "All";
        applyFilters();
    });

    // ─── Add-to-Cart delegation ───────────────────────────────────────
    productGrid?.addEventListener("click", e => {
        const btn = e.target.closest(".mg-add-to-cart");
        if (!btn || btn.disabled) return;

        const productId          = btn.dataset.productId;
        const brandName          = btn.dataset.brandName;
        const unitPrice          = parseFloat(btn.dataset.unitPrice) || 0;
        const requiresPrescription = btn.dataset.requiresPrescription === "true";

        if (requiresPrescription) {
            showNotice(
                "Note: Uploading a valid prescription will be required at checkout for this item."
            );
        }

        addToCart({ productId, brandName, unitPrice, requiresPrescription });

        // Micro-animation feedback
        const icon = btn.querySelector("i");
        if (icon) {
            icon.className = "bi bi-check2 fs-6";
            setTimeout(() => { icon.className = "bi bi-bag-plus fs-6"; }, 1200);
        }
    });

    // ─── Boot ─────────────────────────────────────────────────────────
    updateCartBadge();
    applyFilters();
})();