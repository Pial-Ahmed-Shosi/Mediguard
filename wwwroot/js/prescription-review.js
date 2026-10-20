(() => {
    "use strict";

    // ─── Image Viewer Controls ────────────────────────────────────────
    const img         = document.getElementById("rxPrescriptionImg");
    const btnZoomIn   = document.getElementById("btnZoomIn");
    const btnZoomOut  = document.getElementById("btnZoomOut");
    const btnRotate   = document.getElementById("btnRotate");
    const btnFull     = document.getElementById("btnFullscreen");
    const viewerBody  = document.getElementById("rxViewerBody");

    let scale  = 1.0;
    let rotate = 0;

    function applyTransform() {
        if (!img) return;
        img.style.transform = `scale(${scale}) rotate(${rotate}deg)`;
    }

    btnZoomIn?.addEventListener("click", () => {
        scale = Math.min(scale + 0.25, 4);
        applyTransform();
    });

    btnZoomOut?.addEventListener("click", () => {
        scale = Math.max(scale - 0.25, 0.25);
        applyTransform();
    });

    btnRotate?.addEventListener("click", () => {
        rotate = (rotate + 90) % 360;
        applyTransform();
    });

    btnFull?.addEventListener("click", () => {
        if (!viewerBody) return;
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            viewerBody.requestFullscreen?.();
        }
    });

    // Reset on fullscreen exit
    document.addEventListener("fullscreenchange", () => {
        if (btnFull) {
            const icon = btnFull.querySelector("i");
            if (icon) {
                icon.className = document.fullscreenElement
                    ? "bi bi-fullscreen-exit"
                    : "bi bi-fullscreen";
            }
        }
    });

    // ─── CSRF Token Helper ────────────────────────────────────────────
    function getCsrfToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    }

    // ─── Result Banner ────────────────────────────────────────────────
    function showResult(message, isSuccess) {
        const box = document.getElementById("rxActionResult");
        if (!box) return;
        box.textContent = message;
        box.style.display = "block";
        box.style.background = isSuccess ? "#d1fae5" : "#fee2e2";
        box.style.color      = isSuccess ? "#065f46" : "#991b1b";
        box.style.border     = `1px solid ${isSuccess ? "#6ee7b7" : "#fca5a5"}`;
    }

    function lockActionBox() {
        document.getElementById("btnApprove")?.setAttribute("disabled", "");
        document.getElementById("btnReject")?.setAttribute("disabled", "");
    }

    // ─── Approve ──────────────────────────────────────────────────────
    const btnApprove = document.getElementById("btnApprove");
    btnApprove?.addEventListener("click", async () => {
        const prescriptionId = btnApprove.dataset.prescriptionId;
        if (!prescriptionId) return;

        lockActionBox();

        try {
            const resp = await fetch("/Prescription/Approve", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getCsrfToken()
                },
                credentials: "same-origin",
                body: JSON.stringify({ prescriptionId })
            });

            const data = await resp.json().catch(() => ({}));

            if (resp.ok) {
                showResult(
                    data.message || "Prescription approved. Order status advanced to PROCESSING.",
                    true
                );
                // Update header badge
                const pill = document.querySelector(".rx-status-pill");
                if (pill) {
                    pill.textContent = "APPROVED";
                    pill.className = "rx-status-pill bg-success text-white";
                }
            } else {
                showResult(data.message || "Failed to approve prescription.", false);
                // Re-enable on failure
                btnApprove?.removeAttribute("disabled");
                document.getElementById("btnReject")?.removeAttribute("disabled");
            }
        } catch (err) {
            console.error("Approve error:", err);
            showResult("A network error occurred. Please try again.", false);
            btnApprove?.removeAttribute("disabled");
            document.getElementById("btnReject")?.removeAttribute("disabled");
        }
    });

    // ─── Reject Modal ─────────────────────────────────────────────────
    // Quick-pick chips
    document.querySelectorAll(".rx-reason-chip").forEach(chip => {
        chip.addEventListener("click", () => {
            const input = document.getElementById("rejectReasonInput");
            if (input) input.value = chip.dataset.reason ?? "";
        });
    });

    // Confirm rejection
    const btnConfirmReject = document.getElementById("btnConfirmReject");
    btnConfirmReject?.addEventListener("click", async () => {
        const prescriptionId = btnConfirmReject.dataset.prescriptionId;
        const reasonInput    = document.getElementById("rejectReasonInput");
        const reasonError    = document.getElementById("rejectReasonError");
        const reason         = reasonInput?.value.trim() ?? "";

        if (!reason) {
            if (reasonError) reasonError.hidden = false;
            reasonInput?.focus();
            return;
        }

        if (reasonError) reasonError.hidden = true;
        btnConfirmReject.disabled = true;

        try {
            const resp = await fetch("/Prescription/Reject", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getCsrfToken()
                },
                credentials: "same-origin",
                body: JSON.stringify({ prescriptionId, reason })
            });

            const data = await resp.json().catch(() => ({}));

            // Close modal
            const modalEl = document.getElementById("rejectModal");
            const modalInstance = modalEl
                ? bootstrap.Modal.getInstance(modalEl) ?? new bootstrap.Modal(modalEl)
                : null;
            modalInstance?.hide();

            if (resp.ok) {
                lockActionBox();
                showResult(
                    data.message || "Prescription rejected. The customer has been notified.",
                    true
                );
                const pill = document.querySelector(".rx-status-pill");
                if (pill) {
                    pill.textContent = "REJECTED";
                    pill.className = "rx-status-pill bg-danger text-white";
                }
            } else {
                showResult(data.message || "Failed to reject prescription.", false);
                btnConfirmReject.disabled = false;
            }
        } catch (err) {
            console.error("Reject error:", err);
            showResult("A network error occurred. Please try again.", false);
            btnConfirmReject.disabled = false;
        }
    });

    // Clear reason input on modal close
    document.getElementById("rejectModal")?.addEventListener("hidden.bs.modal", () => {
        const inp = document.getElementById("rejectReasonInput");
        if (inp) inp.value = "";
        const err = document.getElementById("rejectReasonError");
        if (err) err.hidden = true;
        const btn = document.getElementById("btnConfirmReject");
        if (btn) btn.disabled = false;
    });
})();
