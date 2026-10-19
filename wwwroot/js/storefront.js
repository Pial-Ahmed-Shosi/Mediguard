"use strict";

(() => {
    const CART_KEY = "mediguard_storefront_cart";
    const PRESCRIPTION_MESSAGE =
        "Note: Uploading a valid prescription will be required at checkout for this item.";

    // DOM elements
    const noticeContainer = document.getElementById("mg-cart-notice");
    const noticeText = document.getElementById("mg-cart-notice-text");
    const noticeClose = document.getElementById("mg-cart-notice-close");

    const searchInput = document.getElementById("search");
    const locationInput = document.getElementById("location");
    const pharmacyInput = document.getElementById("pharmacy");
    const btnFilter = document.getElementById("btnFilter");

    const categoryPillContainer = document.getElementById("categoryPillContainer");
    const productGrid = document.getElementById("productGrid");
    const productCountBadge = document.getElementById("productCountBadge");
    const noResultsMessage = document.getElementById("noResultsMessage");

    let autoHideTimer = null;

    /* ------------------------------ Cart state ------------------------------ */

    function readCart() {
        try {
            const saved = localStorage.getItem(CART_KEY);
            if (!saved) return [];

            const parsed = JSON.parse(saved);
            if (!Array.isArray(parsed)) return [];

            return parsed.filter(item =>
                item &&
                (typeof item.productId === "string" || typeof item.productId === "number") &&
                typeof item.name === "string" &&
                Number.isFinite(item.unitPrice) &&
                Number.isInteger(item.quantity) &&
                item.quantity > 0
            );
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        try {
            localStorage.setItem(CART_KEY, JSON.stringify(cart));
            updateCartBadge(cart);
            return true;
        } catch {
            showNotice("Your browser could not save the cart. Please enable site storage.");
            return false;
        }
    }

    function updateCartBadge(cart) {
        const currentCart = cart || readCart();
        const totalCount = currentCart.reduce((sum, item) => sum + item.quantity, 0);

        document
            .querySelectorAll("#cartBadge, .cart-count-badge, [data-cart-count]")
            .forEach(badge => {
                badge.textContent = totalCount;
                badge.classList.toggle("d-none", totalCount <= 0);
            });

        window.dispatchEvent(
            new CustomEvent("cartUpdated", { detail: { cart: currentCart, totalCount } })
        );
    }

    /* ----------------------------- Notifications ---------------------------- */

    function showNotice(message) {
        if (!noticeContainer) {
            window.alert(message);
            return;
        }

        if (noticeText) {
            noticeText.textContent = message;
        } else {
            noticeContainer.textContent = message;
        }

        noticeContainer.hidden = false;
        noticeContainer.classList.remove("d-none");

        if (autoHideTimer) clearTimeout(autoHideTimer);
        autoHideTimer = setTimeout(hideNotice, 6000);
    }

    function hideNotice() {
        if (noticeContainer) {
            noticeContainer.hidden = true;
            noticeContainer.classList.add("d-none");
        }
    }

    /* ------------------------------- Add to cart ----------------------------- */

    function addToCart(button) {
        const productId = String(button.dataset.productId || "");
        const name = button.dataset.brandName;
        const unitPrice = Number(button.dataset.unitPrice);
        const stock = Number(button.dataset.stock);
        const requiresPrescription = button.dataset.requiresPrescription === "true";

        if (!productId || !name ||
            !Number.isFinite(unitPrice) || unitPrice < 0 ||
            !Number.isInteger(stock) || stock <= 0) {
            showNotice("This product is currently unavailable.");
            return;
        }

        const cart = readCart();
        const existing = cart.find(item => String(item.productId) === productId);
        const currentQuantity = existing ? existing.quantity : 0;

        if (currentQuantity >= stock) {
            showNotice("You have reached the available stock for this item.");
            return;
        }

        if (existing) {
            existing.quantity += 1;
            existing.unitPrice = unitPrice;
            existing.name = name;
            existing.stock = stock;
            existing.requiresPrescription = requiresPrescription;
        } else {
            cart.push({ productId, name, unitPrice, quantity: 1, stock, requiresPrescription });
        }

        if (!saveCart(cart)) return;

        // Exact prescription notice only for prescription-dependent products
        if (requiresPrescription) {
            showNotice(PRESCRIPTION_MESSAGE);
        } else {
            showNotice(`${name} added to your cart.`);
        }
    }

    /* ------------------------------- Filtering ------------------------------- */

    const norm = value => (value || "").toLowerCase().replace(/\s+/g, " ").trim();

    function filterCatalog() {
        if (!productGrid) return;

        const nameQuery = norm(searchInput && searchInput.value);
        const locationQuery = norm(locationInput && locationInput.value);
        const pharmacyQuery = norm(pharmacyInput && pharmacyInput.value);

        const activePill = categoryPillContainer
            ? categoryPillContainer.querySelector(".mg-pill-btn.active")
            : null;
        const selectedCategory = norm(activePill ? activePill.getAttribute("data-category") : "All");

        const items = productGrid.querySelectorAll(".product-item-col");
        let visibleCount = 0;

        items.forEach(item => {
            const brand = norm(item.getAttribute("data-brand"));
            const generic = norm(item.getAttribute("data-generic"));
            const category = norm(item.getAttribute("data-category"));
            const location = norm(item.getAttribute("data-location"));
            const pharmacy = norm(item.getAttribute("data-pharmacy"));

            const matchesName = !nameQuery || brand.includes(nameQuery) || generic.includes(nameQuery);
            const matchesLocation = !locationQuery || location.includes(locationQuery);
            const matchesPharmacy = !pharmacyQuery || pharmacy.includes(pharmacyQuery);
            const matchesCategory = selectedCategory === "all" || category === selectedCategory;

            const visible = matchesName && matchesLocation && matchesPharmacy && matchesCategory;
            item.classList.toggle("d-none", !visible);
            if (visible) visibleCount++;
        });

        if (productCountBadge) {
            productCountBadge.textContent = `${visibleCount} ${visibleCount === 1 ? "item" : "items"} found`;
        }

        if (noResultsMessage) {
            noResultsMessage.classList.toggle("d-none", !(visibleCount === 0 && items.length > 0));
        }
    }

    /* -------------------------------- Wiring --------------------------------- */

    document.addEventListener("DOMContentLoaded", () => {
        updateCartBadge();

        if (noticeClose) noticeClose.addEventListener("click", hideNotice);
        if (btnFilter) btnFilter.addEventListener("click", filterCatalog);

        [searchInput, locationInput, pharmacyInput].forEach(input => {
            if (input) input.addEventListener("input", filterCatalog);
        });

        if (categoryPillContainer) {
            categoryPillContainer.addEventListener("click", event => {
                const pill = event.target.closest(".mg-pill-btn");
                if (!pill) return;

                categoryPillContainer
                    .querySelectorAll(".mg-pill-btn")
                    .forEach(p => p.classList.remove("active"));
                pill.classList.add("active");

                filterCatalog();
            });
        }

        // Apply any pre-filled search term (e.g. from the server-side query string)
        if (searchInput && searchInput.value) filterCatalog();
    });

    document.addEventListener("click", event => {
        const button = event.target.closest(".mg-add-to-cart");
        if (!button || button.disabled) return;
        addToCart(button);
    });
})();
