/**
 * POS Cart Real-Time Engine (Ticket 26)
 * Handles in-memory state, batch stock limits, live tax/discount calculations, and checkout validation.
 */

// Global In-Memory Cart State
window.posCartState = [];

document.addEventListener('DOMContentLoaded', function () {
    // DOM Elements Reference
    const tableBody = document.getElementById('posCartTableBody');
    const emptyPlaceholder = document.getElementById('emptyCartPlaceholder');
    const itemCountBadge = document.getElementById('cartItemCountBadge');
    
    // Calculation Display Labels
    const lblSubtotal = document.getElementById('lblSubtotal');
    const lblGrandTotal = document.getElementById('lblGrandTotal');
    const lblChangeDue = document.getElementById('lblChangeDue');
    
    // Calculation Inputs
    const inputTaxRate = document.getElementById('inputTaxRate');
    const inputOverallDiscount = document.getElementById('inputOverallDiscount');
    const inputAmountTendered = document.getElementById('inputAmountTendered');
    const selectPaymentMethod = document.getElementById('selectPaymentMethod');
    
    // Action Controls & Warnings
    const btnClearCart = document.getElementById('btnClearPosCart');
    const btnSubmitCheckout = document.getElementById('btnSubmitCheckout');
    const stockWarningContainer = document.getElementById('stockWarningContainer');
    const stockWarningText = document.getElementById('stockWarningText');
    const checkoutErrorContainer = document.getElementById('checkoutErrorContainer');
    const checkoutErrorText = document.getElementById('checkoutErrorText');

    // --- Core State Manipulation API ---

    /**
     * Adds an item or increments existing item quantity in cart.
     * @param {Object} item { id, medicineName, batchId, batchNumber, unitPrice, availableStock, isRx }
     */
    window.addToPosCart = function (item) {
        hideCheckoutError();
        
        const existingIndex = window.posCartState.findIndex(
            i => i.id === item.id && i.batchId === item.batchId
        );

        if (existingIndex > -1) {
            const targetItem = window.posCartState[existingIndex];
            const targetQty = targetItem.quantity + 1;

            if (targetQty > targetItem.availableStock) {
                showStockWarning(`Cannot add more. Available stock for batch ${targetItem.batchNumber} is ${targetItem.availableStock}.`);
            } else {
                hideStockWarning();
                targetItem.quantity = targetQty;
            }
        } else {
            if (item.availableStock <= 0) {
                showStockWarning(`Item ${item.medicineName} (Batch: ${item.batchNumber}) is out of stock!`);
                return;
            }

            hideStockWarning();
            window.posCartState.push({
                id: item.id,
                medicineName: item.medicineName,
                batchId: item.batchId || 'DEFAULT',
                batchNumber: item.batchNumber || 'N/A',
                unitPrice: parseFloat(item.unitPrice) || 0.00,
                availableStock: parseInt(item.availableStock) || 0,
                quantity: 1,
                itemDiscountPercent: 0.00
            });
        }

        renderCart();
    };

    /**
     * Updates item quantity directly from controls.
     */
    function updateQuantity(index, newQty) {
        hideCheckoutError();
        const item = window.posCartState[index];
        if (!item) return;

        const parsedQty = parseInt(newQty) || 1;

        if (parsedQty > item.availableStock) {
            showStockWarning(`Quantity for ${item.medicineName} capped at available stock (${item.availableStock}).`);
            item.quantity = item.availableStock;
        } else if (parsedQty < 1) {
            item.quantity = 1;
            hideStockWarning();
        } else {
            hideStockWarning();
            item.quantity = parsedQty;
        }

        renderCart();
    }

    /**
     * Updates per-item discount percentage.
     */
    function updateItemDiscount(index, discPercent) {
        const item = window.posCartState[index];
        if (!item) return;

        let parsedDisc = parseFloat(discPercent) || 0.00;
        if (parsedDisc < 0) parsedDisc = 0.00;
        if (parsedDisc > 100) parsedDisc = 100.00;

        item.itemDiscountPercent = parsedDisc;
        recalculateTotals();
        updateRowTotalLabel(index);
    }

    /**
     * Removes single item row from cart.
     */
    function removeItem(index) {
        window.posCartState.splice(index, 1);
        hideStockWarning();
        hideCheckoutError();
        renderCart();
    }

    /**
     * Clears all items in cart.
     */
    window.clearPosCart = function () {
        window.posCartState = [];
        inputAmountTendered.value = "0.00";
        inputOverallDiscount.value = "0.00";
        hideStockWarning();
        hideCheckoutError();
        renderCart();
    };

    // --- Calculation & Math Engine ---

    function recalculateTotals() {
        let subtotal = 0.00;

        // 1. Calculate items subtotal after item-level discounts
        window.posCartState.forEach(item => {
            const rowGross = item.unitPrice * item.quantity;
            const rowDiscount = rowGross * (item.itemDiscountPercent / 100.0);
            subtotal += (rowGross - rowDiscount);
        });

        // 2. Read inputs
        const taxRate = Math.max(0, parseFloat(inputTaxRate.value) || 0.00);
        const overallDiscount = Math.max(0, parseFloat(inputOverallDiscount.value) || 0.00);
        let amountTendered = Math.max(0, parseFloat(inputAmountTendered.value) || 0.00);

        // 3. Compute tax and grand total
        const taxAmount = subtotal * (taxRate / 100.0);
        let grandTotal = (subtotal + taxAmount) - overallDiscount;
        if (grandTotal < 0) grandTotal = 0.00;

        // Auto-sync tendered amount for Card / MFS digital payments
        if (selectPaymentMethod.value !== 'CASH') {
            amountTendered = grandTotal;
            inputAmountTendered.value = grandTotal.toFixed(2);
            inputAmountTendered.readOnly = true;
        } else {
            inputAmountTendered.readOnly = false;
        }

        // 4. Compute change due
        const changeDue = amountTendered - grandTotal;

        // 5. Update UI Labels
        lblSubtotal.innerText = `৳${subtotal.toFixed(2)}`;
        lblGrandTotal.innerText = `৳${grandTotal.toFixed(2)}`;
        lblChangeDue.innerText = `৳${(changeDue > 0 ? changeDue : 0.00).toFixed(2)}`;

        // Toggle Checkout Button State
        btnSubmitCheckout.disabled = window.posCartState.length === 0;

        return { subtotal, grandTotal, amountTendered, changeDue };
    }

    function updateRowTotalLabel(index) {
        const item = window.posCartState[index];
        if (!item) return;

        const rowTotalLbl = document.getElementById(`rowTotal_${index}`);
        if (rowTotalLbl) {
            const rowGross = item.unitPrice * item.quantity;
            const rowNet = rowGross - (rowGross * (item.itemDiscountPercent / 100.0));
            rowTotalLbl.innerText = `৳${rowNet.toFixed(2)}`;
        }
    }

    // --- DOM Rendering Engine ---

    function renderCart() {
        tableBody.innerHTML = '';

        if (window.posCartState.length === 0) {
            emptyPlaceholder.classList.remove('d-none');
            itemCountBadge.innerText = '0 items';
        } else {
            emptyPlaceholder.classList.add('d-none');
            const totalQty = window.posCartState.reduce((sum, i) => sum + i.quantity, 0);
            itemCountBadge.innerText = `${totalQty} items`;

            window.posCartState.forEach((item, index) => {
                const rowGross = item.unitPrice * item.quantity;
                const rowNet = rowGross - (rowGross * (item.itemDiscountPercent / 100.0));

                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td>
                        <div class="fw-bold text-dark lh-sm">${escapeHtml(item.medicineName)}</div>
                        <small class="text-muted fs-7">Batch: ${escapeHtml(item.batchNumber)}</small>
                    </td>
                    <td class="fw-semibold">৳${item.unitPrice.toFixed(2)}</td>
                    <td align="center">
                        <div class="d-flex align-items-center justify-content-center">
                            <button type="button" class="btn btn-sm btn-outline-secondary qty-control-btn btn-qty-minus" data-index="${index}">-</button>
                            <input type="number" class="qty-input mx-1 input-qty-value" data-index="${index}" value="${item.quantity}" min="1" max="${item.availableStock}" />
                            <button type="button" class="btn btn-sm btn-outline-secondary qty-control-btn btn-qty-plus" data-index="${index}">+</button>
                        </div>
                    </td>
                    <td align="center">
                        <input type="number" class="disc-input input-disc-value" data-index="${index}" value="${item.itemDiscountPercent}" min="0" max="100" />
                    </td>
                    <td align="right" class="fw-bold text-teal" id="rowTotal_${index}">
                        ৳${rowNet.toFixed(2)}
                    </td>
                    <td align="center">
                        <button type="button" class="btn btn-link text-danger p-0 border-0 btn-remove-item" data-index="${index}">
                            <i class="bi bi-x-circle-fill"></i>
                        </button>
                    </td>
                `;
                tableBody.appendChild(tr);
            });
        }

        bindRowEvents();
        recalculateTotals();
    }

    function bindRowEvents() {
        // Quantity Plus / Minus
        document.querySelectorAll('.btn-qty-minus').forEach(btn => {
            btn.addEventListener('click', function () {
                const idx = parseInt(this.getAttribute('data-index'));
                updateQuantity(idx, window.posCartState[idx].quantity - 1);
            });
        });

        document.querySelectorAll('.btn-qty-plus').forEach(btn => {
            btn.addEventListener('click', function () {
                const idx = parseInt(this.getAttribute('data-index'));
                updateQuantity(idx, window.posCartState[idx].quantity + 1);
            });
        });

        // Direct Input Keyup/Change
        document.querySelectorAll('.input-qty-value').forEach(input => {
            input.addEventListener('input', function () {
                const idx = parseInt(this.getAttribute('data-index'));
                updateQuantity(idx, this.value);
            });
        });

        document.querySelectorAll('.input-disc-value').forEach(input => {
            input.addEventListener('input', function () {
                const idx = parseInt(this.getAttribute('data-index'));
                updateItemDiscount(idx, this.value);
            });
        });

        // Row Remove
        document.querySelectorAll('.btn-remove-item').forEach(btn => {
            btn.addEventListener('click', function () {
                const idx = parseInt(this.getAttribute('data-index'));
                removeItem(idx);
            });
        });
    }

    // --- Real-time Input Event Listeners ---

    inputTaxRate.addEventListener('input', recalculateTotals);
    inputOverallDiscount.addEventListener('input', recalculateTotals);
    inputAmountTendered.addEventListener('input', function () {
        hideCheckoutError();
        recalculateTotals();
    });

    selectPaymentMethod.addEventListener('change', function () {
        hideCheckoutError();
        recalculateTotals();
    });

    btnClearCart.addEventListener('click', function () {
        if (window.posCartState.length === 0) return;
        if (confirm("Are you sure you want to clear the basket?")) {
            window.clearPosCart();
        }
    });

    // --- Acceptance Criteria Validation: Checkout Submission ---

    btnSubmitCheckout.addEventListener('click', function (e) {
        e.preventDefault();
        
        if (window.posCartState.length === 0) {
            showCheckoutError("Basket is empty!");
            return;
        }

        const totals = recalculateTotals();

        // Validation Rule: Prevent checkout if Grand Total > Amount Tendered for CASH sales
        if (selectPaymentMethod.value === 'CASH' && totals.amountTendered < totals.grandTotal) {
            showCheckoutError(`Insufficient Cash! Amount tendered (৳${totals.amountTendered.toFixed(2)}) is less than Grand Total (৳${totals.grandTotal.toFixed(2)}).`);
            return;
        }

        hideCheckoutError();

        // Prepare Payload for POS Order API
        const payload = {
            paymentMethod: selectPaymentMethod.value,
            subtotal: totals.subtotal,
            taxRate: parseFloat(inputTaxRate.value) || 0,
            overallDiscount: parseFloat(inputOverallDiscount.value) || 0,
            grandTotal: totals.grandTotal,
            amountTendered: totals.amountTendered,
            changeDue: totals.changeDue,
            items: window.posCartState.map(i => ({
                medicineId: i.id,
                batchId: i.batchId,
                quantity: i.quantity,
                unitPrice: i.unitPrice,
                discountPercent: i.itemDiscountPercent
            }))
        };

        console.log("Submitting Checkout Payload:", payload);
        alert(`Sale completed successfully!\nGrand Total: ৳${totals.grandTotal.toFixed(2)}\nChange Due: ৳${(totals.changeDue > 0 ? totals.changeDue : 0).toFixed(2)}`);
        
        window.clearPosCart();
    });

    // --- Utility Functions ---

    function showStockWarning(msg) {
        stockWarningText.innerText = msg;
        stockWarningContainer.classList.remove('d-none');
    }

    function hideStockWarning() {
        stockWarningContainer.classList.add('d-none');
    }

    function showCheckoutError(msg) {
        checkoutErrorText.innerText = msg;
        checkoutErrorContainer.classList.remove('d-none');
    }

    function hideCheckoutError() {
        checkoutErrorContainer.classList.add('d-none');
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    // Initial render
    renderCart();
});