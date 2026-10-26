(() => {
    "use strict";

    // ─── State ───────────────────────────────────────────────────────
    const allDeliveries = { pending: [], done: [], cancelled: [] };
    let pendingDeliveryToCancel = null;

    // ─── CSRF Token ──────────────────────────────────────────────────
    function getCsrf() {
        // Extracted from hidden input rendered by the view
        const match = document.body.innerHTML.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
        return match ? match[1] : "";
    }

    // ─── Toast notification ──────────────────────────────────────────
    function showToast(message, isSuccess = true) {
        const wrap = document.getElementById("dvToastWrap");
        if (!wrap) return;

        const el = document.createElement("div");
        el.className = "toast align-items-center text-white border-0 show mb-2";
        el.style.cssText = `background:${isSuccess ? "var(--dv-teal)" : "#dc3545"};
            border-radius:12px; padding:0.9rem 1.1rem; min-width:260px;
            box-shadow:0 4px 18px rgba(0,0,0,0.15); font-size:0.87rem; font-weight:600;`;
        el.setAttribute("role", "alert");
        el.innerHTML = `<i class="bi ${isSuccess ? "bi-check-circle-fill" : "bi-exclamation-circle-fill"} me-2"></i>${message}`;

        wrap.appendChild(el);
        setTimeout(() => el.remove(), 4000);
    }

    // ─── Tab Switching ───────────────────────────────────────────────
    function activateTab(tabKey) {
        document.querySelectorAll(".dv-tab-btn").forEach(btn => {
            const isActive = btn.dataset.tab === tabKey;
            btn.classList.toggle("active", isActive);
            btn.setAttribute("aria-selected", String(isActive));
        });

        document.querySelectorAll(".dv-panel").forEach(panel => {
            panel.classList.toggle("active", panel.id === `panel-${tabKey}`);
        });
    }

    document.querySelectorAll(".dv-tab-btn").forEach(btn => {
        btn.addEventListener("click", () => {
            activateTab(btn.dataset.tab);
        });
    });

    // ─── Render Cards ────────────────────────────────────────────────
    function renderList(tabKey, items) {
        const container = document.getElementById(`list-${tabKey}`);
        if (!container) return;

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="dv-empty">
                    <i class="bi bi-inbox"></i>
                    <p class="fw-semibold">No ${tabKey} deliveries</p>
                    <p class="small">Nothing to show here right now.</p>
                </div>`;
            return;
        }

        container.innerHTML = items.map(d => buildCardHtml(d)).join("");

        // Bind action buttons
        container.querySelectorAll(".js-mark-done").forEach(btn => {
            btn.addEventListener("click", () => handleMarkDone(btn.dataset.deliveryId));
        });

        container.querySelectorAll(".js-cancel-delivery").forEach(btn => {
            btn.addEventListener("click", () => {
                pendingDeliveryToCancel = btn.dataset.deliveryId;
            });
        });
    }

    function buildCardHtml(d) {
        const isBtB   = d.orderType === "B2B";
        const mapsUrl = `https://maps.google.com/?q=${encodeURIComponent(d.shippingAddress || "")}`;
        const orderId = (d.orderId || "").replace(/-/g, "").substring(0, 8).toUpperCase();

        const typeClass = isBtB ? "dv-type-b2b" : "dv-type-b2c";
        const typeLabel = isBtB ? "B2B Wholesale Delivery" : "B2C Customer Delivery";
        const recipient = isBtB ? (d.pharmacyName || d.recipientName) : d.recipientName;

        const phoneHtml = d.recipientPhone
            ? `<div class="dv-meta"><i class="bi bi-telephone-fill"></i>
                <a href="tel:${d.recipientPhone}">${d.recipientPhone}</a></div>` : "";

        const addressHtml = d.shippingAddress
            ? `<div class="dv-meta"><i class="bi bi-geo-alt-fill"></i>
                <a href="${mapsUrl}" target="_blank" rel="noopener noreferrer">${d.shippingAddress}</a></div>` : "";

        const itemsHtml = (d.items || []).map(i =>
            `<li><i class="bi bi-check2-square"></i> ${i.medicineName} &times; ${i.quantity}</li>`
        ).join("");

        const completionHtml = d.status === "DONE" && d.completedAt
            ? `<div class="dv-completion-info text-success">
                <i class="bi bi-check-circle-fill"></i>
                Delivered: ${new Date(d.completedAt).toLocaleString("en-GB", {dateStyle:"medium",timeStyle:"short"})}
               </div>` : "";

        const cancelHtml = d.status === "CANCELLED" && d.cancellationReason
            ? `<div class="dv-completion-info text-danger">
                <i class="bi bi-x-circle-fill"></i> Reason: ${d.cancellationReason}
               </div>` : "";

        const actionsHtml = d.status === "PENDING"
            ? `<div class="dv-card-actions">
                <button type="button" class="btn-dv-done js-mark-done" data-delivery-id="${d.deliveryId}">
                    <i class="bi bi-check2-circle"></i> Mark as Delivered
                </button>
                <button type="button" class="btn-dv-cancel js-cancel-delivery"
                        data-delivery-id="${d.deliveryId}"
                        data-bs-toggle="modal" data-bs-target="#cancelModal">
                    <i class="bi bi-x-circle"></i> Cancel
                </button>
               </div>` : "";

        return `
            <article class="dv-card" data-delivery-id="${d.deliveryId}" id="dv-card-${d.deliveryId}">
                <div class="dv-card-header">
                    <div class="d-flex align-items-center gap-2">
                        <span class="dv-order-type ${typeClass}">${typeLabel}</span>
                    </div>
                    <span class="dv-order-id">#${orderId}</span>
                </div>
                <div class="dv-card-body">
                    <div class="dv-recipient-name">
                        <i class="bi bi-person-fill me-1" style="color:var(--dv-teal)"></i>${recipient || "—"}
                    </div>
                    ${phoneHtml}
                    ${addressHtml}
                    <div class="dv-meta"><i class="bi bi-cash-coin"></i>
                        <span>৳ ${parseFloat(d.totalAmount || 0).toFixed(2)}</span>
                    </div>
                    ${itemsHtml ? `<ul class="dv-items-list">${itemsHtml}</ul>` : ""}
                    ${completionHtml}
                    ${cancelHtml}
                </div>
                ${actionsHtml}
            </article>`;
    }

    // ─── Update badge counts ─────────────────────────────────────────
    function updateBadges() {
        document.getElementById("badge-pending").textContent   = allDeliveries.pending.length;
        document.getElementById("badge-done").textContent      = allDeliveries.done.length;
        document.getElementById("badge-cancelled").textContent = allDeliveries.cancelled.length;
    }

    // ─── Load deliveries from API ─────────────────────────────────────
    async function loadDeliveries(statusFilter) {
        try {
            const resp = await fetch(`/Deliveryman/GetMyDeliveries?statusFilter=${statusFilter}`, {
                credentials: "same-origin"
            });
            const json = await resp.json();
            if (json.success && Array.isArray(json.data)) return json.data;
        } catch (e) {
            console.error("Failed to load deliveries:", e);
        }
        return [];
    }

    async function loadAll() {
        const [pending, done, cancelled] = await Promise.all([
            loadDeliveries("PENDING"),
            loadDeliveries("DONE"),
            loadDeliveries("CANCELLED")
        ]);

        allDeliveries.pending   = pending;
        allDeliveries.done      = done;
        allDeliveries.cancelled = cancelled;

        renderList("pending",   pending);
        renderList("done",      done);
        renderList("cancelled", cancelled);
        updateBadges();
    }

    // ─── Update Delivery Status via AJAX ─────────────────────────────
    async function updateStatus(deliveryId, newStatus, cancellationReason = null) {
        const payload = { deliveryId, newStatus, cancellationReason };

        const resp = await fetch("/Deliveryman/UpdateStatus", {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrf()
            },
            body: JSON.stringify(payload)
        });

        return resp.json().catch(() => ({ success: false, message: "Request failed." }));
    }

    // ─── Mark as Done ─────────────────────────────────────────────────
    async function handleMarkDone(deliveryId) {
        const card = document.getElementById(`dv-card-${deliveryId}`);
        const btns = card?.querySelectorAll(".btn-dv-done, .btn-dv-cancel");
        btns?.forEach(b => b.disabled = true);

        try {
            const result = await updateStatus(deliveryId, "DONE");
            if (result.success) {
                // Move card from pending to done
                const idx = allDeliveries.pending.findIndex(d => d.deliveryId == deliveryId);
                if (idx !== -1) {
                    const delivery = { ...allDeliveries.pending[idx], status: "DONE", completedAt: new Date().toISOString() };
                    allDeliveries.pending.splice(idx, 1);
                    allDeliveries.done.unshift(delivery);
                }
                renderList("pending",   allDeliveries.pending);
                renderList("done",      allDeliveries.done);
                updateBadges();
                showToast("Delivery marked as completed!");
            } else {
                showToast(result.message || "Failed to update status.", false);
                btns?.forEach(b => b.disabled = false);
            }
        } catch {
            showToast("A network error occurred.", false);
            btns?.forEach(b => b.disabled = false);
        }
    }

    // ─── Cancel Modal ─────────────────────────────────────────────────
    document.querySelectorAll(".dv-reason-chip").forEach(chip => {
        chip.addEventListener("click", () => {
            const inp = document.getElementById("cancelReasonInput");
            if (inp) inp.value = chip.dataset.reason ?? "";
        });
    });

    document.getElementById("btnConfirmCancel")?.addEventListener("click", async () => {
        const inp    = document.getElementById("cancelReasonInput");
        const errEl  = document.getElementById("cancelReasonError");
        const reason = inp?.value.trim() ?? "";

        if (!reason) {
            if (errEl) errEl.hidden = false;
            inp?.focus();
            return;
        }
        if (errEl) errEl.hidden = true;

        const confirmBtn = document.getElementById("btnConfirmCancel");
        confirmBtn.disabled = true;

        try {
            const deliveryId = pendingDeliveryToCancel;
            const result = await updateStatus(deliveryId, "CANCELLED", reason);

            // Close modal
            const modalEl = document.getElementById("cancelModal");
            const modal = modalEl
                ? bootstrap.Modal.getInstance(modalEl) ?? new bootstrap.Modal(modalEl)
                : null;
            modal?.hide();

            if (result.success) {
                const idx = allDeliveries.pending.findIndex(d => d.deliveryId == deliveryId);
                if (idx !== -1) {
                    const delivery = {
                        ...allDeliveries.pending[idx],
                        status: "CANCELLED",
                        cancellationReason: reason
                    };
                    allDeliveries.pending.splice(idx, 1);
                    allDeliveries.cancelled.unshift(delivery);
                }
                renderList("pending",   allDeliveries.pending);
                renderList("cancelled", allDeliveries.cancelled);
                updateBadges();
                showToast("Delivery cancelled.");
            } else {
                showToast(result.message || "Failed to cancel delivery.", false);
            }
        } catch {
            showToast("A network error occurred.", false);
        }

        confirmBtn.disabled = false;
        pendingDeliveryToCancel = null;
    });

    // Clear modal state on close
    document.getElementById("cancelModal")?.addEventListener("hidden.bs.modal", () => {
        const inp = document.getElementById("cancelReasonInput");
        if (inp) inp.value = "";
        const err = document.getElementById("cancelReasonError");
        if (err) err.hidden = true;
        const btn = document.getElementById("btnConfirmCancel");
        if (btn) btn.disabled = false;
        pendingDeliveryToCancel = null;
    });

    // ─── Boot ─────────────────────────────────────────────────────────
    loadAll();
})();
