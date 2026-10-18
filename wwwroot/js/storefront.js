"use strict";

(() => {
    const CART_KEY = "mediguard_storefront_cart";
    const PRESCRIPTION_MESSAGE =
        "Note: Uploading a valid prescription will be required at checkout for this item.";

    // DOM References
    const noticeContainer = document.getElementById("mg-cart-notice");
    const noticeText = document.getElementById("mg-cart-notice-text");
    const noticeClose = document.getElementById("mg-cart-notice-close");
    const searchInput = document.getElementById("search");
    const categorySelect = document.getElementById("category");
    const categorySidebar = document.getElementById("categorySidebar");
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
     * Updates header cart badge indicators.
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
     * Safely displays notification without destroying HTML structure inside notice toast.
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

        // Ticket 31 Requirement: Trigger required notification for prescription products
        if (requiresPrescription) {
            showNotice(PRESCRIPTION_MESSAGE);
        } else {
            showNotice(`${name} added to your cart.`);
        }
    }

    /**
     * Real-time client-side catalog filtering for search and category selection.
     */
    function filterCatalog() {
        if (!productGrid) return;

        const query = (searchInput ? searchInput.value : "").toLowerCase().trim();
        const selectedCategory = (categorySelect ? categorySelect.value : "").toLowerCase().trim();

        const items = productGrid.querySelectorAll(".product-item-col");
        let visibleCount = 0;

        items.forEach(item => {
            const brand = (item.getAttribute("data-brand") || "").toLowerCase();
            const generic = (item.getAttribute("data-generic") || "").toLowerCase();
            const category = (item.getAttribute("data-category") || "").toLowerCase();

            const matchesQuery = !query || brand.includes(query) || generic.includes(query);
            const matchesCategory = !selectedCategory || selectedCategory === "all" || category === selectedCategory;

            if (matchesQuery && matchesCategory) {
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

        if (searchInput) {
            searchInput.addEventListener("input", filterCatalog);
        }

        if (categorySelect) {
            categorySelect.addEventListener("change", filterCatalog);
        }

        if (categorySidebar) {
            categorySidebar.addEventListener("click", event => {
                const link = event.target.closest("[data-category]");
                if (!link) return;

                const categoryValue = link.getAttribute("data-category");
                if (categorySelect) {
                    categorySelect.value = categoryValue === "All" ? "" : categoryValue;
                    filterCatalog();
                }
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