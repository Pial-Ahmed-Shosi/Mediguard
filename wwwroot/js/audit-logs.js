
(() => {
    "use strict";

    const filterForm = document.getElementById("auditFilterForm");
    const startDate = document.getElementById("startDate");
    const endDate = document.getElementById("endDate");
    const resetButton = document.getElementById("resetAuditFilters");
    const errorBox = document.getElementById("auditError");

    const modalElement = document.getElementById("auditChangesModal");
    const oldContent = document.getElementById("oldJsonContent");
    const newContent = document.getElementById("newJsonContent");
    const subtitle = document.getElementById("auditChangeSubtitle");

    if (!filterForm || !modalElement) {
        return;
    }

    function showError(message) {
        if (!errorBox) return;

        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }

    function clearError() {
        if (!errorBox) return;

        errorBox.textContent = "";
        errorBox.classList.add("d-none");
    }

    // Validate date range before submitting the server-side filters.
    filterForm.addEventListener("submit", (event) => {
        clearError();

        if (startDate.value && endDate.value &&
            startDate.value > endDate.value) {
            event.preventDefault();
            showError("The start date cannot be later than the end date.");
            endDate.focus();
        }
    });

    if (resetButton) {
        resetButton.addEventListener("click", () => {
            window.location.assign(
                filterForm.action || window.location.pathname
            );
        });
    }

    // Escape every untrusted value before creating syntax-highlighted HTML.
    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    
function highlightJson(json) {
    if (!json) return "";

    // Escape HTML delimiters to prevent XSS, but preserve quotes for JSON regex tokenization
    const safe = String(json)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");

    return safe.replace(
        /("(?:\\.|[^"\\])*"(?=\s*:)|"(?:\\.|[^"\\])*")|(-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)|\b(true|false|null)\b/g,
        (token, stringToken, numberToken, literalToken, offset, whole) => {
            let className = "json-string";

            if (numberToken) {
                className = "json-number";
            } else if (literalToken) {
                className = "json-literal";
            } else if (
                stringToken &&
                whole.slice(offset + token.length).trimStart().startsWith(":")
            ) {
                className = "json-key";
            }

            return `<span class="${className}">${token}</span>`;
        }
    );
}


    function formatJson(rawValue) {
        if (!rawValue) {
            return {
                text: "No snapshot available.",
                valid: true
            };
        }

        try {
            // Handles JSON strings stored as text in the audit record.
            const parsed = JSON.parse(rawValue);

            return {
                text: JSON.stringify(parsed, null, 2),
                valid: true
            };
        } catch {
            return {
                text: "Stored data is not valid JSON.\n\n" + rawValue,
                valid: false
            };
        }
    }

    function renderJson(target, rawValue) {
        const result = formatJson(rawValue);

        if (result.valid) {
            target.innerHTML = highlightJson(result.text);
        } else {
            // Invalid JSON is displayed as text, never trusted as HTML.
            target.textContent = result.text;
        }
    }

    modalElement.addEventListener("show.bs.modal", (event) => {
        const button = event.relatedTarget;

        if (!button) return;

        const user = button.dataset.user || "Unknown user";
        const action = button.dataset.action || "Unknown action";
        const entity = button.dataset.entity || "Unknown entity";
        const timestamp = button.dataset.timestamp || "";

        subtitle.textContent =
            `${user} · ${action} · ${entity} · ${timestamp}`;

        renderJson(oldContent, button.dataset.old || "");
        renderJson(newContent, button.dataset.new || "");
    });

    // Clear sensitive change snapshots when the modal closes.
    modalElement.addEventListener("hidden.bs.modal", () => {
        oldContent.textContent = "";
        newContent.textContent = "";
        subtitle.textContent =
            "Compare recorded values before and after the action.";
    });
})();
