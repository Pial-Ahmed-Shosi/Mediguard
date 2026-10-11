/**
 * MediGuard - Medi+ B2B Network Configuration
 * Front-end controller for Ticket 7
 */

document.addEventListener('DOMContentLoaded', function () {
    // -------------------------------------------------------------------------
    // 1. Toast Notification Utility
    // -------------------------------------------------------------------------
    const toastEl = document.getElementById('liveToast');
    const toastBs = toastEl ? new bootstrap.Toast(toastEl, { delay: 4000 }) : null;
    const toastText = document.getElementById('toastText');
    const toastIcon = document.getElementById('toastIcon');

    function showToast(message, isSuccess = true) {
        if (!toastEl || !toastBs) return;

        toastEl.className = 'toast align-items-center text-white border-0 shadow-lg';
        if (isSuccess) {
            toastEl.classList.add('bg-success');
            if (toastIcon) toastIcon.className = 'bi bi-check-circle-fill fs-5';
        } else {
            toastEl.classList.add('bg-danger');
            if (toastIcon) toastIcon.className = 'bi bi-exclamation-triangle-fill fs-5';
        }

        if (toastText) toastText.textContent = message;
        toastBs.show();
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // -------------------------------------------------------------------------
    // 2. AJAX Configuration Save
    // -------------------------------------------------------------------------
    const btnSaveConfig = document.getElementById('btnSaveConfig');
    const saveSpinner = document.getElementById('saveSpinner');
    const saveIcon = document.getElementById('saveIcon');

    if (btnSaveConfig) {
        btnSaveConfig.addEventListener('click', async function () {
            const supplierToggle = document.getElementById('isSupplierConnectionEnabled');
            const minOrderInput = document.getElementById('minWholesaleOrderValue');
            const autoAcceptCheckbox = document.getElementById('autoAcceptB2BOrders');

            const payload = {
                IsSupplierConnectionEnabled: supplierToggle ? supplierToggle.checked : false,
                MinimumWholesaleOrderValue: minOrderInput ? parseFloat(minOrderInput.value) || 0 : 0,
                AutoAcceptB2BOrders: autoAcceptCheckbox ? autoAcceptCheckbox.checked : false
            };

            // Loading UI state
            btnSaveConfig.disabled = true;
            if (saveSpinner) saveSpinner.classList.remove('d-none');
            if (saveIcon) saveIcon.classList.add('d-none');

            try {
                const response = await fetch('/Admin/SaveMediPlusConfig', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(payload)
                });

                const data = await response.json();

                if (response.ok && data.success) {
                    showToast(data.message || 'B2B configuration saved successfully!', true);
                } else {
                    showToast(data.message || 'Failed to save configuration.', false);
                }
            } catch (err) {
                console.error('Save configuration error:', err);
                showToast('A network error occurred while saving configuration.', false);
            } finally {
                btnSaveConfig.disabled = false;
                if (saveSpinner) saveSpinner.classList.add('d-none');
                if (saveIcon) saveIcon.classList.remove('d-none');
            }
        });
    }

    // -------------------------------------------------------------------------
    // 3. Subscription Upgrade Flow ($49/mo)
    // -------------------------------------------------------------------------
    const btnConfirmSubscribe = document.getElementById('btnConfirmSubscribe');
    const subscribeSpinner = document.getElementById('subscribeSpinner');
    const upgradeModalEl = document.getElementById('upgradeModal');

    if (btnConfirmSubscribe) {
        btnConfirmSubscribe.addEventListener('click', async function () {
            btnConfirmSubscribe.disabled = true;
            if (subscribeSpinner) subscribeSpinner.classList.remove('d-none');

            try {
                const response = await fetch('/Admin/SubscribeMediPlus', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    }
                });

                const data = await response.json();

                if (response.ok && data.success) {
                    showToast(data.message, true);

                    // Close modal
                    if (upgradeModalEl) {
                        const modalInstance = bootstrap.Modal.getInstance(upgradeModalEl);
                        if (modalInstance) modalInstance.hide();
                    }

                    // Dynamically unlock UI without full reload
                    unlockMediPlusUI(data.nextBillingDate);
                } else {
                    showToast(data.message || 'Failed to complete subscription.', false);
                }
            } catch (err) {
                console.error('Subscription error:', err);
                showToast('Network error during checkout.', false);
            } finally {
                btnConfirmSubscribe.disabled = false;
                if (subscribeSpinner) subscribeSpinner.classList.add('d-none');
            }
        });
    }

    function unlockMediPlusUI(nextBillingDate) {
        // Hide lock overlays
        const lockOverlay = document.getElementById('lockOverlay');
        const apiKeyLockOverlay = document.getElementById('apiKeyLockOverlay');
        if (lockOverlay) lockOverlay.classList.add('d-none');
        if (apiKeyLockOverlay) apiKeyLockOverlay.classList.add('d-none');

        // Enable form fields
        const supplierToggle = document.getElementById('isSupplierConnectionEnabled');
        const minOrderInput = document.getElementById('minWholesaleOrderValue');
        const autoAcceptCheckbox = document.getElementById('autoAcceptB2BOrders');
        const btnSave = document.getElementById('btnSaveConfig');
        const btnGenKey = document.getElementById('btnGenerateApiKey');

        if (supplierToggle) supplierToggle.disabled = false;
        if (minOrderInput) minOrderInput.disabled = false;
        if (autoAcceptCheckbox) autoAcceptCheckbox.disabled = false;
        if (btnSave) btnSave.disabled = false;
        if (btnGenKey) btnGenKey.disabled = false;

        // Update Tier & Status Badges
        const tierBadge = document.getElementById('tierBadge');
        const tierBadgeText = document.getElementById('tierBadgeText');
        if (tierBadge && tierBadgeText) {
            tierBadge.className = 'tier-badge tier-badge-active';
            tierBadgeText.textContent = 'Medi+ B2B Tier';
        }

        const statusBadge = document.getElementById('statusBadge');
        const statusBadgeText = document.getElementById('statusBadgeText');
        if (statusBadge && statusBadgeText) {
            statusBadge.className = 'badge bg-success px-3 py-2 rounded-pill fw-semibold';
            statusBadgeText.textContent = 'Active';
        }

        // Update billing date
        const billingDisplay = document.getElementById('nextBillingDateDisplay');
        if (billingDisplay && nextBillingDate) {
            billingDisplay.textContent = nextBillingDate;
        }

        // Replace CTA with Active indicator
        const ctaContainer = document.getElementById('subscriptionCtaContainer');
        if (ctaContainer) {
            ctaContainer.innerHTML = `
                <div class="p-3 rounded-4 bg-white bg-opacity-10 backdrop-blur d-inline-block text-start text-lg-end">
                    <div class="badge bg-success bg-opacity-75 fs-6 px-3 py-2 mb-2">
                        <i class="bi bi-patch-check-fill me-1"></i> Subscription Active
                    </div>
                    <div class="text-white-50 small">
                        Renews on ${nextBillingDate || 'Next Month'}
                    </div>
                </div>
            `;
        }
    }

    // -------------------------------------------------------------------------
    // 4. B2B API Key Generation & Secret Display Box
    // -------------------------------------------------------------------------
    const btnGenerateApiKey = document.getElementById('btnGenerateApiKey');
    const displayApiKey = document.getElementById('displayApiKey');
    const displayApiSecret = document.getElementById('displayApiSecret');
    const btnToggleSecret = document.getElementById('btnToggleSecret');
    const btnCopyApiKey = document.getElementById('btnCopyApiKey');
    const btnCopyApiSecret = document.getElementById('btnCopyApiSecret');

    let currentSecretValue = '';

    if (btnGenerateApiKey) {
        btnGenerateApiKey.addEventListener('click', async function () {
            btnGenerateApiKey.disabled = true;
            btnGenerateApiKey.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Generating...';

            try {
                const response = await fetch('/Admin/GenerateApiKey', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    }
                });

                const data = await response.json();

                if (response.ok && data.success) {
                    if (displayApiKey) displayApiKey.value = data.apiKey;
                    if (displayApiSecret) {
                        currentSecretValue = data.apiSecret;
                        displayApiSecret.value = currentSecretValue;
                        displayApiSecret.type = 'text';
                    }
                    if (btnToggleSecret) {
                        btnToggleSecret.innerHTML = '<i class="bi bi-eye-slash"></i>';
                    }
                    showToast('New B2B API Key generated successfully.', true);
                } else {
                    showToast(data.message || 'Could not generate API key.', false);
                }
            } catch (err) {
                console.error('API key generation error:', err);
                showToast('Failed to generate key.', false);
            } finally {
                btnGenerateApiKey.disabled = false;
                btnGenerateApiKey.innerHTML = '<i class="bi bi-arrow-repeat me-1"></i> Generate New Key';
            }
        });
    }

    // Secret show/hide toggle
    if (btnToggleSecret) {
        btnToggleSecret.addEventListener('click', function () {
            if (!displayApiSecret) return;
            if (displayApiSecret.type === 'password') {
                displayApiSecret.type = 'text';
                btnToggleSecret.innerHTML = '<i class="bi bi-eye-slash"></i>';
            } else {
                displayApiSecret.type = 'password';
                btnToggleSecret.innerHTML = '<i class="bi bi-eye"></i>';
            }
        });
    }

    // Copy to clipboard helpers
    if (btnCopyApiKey && displayApiKey) {
        btnCopyApiKey.addEventListener('click', function () {
            copyToClipboard(displayApiKey.value, 'B2B API Key copied to clipboard!');
        });
    }

    if (btnCopyApiSecret && displayApiSecret) {
        btnCopyApiSecret.addEventListener('click', function () {
            const val = currentSecretValue || displayApiSecret.value;
            copyToClipboard(val, 'B2B API Secret copied to clipboard!');
        });
    }

    function copyToClipboard(text, message) {
        if (!text || text.includes('••••') || text.includes('未生成')) return;
        navigator.clipboard.writeText(text).then(() => {
            showToast(message, true);
        }).catch(() => {
            showToast('Unable to copy to clipboard.', false);
        });
    }

    // -------------------------------------------------------------------------
    // 5. Partner Pharmacy Search & Filter Bar
    // -------------------------------------------------------------------------
    const searchInput = document.getElementById('partnerSearchInput');
    const btnClearSearch = document.getElementById('btnClearSearch');
    const partnerCards = document.querySelectorAll('.partner-card');
    const partnerCountEl = document.getElementById('partnerCount');
    const filterAllBtn = document.getElementById('filterAllPartners');
    const filterReachableBtn = document.getElementById('filterReachablePartners');

    let currentFilterMode = 'all'; // 'all' or 'reachable'

    function filterPartners() {
        const query = searchInput ? searchInput.value.trim().toLowerCase() : '';
        let visibleCount = 0;

        partnerCards.forEach(card => {
            const name = card.getAttribute('data-name') || '';
            const city = card.getAttribute('data-city') || '';
            const license = card.getAttribute('data-license') || '';
            const isActive = card.getAttribute('data-active') === 'true';

            const matchesQuery = !query || name.includes(query) || city.includes(query) || license.includes(query);
            const matchesTab = currentFilterMode === 'all' || (currentFilterMode === 'reachable' && isActive);

            if (matchesQuery && matchesTab) {
                card.classList.remove('d-none');
                visibleCount++;
            } else {
                card.classList.add('d-none');
            }
        });

        if (partnerCountEl) partnerCountEl.textContent = visibleCount;
    }

    if (searchInput) {
        searchInput.addEventListener('input', filterPartners);
    }

    if (btnClearSearch && searchInput) {
        btnClearSearch.addEventListener('click', function () {
            searchInput.value = '';
            filterPartners();
            searchInput.focus();
        });
    }

    if (filterAllBtn && filterReachableBtn) {
        filterAllBtn.addEventListener('click', function () {
            currentFilterMode = 'all';
            filterAllBtn.className = 'btn btn-sm btn-teal-solid rounded-pill active';
            filterReachableBtn.className = 'btn btn-sm btn-outline-secondary rounded-pill';
            filterPartners();
        });

        filterReachableBtn.addEventListener('click', function () {
            currentFilterMode = 'reachable';
            filterReachableBtn.className = 'btn btn-sm btn-teal-solid rounded-pill active';
            filterAllBtn.className = 'btn btn-sm btn-outline-secondary rounded-pill';
            filterPartners();
        });
    }
});
