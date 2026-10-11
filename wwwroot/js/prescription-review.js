document.addEventListener("DOMContentLoaded", function () {
    // Viewer elements
    const viewerContainer = document.getElementById("viewer-container");
    const imageWrapper = document.getElementById("image-wrapper");
    const img = document.getElementById("prescription-img");

    // Control buttons
    const btnZoomIn = document.getElementById("btn-zoom-in");
    const btnZoomOut = document.getElementById("btn-zoom-out");
    const btnRotate = document.getElementById("btn-rotate");
    const btnReset = document.getElementById("btn-reset");
    const btnFullscreen = document.getElementById("btn-fullscreen");

    // Action buttons & Modal elements
    const btnApprove = document.getElementById("btn-approve-rx");
    const btnConfirmReject = document.getElementById("btn-confirm-reject");
    const rejectionReasonInput = document.getElementById("rejection-reason-text");
    const modalValidationError = document.getElementById("modal-validation-error");
    const statusAlert = document.getElementById("verification-status-alert");
    const presetButtons = document.querySelectorAll(".preset-reason-btn");

    // Transform states
    let zoomLevel = 1.0;
    let rotationAngle = 0;

    // --- Interactive Image Viewer Controls ---
    if (imageWrapper && img) {
        function updateTransform() {
            imageWrapper.style.transform = `scale(${zoomLevel}) rotate(${rotationAngle}deg)`;
        }

        btnZoomIn?.addEventListener("click", function () {
            if (zoomLevel < 3.0) {
                zoomLevel += 0.25;
                updateTransform();
            }
        });

        btnZoomOut?.addEventListener("click", function () {
            if (zoomLevel > 0.5) {
                zoomLevel -= 0.25;
                updateTransform();
            }
        });

        btnRotate?.addEventListener("click", function () {
            rotationAngle = (rotationAngle + 90) % 360;
            updateTransform();
        });

        btnReset?.addEventListener("click", function () {
            zoomLevel = 1.0;
            rotationAngle = 0;
            updateTransform();
        });

        btnFullscreen?.addEventListener("click", function () {
            if (!document.fullscreenElement) {
                viewerContainer.requestFullscreen().catch(err => {
                    console.error("Error attempting to enable fullscreen mode:", err);
                });
            } else {
                document.exitFullscreen();
            }
        });
    }

    // --- Rejection Template Presets ---
    presetButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            rejectionReasonInput.value = this.textContent.trim();
            modalValidationError.classList.add("d-none");
        });
    });

    // --- AJAX Approval Processing ---
    btnApprove?.addEventListener("click", function () {
        const prescriptionId = this.getAttribute("data-prescription-id");
        if (!prescriptionId) return;

        if (!confirm("Are you sure you want to approve this prescription and advance the order to PROCESSING?")) {
            return;
        }

        btnApprove.disabled = true;

        fetch("/Prescription/Approve", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ prescriptionId: prescriptionId })
        })
        .then(response => response.json().then(data => ({ status: response.status, body: data })))
        .then(result => {
            if (result.status === 200) {
                showStatusAlert("Prescription approved successfully! Order advanced to PROCESSING.", "success");
                setTimeout(() => { window.location.href = "/Order/Index"; }, 1500);
            } else {
                showStatusAlert(result.body.message || "Failed to approve prescription.", "danger");
                btnApprove.disabled = false;
            }
        })
        .catch(err => {
            console.error("Error approving prescription:", err);
            showStatusAlert("An unexpected error occurred during approval.", "danger");
            btnApprove.disabled = false;
        });
    });

    // --- AJAX Rejection Processing ---
    btnConfirmReject?.addEventListener("click", function () {
        const prescriptionId = this.getAttribute("data-prescription-id");
        const reason = rejectionReasonInput.value.trim();

        if (!reason) {
            modalValidationError.classList.remove("d-none");
            return;
        }

        modalValidationError.classList.add("d-none");
        btnConfirmReject.disabled = true;

        fetch("/Prescription/Reject", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                prescriptionId: prescriptionId,
                reason: reason
            })
        })
        .then(response => response.json().then(data => ({ status: response.status, body: data })))
        .then(result => {
            if (result.status === 200) {
                // Hide bootstrap modal
                const modalEl = document.getElementById("rejectionModal");
                const modalInstance = bootstrap.Modal.getInstance(modalEl);
                modalInstance?.hide();

                showStatusAlert("Prescription rejected. Order has been CANCELLED and customer notified.", "warning");
                setTimeout(() => { window.location.href = "/Order/Index"; }, 1500);
            } else {
                alert(result.body.message || "Failed to reject prescription.");
                btnConfirmReject.disabled = false;
            }
        })
        .catch(err => {
            console.error("Error rejecting prescription:", err);
            alert("An unexpected error occurred during rejection.");
            btnConfirmReject.disabled = false;
        });
    });

    function showStatusAlert(message, type) {
        statusAlert.className = `alert alert-${type} mb-3`;
        statusAlert.textContent = message;
        statusAlert.classList.remove("d-none");
    }
});