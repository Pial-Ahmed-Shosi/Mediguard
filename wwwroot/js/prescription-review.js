document.addEventListener("DOMContentLoaded", function () {
    // 1. Viewer Transformation Elements & State
    const container = document.getElementById("document-viewer-container") || document.getElementById("viewer-container");
    const wrapper = document.getElementById("image-transform-wrapper") || document.getElementById("image-wrapper");
    const img = document.getElementById("prescription-image") || document.getElementById("prescription-img");

    const btnZoomIn = document.getElementById("btn-zoom-in");
    const btnZoomOut = document.getElementById("btn-zoom-out");
    const btnRotate = document.getElementById("btn-rotate");
    const btnReset = document.getElementById("btn-reset-view") || document.getElementById("btn-reset");
    const btnFullscreen = document.getElementById("btn-fullscreen");

    let zoomScale = 1.0;
    let rotationDegree = 0;

    function applyTransformations() {
        if (wrapper) {
            wrapper.style.transform = `scale(${zoomScale}) rotate(${rotationDegree}deg)`;
        }
    }

    if (btnZoomIn) {
        btnZoomIn.addEventListener("click", function () {
            zoomScale = Math.min(zoomScale + 0.25, 3.0);
            applyTransformations();
        });
    }

    if (btnZoomOut) {
        btnZoomOut.addEventListener("click", function () {
            zoomScale = Math.max(zoomScale - 0.25, 0.5);
            applyTransformations();
        });
    }

    if (btnRotate) {
        btnRotate.addEventListener("click", function () {
            rotationDegree = (rotationDegree + 90) % 360;
            applyTransformations();
        });
    }

    if (btnReset) {
        btnReset.addEventListener("click", function () {
            zoomScale = 1.0;
            rotationDegree = 0;
            applyTransformations();
        });
    }

    if (btnFullscreen && container) {
        btnFullscreen.addEventListener("click", function () {
            if (!document.fullscreenElement) {
                container.requestFullscreen().catch(err => {
                    console.error("Error attempting to enable full-screen mode:", err);
                });
            } else {
                document.exitFullscreen();
            }
        });
    }

    // 2. Quick Rejection Reason Selectors
    const quickReasonBtns = document.querySelectorAll(".btn-quick-reason, .preset-reason-btn");
    const rejectionReasonInput = document.getElementById("rejectionReason") || document.getElementById("rejection-reason-text");
    const rejectionError = document.getElementById("rejection-reason-error") || document.getElementById("modal-validation-error");

    quickReasonBtns.forEach(btn => {
        btn.addEventListener("click", function () {
            if (rejectionReasonInput) {
                rejectionReasonInput.value = this.textContent.trim();
                if (rejectionError) rejectionError.classList.add("d-none");
            }
        });
    });

    // 3. Status Alert & Badge Elements
    const statusAlert = document.getElementById("status-message-alert") || document.getElementById("verification-status-alert");
    const orderStatusBadge = document.getElementById("order-status-badge");

    function showStatusAlert(type, htmlMessage) {
        if (statusAlert) {
            statusAlert.className = `alert alert-${type} mb-3`;
            statusAlert.innerHTML = htmlMessage;
            statusAlert.classList.remove("d-none");
        }
    }

    // 4. AJAX Approval Workflow
    const btnApprove = document.getElementById("btn-approve-prescription") || document.getElementById("btn-approve-rx");
    const btnReject = document.getElementById("btn-reject-prescription");

    if (btnApprove) {
        btnApprove.addEventListener("click", function () {
            const prescriptionId = this.getAttribute("data-prescription-id");
            if (!prescriptionId) return;

            btnApprove.disabled = true;
            if (btnReject) btnReject.disabled = true;

            fetch("/Prescription/Approve", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify({ prescriptionId: prescriptionId })
            })
            .then(response => {
                if (!response.ok) throw new Error("Approval processing failed.");
                return response.json();
            })
            .then(data => {
                showStatusAlert("success", "<i class='bi bi-check-circle me-1'></i>Prescription approved successfully! Linked order moved to <strong>PROCESSING</strong>.");
                if (orderStatusBadge) {
                    orderStatusBadge.className = "badge bg-success px-3 py-2 fs-6";
                    orderStatusBadge.textContent = "PROCESSING";
                }
            })
            .catch(err => {
                showStatusAlert("danger", "<i class='bi bi-exclamation-triangle me-1'></i>" + err.message);
                btnApprove.disabled = false;
                if (btnReject) btnReject.disabled = false;
            });
        });
    }

    // 5. AJAX Rejection Workflow
    const btnConfirmReject = document.getElementById("btn-confirm-reject");

    if (btnConfirmReject) {
        btnConfirmReject.addEventListener("click", function () {
            const prescriptionId = this.getAttribute("data-prescription-id") || (btnApprove ? btnApprove.getAttribute("data-prescription-id") : null);
            const reason = rejectionReasonInput ? rejectionReasonInput.value.trim() : "";

            if (!reason) {
                if (rejectionError) rejectionError.classList.remove("d-none");
                return;
            }

            if (rejectionError) rejectionError.classList.add("d-none");

            btnConfirmReject.disabled = true;

            fetch("/Prescription/Reject", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify({
                    prescriptionId: prescriptionId,
                    reason: reason
                })
            })
            .then(response => {
                if (!response.ok) throw new Error("Rejection processing failed.");
                return response.json();
            })
            .then(data => {
                // Dismiss modal
                const modalEl = document.getElementById("rejectionModal");
                if (modalEl && typeof bootstrap !== "undefined" && bootstrap.Modal) {
                    const modalInstance = bootstrap.Modal.getInstance(modalEl);
                    if (modalInstance) modalInstance.hide();
                }

                if (btnApprove) btnApprove.disabled = true;
                if (btnReject) btnReject.disabled = true;

                showStatusAlert("danger", "<i class='bi bi-x-circle me-1'></i>Prescription rejected. Linked order status updated to <strong>CANCELLED</strong>.");
                if (orderStatusBadge) {
                    orderStatusBadge.className = "badge bg-danger px-3 py-2 fs-6";
                    orderStatusBadge.textContent = "CANCELLED";
                }
            })
            .catch(err => {
                alert("Error rejecting prescription: " + err.message);
                btnConfirmReject.disabled = false;
            });
        });
    }
});