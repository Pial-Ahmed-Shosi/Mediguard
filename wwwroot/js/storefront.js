"use strict";

(() => {
    const CART_KEY = "mediguard_storefront_cart";
    const PRESCRIPTION_MESSAGE =
        "Note: Uploading a valid prescription will be required at checkout for this item.";

    // DOM References
    const noticeContainer = document.getElementById("mg-cart-notice");
    const noticeText = document.getElementById("mg-cart-notice-text");
    const noticeClose = document.getElementById("mg-cart-notice-close");

    // 3 Search Inputs
    const searchInput = document.getElementById("search");         // 1. Medicine / Item Name
    const locationInput = document.getElementById("location");     // 2. Location / Area
    const pharmacyInput = document.getElementById("pharmacy");     // 3. Pharmacy / Hospital

    const categorySelect = document.getElementById("category");
    const categoryPillContainer = document.getElementById("categoryPillContainer");
    const productGrid = document.getElementById("productGrid");
    const productCountBadge = document.getElementById("productCountBadge");
    const noResultsMessage = document.getElementById("noResultsMessage");

    let autoHideTimer = null;

    /**
     * Reads saved cart state from LocalStorage safely.
     */
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

    /**
     * Persists cart array to LocalStorage and triggers UI badge update.
     */
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

    /**
     * Updates header cart badge indicators across page loads.
     */
    function updateCartBadge(cart) {
        const currentCart = cart || readCart();
        const totalCount = currentCart.reduce((sum, item) => sum + item.quantity, 0);

        const badges = document.querySelectorAll("#cartBadge, .cart-count-badge, [data-cart-count]");
        badges.forEach(badge => {
            badge.textContent = totalCount;
            if (totalCount > 0) {
                badge.classList.remove("d-none");
            }
        });

        window.dispatchEvent(new CustomEvent("cartUpdated", { detail: { cart: currentCart, totalCount } }));
    }

    /**
     * Displays prescription notification alert notice toast safely.
     */
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
        autoHideTimer = setTimeout(() => {
            hideNotice();
        }, 6000);
    }

    function hideNotice() {
        if (noticeContainer) {
            noticeContainer.hidden = true;
            noticeContainer.classList.add("d-none");
        }
    }

    /**
     * Handles Add to Cart action, enforcing stock boundaries and prescription notification requirements.
     */
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
            cart.push({
                productId,
                name,
                unitPrice,
                quantity: 1,
                stock,
                requiresPrescription
            });
        }

        if (!saveCart(cart)) {
            return;
        }

        // Ticket 31 Requirement: Exact Prescription Warning Notification
        if (requiresPrescription) {
            showNotice(PRESCRIPTION_MESSAGE);
        } else {
            showNotice(`${name} added to your cart.`);
        }
    }

    /**
     * Filters catalog based on all 3 search options (Name, Location, Pharmacy) and Category selection.
     */
    function filterCatalog() {
        if (!productGrid) return;

        const nameQuery = (searchInput ? searchInput.value : "").toLowerCase().trim();
        const locationQuery = (locationInput ? locationInput.value : "").toLowerCase().trim();
        const pharmacyQuery = (pharmacyInput ? pharmacyInput.value : "").toLowerCase().trim();

        // Get category from pills or select dropdown
        const activePill = categoryPillContainer ? categoryPillContainer.querySelector(".mg-pill-btn.active") : null;
        const pillCategory = activePill ? activePill.getAttribute("data-category") : "";
        const dropdownCategory = categorySelect ? categorySelect.value : "";
        const selectedCategory = (pillCategory || dropdownCategory).toLowerCase().trim();

        const items = productGrid.querySelectorAll(".product-item-col");
        let visibleCount = 0;

        items.forEach(item => {
            const brand = (item.getAttribute("data-brand") || "").toLowerCase();
            const generic = (item.getAttribute("data-generic") || "").toLowerCase();
            const category = (item.getAttribute("data-category") || "").toLowerCase();
            const location = (item.getAttribute("data-location") || "").toLowerCase();
            const pharmacy = (item.getAttribute("data-pharmacy") || "").toLowerCase();

            const matchesName = !nameQuery || brand.includes(nameQuery) || generic.includes(nameQuery);
            const matchesLocation = !locationQuery || location.includes(locationQuery) || brand.includes(locationQuery);
            const matchesPharmacy = !pharmacyQuery || pharmacy.includes(pharmacyQuery) || brand.includes(pharmacyQuery);
            const matchesCategory = !selectedCategory || selectedCategory === "all" || category === selectedCategory;

            if (matchesName && matchesLocation && matchesPharmacy && matchesCategory) {
                item.classList.remove("d-none");
                visibleCount++;
            } else {
                item.classList.add("d-none");
            }
        });

        if (productCountBadge) {
            productCountBadge.textContent = `${visibleCount} ${visibleCount === 1 ? "item" : "items"} found`;
        }

        if (noResultsMessage) {
            if (visibleCount === 0 && items.length > 0) {
                noResultsMessage.classList.remove("d-none");
            } else {
                noResultsMessage.classList.add("d-none");
            }
        }
    }

    // Event Bindings
    document.addEventListener("DOMContentLoaded", () => {
        updateCartBadge();

        if (noticeClose) {
            noticeClose.addEventListener("click", hideNotice);
        }

        // Live filtration listeners for all 3 search options
        if (searchInput) searchInput.addEventListener("input", filterCatalog);
        if (locationInput) locationInput.addEventListener("input", filterCatalog);
        if (pharmacyInput) pharmacyInput.addEventListener("input", filterCatalog);
        if (categorySelect) categorySelect.addEventListener("change", filterCatalog);

        if (categoryPillContainer) {
            categoryPillContainer.addEventListener("click", event => {
                const pill = event.target.closest("[data-category]");
                if (!pill) return;

                const pills = categoryPillContainer.querySelectorAll(".mg-pill-btn");
                pills.forEach(p => p.classList.remove("active"));
                pill.classList.add("active");

                filterCatalog();
            });
        }
    });

    document.addEventListener("click", event => {
        const button = event.target.closest(".mg-add-to-cart");
        if (!button || button.disabled) {
            return;
        }

        addToCart(button);
    });
})();