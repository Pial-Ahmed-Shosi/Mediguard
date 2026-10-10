/**
 * Medi+ B2B Wholesale Marketplace Engine (Ticket 32)
 * Handles partner supplier filtering, minimum order requirements per supplier,
 * bulk quantity discount tiers, and B2B wholesale order submission.
 */

window.b2bCartState = [];
window.activeSupplierId = null;
window.activeSupplierName = "";
window.activeMinOrderAmount = 0;

document.addEventListener('DOMContentLoaded', function () {
    // Mock Wholesale Catalog Data matching Medi+ Verified Partners
    const wholesaleCatalog = [
        {
            id: 'med-001',
            brandName: 'Napa Extra 500mg',
            genericName: 'Paracetamol + Caffeine',
            supplierPharmacyId: '11111111-1111-1111-1111-111111111111',
            supplierPharmacyName: 'Square Wholesale Hub',
            minOrderRequirement: 5000.00,
            unitPrice: 2.10,
            availableStock: 5000,
            bulkTier: 500,
            deliveryTime: 'Same Day'
        },
        {
            id: 'med-002',
            brandName: 'Sergel 20mg Capsule',
            genericName: 'Esomeprazole',
            supplierPharmacyId: '11111111-1111-1111-1111-111111111111',
            supplierPharmacyName: 'Square Wholesale Hub',
            minOrderRequirement: 5000.00,
            unitPrice: 6.20,
            availableStock: 3000,
            bulkTier: 100,
            deliveryTime: 'Same Day'
        },
        {
            id: 'med-003',
            brandName: 'Seclo 20mg Capsule',
            genericName: 'Omeprazole',
            supplierPharmacyId: '22222222-2222-2222-2222-222222222222',
            supplierPharmacyName: 'Apex Pharma Ltd',
            minOrderRequirement: 10000.00,
            unitPrice: 4.50,
            availableStock: 8000,
            bulkTier: 1000,
            deliveryTime: '24 Hours'
        },
        {
            id: 'med-004',
            brandName: 'Ciprocin 500mg Tablet',
            genericName: 'Ciprofloxacin',
            supplierPharmacyId: '33333333-3333-3333-3333-333333333333',
            supplierPharmacyName: 'Lazz Pharma B2B',
            minOrderRequirement: 3000.00,
            unitPrice: 12.00,
            availableStock: 1500,
            bulkTier: 100,
            deliveryTime: '48 Hours'
        }
    ];

    // DOM Elements
    const gridContainer = document.getElementById('b2bProductGrid');
    const filterSearch = document.getElementById('filterSearch');
    const filterSupplier = document.getElementById('filterSupplier');
    const filterTier = document.getElementById('filterTier');
    const filterDelivery = document.getElementById('filterDelivery');

    // Cart DOM Elements
    const cartTableBody = document.getElementById('b2bCartTableBody');
    const emptyCartMsg = document.getElementById('b2bEmptyCartMsg');
    const lblActiveSupplier = document.getElementById('lblActiveSupplier');
    const lblMinOrderReq = document.getElementById('lblMinOrderReq');
    const minOrderProgressBar = document.getElementById('minOrderProgressBar');
    const lblMinOrderProgressText = document.getElementById('lblMinOrderProgressText');
    const lblB2BTotalAmount = document.getElementById('lblB2BTotalAmount');
    const inputShippingAddress = document.getElementById('inputShippingAddress');
    const inputOrderNotes = document.getElementById('inputOrderNotes');
    const btnSubmitB2BOrder = document.getElementById('btnSubmitB2BOrder');
    const btnClearCart = document.getElementById('btnClearB2BCart');

    // --- Render Wholesale Items Grid ---
    function renderCatalog(items) {
        gridContainer.innerHTML = '';

        if (items.length === 0) {
            gridContainer.innerHTML = `
                <div class="col-12 text-center py-5 text-muted">
                    <i class="bi bi-search fs-1 text-slate-300 d-block mb-2"></i>
                    No wholesale items match the selected filter criteria.
                </div>
            `;
            return;
        }

        items.forEach(item => {
            const card = document.createElement('div');
            card.className = 'b2b-item-card';
            card.innerHTML = `
                <div>
                    <!-- Supplier Pharmacy Name -->
                    <div class="supplier-chip">
                        <i class="bi bi-building me-1"></i>${escapeHtml(item.supplierPharmacyName)}
                    </div>
                    
                    <h6 class="fw-bold mb-1 text-dark">${escapeHtml(item.brandName)}</h6>
                    <small class="text-muted d-block mb-2">${escapeHtml(item.genericName)}</small>

                    <div class="d-flex gap-1 mb-3">
                        <span class="tier-badge">Tier: ${item.bulkTier}+ pcs</span>
                        <span class="delivery-badge"><i class="bi bi-truck me-1"></i>${item.deliveryTime}</span>
                    </div>
                </div>

                <div>
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <span class="text-muted fs-7">Wholesale Price:</span>
                        <span class="fw-bold text-teal fs-6">৳${item.unitPrice.toFixed(2)}</span>
                    </div>
                    <div class="d-flex justify-content-between align-items-center mb-3 fs-8 text-secondary">
                        <span>Avail Stock:</span>
                        <span>${item.availableStock.toLocaleString()} units</span>
                    </div>

                    <button type="button" class="btn btn-sm btn-outline-teal w-100 fw-bold btn-add-b2b"
                        data-id="${item.id}"
                        data-brand="${escapeHtml(item.brandName)}"
                        data-supplier-id="${item.supplierPharmacyId}"
                        data-supplier-name="${escapeHtml(item.supplierPharmacyName)}"
                        data-min-order="${item.minOrderRequirement}"
                        data-price="${item.unitPrice}"
                        data-stock="${item.availableStock}">
                        <i class="bi bi-plus-lg me-1"></i>Add to Wholesale Order
                    </button>
                </div>
            `;
            gridContainer.appendChild(card);
        });

        bindAddButtons();
    }

    // --- Filtering Engine ---
    function applyFilters() {
        const query = filterSearch.value.trim().toLowerCase();
        const supplier = filterSupplier.value;
        const tier = filterTier.value;
        const delivery = filterDelivery.value;

        const filtered = wholesaleCatalog.filter(item => {
            const matchesSearch = item.brandName.toLowerCase().includes(query) || item.genericName.toLowerCase().includes(query);
            const matchesSupplier = !supplier || item.supplierPharmacyName === supplier;
            const matchesTier = !tier || item.bulkTier >= parseInt(tier);
            const matchesDelivery = !delivery || item.deliveryTime === delivery;

            return matchesSearch && matchesSupplier && matchesTier && matchesDelivery;
        });

        renderCatalog(filtered);
    }

    [filterSearch, filterSupplier, filterTier, filterDelivery].forEach(el => {
        el.addEventListener('input', applyFilters);
        el.addEventListener('change', applyFilters);
    });

    // --- B2B Cart Logic ---
    function bindAddButtons() {
        document.querySelectorAll('.btn-add-b2b').forEach(btn => {
            btn.addEventListener('click', function () {
                const supplierId = this.getAttribute('data-supplier-id');
                const supplierName = this.getAttribute('data-supplier-name');
                const minOrder = parseFloat(this.getAttribute('data-min-order'));

                // Enforce Single Supplier Per B2B Wholesale Order Rule
                if (window.activeSupplierId && window.activeSupplierId !== supplierId && window.b2bCartState.length > 0) {
                    if (!confirm(`Your B2B cart currently contains items from "${window.activeSupplierName}". Would you like to clear the cart to order from "${supplierName}"?`)) {
                        return;
                    }
                    window.b2bCartState = [];
                }

                window.activeSupplierId = supplierId;
                window.activeSupplierName = supplierName;
                window.activeMinOrderAmount = minOrder;

                const itemId = this.getAttribute('data-id');
                const brand = this.getAttribute('data-brand');
                const price = parseFloat(this.getAttribute('data-price'));
                const stock = parseInt(this.getAttribute('data-stock'));

                const existing = window.b2bCartState.find(i => i.id === itemId);
                if (existing) {
                    if (existing.quantity + 50 <= stock) {
                        existing.quantity += 50; // Wholesale default batch jump
                    } else {
                        alert(`Cannot add more than available stock (${stock}).`);
                    }
                } else {
                    window.b2bCartState.push({
                        id: itemId,
                        brandName: brand,
                        unitPrice: price,
                        quantity: 100, // Default wholesale min pack size
                        stock: stock
                    });
                }

                renderB2BCart();
            });
        });
    }

    function renderB2BCart() {
        cartTableBody.innerHTML = '';

        if (window.b2bCartState.length === 0) {
            emptyCartMsg.classList.remove('d-none');
            lblActiveSupplier.innerText = "None Selected";
            lblMinOrderReq.innerText = "৳0.00";
            minOrderProgressBar.style.width = "0%";
            lblMinOrderProgressText.innerText = "0% of min requirement met";
            lblB2BTotalAmount.innerText = "৳0.00";
            btnSubmitB2BOrder.disabled = true;
            window.activeSupplierId = null;
            return;
        }

        emptyCartMsg.classList.add('d-none');
        lblActiveSupplier.innerText = window.activeSupplierName;
        lblMinOrderReq.innerText = `৳${window.activeMinOrderAmount.toFixed(2)}`;

        let totalWholesaleAmount = 0.00;

        window.b2bCartState.forEach((item, index) => {
            const lineTotal = item.unitPrice * item.quantity;
            totalWholesaleAmount += lineTotal;

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td class="fw-bold">${escapeHtml(item.brandName)}</td>
                <td align="center">
                    <input type="number" class="form-control form-control-sm text-center b2b-qty-input" 
                        data-index="${index}" value="${item.quantity}" min="10" step="10" style="width: 65px;" />
                </td>
                <td align="right">৳${item.unitPrice.toFixed(2)}</td>
                <td align="right" class="fw-bold">৳${lineTotal.toFixed(2)}</td>
                <td align="center">
                    <button class="btn btn-link text-danger p-0 border-0 btn-remove-b2b" data-index="${index}"><i class="bi bi-x-circle"></i></button>
                </td>
            `;
            cartTableBody.appendChild(tr);
        });

        // Calculate Minimum Order Requirement Progress
        const progressPct = Math.min(100, (totalWholesaleAmount / window.activeMinOrderAmount) * 100);
        minOrderProgressBar.style.width = `${progressPct}%`;
        lblMinOrderProgressText.innerText = `${progressPct.toFixed(0)}% of minimum order requirement met`;

        if (progressPct < 100) {
            minOrderProgressBar.style.backgroundColor = "#eab308"; // Warning yellow
        } else {
            minOrderProgressBar.style.backgroundColor = "#0f766e"; // Success teal
        }

        lblB2BTotalAmount.innerText = `৳${totalWholesaleAmount.toFixed(2)}`;

        // Enable Submit Button only if minimum requirement met & address supplied
        btnSubmitB2BOrder.disabled = !(progressPct >= 100 && inputShippingAddress.value.trim().length > 0);

        bindCartItemEvents();
    }

    function bindCartItemEvents() {
        document.querySelectorAll('.b2b-qty-input').forEach(input => {
            input.addEventListener('input', function () {
                const index = parseInt(this.getAttribute('data-index'));
                const newQty = parseInt(this.value) || 10;
                window.b2bCartState[index].quantity = newQty;
                renderB2BCart();
            });
        });

        document.querySelectorAll('.btn-remove-b2b').forEach(btn => {
            btn.addEventListener('click', function () {
                const index = parseInt(this.getAttribute('data-index'));
                window.b2bCartState.splice(index, 1);
                renderB2BCart();
            });
        });
    }

    inputShippingAddress.addEventListener('input', function () {
        renderB2BCart();
    });

    btnClearCart.addEventListener('click', function () {
        if (confirm("Clear B2B wholesale basket?")) {
            window.b2bCartState = [];
            renderB2BCart();
        }
    });

    // --- Order Submission via Backend API Workflow ---
    btnSubmitB2BOrder.addEventListener('click', async function () {
        if (window.b2bCartState.length === 0) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // Construct DTO payload matching Ticket 41 backend requirements
        const payload = {
            purchaserPharmacyId: "00000000-0000-0000-0000-000000000000", // Current tenant pharmacy ID
            supplierPharmacyId: window.activeSupplierId,
            shippingAddress: inputShippingAddress.value.trim(),
            notes: inputOrderNotes.value.trim(),
            items: window.b2bCartState.map(i => ({
                medicineId: i.id,
                quantity: i.quantity,
                unitPrice: i.unitPrice
            }))
        };

        try {
            btnSubmitB2BOrder.disabled = true;
            btnSubmitB2BOrder.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Processing B2B Order...`;

            const response = await fetch('/MediPlusB2B/CreateB2BOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (response.ok && result.success) {
                alert(`B2B Order successfully submitted!\nOrder ID: ${result.orderId}`);
                window.b2bCartState = [];
                inputShippingAddress.value = '';
                inputOrderNotes.value = '';
                renderB2BCart();
            } else {
                alert(`Failed to submit B2B order: ${result.message || 'Unknown error'}`);
            }
        } catch (err) {
            console.error("B2B Submission error:", err);
            alert("Network error processing wholesale order.");
        } finally {
            btnSubmitB2BOrder.disabled = false;
            btnSubmitB2BOrder.innerHTML = `<i class="bi bi-send-check me-2"></i>Submit Wholesale B2B Order`;
        }
    });

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    // Initial Catalog Load
    renderCatalog(wholesaleCatalog);
});