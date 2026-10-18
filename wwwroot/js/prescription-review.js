document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const root = document.getElementById('reviewRoot');
    if (!root) return;

    const prescriptionId = root.dataset.prescriptionId;
    const approveUrl = root.dataset.approveUrl || '/Prescription/Approve';
    const rejectUrl = root.dataset.rejectUrl || '/Prescription/Reject';
    const isPdf = root.dataset.isPdf === 'true';

    // ---------- Element references ----------
    const container = document.getElementById('viewerContainer');
    const stage = document.getElementById('viewerStage');
    const doc = document.getElementById('rxDocument');
    const zoomLabel = document.getElementById('zoomLabel');
    const rotationLabel = document.getElementById('rotationLabel');
    const alertBox = document.getElementById('reviewAlert');

    const btnApprove = document.getElementById('btnApprove');
    const btnReject = document.getElementById('btnReject');
    const btnConfirmReject = document.getElementById('btnConfirmReject');
    const rejectReason = document.getElementById('rejectReason');
    const rejectPreset = document.getElementById('rejectPreset');
    const rejectModalEl = document.getElementById('rejectModal');
    const statusBadge = document.getElementById('rxStatusBadge');
    const rejectionBox = document.getElementById('rxRejectionBox');
    const rejectionText = document.getElementById('rxRejectionText');

    // ---------- Document viewer (presentation only; the file is never modified) ----------
    const MIN_ZOOM = 0.25;
    const MAX_ZOOM = 4;
    const ZOOM_STEP = 0.25;

    let zoom = 1;
    let rotation = 0; // cumulative degrees, so the animation never spins backwards
    let baseW = 0;
    let baseH = 0;

    function normalizedRotation() {
        return ((rotation % 360) + 360) % 360;
    }

    function updateLabels() {
        if (zoomLabel) zoomLabel.textContent = Math.round(zoom * 100) + '%';
        if (rotationLabel) rotationLabel.textContent = normalizedRotation() + '\u00B0';
    }

    function applyTransform() {
        updateLabels();
        if (!doc || !stage || !baseW || !baseH) return;

        const swapped = normalizedRotation() % 180 !== 0;
        stage.style.width = (swapped ? baseH : baseW) * zoom + 'px';
        stage.style.height = (swapped ? baseW : baseH) * zoom + 'px';
        doc.style.transform = 'translate(-50%, -50%) rotate(' + rotation + 'deg) scale(' + zoom + ')';
    }

    function computeBaseSize() {
        if (!doc || !container) return;

        const availableWidth = Math.max(200, container.clientWidth - 48);

        if (isPdf) {
            baseW = Math.min(760, availableWidth);
            baseH = Math.max(300, container.clientHeight - 48);
        } else if (doc.naturalWidth > 0) {
            baseW = Math.min(doc.naturalWidth, availableWidth);
            baseH = baseW * (doc.naturalHeight / doc.naturalWidth);
        } else {
            return;
        }

        doc.style.width = baseW + 'px';
        doc.style.height = baseH + 'px';
        applyTransform();
    }

    function showLoadError() {
        if (!stage) return;
        stage.style.width = '';
        stage.style.height = '';
        stage.innerHTML =
            '<div class="rx-load-error">' +
            '<i class="bi bi-exclamation-triangle display-5 d-block mb-2 text-danger"></i>' +
            'The prescription file could not be loaded.<br>' +
            '<small>Check that the file exists and that the storage bucket is accessible.</small>' +
            '</div>';
    }

    if (doc) {
        if (isPdf) {
            computeBaseSize();
        } else {
            if (doc.complete && doc.naturalWidth > 0) {
                computeBaseSize();
            } else if (doc.complete) {
                showLoadError();
            }
            doc.addEventListener('load', computeBaseSize);
            doc.addEventListener('error', showLoadError);
        }
    }

    const btnZoomIn = document.getElementById('btnZoomIn');
    const btnZoomOut = document.getElementById('btnZoomOut');
    const btnRotate = document.getElementById('btnRotate');
    const btnReset = document.getElementById('btnReset');
    const btnFullscreen = document.getElementById('btnFullscreen');

    if (btnZoomIn) {
        btnZoomIn.addEventListener('click', function () {
            zoom = Math.min(MAX_ZOOM, +(zoom + ZOOM_STEP).toFixed(2));
            applyTransform();
        });
    }

    if (btnZoomOut) {
        btnZoomOut.addEventListener('click', function () {
            zoom = Math.max(MIN_ZOOM, +(zoom - ZOOM_STEP).toFixed(2));
            applyTransform();
        });
    }

    if (btnRotate) {
        btnRotate.addEventListener('click', function () {
            rotation += 90;
            applyTransform();
        });
    }

    if (btnReset) {
        btnReset.addEventListener('click', function () {
            zoom = 1;
            rotation = 0;
            applyTransform();
            if (container) container.scrollTo(0, 0);
        });
    }

    if (btnFullscreen && container) {
        btnFullscreen.addEventListener('click', function () {
            if (!document.fullscreenElement) {
                if (container.requestFullscreen) {
                    container.requestFullscreen().catch(function () {
                        showAlert('danger', 'Unable to enter full-screen mode.');
                    });
                }
            } else {
                document.exitFullscreen();
            }
        });
    }

    document.addEventListener('fullscreenchange', computeBaseSize);
    updateLabels();

    // ---------- Helpers ----------
    function getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function showAlert(type, message) {
        if (!alertBox) return;
        alertBox.className = 'alert alert-' + type + ' shadow-sm';
        alertBox.textContent = message;
        alertBox.classList.remove('d-none');
        alertBox.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function setBusy(busy) {
        [btnApprove, btnReject, btnConfirmReject].forEach(function (b) {
            if (b) b.disabled = busy;
        });
    }

    function lockDecision(statusText, badgeClass) {
        if (btnApprove) btnApprove.disabled = true;
        if (btnReject) btnReject.disabled = true;
        if (btnConfirmReject) btnConfirmReject.disabled = true;
        if (statusBadge) {
            statusBadge.textContent = statusText;
            statusBadge.className = 'badge ' + badgeClass;
        }
    }

    function getRejectModal() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Modal || !rejectModalEl) return null;
        return bootstrap.Modal.getOrCreateInstance(rejectModalEl);
    }

    async function postJson(url, payload) {
        let response;
        try {
            response = await fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getToken(),
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(payload)
            });
        } catch (e) {
            throw new Error('Network error. Please check your connection and try again.');
        }

        // A redirect means the session expired or access was denied (login / access-denied page)
        if (response.redirected) {
            throw new Error('Your session has expired or you do not have permission. Please sign in again.');
        }

        const contentType = response.headers.get('content-type') || '';
        let data = null;
        if (contentType.indexOf('application/json') !== -1) {
            try { data = await response.json(); } catch (e) { data = null; }
        }

        if (!response.ok || (data && data.success === false)) {
            const message = (data && (data.message || data.title)) ||
                ('The request could not be completed (HTTP ' + response.status + ').');
            throw new Error(message);
        }

        // 200 without a JSON body is not a confirmed success
        if (!data) {
            throw new Error('The server returned an unexpected response. Please refresh and check the prescription status.');
        }

        return data;
    }

    // ---------- Approve ----------
    if (btnApprove) {
        btnApprove.addEventListener('click', async function () {
            if (!prescriptionId) return;
            if (!confirm('Approve this prescription and move the order to PROCESSING?')) return;

            setBusy(true);
            try {
                const data = await postJson(approveUrl, { prescriptionId: prescriptionId });
                lockDecision('APPROVED', 'bg-success');
                showAlert('success', (data.message || 'Prescription approved successfully.') +
                    ' The order is moving to PROCESSING.');
            } catch (err) {
                showAlert('danger', err.message);
                setBusy(false);
            }
        });
    }

    // ---------- Reject ----------
    if (btnReject) {
        btnReject.addEventListener('click', function () {
            if (rejectReason) {
                rejectReason.value = '';
                rejectReason.classList.remove('is-invalid');
            }
            if (rejectPreset) rejectPreset.value = '';

            const modal = getRejectModal();
            if (modal) {
                modal.show();
            } else {
                showAlert('danger', 'The rejection dialog could not be opened. Please refresh the page.');
            }
        });
    }

    if (rejectModalEl) {
        rejectModalEl.addEventListener('shown.bs.modal', function () {
            if (rejectReason) rejectReason.focus();
        });
    }

    if (rejectPreset) {
        rejectPreset.addEventListener('change', function () {
            if (rejectPreset.value && rejectReason) {
                rejectReason.value = rejectPreset.value;
                rejectReason.classList.remove('is-invalid');
            }
        });
    }

    if (rejectReason) {
        rejectReason.addEventListener('input', function () {
            if (rejectReason.value.trim()) rejectReason.classList.remove('is-invalid');
        });
    }

    if (btnConfirmReject) {
        btnConfirmReject.addEventListener('click', async function () {
            const reason = rejectReason ? rejectReason.value.trim() : '';

            // Mandatory reason: nothing is sent while it is empty
            if (!reason) {
                if (rejectReason) {
                    rejectReason.classList.add('is-invalid');
                    rejectReason.focus();
                }
                return;
            }

            setBusy(true);
            try {
                const data = await postJson(rejectUrl, { prescriptionId: prescriptionId, reason: reason });

                const modal = getRejectModal();
                if (modal) modal.hide();

                lockDecision('REJECTED', 'bg-danger');

                if (rejectionBox && rejectionText) {
                    rejectionText.textContent = reason;
                    rejectionBox.classList.remove('d-none');
                }

                showAlert('success', data.message || 'Prescription rejected successfully.');
            } catch (err) {
                // Keep the modal open so the pharmacist can retry
                showAlert('danger', err.message);
                setBusy(false);
            }
        });
    }
});