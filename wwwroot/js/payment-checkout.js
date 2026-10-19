(() => {
    "use strict";

    const form = document.getElementById("payment-form");
    if (!form) return;

    // ---------- Config (read from data-* attributes; no secrets here) ----------
    const orderId = form.dataset.orderId;
    const publishableKey = form.dataset.publishableKey;
    const currency = form.dataset.currency || "৳";
    const unitPrice = parseFloat(form.dataset.unitPrice) || 0;
    const deliveryFeeBase = parseFloat(form.dataset.deliveryFee) || 0;
    const singleItem = form.dataset.singleItem === "true";
    const MAX_QTY = 20;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const els = {
        message: $("payment-message"),
        payBtn: $("pay-button"),
        payText: $("pay-button-text"),
        paySpinner: $("pay-spinner"),
        payIcon: $("pay-icon"),
        cardSection: $("card-section"),
        paymentMount: $("payment-element"),
        details: $("delivery-details"),
        address: $("address"),
        phone: $("phone"),
        instructions: $("instructions"),
        qtyValue: $("qty-value"),
        qtyMinus: $("qty-minus"),
        qtyPlus: $("qty-plus"),
        lineSubtotal: $("line-subtotal"),
        lineDelivery: $("line-delivery"),
        lineTotal: $("line-total"),
        etaText: $("eta-text")
    };
    const csrfToken = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    // ---------- State ----------
    const state = {
        quantity: parseInt(form.dataset.quantity, 10) || 1,
        fulfillment: "delivery",   // delivery | pickup
        method: "card",            // cod | bkash | card
        stripe: null,
        elements: null,
        paymentElement: null,
        ready: false,              // Payment Element finished loading
        submitting: false,
        initToken: 0               // guards against out-of-order async init
    };

    // ---------- Helpers ----------
    const money = (n) => `${currency} ${n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    const debounce = (fn, ms) => { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; };
    const deliveryFee = () => (state.fulfillment === "pickup" ? 0 : deliveryFeeBase);

    function showMessage(text) { els.message.textContent = text; els.message.hidden = false; }
    function clearMessage() { els.message.textContent = ""; els.message.hidden = true; }

    function fieldError(name, text) {
        const box = form.querySelector(`[data-error-for="${name}"]`);
        const input = $(name);
        if (box) { box.textContent = text || ""; box.hidden = !text; }
        if (input) input.classList.toggle("invalid", !!text);
    }

    /** Single place that decides the button state, so it can never get stuck. */
    function render() {
        const needsStripe = state.method === "card";
        els.cardSection.hidden = !needsStripe;
        els.paySpinner.hidden = !state.submitting;
        if (els.payIcon) els.payIcon.hidden = state.submitting;

        const canPay = !state.submitting && (!needsStripe || state.ready);
        els.payBtn.disabled = !canPay;
        els.payBtn.setAttribute("aria-busy", String(state.submitting));

        if (state.submitting) els.payText.textContent = "Processing payment...";
        else if (needsStripe && !state.ready) els.payText.textContent = "Preparing secure payment...";
        else els.payText.textContent = "Confirm & Pay";
    }

    function renderTotals() {
        const subtotal = unitPrice * state.quantity;
        const fee = deliveryFee();
        if (els.qtyValue) els.qtyValue.textContent = state.quantity;
        if (els.qtyMinus) els.qtyMinus.disabled = state.quantity <= 1 || state.submitting;
        if (els.qtyPlus) els.qtyPlus.disabled = state.quantity >= MAX_QTY || state.submitting;
        if (singleItem) els.lineSubtotal.textContent = money(subtotal);
        els.lineDelivery.textContent = money(fee);
        if (singleItem) els.lineTotal.textContent = money(subtotal + fee);
        els.etaText.textContent = state.fulfillment === "pickup" ? "ready for pickup in 30-45 minutes" : "within 1-2 hours";
    }

    // ---------- Validation (delivery fields only; card data never touched) ----------
    function validateDelivery() {
        fieldError("address", ""); fieldError("phone", "");
        if (state.fulfillment !== "delivery") return true;

        let ok = true;
        if (!els.address.value.trim() || els.address.value.trim().length < 8) {
            fieldError("address", "Please enter your full delivery address."); ok = false;
        }
        if (!/^(?:\+?88)?01[3-9]\d{8}$/.test(els.phone.value.replace(/[\s-]/g, ""))) {
            fieldError("phone", "Enter a valid Bangladeshi mobile number."); ok = false;
        }
        if (!ok) (form.querySelector(".invalid") || els.address).focus();
        return ok;
    }

    function orderPayload() {
        return {
            orderId,
            quantity: state.quantity,
            fulfillmentMethod: state.fulfillment,
            deliveryAddress: state.fulfillment === "delivery" ? els.address.value.trim() : null,
            phone: els.phone.value.trim(),
            deliveryInstructions: els.instructions.value.trim() || null
        };
    }

    // ---------- Stripe: create intent + mount ----------
    function unmountPayment() {
        try { state.paymentElement?.destroy(); } catch { /* already gone */ }
        state.paymentElement = null; state.elements = null; state.ready = false;
    }

    async function initializePayment() {
        if (state.method !== "card") return;
        const token = ++state.initToken;

        unmountPayment();
        clearMessage();
        render();

        try {
            if (!orderId) throw new Error("The order ID is missing.");
            if (!publishableKey || !window.Stripe)
                throw new Error("Online payment is not configured yet. Please contact support.");

            // Intent creation only prepares the payment; it is NOT a completed charge.
            const response = await fetch("/Payment/CreateIntent", {
                method: "POST",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": csrfToken },
                credentials: "same-origin",
                body: JSON.stringify(orderPayload())
            });
            const data = await response.json().catch(() => ({}));
            if (token !== state.initToken) return; // a newer init superseded this one

            if (!response.ok) throw new Error(data.message || data.error || "Unable to start the payment session. Please try again.");
            if (!data.clientSecret) throw new Error("The server did not return a valid client secret.");

            state.stripe ??= window.Stripe(publishableKey);
            state.elements = state.stripe.elements({
                clientSecret: data.clientSecret,
                appearance: {
                    theme: "stripe",
                    variables: { colorPrimary: "#0b7a6b", colorText: "#14323a", borderRadius: "8px", fontFamily: "inherit" }
                }
            });

            els.paymentMount.innerHTML = "";
            state.paymentElement = state.elements.create("payment", { layout: "tabs" });
            state.paymentElement.mount("#payment-element");

            state.paymentElement.on("ready", () => { if (token === state.initToken) { state.ready = true; render(); } });
            state.paymentElement.on("loaderror", () => {
                if (token !== state.initToken) return;
                state.ready = false; render();
                showMessage("The secure payment form could not load. Please refresh the page or try again later.");
            });
        } catch (err) {
            if (token !== state.initToken) return;
            console.error("Checkout initialization error:", err);
            state.ready = false; render();
            showMessage(err.message || "Could not prepare payment. Please try again later.");
        }
    }
    const reinitDebounced = debounce(initializePayment, 450);

    // ---------- UI wiring ----------
    function selectOption(group, value) {
        form.querySelectorAll(`.mg-option[data-group="${group}"]`).forEach((label) => {
            const input = label.querySelector("input");
            const on = input.value === value;
            input.checked = on;
            label.classList.toggle("selected", on);
        });
    }

    form.addEventListener("change", (e) => {
        const input = e.target;
        if (!(input instanceof HTMLInputElement) || input.type !== "radio") return;
        selectOption(input.name === "fulfillment" ? "fulfillment" : "method", input.value);

        if (input.name === "fulfillment") {
            state.fulfillment = input.value;
            els.details.hidden = state.fulfillment === "pickup";
            renderTotals();
            if (state.method === "card") { state.ready = false; render(); reinitDebounced(); } // amount changed -> new intent
        } else if (input.name === "method") {
            state.method = input.value;
            clearMessage();
            render();
            if (state.method === "card" && !state.paymentElement) initializePayment();
        }
    });

    function changeQty(delta) {
        const next = Math.min(MAX_QTY, Math.max(1, state.quantity + delta));
        if (next === state.quantity || state.submitting) return;
        state.quantity = next;
        renderTotals();
        if (state.method === "card") { state.ready = false; render(); reinitDebounced(); } // keep intent in sync with total
    }
    els.qtyMinus?.addEventListener("click", () => changeQty(-1));
    els.qtyPlus?.addEventListener("click", () => changeQty(1));

    [els.address, els.phone].forEach((el) => el.addEventListener("input", () => fieldError(el.id, "")));

    // ---------- Submit lifecycle ----------
    function setSubmitting(value) { state.submitting = value; render(); renderTotals(); }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (state.submitting) return; // duplicate-charge guard
        clearMessage();

        if (!validateDelivery()) return;

        if (state.method !== "card") {
            showMessage("This payment method is not available for online checkout yet. Please choose Card.");
            return;
        }
        if (!state.stripe || !state.elements || !state.ready) return;

        setSubmitting(true);
        try {
            const confirmationUrl = `${window.location.origin}/Checkout/Confirmation?orderId=${encodeURIComponent(orderId)}`;

            const { error, paymentIntent } = await state.stripe.confirmPayment({
                elements: state.elements,
                confirmParams: {
                    return_url: confirmationUrl,
                    payment_method_data: { billing_details: { phone: els.phone.value.trim() || undefined } }
                },
                redirect: "if_required"
            });

            if (error) {
                // Decline / validation error: show inline, unlock for another attempt, no page refresh.
                showMessage(error.message || "Payment failed. Please check your card details and try again.");
                setSubmitting(false);
                return;
            }

            if (paymentIntent && ["succeeded", "processing", "requires_capture"].includes(paymentIntent.status)) {
                window.location.assign(confirmationUrl);
                return; // keep button locked while navigating
            }

            showMessage("The payment was not completed. Please try again.");
            setSubmitting(false);
        } catch (err) {
            console.error("Payment authorization error:", err);
            showMessage("We couldn't confirm the payment. Check your order status before trying again.");
            setSubmitting(false);
        }
    });

    // ---------- Boot ----------
    renderTotals();
    render();
    initializePayment();
})();
