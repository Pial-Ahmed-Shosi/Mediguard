document.addEventListener('DOMContentLoaded', function () {

    // -------------------------------------------------------------------------
    // 1. Quick Category Modal Handler
    // -------------------------------------------------------------------------
    const categoryForm = document.getElementById('quickCategoryForm');
    if (categoryForm) {
        categoryForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const errorAlert = document.getElementById('categoryModalError');
            const submitBtn = document.getElementById('saveCategoryBtn');
            const spinner = submitBtn ? submitBtn.querySelector('.spinner-border') : null;
            const nameInput = document.getElementById('categoryModalName');
            const descInput = document.getElementById('categoryModalDescription');

            clearError(errorAlert);

            const nameValue = nameInput ? nameInput.value.trim() : '';
            if (!nameValue) {
                showError(errorAlert, 'Category name is required.');
                return;
            }

            const payload = {
                Name: nameValue,
                Description: descInput ? descInput.value.trim() : ''
            };

            setLoading(submitBtn, spinner, true);

            try {
                const response = await fetch('/Category/CreateQuick', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(payload)
                });

                const data = await response.json();

                if (response.ok && data.id && data.name) {
                    injectOptionAndSelect('#CategoryId, select[name="CategoryId"]', data.id, data.name);
                    categoryForm.reset();
                    hideModal('categoryModal');
                } else {
                    showError(errorAlert, data.message || 'Failed to create category.');
                }
            } catch (err) {
                showError(errorAlert, 'A network error occurred. Please try again.');
            } finally {
                setLoading(submitBtn, spinner, false);
            }
        });
    }

    // -------------------------------------------------------------------------
    // 2. Quick Manufacturer Modal Handler
    // -------------------------------------------------------------------------
    const manufacturerForm = document.getElementById('quickManufacturerForm');
    if (manufacturerForm) {
        manufacturerForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const errorAlert = document.getElementById('manufacturerModalError');
            const submitBtn = document.getElementById('saveManufacturerBtn');
            const spinner = submitBtn ? submitBtn.querySelector('.spinner-border') : null;

            const nameInput = document.getElementById('manufacturerModalName');
            const emailInput = document.getElementById('manufacturerModalEmail');
            const phoneInput = document.getElementById('manufacturerModalPhone');
            const addressInput = document.getElementById('manufacturerModalAddress');

            clearError(errorAlert);

            const nameValue = nameInput ? nameInput.value.trim() : '';
            if (!nameValue) {
                showError(errorAlert, 'Manufacturer name is required.');
                return;
            }

            const payload = {
                Name: nameValue,
                ContactEmail: emailInput ? emailInput.value.trim() : '',
                Phone: phoneInput ? phoneInput.value.trim() : '',
                Address: addressInput ? addressInput.value.trim() : ''
            };

            setLoading(submitBtn, spinner, true);

            try {
                const response = await fetch('/Manufacturer/CreateQuick', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(payload)
                });

                const data = await response.json();

                if (response.ok && data.id && data.name) {
                    injectOptionAndSelect('#ManufacturerId, select[name="ManufacturerId"]', data.id, data.name);
                    manufacturerForm.reset();
                    hideModal('manufacturerModal');
                } else {
                    showError(errorAlert, data.message || 'Failed to create manufacturer.');
                }
            } catch (err) {
                showError(errorAlert, 'A network error occurred. Please try again.');
            } finally {
                setLoading(submitBtn, spinner, false);
            }
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    function injectOptionAndSelect(selector, id, name) {
        const dropdowns = document.querySelectorAll(selector);
        dropdowns.forEach(selectEl => {
            const option = new Option(name, id, true, true);
            selectEl.add(option);
            selectEl.value = id;
            selectEl.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }

    function showError(element, message) {
        if (!element) return;
        element.textContent = message;
        element.classList.remove('d-none');
    }

    function clearError(element) {
        if (!element) return;
        element.textContent = '';
        element.classList.add('d-none');
    }

    function setLoading(button, spinner, isLoading) {
        if (!button) return;
        button.disabled = isLoading;
        if (spinner) {
            spinner.classList.toggle('d-none', !isLoading);
        }
    }

    function hideModal(modalId) {
        const modalEl = document.getElementById(modalId);
        if (modalEl) {
            const modalInstance = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modalInstance.hide();
        }
    }
});