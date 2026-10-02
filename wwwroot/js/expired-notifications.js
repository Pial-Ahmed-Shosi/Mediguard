document.addEventListener("DOMContentLoaded", function () {
    const selectAllCheckbox = document.getElementById("selectAllCheckbox");
    const batchCheckboxes = document.querySelectorAll(".batch-checkbox");
    const btnBatchDispose = document.getElementById("btnBatchDispose");
    const targetBatchIdsInput = document.getElementById("targetBatchIds");
    const confirmDisposeBtn = document.getElementById("confirmDisposeBtn");
    
    // Select All Logic
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener("change", function () {
            batchCheckboxes.forEach(cb => {
                if (cb.closest('tr').style.display !== 'none') {
                    cb.checked = selectAllCheckbox.checked;
                }
            });
            toggleBatchButton();
        });
    }

    // Individual Checkbox Logic
    batchCheckboxes.forEach(cb => {
        cb.addEventListener("change", toggleBatchButton);
    });

    function toggleBatchButton() {
        const checkedBoxes = document.querySelectorAll(".batch-checkbox:checked");
        btnBatchDispose.disabled = checkedBoxes.length === 0;
    }

    // Set batch IDs for Batch Dispose button
    if (btnBatchDispose) {
        btnBatchDispose.addEventListener("click", function () {
            const checkedBoxes = document.querySelectorAll(".batch-checkbox:checked");
            const ids = Array.from(checkedBoxes).map(cb => cb.value);
            targetBatchIdsInput.value = ids.join(",");
        });
    }

    // Set batch ID for Single Dispose button
    const singleDisposeBtns = document.querySelectorAll(".btn-dispose-single");
    singleDisposeBtns.forEach(btn => {
        btn.addEventListener("click", function () {
            targetBatchIdsInput.value = this.getAttribute("data-id");
        });
    });

    // Confirm Dispose Action
    if (confirmDisposeBtn) {
        confirmDisposeBtn.addEventListener("click", function () {
            const reason = document.getElementById("disposalReason").value;
            const batchIds = targetBatchIdsInput.value.split(",");

            if (!reason) {
                alert("Please select a disposal reason.");
                return;
            }

            // AJAX call to Backend Controller to process disposal
            fetch('/Notification/DisposeBatches', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // Add AntiForgeryToken here if enabled
                },
                body: JSON.stringify({ batchIds: batchIds, reason: reason })
            })
            .then(response => {
                if(response.ok) {
                    location.reload(); // Reload page to update metrics and remove rows
                } else {
                    alert("Error processing disposal.");
                }
            })
            .catch(error => console.error('Error:', error));
        });
    }
});

// Status Filter Function
function filterStatus(status) {
    const rows = document.querySelectorAll(".batch-row");
    rows.forEach(row => {
        const rowStatus = row.getAttribute("data-status");
        if (status === 'ALL') {
            row.style.display = "";
        } else if (status === 'EXPIRED' && rowStatus === 'EXPIRED') {
            row.style.display = "";
        } else if (status === 'EXPIRING_SOON' && (rowStatus === 'CRITICAL_30_DAYS' || rowStatus === 'WARNING_60_DAYS')) {
            row.style.display = "";
        } else {
            row.style.display = "none";
            // Uncheck hidden rows
            const cb = row.querySelector('.batch-checkbox');
            if(cb) cb.checked = false;
        }
    });
    // Trigger button toggle check
    const event = new Event('change');
    document.querySelector('.batch-checkbox')?.dispatchEvent(event);
}