
(() => {
    "use strict";

    const checkout = document.getElementById("paymentCheckout");
    const form = document.getElementById("payment-form");

    // This script is used only on the checkout page.
    if (!checkout || !form) return;

    const orderId = checkout.dataset.orderId;
    const publishableKey = checkout.dataset.publishableKey;

    const paymentElementContainer = document.getElementById("payment-element");
    const paymentMessage = document.getElementById("payment-message");
    const payButton = document.getElementById("pay-button");
    const payButtonText = document.getElementById("pay-button-text");
    const paySpinner = document.getElementById("pay-spinner");
    const csrfToken = form.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    let stripe = null;
    let elements = null;
    let paymentElement = null;
    let isSubmitting = false;

    function showMessage(message) {
        paymentMessage.textContent = message;
        paymentMessage.hidden = false;
    }

    function clearMessage() {
        paymentMessage.textContent = "";
        paymentMessage.hidden = true;
    }

    function setLoading(loading, text) {
        isSubmitting = loading;
        payButton.disabled = loading || !paymentElement;
        payButtonText.textContent = text;
        paySpinner.hidden = !loading;
    }

    async function initializePayment() {
        try {
            if (!orderId) {
                throw new Error("The order ID is missing.");
            }

            if (!publishableKey || !window.Stripe) {
                throw new Error(
                    "Online payment is not configured yet. Please contact support."
                );
            }

            setLoading(true, "Preparing secure payment...");

            // The server must calculate the amount from the database.
            // Never send a trusted payment amount from the browser.
            const response = await fetch("/Payment/CreateIntent", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": csrfToken ?? ""
                },
                credentials: "same-origin",
                body: JSON.stringify({ orderId: orderId })
            });

            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    data.error ||
                    "Unable to initialize payment. Please try again."
                );
            }

            if (!data.clientSecret) {
                throw new Error(
                    "The server did not return a payment client secret."
                );
            }

            stripe = window.Stripe(publishableKey);
            elements = stripe.elements({
                clientSecret: data.clientSecret
            });

            paymentElement = elements.create("payment");
            paymentElement.mount("#payment-element");

            paymentElement.on("ready", () => {
                setLoading(false, "Pay securely");
                clearMessage();
            });

            paymentElement.on("loaderror", () => {
                setLoading(true, "Payment form unavailable");
                showMessage(
                    "The secure payment form could not load. Please refresh or try again later."
                );
            });
        } catch (error) {
            console.error("Checkout initialization error:", error);
            showMessage(
                error.message ||
                "Could not prepare payment. Please try again later."
            );
            setLoading(true, "Payment unavailable");
        }
    }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (isSubmitting || !stripe || !elements || !paymentElement) {
            return;
        }

        clearMessage();
        setLoading(true, "Processing payment...");

        try {
            const result = await stripe.confirmPayment({
                elements,
                confirmParams: {
                    // Implement this server route before enabling live payments.
                    return_url:
                        window.location.origin +
                        "/Payment/Result?orderId=" +
                        encodeURIComponent(orderId)
                },
                redirect: "if_required"
            });

            if (result.error) {
                // For example: a declined card or invalid payment details.
                showMessage(
                    result.error.message ||
                    "Payment failed. Please check your details and try again."
                );

                setLoading(false, "Try payment again");
                return;
            }

            // Do not mark the order as paid from browser-side information.
            // The backend must verify the payment, preferably via webhook.
            showMessage(
                "Payment submitted. Please wait while the server confirms the payment status."
            );

            setLoading(true, "Confirming payment...");
        } catch (error) {
            console.error("Payment confirmation error:", error);

            showMessage(
                "We couldn't confirm the payment. Check your order status before trying again."
            );

            setLoading(false, "Try payment again");
        }
    });

    initializePayment();
})();
