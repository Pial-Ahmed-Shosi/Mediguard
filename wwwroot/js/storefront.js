"use strict";

(() => {
    const CART_KEY = "mediguard_storefront_cart";
    const PRESCRIPTION_MESSAGE =
        "Note: Uploading a valid prescription will be required at checkout for this item.";

    const notice = document.getElementById("mg-cart-notice");

    function readCart() {
        try {
            const saved = localStorage.getItem(CART_KEY);

            if (!saved) {
                return [];
            }

            const parsed = JSON.parse(saved);

            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed.filter(item =>
                item &&
                typeof item.productId === "string" &&
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
            return true;
        } catch {
            showNotice(
                "Your browser could not save the cart. Please enable site storage."
            );
            return false;
        }
    }

    function showNotice(message) {
        if (!notice) {
            window.alert(message);
            return;
        }

        notice.textContent = message;
        notice.hidden = false;
    }

    function addToCart(button) {
        const productId = button.dataset.productId;
        const name = button.dataset.brandName;
        const unitPrice = Number(button.dataset.unitPrice);
        const stock = Number(button.dataset.stock);
        const requiresPrescription =
            button.dataset.requiresPrescription === "true";

        if (!productId || !name ||
            !Number.isFinite(unitPrice) || unitPrice < 0 ||
            !Number.isInteger(stock) || stock <= 0) {
            showNotice("This product is currently unavailable.");
            return;
        }

        const cart = readCart();
        const existing = cart.find(item => item.productId === productId);
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

        if (requiresPrescription) {
            showNotice(PRESCRIPTION_MESSAGE);
        } else {
            showNotice(`${name} added to your cart.`);
        }
    }

    document.addEventListener("click", event => {
        const button = event.target.closest(".mg-add-to-cart");

        if (!button || button.disabled) {
            return;
        }

        addToCart(button);
    });
})();